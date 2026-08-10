using System.IO;
using System.Text.Json;
using OD.Planner.Data;

namespace OD.Planner.Services;

public static class SettingsService
{
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static string SettingsDirectory { get; } = ResolveSettingsDirectory();

    public static string SettingsFile { get; } = Path.Combine(SettingsDirectory, FileName);

    /// <summary>
    /// Raised when a settings operation fails. Subscribers can display
    /// a non-intrusive notification to the user or log the error.
    /// </summary>
    public static event Action<string>? ErrorOccurred;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Impossible de charger les paramètres : {ex.Message}");
        }

        return new AppSettings();
    }

    public static bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tmp = SettingsFile + ".tmp";

            // Backup existing settings before overwriting
            if (File.Exists(SettingsFile))
            {
                var backup = SettingsFile + ".bak";
                File.Copy(SettingsFile, backup, overwrite: true);
            }

            File.WriteAllText(tmp, json);
            File.Move(tmp, SettingsFile, overwrite: true);

            // Remove backup on successful save
            var backupFile = SettingsFile + ".bak";
            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }

            return true;
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Impossible d'enregistrer les paramètres : {ex.Message}");

            // Attempt to restore from backup on failure
            var backupFile = SettingsFile + ".bak";
            if (File.Exists(backupFile) && !File.Exists(SettingsFile))
            {
                try
                {
                    File.Move(backupFile, SettingsFile);
                }
                catch { /* Best effort */ }
            }

            return false;
        }
    }

    private static void OnErrorOccurred(string message)
    {
        ErrorOccurred?.Invoke(message);
    }

    private static string ResolveSettingsDirectory()
    {
        var exeDir = AppContext.BaseDirectory;
        try
        {
            var probe = Path.Combine(exeDir, ".odp_write_probe");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return exeDir;
        }
        catch
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OD.Planner");
            Directory.CreateDirectory(appData);
            return appData;
        }
    }
}
