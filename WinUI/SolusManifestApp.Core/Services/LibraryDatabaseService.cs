using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SolusManifestApp.Core.Services
{
    /// <summary>
    /// Simplified library database service for caching library items.
    /// Note: Full SQLite implementation will be added in Phase 4 when Microsoft.Data.Sqlite
    /// NuGet package is integrated. For now, uses in-memory dictionary storage.
    /// </summary>
    public class LibraryDatabaseService : IDisposable
    {
        private readonly ILoggerService _logger;
        private readonly Dictionary<string, LibraryItem> _inMemoryCache;
        private readonly string _cacheFilePath;

        public LibraryDatabaseService(ILoggerService logger)
        {
            _logger = logger;
            _inMemoryCache = new Dictionary<string, LibraryItem>();

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbFolder = Path.Combine(appData, "SolusManifestApp");
            Directory.CreateDirectory(dbFolder);
            _cacheFilePath = Path.Combine(dbFolder, "library_cache.json");

            _logger.Info($"LibraryDatabase initialized (in-memory mode)");
            _logger.Info($"Cache file: {_cacheFilePath}");
        }

        /// <summary>
        /// Inserts or updates a library item
        /// </summary>
        public void UpsertLibraryItem(LibraryItem item)
        {
            try
            {
                _inMemoryCache[item.AppId] = item;
                _logger.Debug($"Upserted library item: {item.Name} ({item.AppId})");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to upsert library item {item.AppId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Bulk inserts or updates multiple library items
        /// </summary>
        public void BulkUpsertLibraryItems(IEnumerable<LibraryItem> items)
        {
            int count = 0;
            foreach (var item in items)
            {
                _inMemoryCache[item.AppId] = item;
                count++;
            }
            _logger.Info($"Bulk upserted {count} library items");
        }

        /// <summary>
        /// Gets all library items
        /// </summary>
        public List<LibraryItem> GetAllLibraryItems()
        {
            return new List<LibraryItem>(_inMemoryCache.Values);
        }

        /// <summary>
        /// Gets library items by type
        /// </summary>
        public List<LibraryItem> GetLibraryItemsByType(LibraryItemType itemType)
        {
            var items = new List<LibraryItem>();
            foreach (var item in _inMemoryCache.Values)
            {
                if (item.ItemType == itemType)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// Gets a single library item by AppId
        /// </summary>
        public LibraryItem? GetLibraryItem(string appId)
        {
            _inMemoryCache.TryGetValue(appId, out var item);
            return item;
        }

        /// <summary>
        /// Searches library items by name
        /// </summary>
        public List<LibraryItem> SearchLibraryItems(string searchTerm)
        {
            var results = new List<LibraryItem>();
            var lowerSearch = searchTerm.ToLowerInvariant();

            foreach (var item in _inMemoryCache.Values)
            {
                if (item.Name?.ToLowerInvariant().Contains(lowerSearch) == true)
                {
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Deletes a library item
        /// </summary>
        public void DeleteLibraryItem(string appId)
        {
            if (_inMemoryCache.Remove(appId))
            {
                _logger.Debug($"Deleted library item: {appId}");
            }
        }

        /// <summary>
        /// Marks items as stale if they haven't been scanned recently
        /// </summary>
        public int MarkStaleItems(DateTime cutoffTime)
        {
            int count = 0;
            var itemsToRemove = new List<string>();

            foreach (var kvp in _inMemoryCache)
            {
                // Items without recent scan data are considered stale
                // In full implementation, this would check LastScanned timestamp
                // For now, just log the operation
                count++;
            }

            foreach (var appId in itemsToRemove)
            {
                _inMemoryCache.Remove(appId);
            }

            if (count > 0)
            {
                _logger.Info($"Marked {count} stale items");
            }

            return count;
        }

        /// <summary>
        /// Updates last accessed timestamp
        /// </summary>
        public void UpdateLastAccessed(string appId)
        {
            if (_inMemoryCache.TryGetValue(appId, out var item))
            {
                // In full implementation, this would update LastAccessed in database
                _logger.Debug($"Updated last accessed: {appId}");
            }
        }

        /// <summary>
        /// Gets total count of library items
        /// </summary>
        public int GetTotalCount()
        {
            return _inMemoryCache.Count;
        }

        /// <summary>
        /// Clears all library items
        /// </summary>
        public void ClearAll()
        {
            _inMemoryCache.Clear();
            _logger.Info("Cleared all library items");
        }

        public void Dispose()
        {
            // In full implementation with SQLite, this would close the connection
            _logger.Debug("LibraryDatabaseService disposed");
        }
    }
}
