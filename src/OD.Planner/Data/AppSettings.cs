namespace OD.Planner.Data;

public sealed class AppSettings
{
    public string? DbPath { get; set; }

    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }

    public bool IsDarkTheme { get; set; }
    public bool ShowCompleted { get; set; } = true;
    public bool AutoStartEnabled { get; set; }
    public bool ReduceAnimations { get; set; }

    public bool SoundEnabled { get; set; } = true;
    public bool J1Enabled { get; set; } = true;
    public bool J0Enabled { get; set; } = true;
    public bool OverdueEnabled { get; set; } = true;
}
