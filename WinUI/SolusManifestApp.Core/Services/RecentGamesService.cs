using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SolusManifestApp.Core.Services;

public class RecentGameInfo
{
    public string AppId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? IconPath { get; set; }
    public DateTime LastAccessed { get; set; }
}

/// <summary>
/// Simple file-based service to track recently accessed games
/// </summary>
public class RecentGamesService
{
    private readonly ILoggerService _logger;
    private readonly string _recentGamesPath;
    private readonly int _maxRecentGames = 10;

    public RecentGamesService(ILoggerService logger)
    {
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "SolusManifestApp");
        Directory.CreateDirectory(appFolder);
        _recentGamesPath = Path.Combine(appFolder, "recent_games.json");
    }

    public void MarkAsRecentlyAccessed(string appId, string? gameName = null)
    {
        try
        {
            var recentGames = LoadRecentGames();

            // Remove if already exists
            recentGames.RemoveAll(g => g.AppId == appId);

            // Add to front
            recentGames.Insert(0, new RecentGameInfo
            {
                AppId = appId,
                Name = gameName ?? appId,
                LastAccessed = DateTime.Now
            });

            // Keep only max entries
            if (recentGames.Count > _maxRecentGames)
            {
                recentGames = recentGames.Take(_maxRecentGames).ToList();
            }

            SaveRecentGames(recentGames);
            _logger.Info($"Marked {appId} as recently accessed");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to mark {appId} as recent: {ex.Message}");
        }
    }

    public List<RecentGameInfo> GetRecentGames(int limit = 5)
    {
        try
        {
            var recentGames = LoadRecentGames();
            return recentGames.Take(Math.Min(limit, _maxRecentGames)).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get recent games: {ex.Message}");
            return new List<RecentGameInfo>();
        }
    }

    public void ClearRecentGames()
    {
        try
        {
            if (File.Exists(_recentGamesPath))
            {
                File.Delete(_recentGamesPath);
                _logger.Info("Cleared recent games");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to clear recent games: {ex.Message}");
        }
    }

    private List<RecentGameInfo> LoadRecentGames()
    {
        if (!File.Exists(_recentGamesPath))
        {
            return new List<RecentGameInfo>();
        }

        try
        {
            var json = File.ReadAllText(_recentGamesPath);
            return JsonSerializer.Deserialize<List<RecentGameInfo>>(json) ?? new List<RecentGameInfo>();
        }
        catch
        {
            return new List<RecentGameInfo>();
        }
    }

    private void SaveRecentGames(List<RecentGameInfo> games)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(games, options);
        File.WriteAllText(_recentGamesPath, json);
    }
}
