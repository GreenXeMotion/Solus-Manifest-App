using SolusManifestApp.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class UpdateInfo
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public UpdateAsset[] Assets { get; set; } = Array.Empty<UpdateAsset>();
}

public class UpdateAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public class UpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerService _logger;
    private const string GitHubApiUrl = "https://api.github.com/repos/{owner}/{repo}/releases/latest";
    private const string Owner = "GreenXeMotion";
    private const string Repo = "Solus-Manifest-App";

    public UpdateService(IHttpClientFactory httpClientFactory, ILoggerService logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("Default");
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "SolusManifestApp");
        }
        return client;
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    public async Task<(bool hasUpdate, UpdateInfo? updateInfo)> CheckForUpdatesAsync()
    {
        try
        {
            var client = CreateClient();
            var url = GitHubApiUrl.Replace("{owner}", Owner).Replace("{repo}", Repo);
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning($"Update check failed: {response.StatusCode}");
                return (false, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json);

            if (updateInfo == null)
            {
                _logger.Warning("Failed to parse update info");
                return (false, null);
            }

            var currentVersion = GetCurrentVersion();
            var latestVersion = updateInfo.TagName.TrimStart('v');

            var hasUpdate = CompareVersions(currentVersion, latestVersion) < 0;

            if (hasUpdate)
            {
                _logger.Info($"Update available: {currentVersion} → {latestVersion}");
            }

            return (hasUpdate, updateInfo);
        }
        catch (Exception ex)
        {
            _logger.Error($"Update check error: {ex.Message}");
            return (false, null);
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null)
    {
        try
        {
            var exeAsset = updateInfo.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            if (exeAsset == null)
            {
                _logger.Error("No .exe file found in release assets");
                return false;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), "SolusManifestApp_Update");
            Directory.CreateDirectory(tempPath);

            var downloadPath = Path.Combine(tempPath, exeAsset.Name);
            _logger.Info($"Downloading update from: {exeAsset.BrowserDownloadUrl}");

            var client = CreateClient();
            using var response = await client.GetAsync(exeAsset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Download failed: {response.StatusCode}");
                return false;
            }

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            await using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            await using (var httpStream = await response.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        progress?.Report((double)downloadedBytes / totalBytes * 100);
                    }
                }
            }

            _logger.Info($"Downloaded update to: {downloadPath}");

            // Create batch file to replace executable
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExe))
            {
                _logger.Error("Failed to get current executable path");
                return false;
            }

            var batchFile = Path.Combine(tempPath, "update.bat");
            var batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
taskkill /IM SolusManifestApp.exe /F > nul 2>&1
timeout /t 1 /nobreak > nul
copy /Y ""{downloadPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""{batchFile}""
";

            File.WriteAllText(batchFile, batchContent);
            _logger.Info("Starting update process...");

            Process.Start(new ProcessStartInfo
            {
                FileName = batchFile,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Update installation failed: {ex.Message}");
            return false;
        }
    }

    private int CompareVersions(string current, string latest)
    {
        var currentParts = current.Split('.').Select(int.Parse).ToArray();
        var latestParts = latest.Split('.').Select(int.Parse).ToArray();

        int maxLength = Math.Max(currentParts.Length, latestParts.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var currentPart = i < currentParts.Length ? currentParts[i] : 0;
            var latestPart = i < latestParts.Length ? latestParts[i] : 0;

            if (currentPart < latestPart)
                return -1;
            if (currentPart > latestPart)
                return 1;
        }

        return 0;
    }
}
