using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace OD.Planner.Localization;

/// <summary>
/// Provides localization services for the application.
/// Manages language switching and resource access.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _instance = new();
    private CultureInfo _currentCulture = new("fr");

    /// <summary>
    /// Gets the singleton instance of the <see cref="LocalizationService"/>.
    /// </summary>
    public static LocalizationService Instance => _instance;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService()
    {
    }

    /// <summary>
    /// Gets or sets the current culture for localization.
    /// Setting this property triggers a <see cref="LanguageChanged"/> event.
    /// </summary>
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Name != value.Name)
            {
                _currentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
                OnPropertyChanged();
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Occurs when the application language has changed.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    public string this[string key]
    {
        get
        {
            var resource = Application.Current.TryFindResource(key);
            if (resource is string value)
            {
                return value;
            }
            return key;
        }
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
