using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OD.Planner.Data;
using OD.Planner.Localization;
using OD.Planner.Services;
using OD.Planner.ViewModels;
using OD.Planner.Views;

namespace OD.Planner;

/// <summary>
/// The main application class for OD.Planner.
/// Handles startup, shutdown, and global services.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the current application instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    public AppSettings Settings { get; private set; } = new();

    /// <summary>
    /// Gets the theme service for managing light/dark themes.
    /// </summary>
    public ThemeService Themes { get; } = new();

    private AppDatabase? _db;
    private SoundService? _sounds;
    private AlarmEngine? _alarmEngine;
    private MainViewModel? _mainViewModel;

    /// <summary>
    /// Raises the <see cref="E:System.Windows.Application.Startup"/> event.
    /// </summary>
    /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the data for the event.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        Settings = SettingsService.Load();
        Themes.Apply(Settings.IsDarkTheme);
        StartupService.SetEnabled(Settings.AutoStartEnabled);

        ApplyLanguage(Settings.Language);

        if (string.IsNullOrWhiteSpace(Settings.DbPath))
        {
            var firstRunVm = new FirstRunViewModel();
            var firstRunDialog = new FirstRunDialog { DataContext = firstRunVm };
            if (firstRunDialog.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            Settings.DbPath = firstRunVm.DbPath;
            SettingsService.Save(Settings);
        }

        if (Settings.DbPath is null)
        {
            Shutdown();
            return;
        }

        _db = new AppDatabase(Settings.DbPath);
        _db.EnsureCreated();

        _sounds = new SoundService();
        _alarmEngine = new AlarmEngine(() => _db.GetTasks(), Settings, _sounds);
        _mainViewModel = new MainViewModel(_db, Settings, Themes, _alarmEngine);

        var window = new MainWindow(_mainViewModel, Settings, _alarmEngine);
        MainWindow = window;
        window.Show();

        _alarmEngine.Start();
    }

    /// <summary>
    /// Applies the specified language to the application resources.
    /// </summary>
    /// <param name="language">The language code (e.g., "en" or "fr").</param>
    public void ApplyLanguage(string? language)
    {
        var culture = new CultureInfo(language ?? "en");

        // Replace the resource dictionary BEFORE notifying bindings so that
        // TryFindResource finds the new language values immediately.
        var dicts = Current.Resources.MergedDictionaries;
        var newDict = new ResourceDictionary
        {
            Source = new Uri($"Localization/Strings.{culture.Name}.xaml", UriKind.Relative)
        };

        // Replace the language dictionary in place to maintain DynamicResource links
        for (var i = 0; i < dicts.Count; i++)
        {
            if (dicts[i].Source != null &&
                dicts[i].Source.OriginalString.StartsWith("Localization/Strings."))
            {
                dicts[i] = newDict;
                break;
            }
        }

        // Update the XmlLanguage for all open WPF windows so that controls
        // like DatePicker use the correct date format (e.g., dd/MM/yyyy vs MM/dd/yyyy)
        var xmlLanguage = System.Windows.Markup.XmlLanguage.GetLanguage(culture.Name);
        foreach (Window window in Current.Windows)
        {
            window.Language = xmlLanguage;
        }

        // Now update the culture and notify bindings - they will pick up the new language
        LocalizationService.Instance.CurrentCulture = culture;
    }

    /// <summary>
    /// Handles unhandled exceptions on the dispatcher thread.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            string.Format(LocalizationService.Instance["ErrorOccurred"], e.Exception.Message),
            LocalizationService.Instance["ErrorTitle"],
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    /// <summary>
    /// Raises the <see cref="E:System.Windows.Application.Exit"/> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs"/> that contains the event data.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        _alarmEngine?.Dispose();
        _mainViewModel?.Dispose();
        _sounds?.Dispose();
        base.OnExit(e);
    }
}
