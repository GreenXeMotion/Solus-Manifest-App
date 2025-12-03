using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SolusManifestApp.Core.Services;

public class SteamLibraryService
{
    private readonly ISteamService _steamService;

    public SteamLibraryService(ISteamService steamService)
    {
        _steamService = steamService;
    }

    public List<string> GetLibraryFolders()
    {
        var libraryFolders = new List<string>();

        try
        {
            var steamPath = _steamService.GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                return libraryFolders;
            }

            var mainLibraryPath = Path.Combine(steamPath, "steamapps");
            if (Directory.Exists(mainLibraryPath))
            {
                libraryFolders.Add(mainLibraryPath);
            }

            var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFoldersPath))
            {
                return libraryFolders;
            }

            var content = File.ReadAllText(libraryFoldersPath);
            var additionalPaths = ParseLibraryFoldersVdf(content);
            libraryFolders.AddRange(additionalPaths);
        }
        catch (Exception)
        {
            // Return what we have even if there's an error
        }

        return libraryFolders.Distinct().ToList();
    }

    private List<string> ParseLibraryFoldersVdf(string content)
    {
        var libraryPaths = new List<string>();

        try
        {
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("\"path\""))
                {
                    var parts = trimmed.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var pathValue = parts[parts.Length - 1].Trim('"');
                        pathValue = pathValue.Replace("\\\\", "\\");
                        var steamappsPath = Path.Combine(pathValue, "steamapps");
                        if (Directory.Exists(steamappsPath))
                        {
                            libraryPaths.Add(steamappsPath);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Return empty list on parse error
        }

        return libraryPaths;
    }
}
