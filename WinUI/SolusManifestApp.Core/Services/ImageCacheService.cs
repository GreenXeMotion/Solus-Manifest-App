using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services
{
    /// <summary>
    /// In-memory cache service for image paths to improve Library page performance.
    /// Framework-agnostic - does not handle image decoding, only path caching and management.
    /// </summary>
    public class ImageCacheService
    {
        private readonly Dictionary<string, string> _pathCache = new();
        private readonly object _cacheLock = new object();
        private readonly ILoggerService? _logger;
        private const int MAX_CACHE_SIZE = 200; // Maximum number of image paths to cache

        public ImageCacheService(ILoggerService? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets an image path from cache or validates and caches it.
        /// </summary>
        /// <param name="appId">Steam App ID</param>
        /// <param name="imagePath">Full path to the image file on disk</param>
        /// <returns>Cached image path, or null if file doesn't exist</returns>
        public async Task<string?> GetImagePathAsync(string appId, string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            var cacheKey = $"steam_{appId}";

            // Check cache first (thread-safe)
            lock (_cacheLock)
            {
                if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
                {
                    _logger?.Debug($"Image path cache HIT for {appId}");
                    return cachedPath;
                }
            }

            // Not in cache - validate file exists
            _logger?.Debug($"Image path cache MISS for {appId}, validating: {imagePath}");

            try
            {
                // Validate file exists on background thread
                var exists = await Task.Run(() => File.Exists(imagePath));

                if (exists)
                {
                    // Add to cache (thread-safe)
                    lock (_cacheLock)
                    {
                        // Check if cache is full
                        if (_pathCache.Count >= MAX_CACHE_SIZE)
                        {
                            _logger?.Info($"Image path cache full ({MAX_CACHE_SIZE} items), clearing oldest entries");
                            // Simple strategy: clear 20% of cache to make room
                            var itemsToRemove = MAX_CACHE_SIZE / 5;
                            var keysToRemove = new List<string>();
                            int removed = 0;

                            foreach (var key in _pathCache.Keys)
                            {
                                keysToRemove.Add(key);
                                removed++;
                                if (removed >= itemsToRemove)
                                    break;
                            }

                            foreach (var key in keysToRemove)
                            {
                                _pathCache.Remove(key);
                            }
                        }

                        // Add to cache if not already there
                        if (!_pathCache.ContainsKey(cacheKey))
                        {
                            _pathCache[cacheKey] = imagePath;
                            _logger?.Info($"✓ Cached image path for {appId} (cache size: {_pathCache.Count})");
                        }
                    }

                    return imagePath;
                }
                else
                {
                    _logger?.Warning($"Image file not found: {imagePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to validate image path for {appId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a cached image path synchronously (for UI binding).
        /// Returns null if not in cache - use GetImagePathAsync to load.
        /// </summary>
        public string? GetCachedImagePath(string appId)
        {
            var cacheKey = $"steam_{appId}";

            lock (_cacheLock)
            {
                if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
                {
                    return cachedPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Pre-loads multiple image paths into cache asynchronously.
        /// </summary>
        public async Task PreloadImagePathsAsync(Dictionary<string, string> appIdToPathMap)
        {
            _logger?.Info($"Pre-loading {appIdToPathMap.Count} image paths into cache...");

            var tasks = new List<Task>();

            foreach (var kvp in appIdToPathMap)
            {
                tasks.Add(GetImagePathAsync(kvp.Key, kvp.Value));
            }

            await Task.WhenAll(tasks);

            _logger?.Info($"✓ Pre-load complete. Cache size: {_pathCache.Count}");
        }

        /// <summary>
        /// Clears all cached image paths from memory.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                var count = _pathCache.Count;
                _pathCache.Clear();
                _logger?.Info($"Image path cache cleared ({count} items removed)");
            }
        }

        /// <summary>
        /// Gets the current number of cached image paths.
        /// </summary>
        public int GetCacheSize()
        {
            lock (_cacheLock)
            {
                return _pathCache.Count;
            }
        }

        /// <summary>
        /// Removes a specific entry from cache.
        /// </summary>
        public void RemoveFromCache(string appId)
        {
            var cacheKey = $"steam_{appId}";

            lock (_cacheLock)
            {
                if (_pathCache.Remove(cacheKey))
                {
                    _logger?.Debug($"Removed {appId} from cache");
                }
            }
        }

        /// <summary>
        /// Adds or updates a cache entry directly.
        /// </summary>
        public void CacheImagePath(string appId, string imagePath)
        {
            var cacheKey = $"steam_{appId}";

            lock (_cacheLock)
            {
                _pathCache[cacheKey] = imagePath;
                _logger?.Debug($"Manually cached image path for {appId}");
            }
        }
    }
}
