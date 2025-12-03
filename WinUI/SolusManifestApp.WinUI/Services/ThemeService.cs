using Microsoft.UI.Xaml;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;

namespace SolusManifestApp.WinUI.Services;

/// <summary>
/// Service for managing theme switching in WinUI 3
/// </summary>
public class ThemeService : IThemeService
{
    private readonly Application _app;
    private AppTheme _currentTheme = AppTheme.Default;

    public ThemeService()
    {
        _app = Application.Current;
    }

    public AppTheme CurrentTheme => _currentTheme;

    public string[] GetAvailableThemes()
    {
        return Enum.GetNames(typeof(AppTheme));
    }

    public void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;

        var themeName = theme.ToString();
        var themeUri = new Uri($"ms-appx:///Resources/Themes/{themeName}Theme.xaml");

        try
        {
            var themeDict = new ResourceDictionary { Source = themeUri };

            // Clear existing theme dictionaries (keep system ones)
            var dictionaries = _app.Resources.MergedDictionaries;
            for (int i = dictionaries.Count - 1; i >= 0; i--)
            {
                var dict = dictionaries[i];
                if (dict.Source != null && dict.Source.ToString().Contains("/Themes/"))
                {
                    dictionaries.RemoveAt(i);
                }
            }

            // Add new theme
            dictionaries.Add(themeDict);
        }
        catch
        {
            // Silently fail if theme not found, keep current theme
        }
    }

    public void ApplyTheme(string themeName)
    {
        if (Enum.TryParse<AppTheme>(themeName, out var theme))
        {
            ApplyTheme(theme);
        }
    }
}
