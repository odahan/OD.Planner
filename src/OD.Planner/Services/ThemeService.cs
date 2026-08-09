using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OD.Planner.Services;

public sealed class ThemeService
{
    public const string LightMarkerKey = "OD.Planner.Theme.IsLight";
    public const string DarkMarkerKey = "OD.Planner.Theme.IsDark";

    public event Action<bool>? ThemeChanged;

    public void Apply(bool isDark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var uri = isDark ? "/Themes/Dark.xaml" : "/Themes/Light.xaml";

        // Replace the active theme dictionary IN PLACE, at its current index.
        // Remove-then-add detaches the DynamicResource listeners that resolved
        // from the old dictionary, leaving open windows stuck on the old values.
        // An in-place replacement keeps the link and invalidates them correctly.
        var index = -1;
        for (var i = 0; i < dicts.Count; i++)
        {
            if (dicts[i].Contains(LightMarkerKey) || dicts[i].Contains(DarkMarkerKey))
            {
                index = i;
                break;
            }
        }

        var theme = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
        if (index >= 0)
        {
            dicts[index] = theme;
        }
        else
        {
            dicts.Insert(0, theme);
        }

        ThemeChanged?.Invoke(isDark);
    }
}
