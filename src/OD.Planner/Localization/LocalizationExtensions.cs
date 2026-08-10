using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using OD.Planner.Converters;

namespace OD.Planner.Localization;

/// <summary>
/// Markup extension for accessing localized strings from XAML.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocExtension"/> class.
    /// </summary>
    /// <param name="key">The resource key for the localized string.</param>
    public LocExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Gets the resource key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Returns the localized string for the specified key.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The localized string.</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}

/// <summary>
/// Converter that returns Visibility.Visible when the bound value matches the parameter.
/// Used for language selection visibility.
/// </summary>
public sealed class LanguageToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a culture name to Visibility based on match with parameter.
    /// </summary>
    /// <param name="value">The current culture name.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The culture name to match against.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>Visibility.Visible if matching, otherwise Visibility.Collapsed.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var currentCulture = value as string;
        var targetCulture = parameter as string;
        return currentCulture == targetCulture ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Not supported.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
