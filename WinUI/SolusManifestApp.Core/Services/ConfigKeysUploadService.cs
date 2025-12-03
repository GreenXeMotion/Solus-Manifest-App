using SolusManifestApp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services
{
    /// <summary>
    /// Service for automatically uploading depot keys from Steam's config.vdf to Morrenus API.
    /// Note: Requires ConfigVdfKeyExtractor tool integration (not included in Core).
    /// </summary>
    public class ConfigKeysUploadService : IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILoggerService _logger;
        private readonly INotificationService _notificationService;
        private readonly ISteamService _steamService;
        private readonly IHttpClientFactory _httpClientFactory;
        private Timer? _uploadTimer;
        private bool _isUploading = false;
        private readonly TimeSpan _uploadInterval = TimeSpan.FromHours(1); // Upload every hour

        public ConfigKeysUploadService(
            ISettingsService settingsService,
            ILoggerService logger,
            INotificationService notificationService,
            ISteamService steamService,
            IHttpClientFactory httpClientFactory)
        {
            _settingsService = settingsService;
            _logger = logger;
            _notificationService = notificationService;
            _steamService = steamService;
            _httpClientFactory = httpClientFactory;
        }

        public void Start()
        {
            var settings = _settingsService.GetSettings<Models.AppSettings>();
            if (settings == null || !settings.AutoUploadConfigKeys)
            {
                _logger.Info("Config keys auto-upload is disabled");
                return;
            }

            _logger.Info("Config keys auto-upload service started");

            // Upload immediately on startup
            _ = UploadNewKeysAsync();

            // Then schedule periodic uploads
            _uploadTimer = new Timer(
                async _ => await UploadNewKeysAsync(),
                null,
                _uploadInterval,
                _uploadInterval
            );
        }

        public void Stop()
        {
            _uploadTimer?.Dispose();
            _uploadTimer = null;
        }

        private async Task UploadNewKeysAsync()
        {
            if (_isUploading)
            {
                _logger.Info("Config keys upload already in progress, skipping...");
                return;
            }

            _isUploading = true;

            try
            {
                var settings = _settingsService.GetSettings<Models.AppSettings>();

                if (settings == null || !settings.AutoUploadConfigKeys)
                {
                    return;
                }

                // Check if enough time has passed since last upload
                var timeSinceLastUpload = DateTime.Now - settings.LastConfigKeysUpload;
                if (timeSinceLastUpload < _uploadInterval)
                {
                    var remainingTime = _uploadInterval - timeSinceLastUpload;
                    _logger.Info($"Skipping config keys upload - next upload in {remainingTime.TotalMinutes:F0} minutes");
                    return;
                }

                if (string.IsNullOrEmpty(settings.ApiKey))
                {
                    _logger.Info("Cannot upload config keys: API key not configured");
                    return;
                }

                // Get Steam directory
                string? steamPath = _steamService.GetSteamPath();
                if (string.IsNullOrEmpty(steamPath))
                {
                    _logger.Info("Cannot upload config keys: Steam directory not detected");
                    _notificationService.ShowInfo("Config Keys Check", "Steam directory not detected. Please set your Steam path in Settings.");
                    return;
                }

                string configVdfPath = Path.Combine(steamPath, "config", "config.vdf");
                if (!File.Exists(configVdfPath))
                {
                    _logger.Info($"Config.vdf not found at: {configVdfPath}");
                    _notificationService.ShowInfo("Config Keys Check", "Steam config.vdf file not found. Make sure Steam has been run at least once.");
                    return;
                }

                _logger.Info("Extracting depot keys from config.vdf...");

                // Note: Actual key extraction would require VdfKeyExtractor tool
                // For now, this is a placeholder that shows the architecture
                var extractedKeys = new Dictionary<string, string>();

                // TODO: Integrate with ConfigVdfKeyExtractor tool
                // var extractionResult = VdfKeyExtractor.ExtractKeysFromVdf(configVdfPath, null);
                // extractedKeys = extractionResult.Keys;

                if (extractedKeys.Count == 0)
                {
                    _logger.Info("No new keys found in config.vdf");
                    return;
                }

                _logger.Info($"Extracted {extractedKeys.Count} keys from config.vdf");

                // Get existing depot IDs from server
                _logger.Info("Fetching existing depot IDs from server...");
                var existingDepotIds = await GetExistingDepotIdsAsync(settings.ApiKey);

                if (existingDepotIds == null)
                {
                    _logger.Info("Failed to fetch existing depot IDs from server");
                    return;
                }

                _logger.Info($"Server has {existingDepotIds.Count} existing depot IDs");

                // Filter to only new keys
                var newKeys = extractedKeys
                    .Where(kvp => !existingDepotIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (newKeys.Count == 0)
                {
                    _logger.Info("No new keys to upload - all keys already exist on server");
                    _notificationService.ShowInfo("Config Keys Check", "No new depot keys to upload. All keys are already on the server.");
                    return;
                }

                _logger.Info($"Found {newKeys.Count} new keys to upload");
                _notificationService.ShowInfo("Config Keys Upload", $"Uploading {newKeys.Count} new depot keys to Morrenus...");

                // Save to uniquely named file
                string machineName = Environment.MachineName.Replace(" ", "_");
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileName = $"{machineName}_{timestamp}_keys.txt";
                string tempPath = Path.Combine(Path.GetTempPath(), fileName);

                string keyContent = FormatKeysAsText(newKeys);
                await File.WriteAllTextAsync(tempPath, keyContent);

                _logger.Info($"Saved new keys to: {fileName}");

                // Upload to server
                _logger.Info("Uploading new keys to server...");
                var uploadResult = await UploadKeysFileAsync(tempPath, settings.ApiKey);

                if (uploadResult.Success)
                {
                    _logger.Info($"Successfully uploaded {newKeys.Count} new keys! ({uploadResult.Message})");
                    _notificationService.ShowSuccess($"Successfully uploaded {newKeys.Count} new depot keys!", "Config Keys Upload");

                    // Update last upload timestamp
                    settings.LastConfigKeysUpload = DateTime.Now;
                    // Note: SaveSettings not available in current interface

                    // Clean up temp file
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
                else
                {
                    _logger.Error($"Failed to upload keys: {uploadResult.Message}");
                    _notificationService.ShowError($"Failed to upload keys: {uploadResult.Message}", "Config Keys Upload Failed");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during config keys upload: {ex.Message}");
                _notificationService.ShowError($"Error during config keys upload: {ex.Message}", "Config Keys Upload Error");
            }
            finally
            {
                _isUploading = false;
            }
        }

        private async Task<HashSet<string>?> GetExistingDepotIdsAsync(string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Default");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await client.GetAsync("https://manifest.morrenus.xyz/api/v1/depot-keys");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("existing_depot_ids", out var depotIdsElement))
                {
                    var depotIds = new HashSet<string>();
                    foreach (var id in depotIdsElement.EnumerateArray())
                    {
                        var idStr = id.GetString();
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            depotIds.Add(idStr);
                        }
                    }
                    return depotIds;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get existing depot IDs: {ex.Message}");
                return null;
            }
        }

        private async Task<(bool Success, string Message)> UploadKeysFileAsync(string filePath, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Default");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                using var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

                string fileName = Path.GetFileName(filePath);
                form.Add(fileContent, "file", fileName);

                var response = await client.PostAsync("https://manifest.morrenus.xyz/api/v1/upload-machine-keys", form);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseString);
                        int validLines = jsonDoc.RootElement.TryGetProperty("valid_lines", out var validElement)
                            ? validElement.GetInt32() : 0;
                        int invalidLines = jsonDoc.RootElement.TryGetProperty("invalid_lines_removed", out var invalidElement)
                            ? invalidElement.GetInt32() : 0;

                        string message = $"{validLines} valid lines";
                        if (invalidLines > 0)
                        {
                            message += $", {invalidLines} invalid removed";
                        }

                        return (true, message);
                    }
                    catch
                    {
                        return (true, "Upload successful");
                    }
                }

                return (false, $"HTTP {(int)response.StatusCode}: {responseString}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private string FormatKeysAsText(Dictionary<string, string> keys)
        {
            return string.Join(Environment.NewLine, keys.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
