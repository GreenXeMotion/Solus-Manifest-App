using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SolusManifestApp.Core.Services;

/// <summary>
/// Manages GreenLuma AppList.txt file for game unlocking
/// </summary>
public class LuaFileManager
{
    private readonly ISteamService _steamService;
    private readonly ILoggerService _logger;

    public LuaFileManager(ISteamService steamService, ILoggerService logger)
    {
        _steamService = steamService;
        _logger = logger;
    }

    public string GetAppListPath()
    {
        var steamPath = _steamService.GetSteamPath();
        if (string.IsNullOrEmpty(steamPath))
        {
            throw new Exception("Steam path not found");
        }

        var configPath = Path.Combine(steamPath, "config", "stplug-in");
        Directory.CreateDirectory(configPath);

        return Path.Combine(configPath, "AppList.txt");
    }

    public List<string> ReadAppList()
    {
        try
        {
            var path = GetAppListPath();

            if (!File.Exists(path))
            {
                return new List<string>();
            }

            var lines = File.ReadAllLines(path);
            return lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to read AppList: {ex.Message}");
            return new List<string>();
        }
    }

    public bool WriteAppList(List<string> appIds)
    {
        try
        {
            var path = GetAppListPath();
            var distinctIds = appIds.Distinct().OrderBy(id => id).ToList();

            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(path, distinctIds, Encoding.UTF8);
            _logger.Info($"Wrote {distinctIds.Count} app IDs to AppList.txt");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to write AppList: {ex.Message}");
            return false;
        }
    }

    public bool AddAppId(string appId)
    {
        try
        {
            var appList = ReadAppList();

            if (!appList.Contains(appId))
            {
                appList.Add(appId);
                return WriteAppList(appList);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to add app ID: {ex.Message}");
            return false;
        }
    }

    public bool RemoveAppId(string appId)
    {
        try
        {
            var appList = ReadAppList();

            if (appList.Remove(appId))
            {
                return WriteAppList(appList);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to remove app ID: {ex.Message}");
            return false;
        }
    }

    public bool ClearAppList()
    {
        try
        {
            var path = GetAppListPath();

            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.Info("Cleared AppList.txt");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to clear AppList: {ex.Message}");
            return false;
        }
    }

    public int GetAppListCount()
    {
        return ReadAppList().Count;
    }

    public bool IsAppIdInList(string appId)
    {
        return ReadAppList().Contains(appId);
    }
}
