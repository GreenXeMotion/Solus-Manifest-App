using Microsoft.Win32;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;

namespace SolusManifestApp.Core.Services;

/// <summary>
/// Steam service for path detection and management
/// Framework-agnostic - works with both WPF and WinUI 3
/// </summary>
public class SteamService : ISteamService
{
    private string? _cachedSteamPath;
    private readonly ISettingsService _settingsService;

    public SteamService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public string? GetSteamPath()
    {
        if (!string.IsNullOrEmpty(_cachedSteamPath))
            return _cachedSteamPath;

        // Try registry first (64-bit)
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (key != null)
            {
                var installPath = key.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    _cachedSteamPath = installPath;
                    return installPath;
                }
            }
        }
        catch
        {
            // Continue to next method
        }

        // Try registry (32-bit)
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key != null)
            {
                var installPath = key.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    _cachedSteamPath = installPath;
                    return installPath;
                }
            }
        }
        catch
        {
            // Continue to fallback
        }

        // Fallback to common locations
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
            {
                _cachedSteamPath = path;
                return path;
            }
        }

        return null;
    }

    public bool IsSteamInstalled()
    {
        return !string.IsNullOrEmpty(GetSteamPath());
    }

    public bool IsSteamRunning()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("steam");
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestartSteamAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Kill Steam
                var processes = System.Diagnostics.Process.GetProcessesByName("steam");
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }

                Thread.Sleep(2000);

                // Get settings
                var settings = _settingsService.GetSettings<AppSettings>();
                var steamPath = GetSteamPath();

                if (string.IsNullOrEmpty(steamPath))
                {
                    return false;
                }

                var steamExe = Path.Combine(steamPath, "steam.exe");
                if (!File.Exists(steamExe))
                {
                    return false;
                }

                // Check if GreenLuma mode is enabled (Normal or StealthAnyFolder)
                bool isGreenLumaMode = settings.Mode == ToolMode.GreenLuma &&
                                      (settings.GreenLumaSubMode == GreenLumaMode.Normal ||
                                       settings.GreenLumaSubMode == GreenLumaMode.StealthAnyFolder);

                if (isGreenLumaMode && !string.IsNullOrEmpty(settings.DLLInjectorPath))
                {
                    // Use DLL Injector to start Steam
                    if (File.Exists(settings.DLLInjectorPath))
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = settings.DLLInjectorPath,
                            WorkingDirectory = Path.GetDirectoryName(settings.DLLInjectorPath),
                            UseShellExecute = false
                        };
                        System.Diagnostics.Process.Start(startInfo);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // Start Steam normally
                    System.Diagnostics.Process.Start(steamExe);
                }

                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public string? FindSteamExecutable()
    {
        var steamPath = GetSteamPath();
        if (string.IsNullOrEmpty(steamPath))
            return null;

        var steamExe = Path.Combine(steamPath, "steam.exe");
        return File.Exists(steamExe) ? steamExe : null;
    }

    public string? GetStPluginPath()
    {
        var steamPath = GetSteamPath();
        if (string.IsNullOrEmpty(steamPath))
            return null;

        return Path.Combine(steamPath, "config", "stplug-in");
    }

    public bool EnsureStPluginDirectory()
    {
        var stpluginPath = GetStPluginPath();
        if (string.IsNullOrEmpty(stpluginPath))
            return false;

        try
        {
            if (!Directory.Exists(stpluginPath))
            {
                Directory.CreateDirectory(stpluginPath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateSteamPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return false;

        return File.Exists(Path.Combine(path, "steam.exe"));
    }

    public void SetCustomSteamPath(string path)
    {
        if (ValidateSteamPath(path))
        {
            _cachedSteamPath = path;
        }
    }
}
