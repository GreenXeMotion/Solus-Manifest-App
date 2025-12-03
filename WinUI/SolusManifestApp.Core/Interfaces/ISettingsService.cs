namespace SolusManifestApp.Core.Interfaces;

/// <summary>
/// Interface for settings service
/// Handles loading, saving, and managing application settings
/// </summary>
public interface ISettingsService
{
    Task<T?> LoadSettingsAsync<T>() where T : class, new();
    Task SaveSettingsAsync<T>(T settings) where T : class;
    T GetSettings<T>() where T : class, new();
}
