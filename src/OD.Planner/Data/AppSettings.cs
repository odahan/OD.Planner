namespace OD.Planner.Data;

/// <summary>
/// Represents the application settings that are persisted between sessions.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the path to the SQLite database file.
    /// </summary>
    public string? DbPath { get; set; }

    /// <summary>
    /// Gets or sets the saved window left position.
    /// </summary>
    public double? WindowLeft { get; set; }

    /// <summary>
    /// Gets or sets the saved window top position.
    /// </summary>
    public double? WindowTop { get; set; }

    /// <summary>
    /// Gets or sets the saved window width.
    /// </summary>
    public double? WindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the saved window height.
    /// </summary>
    public double? WindowHeight { get; set; }

    /// <summary>
    /// Gets or sets whether the dark theme is enabled.
    /// </summary>
    public bool IsDarkTheme { get; set; }

    /// <summary>
    /// Gets or sets whether completed tasks are shown.
    /// </summary>
    public bool ShowCompleted { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the application starts with Windows.
    /// </summary>
    public bool AutoStartEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether animations are reduced for accessibility.
    /// </summary>
    public bool ReduceAnimations { get; set; }

    /// <summary>
    /// Gets or sets whether sound is enabled for alarms.
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the J-1 (day before) alarm is enabled.
    /// </summary>
    public bool J1Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the J0 (due day) alarm is enabled.
    /// </summary>
    public bool J0Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the overdue alarm is enabled.
    /// </summary>
    public bool OverdueEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the language code (e.g., "en" or "fr").
    /// </summary>
    public string Language { get; set; } = "fr";
}
