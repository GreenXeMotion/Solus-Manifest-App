namespace SolusManifestApp.Core.Interfaces;

/// <summary>
/// Service for managing application themes
/// </summary>
public interface IThemeService
{
    string[] GetAvailableThemes();
    void ApplyTheme(string themeName);
}
