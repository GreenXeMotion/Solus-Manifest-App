using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SolusManifestApp.Core.Services
{
    public class LuaDepotInfo
    {
        public string DepotId { get; set; } = "";
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public bool IsTokenBased { get; set; }
        public string? DlcAppId { get; set; }
        public string? DlcName { get; set; }
    }

    public class LuaParser
    {
        /// <summary>
        /// Extracts all AppIDs that have addtoken() calls
        /// </summary>
        public HashSet<string> ParseTokenAppIds(string luaContent)
        {
            var tokenAppIds = new HashSet<string>();
            var lines = luaContent.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Match: addtoken(3282720, "186020997252537705")
                var tokenMatch = Regex.Match(trimmedLine, @"addtoken\((\d+)");
                if (tokenMatch.Success)
                {
                    var appId = tokenMatch.Groups[1].Value;
                    tokenAppIds.Add(appId);
                }
            }

            return tokenAppIds;
        }

        public List<LuaDepotInfo> ParseDepotsFromLua(string luaContent, string? mainAppId = null)
        {
            var depots = new List<LuaDepotInfo>();
            var lines = luaContent.Split('\n');
            var depotMap = new Dictionary<string, LuaDepotInfo>();

            // Get token-based AppIDs to filter them out
            var tokenAppIds = ParseTokenAppIds(luaContent);

            // Track current DLC context by looking for DLC section comments
            string? currentDlcId = null;
            string? currentDlcName = null;

            // First pass: Parse addappid lines to get depot IDs and names
            for (int i = 0; i < lines.Length; i++)
            {
                var trimmedLine = lines[i].Trim();

                // Check for DLC section comments like "-- SILENT HILL f - Bonus Content (AppID: 3282720)"
                var dlcCommentMatch = Regex.Match(trimmedLine, @"--\s*(.+?)\s*\(AppID:\s*(\d+)\)");
                if (dlcCommentMatch.Success)
                {
                    currentDlcName = dlcCommentMatch.Groups[1].Value.Trim();
                    var dlcId = dlcCommentMatch.Groups[2].Value;
                    currentDlcId = dlcId;
                    continue;
                }

                // Reset DLC context when we see a main section (base game)
                if (trimmedLine.StartsWith("-- Base") || trimmedLine.Contains("(Base Game)"))
                {
                    currentDlcId = null;
                    currentDlcName = null;
                    continue;
                }

                // Match: addappid(285311, 1, "hash") -- Rollercoaster Tycoon Content
                var addAppIdMatch = Regex.Match(trimmedLine, @"addappid\((\d+)(?:,.*?)?\)\s*--\s*(.+)");
                if (addAppIdMatch.Success)
                {
                    var depotId = addAppIdMatch.Groups[1].Value;
                    var depotName = addAppIdMatch.Groups[2].Value.Trim();

                    // Check if this depot is token-based
                    bool isTokenBased = tokenAppIds.Contains(depotId) ||
                                       (currentDlcId != null && tokenAppIds.Contains(currentDlcId));

                    if (!depotMap.ContainsKey(depotId))
                    {
                        depotMap[depotId] = new LuaDepotInfo
                        {
                            DepotId = depotId,
                            Name = depotName,
                            Size = 0,
                            IsTokenBased = isTokenBased,
                            DlcAppId = currentDlcId,
                            DlcName = currentDlcName
                        };
                    }
                }
            }

            // Second pass: Parse size comments and update depotMap
            for (int i = 0; i < lines.Length; i++)
            {
                var trimmedLine = lines[i].Trim();

                // Match size comments: -- Size: 12.34 GB (123456789 bytes)
                var sizeCommentMatch = Regex.Match(trimmedLine, @"--\s*Size:\s*[\d.]+\s*[KMGT]?B\s*\((\d+)\s*bytes\)");
                if (sizeCommentMatch.Success)
                {
                    var sizeBytes = long.Parse(sizeCommentMatch.Groups[1].Value);

                    // Look back for the depot ID on previous lines
                    for (int j = i - 1; j >= 0 && j >= i - 5; j--)
                    {
                        var prevLine = lines[j].Trim();
                        var depotMatch = Regex.Match(prevLine, @"addappid\((\d+)");
                        if (depotMatch.Success)
                        {
                            var depotId = depotMatch.Groups[1].Value;
                            if (depotMap.ContainsKey(depotId))
                            {
                                depotMap[depotId].Size = sizeBytes;
                            }
                            break;
                        }
                    }
                }
            }

            return new List<LuaDepotInfo>(depotMap.Values);
        }

        /// <summary>
        /// Calculates total size of all depots in bytes
        /// </summary>
        public long CalculateTotalSize(List<LuaDepotInfo> depots)
        {
            long total = 0;
            foreach (var depot in depots)
            {
                total += depot.Size;
            }
            return total;
        }

        /// <summary>
        /// Formats bytes into human-readable size (e.g., "12.34 GB")
        /// </summary>
        public string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
