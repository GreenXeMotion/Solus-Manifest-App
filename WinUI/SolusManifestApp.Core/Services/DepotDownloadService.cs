using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services
{
    public class DepotInfo
    {
        public string DepotId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Language { get; set; }
        public long Size { get; set; }
        public bool IsLanguageSpecific { get; set; }
        public string? DecryptionKey { get; set; }
        public bool IsSelected { get; set; } = true;
        public bool IsTokenBased { get; set; }
        public string? DlcAppId { get; set; }
        public string? DlcName { get; set; }
        public bool IsMainAppId { get; set; }
    }

    public class LanguageOption
    {
        public string Language { get; set; } = "";
        public List<string> RequiredDepots { get; set; } = new();
        public long TotalSize { get; set; }
    }

    public class DepotDownloadService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LuaParser _luaParser;
        private readonly ILoggerService _logger;

        public DepotDownloadService(IHttpClientFactory httpClientFactory, LuaParser luaParser, ILoggerService logger)
        {
            _httpClientFactory = httpClientFactory;
            _luaParser = luaParser;
            _logger = logger;
        }

        private HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient("Default");
        }

        public async Task<List<DepotInfo>> GetDepotsFromSteamCMD(string appId)
        {
            try
            {
                var client = CreateClient();
                var url = $"https://api.steamcmd.net/v1/info/{appId}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warning($"SteamCMD API returned {response.StatusCode} for app {appId}");
                    return new List<DepotInfo>();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var depots = new List<DepotInfo>();

                if (root.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty(appId, out var appElement) &&
                    appElement.TryGetProperty("depots", out var depotsElement))
                {
                    foreach (var depot in depotsElement.EnumerateObject())
                    {
                        var depotId = depot.Name;

                        // Skip non-numeric depot IDs (like "branches")
                        if (!long.TryParse(depotId, out _))
                            continue;

                        var depotData = depot.Value;

                        long size = 0;
                        if (depotData.TryGetProperty("manifests", out var manifestsElement) &&
                            manifestsElement.TryGetProperty("public", out var publicElement) &&
                            publicElement.TryGetProperty("size", out var sizeElement))
                        {
                            size = sizeElement.GetInt64();
                        }

                        string? language = null;
                        if (depotData.TryGetProperty("config", out var configElement) &&
                            configElement.TryGetProperty("language", out var langElement))
                        {
                            language = langElement.GetString();
                        }

                        var depotInfo = new DepotInfo
                        {
                            DepotId = depotId,
                            Language = language ?? "english",
                            Size = size,
                            IsLanguageSpecific = !string.IsNullOrEmpty(language)
                        };

                        depots.Add(depotInfo);
                    }
                }

                return depots;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get depots from SteamCMD: {ex.Message}");
                return new List<DepotInfo>();
            }
        }

        public List<LanguageOption> AnalyzeLanguageOptions(List<DepotInfo> depots)
        {
            var languageOptions = new List<LanguageOption>();

            // Get the base depot (usually the one without specific language or the largest)
            var baseDepot = depots.FirstOrDefault(d => !d.IsLanguageSpecific)
                ?? depots.OrderByDescending(d => d.Size).FirstOrDefault();

            if (baseDepot == null)
                return languageOptions;

            // Group by language
            var languageGroups = depots
                .Where(d => !string.IsNullOrEmpty(d.Language))
                .GroupBy(d => d.Language);

            foreach (var group in languageGroups)
            {
                var language = group.Key ?? "english";
                var languageDepots = group.ToList();

                // Check if language-specific depots are close in size to base depot
                var totalLanguageSize = languageDepots.Sum(d => d.Size);
                var baseSize = baseDepot.Size;

                var requiredDepots = new List<string>();

                // If language depot is close to base size (within 500 MB), it's complete
                if (totalLanguageSize > 0 && Math.Abs(totalLanguageSize - baseSize) < 500_000_000)
                {
                    // Language depot has full game, don't need base
                    requiredDepots = languageDepots.Select(d => d.DepotId).ToList();
                }
                else
                {
                    // Need base depot + language depot
                    requiredDepots.Add(baseDepot.DepotId);
                    requiredDepots.AddRange(languageDepots.Select(d => d.DepotId));
                }

                languageOptions.Add(new LanguageOption
                {
                    Language = language,
                    RequiredDepots = requiredDepots,
                    TotalSize = requiredDepots.Sum(id => depots.FirstOrDefault(d => d.DepotId == id)?.Size ?? 0)
                });
            }

            // If no language-specific depots, just use base
            if (!languageOptions.Any() && baseDepot != null)
            {
                languageOptions.Add(new LanguageOption
                {
                    Language = "english",
                    RequiredDepots = new List<string> { baseDepot.DepotId },
                    TotalSize = baseDepot.Size
                });
            }

            return languageOptions;
        }

        /// <summary>
        /// Combines depot info from lua file (names, sizes) with SteamCMD data (languages)
        /// </summary>
        public async Task<List<DepotInfo>> GetCombinedDepotInfo(string appId, string luaContent)
        {
            _logger.Debug($"Combining depot info for app {appId}");

            // Parse depot info from lua file (filter out main AppID)
            var luaDepots = _luaParser.ParseDepotsFromLua(luaContent, appId);

            // Get language info from SteamCMD
            var steamCmdDepots = await GetDepotsFromSteamCMD(appId);

            // Combine the data
            var combinedDepots = new List<DepotInfo>();

            foreach (var luaDepot in luaDepots)
            {
                // Find matching SteamCMD depot for language info
                var steamDepot = steamCmdDepots.FirstOrDefault(d => d.DepotId == luaDepot.DepotId);

                var depotInfo = new DepotInfo
                {
                    DepotId = luaDepot.DepotId,
                    Name = luaDepot.Name,
                    Size = luaDepot.Size,
                    Language = steamDepot?.Language ?? "Unknown",
                    IsLanguageSpecific = steamDepot?.IsLanguageSpecific ?? false,
                    IsSelected = true,
                    IsTokenBased = luaDepot.IsTokenBased,
                    DlcAppId = luaDepot.DlcAppId,
                    DlcName = luaDepot.DlcName
                };

                combinedDepots.Add(depotInfo);
            }

            _logger.Info($"Combined {combinedDepots.Count} depots for app {appId}");
            return combinedDepots;
        }
    }
}
