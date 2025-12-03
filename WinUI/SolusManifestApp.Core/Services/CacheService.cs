using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class CacheService : ICacheService
{
    private readonly string _cacheFolder;
    private readonly string _iconCacheFolder;
    private readonly string _dataCacheFolder;
    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;

    public CacheService(ILoggerService logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cacheFolder = Path.Combine(appData, "SolusManifestApp", "Cache");
        _iconCacheFolder = Path.Combine(_cacheFolder, "Icons");
        _dataCacheFolder = Path.Combine(_cacheFolder, "Data");

        Directory.CreateDirectory(_iconCacheFolder);
        Directory.CreateDirectory(_dataCacheFolder);

        _httpClient = httpClientFactory.CreateClient("Default");
    }

    // Icon Caching
    public async Task<string?> GetIconAsync(string appId, string iconUrl)
    {
        if (string.IsNullOrEmpty(appId))
            return null;

        var iconPath = Path.Combine(_iconCacheFolder, $"{appId}.jpg");

        if (File.Exists(iconPath))
            return iconPath;

        // Try provided URL first
        if (!string.IsNullOrEmpty(iconUrl))
        {
            try
            {
                _logger.Debug($"Downloading icon for {appId} from: {iconUrl}");
                var response = await _httpClient.GetAsync(iconUrl);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (bytes.Length > 0)
                    {
                        await WriteFileSafelyAsync(iconPath, bytes);
                        _logger.Info($"Downloaded icon for {appId} ({bytes.Length} bytes)");
                        ManageIconCacheSize();
                        return iconPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Provided URL failed: {ex.Message}");
            }
        }

        // Fallback to Steam CDN
        var fallbackUrls = new[]
        {
            $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg",
            $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg"
        };

        foreach (var url in fallbackUrls)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (bytes.Length > 0)
                    {
                        await WriteFileSafelyAsync(iconPath, bytes);
                        _logger.Info($"Downloaded icon for {appId} from CDN ({bytes.Length} bytes)");
                        ManageIconCacheSize();
                        return iconPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"CDN failed for {url}: {ex.Message}");
            }
        }

        _logger.Warning($"All icon download methods failed for {appId}");
        return null;
    }

    public bool HasCachedIcon(string appId)
    {
        var iconPath = Path.Combine(_iconCacheFolder, $"{appId}.jpg");
        return File.Exists(iconPath);
    }

    public string? GetCachedIconPath(string appId)
    {
        var iconPath = Path.Combine(_iconCacheFolder, $"{appId}.jpg");
        return File.Exists(iconPath) ? iconPath : null;
    }

    public void ClearIconCache()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_iconCacheFolder))
            {
                File.Delete(file);
            }
            _logger.Info("Icon cache cleared");
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to clear icon cache: {ex.Message}");
        }
    }

    private void ManageIconCacheSize()
    {
        try
        {
            const long maxCacheSizeBytes = 200 * 1024 * 1024; // 200 MB
            const long targetSizeBytes = 180 * 1024 * 1024;   // 180 MB

            var iconFiles = new DirectoryInfo(_iconCacheFolder).GetFiles("*.jpg");
            long totalSize = iconFiles.Sum(f => f.Length);

            _logger.Debug($"Icon cache size: {totalSize / 1024 / 1024} MB ({iconFiles.Length} files)");

            if (totalSize <= maxCacheSizeBytes)
                return;

            _logger.Info($"Icon cache exceeded 200MB. Cleaning up...");

            var sortedFiles = iconFiles.OrderBy(f => f.LastAccessTime).ToArray();
            long currentSize = totalSize;
            int deletedCount = 0;

            foreach (var file in sortedFiles)
            {
                if (currentSize <= targetSizeBytes)
                    break;

                try
                {
                    var fileSize = file.Length;
                    file.Delete();
                    currentSize -= fileSize;
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to delete {file.Name}: {ex.Message}");
                }
            }

            _logger.Info($"Deleted {deletedCount} files. New size: {currentSize / 1024 / 1024} MB");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error managing icon cache: {ex.Message}");
        }
    }

    // Data Caching
    public void CacheManifests(List<Manifest> manifests)
    {
        try
        {
            var json = JsonSerializer.Serialize(manifests, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(_dataCacheFolder, "manifests.json");
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to cache manifests: {ex.Message}");
        }
    }

    public List<Manifest>? GetCachedManifests()
    {
        try
        {
            var filePath = Path.Combine(_dataCacheFolder, "manifests.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<Manifest>>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to read cached manifests: {ex.Message}");
        }
        return null;
    }

    public void CacheGameStatus(string appId, string jsonData)
    {
        try
        {
            var cacheInfo = new { timestamp = DateTime.Now, data = jsonData };
            var json = JsonSerializer.Serialize(cacheInfo, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(_dataCacheFolder, $"status_{appId}.json");
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to cache status for {appId}: {ex.Message}");
        }
    }

    public (string? data, DateTime? timestamp) GetCachedGameStatus(string appId)
    {
        try
        {
            var filePath = Path.Combine(_dataCacheFolder, $"status_{appId}.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var data = root.GetProperty("data").GetString();
                var timestamp = root.GetProperty("timestamp").GetDateTime();

                return (data, timestamp);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to read cached status for {appId}: {ex.Message}");
        }
        return (null, null);
    }

    public bool IsGameStatusCacheValid(string appId, TimeSpan maxAge)
    {
        var (_, timestamp) = GetCachedGameStatus(appId);
        return timestamp.HasValue && (DateTime.Now - timestamp.Value < maxAge);
    }

    public void ClearAllCache()
    {
        _logger.Info("Clearing all cache");
        ClearIconCache();

        try
        {
            foreach (var file in Directory.GetFiles(_dataCacheFolder))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to clear data cache: {ex.Message}");
        }

        _logger.Info("Cache cleared successfully");
    }

    public long GetCacheSize()
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.GetFiles(_cacheFolder, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(file).Length;
            }
        }
        catch (Exception ex)
        {
            _logger.Debug($"Error calculating cache size: {ex.Message}");
        }
        return size;
    }

    private async Task WriteFileSafelyAsync(string filePath, byte[] bytes)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
        await fileStream.WriteAsync(bytes, 0, bytes.Length);
        await fileStream.FlushAsync();
    }
}
