using SolusManifestApp.Core.Interfaces;
using System.Text.Json;

namespace SolusManifestApp.Core.Services;

/// <summary>
/// Settings service implementation
/// Handles JSON serialization to AppData folder
/// Framework-agnostic - works with both WPF and WinUI 3
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFolder;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService()
    {
        _settingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SolusManifestApp"
        );

        if (!Directory.Exists(_settingsFolder))
        {
            Directory.CreateDirectory(_settingsFolder);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> LoadSettingsAsync<T>() where T : class, new()
    {
        var filePath = GetSettingsFilePath<T>();

        if (!File.Exists(filePath))
        {
            return new T();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public async Task SaveSettingsAsync<T>(T settings) where T : class
    {
        var filePath = GetSettingsFilePath<T>();
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public T GetSettings<T>() where T : class, new()
    {
        // Synchronous version for compatibility
        return LoadSettingsAsync<T>().GetAwaiter().GetResult() ?? new T();
    }

    private string GetSettingsFilePath<T>()
    {
        return Path.Combine(_settingsFolder, $"{typeof(T).Name}.json");
    }
}
