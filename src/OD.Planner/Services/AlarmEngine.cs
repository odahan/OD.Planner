using System.Windows.Threading;
using OD.Planner.Data;
using OD.Planner.Logic;
using OD.Planner.Models;

namespace OD.Planner.Services;

public sealed class AlarmEngine : IDisposable
{
    private static readonly TimeSpan SnoozeInterval = TimeSpan.FromHours(1);

    private readonly Func<IReadOnlyList<PlannerTask>> _getTasks;
    private readonly AppSettings _settings;
    private readonly SoundService _sounds;
    private readonly HashSet<(long TaskId, AlarmLevel Level)> _stopped = new();
    private readonly Dictionary<(long TaskId, AlarmLevel Level), DispatcherTimer> _snoozes = new();
    private DispatcherTimer? _midnightTimer;

    public AlarmEngine(Func<IReadOnlyList<PlannerTask>> getTasks, AppSettings settings, SoundService sounds)
    {
        _getTasks = getTasks;
        _settings = settings;
        _sounds = sounds;
    }

    public event Action<IReadOnlyList<AlarmEntry>>? AlarmRaised;

    public event Action? MidnightPassed;

    public void Start()
    {
        ScheduleMidnight();
        CheckNow();
    }

    public void CheckNow()
    {
        var today = DateTime.Today;
        var pending = new List<AlarmEntry>();

        foreach (var task in _getTasks())
        {
            if (task.IsCompleted)
            {
                continue;
            }

            var days = DeadlineCalculator.GetDaysRemaining(task, today);
            if (!days.HasValue)
            {
                continue;
            }

            var level = AlarmEvaluator.GetLevel(days.Value);
            if (level == AlarmLevel.None || !IsLevelEnabled(level))
            {
                continue;
            }

            var key = (task.Id, level);
            if (_stopped.Contains(key) || _snoozes.ContainsKey(key))
            {
                continue;
            }

            pending.Add(new AlarmEntry { Task = task, Level = level, DaysRemaining = days.Value });
        }

        if (pending.Count > 0)
        {
            Raise(pending);
        }
    }

    public void Snooze(AlarmEntry entry)
    {
        var key = (entry.Task.Id, entry.Level);
        if (_stopped.Contains(key) || _snoozes.ContainsKey(key))
        {
            return;
        }

        var timer = new DispatcherTimer { Interval = SnoozeInterval };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _snoozes.Remove(key);
            ReFire(key);
        };
        _snoozes[key] = timer;
        timer.Start();
    }

    public void Stop(AlarmEntry entry)
    {
        var key = (entry.Task.Id, entry.Level);
        _stopped.Add(key);
        if (_snoozes.Remove(key, out var timer))
        {
            timer.Stop();
        }
    }

    public void SnoozeAll(IEnumerable<AlarmEntry> entries)
    {
        foreach (var entry in entries.ToList())
        {
            Snooze(entry);
        }
    }

    public void StopAll(IEnumerable<AlarmEntry> entries)
    {
        foreach (var entry in entries.ToList())
        {
            Stop(entry);
        }
    }

    public void ResetSession()
    {
        _stopped.Clear();
        foreach (var timer in _snoozes.Values)
        {
            timer.Stop();
        }

        _snoozes.Clear();
    }

    private void ReFire((long TaskId, AlarmLevel Level) key)
    {
        var task = _getTasks().FirstOrDefault(t => t.Id == key.TaskId);
        if (task is null || task.IsCompleted || _stopped.Contains(key))
        {
            return;
        }

        var days = DeadlineCalculator.GetDaysRemaining(task, DateTime.Today);
        if (!days.HasValue)
        {
            return;
        }

        var level = AlarmEvaluator.GetLevel(days.Value);
        if (level == AlarmLevel.None || !IsLevelEnabled(level))
        {
            return;
        }

        Raise(new List<AlarmEntry>
        {
            new() { Task = task, Level = level, DaysRemaining = days.Value },
        });
    }

    private void Raise(IReadOnlyList<AlarmEntry> entries)
    {
        foreach (var entry in entries)
        {
            _sounds.Play(entry.Level);
        }

        AlarmRaised?.Invoke(entries);
    }

    private bool IsLevelEnabled(AlarmLevel level) => level switch
    {
        AlarmLevel.Attention => _settings.SoundEnabled && _settings.J1Enabled,
        AlarmLevel.Due => _settings.SoundEnabled && _settings.J0Enabled,
        AlarmLevel.Overdue => _settings.SoundEnabled && _settings.OverdueEnabled,
        _ => false,
    };

    private void ScheduleMidnight()
    {
        _midnightTimer?.Stop();
        var next = DateTime.Today.AddDays(1).AddSeconds(2);
        _midnightTimer = new DispatcherTimer { Interval = next - DateTime.Now };
        _midnightTimer.Tick += (_, _) =>
        {
            _midnightTimer.Stop();
            CheckNow();
            MidnightPassed?.Invoke();
            ScheduleMidnight();
        };
        _midnightTimer.Start();
    }

    public void Dispose()
    {
        _midnightTimer?.Stop();
        foreach (var timer in _snoozes.Values)
        {
            timer.Stop();
        }

        _snoozes.Clear();
    }
}
