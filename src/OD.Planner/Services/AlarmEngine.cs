using System.Windows.Threading;
using OD.Planner.Data;
using OD.Planner.Logic;
using OD.Planner.Models;

namespace OD.Planner.Services;

public sealed class AlarmEngine : IDisposable
{
    private static readonly TimeSpan SnoozeInterval = TimeSpan.FromHours(1);

    private Func<IReadOnlyList<PlannerTask>> _getTasks;
    private readonly AppSettings _settings;
    private readonly SoundService _sounds;
    private readonly Dictionary<(long TaskId, AlarmLevel Level), DateTime?> _stopped = new();
    private readonly Dictionary<(long TaskId, AlarmLevel Level), SnoozeState> _snoozes = new();
    private DispatcherTimer? _midnightTimer;

    private sealed class SnoozeState
    {
        public required DispatcherTimer Timer { get; init; }
        public required PlannerTask TaskSnapshot { get; init; }
        public required AlarmLevel Level { get; init; }
    }

    public AlarmEngine(Func<IReadOnlyList<PlannerTask>> getTasks, AppSettings settings, SoundService sounds)
    {
        _getTasks = getTasks;
        _settings = settings;
        _sounds = sounds;
    }

    public event Action<IReadOnlyList<AlarmEntry>>? AlarmRaised;

    public event Action? MidnightPassed;

    /// <summary>
    /// Replaces the task source used by the alarm engine.
    /// </summary>
    public void SetTaskSource(Func<IReadOnlyList<PlannerTask>> getTasks)
    {
        ArgumentNullException.ThrowIfNull(getTasks);
        _getTasks = getTasks;
        ResetSession();
    }

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

            var deadline = DeadlineCalculator.GetEffectiveDeadline(task);
            if (!deadline.HasValue)
            {
                continue;
            }

            var days = (int)(deadline.Value.Date - today).TotalDays;

            var level = AlarmEvaluator.GetLevel(days);
            if (level == AlarmLevel.None || !IsLevelEnabled(level))
            {
                continue;
            }

            var key = (task.Id, level);
            if (IsStoppedForCurrentDeadline(key, deadline) || IsSnoozedForCurrentDeadline(key, deadline))
            {
                continue;
            }

            pending.Add(new AlarmEntry { Task = task, Level = level, DaysRemaining = days });
        }

        if (pending.Count > 0)
        {
            Raise(pending);
        }
    }

    public void Snooze(AlarmEntry entry)
    {
        var key = (entry.Task.Id, entry.Level);
        var deadline = DeadlineCalculator.GetEffectiveDeadline(entry.Task);
        if (IsStoppedForCurrentDeadline(key, deadline) || IsSnoozedForCurrentDeadline(key, deadline))
        {
            return;
        }

        var taskSnapshot = entry.Task.Clone();
        var timer = new DispatcherTimer { Interval = SnoozeInterval };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_snoozes.Remove(key, out var state))
            {
                ReFire(state);
            }
        };
        _snoozes[key] = new SnoozeState { Timer = timer, TaskSnapshot = taskSnapshot, Level = entry.Level };
        timer.Start();
    }

    public void Stop(AlarmEntry entry)
    {
        var key = (entry.Task.Id, entry.Level);
        _stopped[key] = DeadlineCalculator.GetEffectiveDeadline(entry.Task);
        if (_snoozes.Remove(key, out var state))
        {
            state.Timer.Stop();
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
        foreach (var state in _snoozes.Values)
        {
            state.Timer.Stop();
        }

        _snoozes.Clear();
    }

    private void ReFire(SnoozeState state)
    {
        var task = _getTasks().FirstOrDefault(candidate => candidate.Id == state.TaskSnapshot.Id);
        if (task is null || task.IsCompleted)
        {
            return;
        }

        var deadline = DeadlineCalculator.GetEffectiveDeadline(task);
        if (!deadline.HasValue)
        {
            return;
        }

        var days = (int)(deadline.Value.Date - DateTime.Today).TotalDays;
        var level = AlarmEvaluator.GetLevel(days);
        var key = (task.Id, level);
        if (level == AlarmLevel.None || IsStoppedForCurrentDeadline(key, deadline) || !IsLevelEnabled(level))
        {
            return;
        }

        Raise(new List<AlarmEntry>
        {
            new() { Task = task, Level = level, DaysRemaining = days },
        });
    }

    private bool IsStoppedForCurrentDeadline((long TaskId, AlarmLevel Level) key, DateTime? deadline)
    {
        if (!_stopped.TryGetValue(key, out var stoppedDeadline))
        {
            return false;
        }

        if (stoppedDeadline == deadline)
        {
            return true;
        }

        _stopped.Remove(key);
        return false;
    }

    private bool IsSnoozedForCurrentDeadline((long TaskId, AlarmLevel Level) key, DateTime? deadline)
    {
        if (!_snoozes.TryGetValue(key, out var state))
        {
            return false;
        }

        if (DeadlineCalculator.GetEffectiveDeadline(state.TaskSnapshot) == deadline)
        {
            return true;
        }

        state.Timer.Stop();
        _snoozes.Remove(key);
        return false;
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

    private DateTime _lastMidnightDate = DateTime.Today;

    private void ScheduleMidnight()
    {
        _midnightTimer?.Stop();
        // Poll every minute to check for midnight crossing.
        // This is resilient to system clock changes (NTP sync, DST).
        _midnightTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _midnightTimer.Tick += (_, _) =>
        {
            var today = DateTime.Today;
            if (today > _lastMidnightDate)
            {
                _lastMidnightDate = today;
                CheckNow();
                MidnightPassed?.Invoke();
            }
        };
        _midnightTimer.Start();
    }

    public void Dispose()
    {
        _midnightTimer?.Stop();
        foreach (var state in _snoozes.Values)
        {
            state.Timer.Stop();
        }

        _snoozes.Clear();
    }
}
