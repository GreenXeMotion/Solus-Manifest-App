using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class ManifestApiService : IManifestApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cacheService;
    private const string BaseUrl = "https://manifest.morrenus.xyz/api/v1";
    private readonly TimeSpan _statusCacheExpiration = TimeSpan.FromMinutes(5);

    public ManifestApiService(IHttpClientFactory httpClientFactory, ICacheService cacheService)
    {
        _httpClientFactory = httpClientFactory;
        _cacheService = cacheService;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("Default");
    }

    public async Task<Manifest?> GetManifestAsync(string appId, string apiKey)
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/manifest/{appId}?api_key={apiKey}";
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var preview = json.Length > 200 ? json.Substring(0, 200) : json;
                throw new Exception($"Manifest not available for {appId}. API returned {response.StatusCode}: {preview}");
            }

            var manifest = JsonSerializer.Deserialize<Manifest>(json);
            return manifest;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch manifest for {appId}: {ex.Message}", ex);
        }
    }

    public async Task<List<Manifest>?> SearchGamesAsync(string query, string apiKey)
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&api_key={apiKey}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<Manifest>>(json);
            return results;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search games: {ex.Message}", ex);
        }
    }

    public async Task<List<Manifest>?> GetAllGamesAsync(string apiKey)
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/games?api_key={apiKey}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<Manifest>>(json);
            return results;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch games list: {ex.Message}", ex);
        }
    }

    public bool ValidateApiKey(string apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey) && apiKey.StartsWith("smm", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> TestApiKeyAsync(string apiKey)
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/status/10?api_key={apiKey}";
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<GameStatus?> GetGameStatusAsync(string appId, string apiKey)
    {
        // Check cache first
        if (_cacheService.IsGameStatusCacheValid(appId, _statusCacheExpiration))
        {
            var (cachedJson, _) = _cacheService.GetCachedGameStatus(appId);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    return JsonSerializer.Deserialize<GameStatus>(cachedJson);
                }
                catch
                {
                    // Ignore and fetch fresh
                }
            }
        }

        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/status/{appId}?api_key={apiKey}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _cacheService.CacheGameStatus(appId, json);

            return JsonSerializer.Deserialize<GameStatus>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<LibraryResponse?> GetLibraryAsync(string apiKey, int limit = 100, int offset = 0, string? search = null, string sortBy = "updated")
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/library?api_key={apiKey}&limit={limit}&offset={offset}&sort_by={sortBy}";
            if (!string.IsNullOrEmpty(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LibraryResponse>(json);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch library: {ex.Message}", ex);
        }
    }

    public async Task<SearchResponse?> SearchLibraryAsync(string query, string apiKey, int limit = 50)
    {
        try
        {
            var client = CreateClient();
            var url = $"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&api_key={apiKey}&limit={limit}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SearchResponse>(json);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search library: {ex.Message}", ex);
        }
    }
}
