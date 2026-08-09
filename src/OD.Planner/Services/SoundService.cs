using System.IO;
using System.Media;
using OD.Planner.Models;

namespace OD.Planner.Services;

public sealed class SoundService
{
    private readonly object _lock = new();
    private readonly Queue<AlarmLevel> _queue = new();
    private bool _playing;

    private readonly byte[] _attention;
    private readonly byte[] _due;
    private readonly byte[] _overdue;

    public SoundService()
    {
        _attention = GenerateWav(
            (880, 150, 100, 0.45),
            (880, 150, 0, 0.45));

        _due = GenerateWav(
            (880, 200, 120, 0.65),
            (1046, 200, 120, 0.65),
            (1046, 200, 0, 0.65));

        var overdue = new List<(double Freq, int Ms, int GapMs, double Vol)>();
        for (var group = 0; group < 3; group++)
        {
            for (var i = 0; i < 4; i++)
            {
                overdue.Add((1046, 150, 60, 0.8));
            }

            if (group < 2)
            {
                overdue.Add((0, 0, 250, 0));
            }
        }

        _overdue = GenerateWav(overdue.ToArray());
    }

    public void Play(AlarmLevel level)
    {
        if (level == AlarmLevel.None)
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

        _ = PlayLoopAsync();
    }

    private async Task PlayLoopAsync()
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

            var bytes = GetBytes(level);
            if (bytes is not null)
            {
                using var stream = new MemoryStream(bytes, writable: false);
                new SoundPlayer(stream).Play();
            }

            await Task.Delay(400);
        }
    }

    private byte[]? GetBytes(AlarmLevel level) => level switch
    {
        AlarmLevel.Attention => _attention,
        AlarmLevel.Due => _due,
        AlarmLevel.Overdue => _overdue,
        _ => null,
    };

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
