using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class SteamApp
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class SteamAppList
{
    [JsonPropertyName("apps")]
    public List<SteamApp> Apps { get; set; } = new();
}

public class SteamApiResponse
{
    [JsonPropertyName("applist")]
    public SteamAppList? AppList { get; set; }
}

/// <summary>
/// Service for accessing Steam Web API
/// </summary>
public class SteamApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerService _logger;
    private const string BaseUrl = "https://api.steampowered.com";

    public SteamApiService(IHttpClientFactory httpClientFactory, ILoggerService logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("Default");
    }

    /// <summary>
    /// Get list of all Steam apps (large response ~15MB)
    /// </summary>
    public async Task<List<SteamApp>?> GetAppListAsync()
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/ISteamApps/GetAppList/v2/";

            _logger.Debug("Fetching Steam app list...");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning($"Steam API returned {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SteamApiResponse>(json);

            var apps = result?.AppList?.Apps ?? new List<SteamApp>();
            _logger.Info($"Retrieved {apps.Count} Steam apps");

            return apps;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get Steam app list: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Search for games by name
    /// </summary>
    public async Task<List<SteamApp>> SearchGamesAsync(string query)
    {
        try
        {
            var allApps = await GetAppListAsync();
            if (allApps == null)
                return new List<SteamApp>();

            var queryLower = query.ToLowerInvariant();
            var results = allApps
                .Where(app => app.Name.ToLowerInvariant().Contains(queryLower))
                .OrderBy(app => app.Name)
                .Take(50)
                .ToList();

            _logger.Debug($"Found {results.Count} results for '{query}'");
            return results;
        }
        catch (Exception ex)
        {
            _logger.Error($"Search failed: {ex.Message}");
            return new List<SteamApp>();
        }
    }

    /// <summary>
    /// Get app name by AppID
    /// </summary>
    public async Task<string?> GetAppNameAsync(string appId)
    {
        try
        {
            var allApps = await GetAppListAsync();
            if (allApps == null || !int.TryParse(appId, out var appIdInt))
                return null;

            var app = allApps.FirstOrDefault(a => a.AppId == appIdInt);
            return app?.Name;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get app name: {ex.Message}");
            return null;
        }
    }
}
