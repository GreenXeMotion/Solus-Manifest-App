using SolusManifestApp.Core.Helpers;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SolusManifestApp.Core.Services;

public class SteamGamesService
{
    private readonly ISteamService _steamService;

    public SteamGamesService(ISteamService steamService)
    {
        _steamService = steamService;
    }

    public List<SteamGame> GetInstalledGames()
    {
        var games = new List<SteamGame>();

        try
        {
            var libraryFolders = GetLibraryFolders();

            foreach (var libraryPath in libraryFolders)
            {
                var steamappsPath = Path.Combine(libraryPath, "steamapps");
                if (!Directory.Exists(steamappsPath))
                    continue;

                var manifestFiles = Directory.GetFiles(steamappsPath, "appmanifest_*.acf");

                foreach (var manifestFile in manifestFiles)
                {
                    try
                    {
                        var game = ParseAppManifest(manifestFile, libraryPath);
                        if (game != null)
                        {
                            games.Add(game);
                        }
                    }
                    catch
                    {
                        // Skip invalid manifests
                    }
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return games.GroupBy(g => g.AppId)
                   .Select(g => g.First())
                   .OrderBy(g => g.Name)
                   .ToList();
    }

    private List<string> GetLibraryFolders()
    {
        var folders = new List<string>();

        var steamPath = _steamService.GetSteamPath();
        if (string.IsNullOrEmpty(steamPath))
        {
            throw new Exception("Steam installation not found");
        }

        folders.Add(steamPath);

        var libraryFoldersFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersFile))
        {
            libraryFoldersFile = Path.Combine(steamPath, "config", "libraryfolders.vdf");
        }

        if (File.Exists(libraryFoldersFile))
        {
            try
            {
                var data = VdfParser.Parse(libraryFoldersFile);
                var libraryFoldersObj = VdfParser.GetObject(data, "libraryfolders");

                if (libraryFoldersObj != null)
                {
                    foreach (var key in libraryFoldersObj.Keys)
                    {
                        if (int.TryParse(key, out _))
                        {
                            var folderData = VdfParser.GetObject(libraryFoldersObj, key);
                            if (folderData != null)
                            {
                                var path = VdfParser.GetValue(folderData, "path");
                                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                                {
                                    folders.Add(path);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Continue with main folder only
            }
        }

        return folders;
    }

    private SteamGame? ParseAppManifest(string manifestPath, string libraryPath)
    {
        try
        {
            var data = VdfParser.Parse(manifestPath);
            var appStateData = VdfParser.GetObject(data, "AppState");

            if (appStateData == null)
                return null;

            var appId = VdfParser.GetValue(appStateData, "appid");
            var name = VdfParser.GetValue(appStateData, "name");
            var installDir = VdfParser.GetValue(appStateData, "installdir");
            var sizeOnDisk = VdfParser.GetLong(appStateData, "SizeOnDisk");
            var lastUpdated = VdfParser.GetLong(appStateData, "LastUpdated");
            var stateFlags = VdfParser.GetValue(appStateData, "StateFlags");
            var buildId = VdfParser.GetValue(appStateData, "buildid");

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(name))
                return null;

            return new SteamGame
            {
                AppId = appId,
                Name = name,
                InstallDir = installDir ?? "",
                SizeOnDisk = sizeOnDisk,
                LastUpdated = lastUpdated > 0 ? DateTimeOffset.FromUnixTimeSeconds(lastUpdated).DateTime : null,
                LibraryPath = libraryPath,
                StateFlags = stateFlags ?? "0",
                IsFullyInstalled = (int.TryParse(stateFlags, out var flags) && (flags & 4) == 4),
                BuildId = buildId ?? ""
            };
        }
        catch
        {
            return null;
        }
    }
}
