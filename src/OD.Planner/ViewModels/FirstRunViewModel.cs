using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OD.Planner.ViewModels;

public sealed partial class FirstRunViewModel : ObservableObject
{
    [ObservableProperty]
    private string dbPath;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private bool dbExists;

    public FirstRunViewModel()
    {
        dbPath = Path.Combine(AppContext.BaseDirectory, "tasks.db");
        UpdateDbExists();
    }

    partial void OnDbPathChanged(string value)
    {
        UpdateDbExists();
    }

    private void UpdateDbExists()
    {
        DbExists = !string.IsNullOrWhiteSpace(DbPath) && File.Exists(DbPath);
    }

    public bool Validate()
    {
        Error = null;

        if (string.IsNullOrWhiteSpace(DbPath))
        {
            Error = "Le chemin est vide.";
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
            Error = $"Impossible de créer le dossier : {ex.Message}";
            return false;
        }

        return true;
    }
}
