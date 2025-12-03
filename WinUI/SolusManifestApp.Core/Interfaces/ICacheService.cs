using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Interfaces;

public interface ICacheService
{
    Task<string?> GetIconAsync(string appId, string iconUrl);
    bool HasCachedIcon(string appId);
    string? GetCachedIconPath(string appId);
    void ClearIconCache();
    void CacheManifests(List<Manifest> manifests);
    List<Manifest>? GetCachedManifests();
    void CacheGameStatus(string appId, string jsonData);
    (string? data, DateTime? timestamp) GetCachedGameStatus(string appId);
    bool IsGameStatusCacheValid(string appId, TimeSpan maxAge);
    void ClearAllCache();
    long GetCacheSize();
}
