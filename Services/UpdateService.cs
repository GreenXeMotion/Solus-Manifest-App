using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace SolusManifestApp.Services
{
    public class UpdateInfo
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("body")]
        public string Body { get; set; } = string.Empty;

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("assets")]
        public UpdateAsset[] Assets { get; set; } = Array.Empty<UpdateAsset>();
    }

    public class UpdateAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private const string GitHubApiUrl = "https://api.github.com/repos/{owner}/{repo}/releases/latest";
        private const string Owner = "MorrenusGames";
        private const string Repo = "Solus-Manifest-App";

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SolusManifestApp");
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
                var url = GitHubApiUrl.Replace("{owner}", Owner).Replace("{repo}", Repo);
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null);
                }

                var json = await response.Content.ReadAsStringAsync();
                var updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(json);

                if (updateInfo == null)
                {
                    return (false, null);
                }

                var currentVersion = GetCurrentVersion();
                var latestVersion = updateInfo.TagName.TrimStart('v');

                var hasUpdate = CompareVersions(currentVersion, latestVersion) < 0;

                return (hasUpdate, updateInfo);
            }
            catch
            {
                return (false, null);
            }
        }

        public async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null)
        {
            try
            {
                // Get current exe size to match the closest variant
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                var currentExeSize = !string.IsNullOrEmpty(currentExePath) && File.Exists(currentExePath)
                    ? new FileInfo(currentExePath).Length
                    : 0;

                // Find all SolusManifestApp zip variants
                var zipAssets = updateInfo.Assets
                    .Where(a => a.Name.StartsWith("SolusManifestApp-", StringComparison.OrdinalIgnoreCase)
                             && a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (zipAssets.Count == 0)
                {
                    return null;
                }

                // Pick the variant with size closest to current exe
                // singlefile.zip ≈ 9MB (framework-dependent)
                // full.zip ≈ 70MB (self-contained)
                // compressed.zip ≈ 72MB (self-contained compressed)
                UpdateAsset? selectedAsset;

                if (currentExeSize < 20_000_000) // Less than 20MB = singlefile
                {
                    selectedAsset = zipAssets.FirstOrDefault(a => a.Name.Contains("-singlefile.zip", StringComparison.OrdinalIgnoreCase));
                }
                else if (currentExeSize < 71_000_000) // 20-71MB = full
                {
                    selectedAsset = zipAssets.FirstOrDefault(a => a.Name.Contains("-full.zip", StringComparison.OrdinalIgnoreCase));
                }
                else // Over 71MB = compressed
                {
                    selectedAsset = zipAssets.FirstOrDefault(a => a.Name.Contains("-compressed.zip", StringComparison.OrdinalIgnoreCase));
                }

                // Fallback to singlefile if specific variant not found
                var zipAsset = selectedAsset ?? zipAssets.FirstOrDefault(a => a.Name.Contains("-singlefile.zip", StringComparison.OrdinalIgnoreCase));

                if (zipAsset == null)
                {
                    // Ultimate fallback - just pick the first zip
                    zipAsset = zipAssets.First();
                }

                var tempZipPath = Path.Combine(Path.GetTempPath(), "SolusManifestApp_Update.zip");
                var tempExtractPath = Path.Combine(Path.GetTempPath(), "SolusManifestApp_Update_Extract");

                // Download ZIP
                using (var response = await _httpClient.GetAsync(zipAsset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var downloadedBytes = 0L;

                    using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var contentStream = await response.Content.ReadAsStreamAsync();

                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0 && progress != null)
                        {
                            var progressPercent = (double)downloadedBytes / totalBytes * 100;
                            progress.Report(progressPercent);
                        }
                    }
                }

                // Extract ZIP
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
                Directory.CreateDirectory(tempExtractPath);

                System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

                // Find the exe in extracted files
                var exePath = Directory.GetFiles(tempExtractPath, "SolusManifestApp.exe", SearchOption.AllDirectories).FirstOrDefault();

                if (string.IsNullOrEmpty(exePath))
                {
                    return null;
                }

                // Move exe to final temp location
                var finalExePath = Path.Combine(Path.GetTempPath(), "SolusManifestApp_Update.exe");
                if (File.Exists(finalExePath))
                {
                    File.Delete(finalExePath);
                }
                File.Move(exePath, finalExePath);

                // Cleanup
                File.Delete(tempZipPath);
                Directory.Delete(tempExtractPath, true);

                return finalExePath;
            }
            catch
            {
                return null;
            }
        }

        public void InstallUpdate(string updatePath)
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath))
                    return;

                // Create a batch script to replace the exe after the app closes
                var batchPath = Path.Combine(Path.GetTempPath(), "update_solus.bat");
                var batchContent = $@"
@echo off
timeout /t 2 /nobreak > nul
del ""{currentExePath}""
move /y ""{updatePath}"" ""{currentExePath}""
start """" ""{currentExePath}""
del ""{batchPath}""
";

                File.WriteAllText(batchPath, batchContent);

                // Start the batch file and exit
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                System.Windows.Application.Current.Shutdown();
            }
            catch
            {
                // Failed to install update
            }
        }

        private int CompareVersions(string current, string latest)
        {
            try
            {
                var currentParts = current.Split('.').Select(int.Parse).ToArray();
                var latestParts = latest.Split('.').Select(int.Parse).ToArray();

                var maxLength = Math.Max(currentParts.Length, latestParts.Length);

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
            catch
            {
                return 0;
            }
        }
    }
}
