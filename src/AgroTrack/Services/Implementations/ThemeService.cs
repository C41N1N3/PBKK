using System.Windows;
using AgroTrack.Enums;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.Services.Implementations;

public class ThemeService : IThemeService
{
    private AppTheme _currentTheme = AppTheme.Light;

    public AppTheme CurrentTheme => _currentTheme;
    public event EventHandler<AppTheme>? ThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        _currentTheme = theme;
        ApplyThemeToApplication(theme);
        ThemeChanged?.Invoke(this, theme);
    }

    public void ToggleTheme()
    {
        SetTheme(_currentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
    }

    private static void ApplyThemeToApplication(AppTheme theme)
    {
        if (Application.Current == null)
            return;

        var dictUri = theme == AppTheme.Dark
            ? new Uri("pack://application:,,,/AgroTrack;component/Themes/DarkTheme.xaml", UriKind.Absolute)
            : new Uri("pack://application:,,,/AgroTrack;component/Themes/LightTheme.xaml", UriKind.Absolute);

        try
        {
            var newThemeDict = new ResourceDictionary { Source = dictUri };

            // Find existing theme dictionary
            ResourceDictionary? existingThemeDict = null;
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                if (dict.Source != null && (dict.Source.ToString().Contains("LightTheme.xaml") || dict.Source.ToString().Contains("DarkTheme.xaml")))
                {
                    existingThemeDict = dict;
                    break;
                }
            }

            if (existingThemeDict != null)
            {
                var index = Application.Current.Resources.MergedDictionaries.IndexOf(existingThemeDict);
                Application.Current.Resources.MergedDictionaries[index] = newThemeDict;
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
            }
        }
        catch
        {
            // Fallback gracefully in testing or headless mode
        }
    }
}
