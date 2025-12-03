using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class SteamCmdDepotData
{
    [JsonPropertyName("data")]
    public Dictionary<string, AppData> Data { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}

public class AppData
{
    [JsonPropertyName("depots")]
    public Dictionary<string, DepotData> Depots { get; set; } = new();

    [JsonPropertyName("common")]
    public CommonData Common { get; set; } = new();
}

public class CommonData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class DepotData
{
    [JsonPropertyName("config")]
    public DepotConfig? Config { get; set; }

    [JsonPropertyName("manifests")]
    public Dictionary<string, ManifestData>? Manifests { get; set; }

    [JsonPropertyName("dlcappid")]
    public string? DlcAppId { get; set; }

    [JsonPropertyName("depotfromapp")]
    public string? DepotFromApp { get; set; }

    [JsonPropertyName("sharedinstall")]
    public string? SharedInstall { get; set; }
}

public class DepotConfig
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("oslist")]
    public string? OsList { get; set; }

    [JsonPropertyName("lowviolence")]
    public string? LowViolence { get; set; }

    [JsonPropertyName("realm")]
    public string? Realm { get; set; }
}

public class ManifestData
{
    [JsonPropertyName("gid")]
    public string? Gid { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("download")]
    public long Download { get; set; }
}

/// <summary>
/// Service for accessing SteamCMD API for depot information
/// </summary>
public class SteamCmdApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerService _logger;

    public SteamCmdApiService(IHttpClientFactory httpClientFactory, ILoggerService logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("Default");
    }

    public async Task<SteamCmdDepotData?> GetDepotInfoAsync(string appId)
    {
        try
        {
            var client = CreateClient();
            var url = $"https://api.steamcmd.net/v1/info/{appId}";

            _logger.Debug($"Fetching depot info for {appId}");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning($"SteamCMD API returned {response.StatusCode} for {appId}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SteamCmdDepotData>(json);

            if (result?.Data != null)
            {
                _logger.Info($"Retrieved depot info for {appId}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get depot info: {ex.Message}");
            return null;
        }
    }

    public async Task<List<(string depotId, long size, string? language)>> GetDepotsForAppAsync(string appId)
    {
        try
        {
            var depotData = await GetDepotInfoAsync(appId);
            if (depotData?.Data == null || !depotData.Data.ContainsKey(appId))
            {
                return new List<(string, long, string?)>();
            }

            var appData = depotData.Data[appId];
            var depots = new List<(string depotId, long size, string? language)>();

            foreach (var depot in appData.Depots)
            {
                // Skip non-numeric depot IDs
                if (!long.TryParse(depot.Key, out _))
                    continue;

                var size = depot.Value.Manifests?.GetValueOrDefault("public")?.Size ?? 0;
                var language = depot.Value.Config?.Language;

                depots.Add((depot.Key, size, language));
            }

            _logger.Debug($"Found {depots.Count} depots for {appId}");
            return depots;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get depots: {ex.Message}");
            return new List<(string, long, string?)>();
        }
    }

    public async Task<string?> GetGameNameAsync(string appId)
    {
        try
        {
            var depotData = await GetDepotInfoAsync(appId);
            if (depotData?.Data != null && depotData.Data.ContainsKey(appId))
            {
                return depotData.Data[appId].Common?.Name;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get game name: {ex.Message}");
            return null;
        }
    }
}
