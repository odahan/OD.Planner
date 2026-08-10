using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OD.Planner.Data;
using OD.Planner.Services;
using OD.Planner.ViewModels;
using OD.Planner.Views;

namespace OD.Planner;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public AppSettings Settings { get; private set; } = new();
    public ThemeService Themes { get; } = new();

    private AppDatabase? _db;
    private SoundService? _sounds;
    private AlarmEngine? _alarmEngine;
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        Settings = SettingsService.Load();
        Themes.Apply(Settings.IsDarkTheme);
        StartupService.SetEnabled(Settings.AutoStartEnabled);

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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Une erreur est survenue :\n{e.Exception.Message}",
            "OD.Planner",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _alarmEngine?.Dispose();
        _mainViewModel?.Dispose();
        _sounds?.Dispose();
        base.OnExit(e);
    }
}
