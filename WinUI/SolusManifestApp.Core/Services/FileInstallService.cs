using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

public class FileInstallService
{
    private readonly ISteamService _steamService;
    private readonly ILoggerService _logger;

    public FileInstallService(ISteamService steamService, ILoggerService logger)
    {
        _steamService = steamService;
        _logger = logger;
    }

    public async Task<bool> InstallManifestAsync(string manifestPath, string depotId, string manifestId)
    {
        try
        {
            var steamPath = _steamService.GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                _logger.Error("Steam path not found");
                return false;
            }

            var depotCachePath = Path.Combine(steamPath, "depotcache");
            Directory.CreateDirectory(depotCachePath);

            var targetFileName = $"{depotId}_{manifestId}.manifest";
            var targetPath = Path.Combine(depotCachePath, targetFileName);

            await Task.Run(() => File.Copy(manifestPath, targetPath, overwrite: true));

            _logger.Info($"Installed manifest: {targetFileName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install manifest: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> InstallFromZipAsync(string zipPath, Action<string>? progressCallback = null)
    {
        try
        {
            progressCallback?.Invoke("Extracting ZIP file...");

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, tempDir));

                progressCallback?.Invoke("Installing files...");

                var manifestFiles = Directory.GetFiles(tempDir, "*.manifest", SearchOption.AllDirectories);

                if (manifestFiles.Length == 0)
                {
                    throw new Exception("No .manifest files found in ZIP");
                }

                var steamPath = _steamService.GetSteamPath();
                if (string.IsNullOrEmpty(steamPath))
                {
                    throw new Exception("Steam path not found");
                }

                var depotCachePath = Path.Combine(steamPath, "depotcache");
                Directory.CreateDirectory(depotCachePath);

                foreach (var manifestFile in manifestFiles)
                {
                    var fileName = Path.GetFileName(manifestFile);
                    var targetPath = Path.Combine(depotCachePath, fileName);
                    File.Copy(manifestFile, targetPath, overwrite: true);
                    _logger.Info($"Installed: {fileName}");
                }

                progressCallback?.Invoke($"Installed {manifestFiles.Length} manifest files");
                return true;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install from ZIP: {ex.Message}");
            progressCallback?.Invoke($"Error: {ex.Message}");
            return false;
        }
    }

    public bool CreateFakeAppManifest(string appId, string gameName, string installDir, string libraryPath)
    {
        try
        {
            var steamappsPath = Path.Combine(libraryPath, "steamapps");
            Directory.CreateDirectory(steamappsPath);

            var manifestPath = Path.Combine(steamappsPath, $"appmanifest_{appId}.acf");

            var content = $@"""AppState""
{{
    ""appid""        ""{appId}""
    ""Universe""     ""1""
    ""name""         ""{gameName}""
    ""StateFlags""   ""4""
    ""installdir""   ""{installDir}""
    ""LastUpdated""  ""{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}""
    ""UpdateResult"" ""0""
    ""SizeOnDisk""   ""0""
    ""buildid""      ""0""
    ""LastOwner""    ""0""
    ""BytesToDownload"" ""0""
    ""BytesDownloaded"" ""0""
    ""AutoUpdateBehavior"" ""0""
    ""AllowOtherDownloadsWhileRunning"" ""0""
}}";

            File.WriteAllText(manifestPath, content);
            _logger.Info($"Created fake app manifest for {appId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create fake manifest: {ex.Message}");
            return false;
        }
    }

    public bool DeleteAppManifest(string appId, string libraryPath)
    {
        try
        {
            var manifestPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{appId}.acf");

            if (File.Exists(manifestPath))
            {
                File.Delete(manifestPath);
                _logger.Info($"Deleted app manifest for {appId}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete manifest: {ex.Message}");
            return false;
        }
    }
}
