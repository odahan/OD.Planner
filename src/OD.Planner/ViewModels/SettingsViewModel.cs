using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OD.Planner.Data;
using OD.Planner.Models;
using OD.Planner.Services;

namespace OD.Planner.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly AppDatabase _db;
    private readonly ThemeService _themes;
    private readonly Action<string> _onDatabaseChanged;
    private readonly Action<bool> _onShowCompletedChanged;
    private readonly Action _onCategoriesChanged;

    public ObservableCollection<Category> Categories { get; } = new();

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private string newCategoryName = string.Empty;

    [ObservableProperty]
    private bool isRenameMode;

    [ObservableProperty]
    private string? categoryError;

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private bool showCompleted;

    [ObservableProperty]
    private bool autoStart;

    [ObservableProperty]
    private bool soundEnabled;

    [ObservableProperty]
    private bool j1Enabled;

    [ObservableProperty]
    private bool j0Enabled;

    [ObservableProperty]
    private bool overdueEnabled;

    [ObservableProperty]
    private bool reduceAnimations;

    [ObservableProperty]
    private string dbPath = string.Empty;

    public SettingsViewModel(
        AppSettings settings,
        AppDatabase db,
        ThemeService themes,
        Action<string> onDatabaseChanged,
        Action<bool> onShowCompletedChanged,
        Action onCategoriesChanged)
    {
        _settings = settings;
        _db = db;
        _themes = themes;
        _onDatabaseChanged = onDatabaseChanged;
        _onShowCompletedChanged = onShowCompletedChanged;
        _onCategoriesChanged = onCategoriesChanged;

        isDarkTheme = settings.IsDarkTheme;
        showCompleted = settings.ShowCompleted;
        autoStart = settings.AutoStartEnabled;
        soundEnabled = settings.SoundEnabled;
        j1Enabled = settings.J1Enabled;
        j0Enabled = settings.J0Enabled;
        overdueEnabled = settings.OverdueEnabled;
        reduceAnimations = settings.ReduceAnimations;
        dbPath = settings.DbPath ?? string.Empty;

        ReloadCategories();
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _settings.IsDarkTheme = value;
        _themes.Apply(value);
        SettingsService.Save(_settings);
    }

    partial void OnShowCompletedChanged(bool value) => _onShowCompletedChanged(value);

    partial void OnAutoStartChanged(bool value)
    {
        _settings.AutoStartEnabled = value;
        StartupService.SetEnabled(value);
        SettingsService.Save(_settings);
    }

    partial void OnSoundEnabledChanged(bool value)
    {
        _settings.SoundEnabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnJ1EnabledChanged(bool value)
    {
        _settings.J1Enabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnJ0EnabledChanged(bool value)
    {
        _settings.J0Enabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnOverdueEnabledChanged(bool value)
    {
        _settings.OverdueEnabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnReduceAnimationsChanged(bool value)
    {
        _settings.ReduceAnimations = value;
        SettingsService.Save(_settings);
    }

    // ----- Categories -----

    [RelayCommand]
    private void AddCategory()
    {
        CategoryError = null;
        var name = NewCategoryName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            if (IsRenameMode)
            {
                if (SelectedCategory is null)
                {
                    return;
                }

                _db.RenameCategory(SelectedCategory.Id, name);
                IsRenameMode = false;
            }
            else
            {
                _db.AddCategory(name);
            }

            NewCategoryName = string.Empty;
            ReloadCategories();
            _onCategoriesChanged();
        }
        catch (Exception)
        {
            CategoryError = "Une catégorie de ce nom existe déjà.";
        }
    }

    [RelayCommand]
    private void BeginRename()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        CategoryError = null;
        NewCategoryName = SelectedCategory.Name;
        IsRenameMode = true;
    }

    [RelayCommand]
    private void CancelRename()
    {
        IsRenameMode = false;
        NewCategoryName = string.Empty;
        CategoryError = null;
    }

    [RelayCommand]
    private void DeleteCategory()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Supprimer la catégorie « {SelectedCategory.Name} » ?\nLes tâches associées seront déplacées en « sans catégorie ».",
            "OD.Planner",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _db.DeleteCategory(SelectedCategory.Id);
        ReloadCategories();
        _onCategoriesChanged();
    }

    // ----- Database -----

    public void ChangeDatabase(string path)
    {
        _settings.DbPath = path;
        SettingsService.Save(_settings);
        DbPath = path;
        _onDatabaseChanged(path);
        ReloadCategories();
    }

    private void ReloadCategories()
    {
        Categories.Clear();
        foreach (var category in _db.GetCategories())
        {
            Categories.Add(category);
        }

        SelectedCategory = null;
    }
}
