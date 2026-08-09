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
        catch
        {
            // Corrupted settings: fall back to defaults.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tmp = SettingsFile + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, SettingsFile, overwrite: true);
        }
        catch
        {
            // Best effort: never crash the app because settings can't be written.
        }
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
