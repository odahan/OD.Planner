using System.IO;
using System.Media;
using OD.Planner.Models;

namespace OD.Planner.Services;

/// <summary>
/// Configuration for alarm sounds. Allows customizing the tone patterns
/// for each alarm level.
/// </summary>
public sealed class SoundConfiguration
{
    public (double Freq, int Ms, int GapMs, double Vol)[] AttentionBeeps { get; set; } =
    [
        (880, 150, 100, 0.45),
        (880, 150, 0, 0.45)
    ];

    public (double Freq, int Ms, int GapMs, double Vol)[] DueBeeps { get; set; } =
    [
        (880, 200, 120, 0.65),
        (1046, 200, 120, 0.65),
        (1046, 200, 0, 0.65)
    ];

    public (double Freq, int Ms, int GapMs, double Vol)[] OverdueBeeps { get; set; } =
    [
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (0, 0, 250, 0),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (0, 0, 250, 0),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8),
        (1046, 150, 60, 0.8)
    ];
}

public sealed class SoundService : IDisposable
{
    private readonly object _lock = new();
    private readonly Queue<AlarmLevel> _queue = new();
    private readonly Dictionary<AlarmLevel, SoundPlayer> _players = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _playing;
    private bool _disposed;

    public SoundService() : this(new SoundConfiguration())
    {
    }

    public SoundService(SoundConfiguration config)
    {
        _players[AlarmLevel.Attention] = LoadPlayer(GenerateWav(config.AttentionBeeps));
        _players[AlarmLevel.Due] = LoadPlayer(GenerateWav(config.DueBeeps));
        _players[AlarmLevel.Overdue] = LoadPlayer(GenerateWav(config.OverdueBeeps));
    }

    private static SoundPlayer LoadPlayer(byte[] wavBytes)
    {
        var stream = new MemoryStream(wavBytes, writable: false);
        return new SoundPlayer(stream);
    }

    public void Play(AlarmLevel level)
    {
        if (level == AlarmLevel.None || _disposed)
        {
            return;
        }

        lock (_lock)
        {
            _queue.Enqueue(level);
            if (_playing)
            {
                return;
            }

            _playing = true;
        }

        _ = PlayLoopAsync(_cts.Token);
    }

    private async Task PlayLoopAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            AlarmLevel level;
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    _playing = false;
                    return;
                }

                level = _queue.Dequeue();
            }

            if (_players.TryGetValue(level, out var player) && !_disposed)
            {
                try
                {
                    player.Play();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }

            try
            {
                await Task.Delay(400, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();

        foreach (var player in _players.Values)
        {
            player.Dispose();
        }
        _players.Clear();
        _cts.Dispose();
    }

    private static byte[] GenerateWav(params (double Freq, int Ms, int GapMs, double Vol)[] beeps)
    {
        const int sampleRate = 22050;
        var totalMs = beeps.Sum(b => b.Ms + b.GapMs);
        var totalSamples = (int)(totalMs / 1000.0 * sampleRate);
        var samples = new short[totalSamples];
        var pos = 0;

        foreach (var (freq, ms, gapMs, vol) in beeps)
        {
            var count = (int)(ms / 1000.0 * sampleRate);
            var step = 2 * Math.PI * freq / sampleRate;
            for (var i = 0; i < count && pos < samples.Length; i++)
            {
                samples[pos++] = (short)(vol * 32000 * Math.Sin(step * i));
            }

            pos += (int)(gapMs / 1000.0 * sampleRate);
        }

        using var mem = new MemoryStream();
        using var writer = new BinaryWriter(mem);
        var dataBytes = samples.Length * 2;

        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return mem.ToArray();
    }
}
