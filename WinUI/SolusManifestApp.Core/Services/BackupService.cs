using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class BackupData
{
    public DateTime BackupDate { get; set; }
    public string Version { get; set; } = "1.0";
    public AppSettings Settings { get; set; } = new AppSettings();
    public List<string> InstalledAppIds { get; set; } = new List<string>();
    public List<Manifest> GameMetadata { get; set; } = new List<Manifest>();
}

public class RestoreResult
{
    public bool Success { get; set; }
    public bool SettingsRestored { get; set; }
    public int GamesRestored { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BackupService
{
    private readonly ISettingsService _settingsService;
    private readonly ICacheService _cacheService;
    private readonly ILoggerService _logger;

    public BackupService(
        ISettingsService settingsService,
        ICacheService cacheService,
        ILoggerService logger)
    {
        _settingsService = settingsService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(string backupPath, List<string>? installedAppIds = null)
    {
        try
        {
            var backup = new BackupData
            {
                BackupDate = DateTime.Now,
                Settings = _settingsService.GetSettings<AppSettings>(),
                InstalledAppIds = installedAppIds ?? new List<string>()
            };

            // Try to get cached metadata
            foreach (var appId in backup.InstalledAppIds)
            {
                var manifest = _cacheService.GetCachedManifests()?.FirstOrDefault(m => m.AppId == appId);
                if (manifest != null)
                {
                    backup.GameMetadata.Add(manifest);
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(backup, options);

            var fileName = $"SolusBackup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var fullPath = Path.Combine(backupPath, fileName);

            Directory.CreateDirectory(backupPath);
            await File.WriteAllTextAsync(fullPath, json);

            _logger.Info($"Backup created: {fullPath}");
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create backup: {ex.Message}");
            throw new Exception($"Failed to create backup: {ex.Message}", ex);
        }
    }

    public async Task<BackupData> LoadBackupAsync(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file not found");
            }

            var json = await File.ReadAllTextAsync(backupFilePath);
            var backup = JsonSerializer.Deserialize<BackupData>(json);

            if (backup == null)
            {
                throw new Exception("Invalid backup file format");
            }

            _logger.Info($"Loaded backup from: {backupFilePath}");
            return backup;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load backup: {ex.Message}");
            throw new Exception($"Failed to load backup: {ex.Message}", ex);
        }
    }

    public async Task<RestoreResult> RestoreBackupAsync(BackupData backup, bool restoreSettings = true)
    {
        var result = new RestoreResult();

        try
        {
            if (restoreSettings && backup.Settings != null)
            {
                // Note: ISettingsService doesn't expose SaveSettings publicly
                // Settings restoration would need to be done through UpdateSettings calls
                // or by extending the interface
                result.SettingsRestored = false;
                _logger.Info("Settings restoration not fully implemented (interface limitation)");
            }

            // Cache metadata for reference
            if (backup.GameMetadata != null)
            {
                _cacheService.CacheManifests(backup.GameMetadata);
                result.GamesRestored = backup.GameMetadata.Count;
            }

            result.Success = true;
            _logger.Info($"Backup restored: {result.GamesRestored} games");
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.Error($"Backup restore failed: {ex.Message}");
            return result;
        }
    }
}
