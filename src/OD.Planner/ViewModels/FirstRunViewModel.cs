using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using OD.Planner.Localization;

namespace OD.Planner.ViewModels;

/// <summary>
/// ViewModel for the First Run dialog.
/// Handles initial database location selection.
/// </summary>
public sealed partial class FirstRunViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the database file path.
    /// </summary>
    [ObservableProperty]
    private string dbPath;

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    [ObservableProperty]
    private string? error;

    /// <summary>
    /// Gets whether a database already exists at the selected path.
    /// </summary>
    [ObservableProperty]
    private bool dbExists;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstRunViewModel"/> class.
    /// </summary>
    public FirstRunViewModel()
    {
        dbPath = Path.Combine(AppContext.BaseDirectory, "tasks.db");
        UpdateDbExists();
    }

    partial void OnDbPathChanged(string value)
    {
        UpdateDbExists();
    }

    /// <summary>
    /// Updates the DbExists property based on the current path.
    /// </summary>
    private void UpdateDbExists()
    {
        DbExists = !string.IsNullOrWhiteSpace(DbPath) && File.Exists(DbPath);
    }

    /// <summary>
    /// Validates the database path.
    /// </summary>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    public bool Validate()
    {
        Error = null;

        if (string.IsNullOrWhiteSpace(DbPath))
        {
            Error = LocalizationService.Instance["DbPathEmpty"];
            return false;
        }

        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(DbPath));
            if (string.IsNullOrEmpty(dir))
            {
                dir = AppContext.BaseDirectory;
            }

            Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            Error = string.Format(LocalizationService.Instance["CannotCreateFolder"], ex.Message);
            return false;
        }

        return true;
    }
}
