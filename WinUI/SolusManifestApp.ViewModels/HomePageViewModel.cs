using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Home Page
/// </summary>
public partial class HomePageViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ISteamService _steamService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _showLaunchSteam;

    [ObservableProperty]
    private bool _showLaunchGreenLuma;

    [ObservableProperty]
    private string _currentModeText = string.Empty;

    [ObservableProperty]
    private string _currentModeDescription = string.Empty;

    [ObservableProperty]
    private string _appVersion = "3.0.0 (WinUI 3 Migration - Phase 4 In Progress)";

    public HomePageViewModel(
        ISettingsService settingsService,
        ISteamService steamService,
        INotificationService notificationService)
    {
        _settingsService = settingsService;
        _steamService = steamService;
        _notificationService = notificationService;

        _ = RefreshModeAsync();
    }

    public async Task RefreshModeAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync<AppSettings>();
        if (settings == null) return;

        ShowLaunchSteam = settings.Mode == ToolMode.SteamTools ||
                          (settings.Mode == ToolMode.GreenLuma && settings.GreenLumaSubMode == GreenLumaMode.StealthUser32);
        ShowLaunchGreenLuma = settings.Mode == ToolMode.GreenLuma &&
                              (settings.GreenLumaSubMode == GreenLumaMode.Normal || settings.GreenLumaSubMode == GreenLumaMode.StealthAnyFolder);

        CurrentModeText = settings.Mode switch
        {
            ToolMode.SteamTools => "Current Mode: SteamTools",
            ToolMode.GreenLuma => settings.GreenLumaSubMode switch
            {
                GreenLumaMode.Normal => "Current Mode: GreenLuma (Normal)",
                GreenLumaMode.StealthAnyFolder => "Current Mode: GreenLuma (Stealth - Any Folder)",
                GreenLumaMode.StealthUser32 => "Current Mode: GreenLuma (Stealth)",
                _ => "Current Mode: GreenLuma"
            },
            ToolMode.DepotDownloader => "Current Mode: DepotDownloader",
            _ => "Current Mode: Unknown"
        };

        CurrentModeDescription = settings.Mode switch
        {
            ToolMode.SteamTools => "SteamTools mode: Standard download mode with .lua files installed to stplug-in folder.",
            ToolMode.GreenLuma when settings.GreenLumaSubMode == GreenLumaMode.Normal =>
                "GreenLuma (Normal): DLLInjector at {SteamPath}/DLLInjector.exe, AppList at {SteamPath}/AppList.",
            ToolMode.GreenLuma when settings.GreenLumaSubMode == GreenLumaMode.StealthAnyFolder =>
                "GreenLuma (Stealth - Any Folder): DLLInjector in custom location for security.",
            ToolMode.GreenLuma when settings.GreenLumaSubMode == GreenLumaMode.StealthUser32 =>
                "GreenLuma (Stealth): Launches Steam normally with AppList entries and depot keys in config.vdf.",
            ToolMode.DepotDownloader => "DepotDownloader mode: Direct depot downloads from Steam CDN.",
            _ => "No mode selected. Please configure your tool mode in Settings."
        };
    }

    [RelayCommand]
    private void LaunchSteam()
    {
        try
        {
            var steamPath = _steamService.GetSteamPath();

            if (string.IsNullOrEmpty(steamPath))
            {
                _notificationService.ShowError("Steam path is not configured. Please set it in Settings.");
                return;
            }

            var steamExePath = Path.Combine(steamPath, "steam.exe");

            if (!File.Exists(steamExePath))
            {
                _notificationService.ShowError($"Steam.exe not found at: {steamExePath}");
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = steamExePath,
                UseShellExecute = true,
                WorkingDirectory = steamPath
            });

            _notificationService.ShowSuccess("Steam launched successfully!");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to launch Steam: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LaunchGreenLumaAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<AppSettings>();
            if (settings == null) return;

            if (string.IsNullOrEmpty(settings.DLLInjectorPath))
            {
                _notificationService.ShowError("DLLInjector path is not set. Please configure it in Settings.");
                return;
            }

            if (!File.Exists(settings.DLLInjectorPath))
            {
                _notificationService.ShowError($"DLLInjector.exe not found at: {settings.DLLInjectorPath}");
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = settings.DLLInjectorPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(settings.DLLInjectorPath)
            });

            _notificationService.ShowSuccess("GreenLuma launched successfully!");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to launch GreenLuma: {ex.Message}");
        }
    }
}
