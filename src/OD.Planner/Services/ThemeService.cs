using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OD.Planner.Services;

public sealed class ThemeService
{
    public const string LightMarkerKey = "OD.Planner.Theme.IsLight";
    public const string DarkMarkerKey = "OD.Planner.Theme.IsDark";

    private ResourceDictionary? _lightTheme;
    private ResourceDictionary? _darkTheme;
    private int _themeIndex = -1;

    public event Action<bool>? ThemeChanged;

    public void Apply(bool isDark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;

        // Cache theme dictionaries to avoid recreating them on every switch
        _lightTheme ??= LoadTheme("/Themes/Light.xaml");
        _darkTheme ??= LoadTheme("/Themes/Dark.xaml");

        // Find the current theme index on first use
        if (_themeIndex < 0)
        {
            for (var i = 0; i < dicts.Count; i++)
            {
                if (dicts[i].Contains(LightMarkerKey) || dicts[i].Contains(DarkMarkerKey))
                {
                    _themeIndex = i;
                    break;
                }
            }
        }

        var newTheme = isDark ? _darkTheme : _lightTheme;

        // Replace the active theme dictionary IN PLACE, at its current index.
        // Remove-then-add detaches the DynamicResource listeners that resolved
        // from the old dictionary, leaving open windows stuck on the old values.
        // An in-place replacement keeps the link and invalidates them correctly.
        if (_themeIndex >= 0)
        {
            dicts[_themeIndex] = newTheme;
        }
        else
        {
            dicts.Insert(0, newTheme);
            _themeIndex = 0;
        }

        ThemeChanged?.Invoke(isDark);
    }

    private static ResourceDictionary LoadTheme(string uri)
    {
        return new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
    }
}
