using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using SolusManifestApp.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Settings page - comprehensive settings management
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ISteamService _steamService;
    private readonly ILoggerService _logger;
    private readonly IManifestApiService _manifestApiService;

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string _statusMessage = "Settings loaded";

    // General Settings
    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private bool _apiKeyValid;

    [ObservableProperty]
    private string _apiKeyStatus = "Not validated";

    // Paths
    [ObservableProperty]
    private string _steamPath = string.Empty;

    [ObservableProperty]
    private string _downloadsPath = string.Empty;

    [ObservableProperty]
    private string _appListPath = string.Empty;

    [ObservableProperty]
    private string _dllInjectorPath = string.Empty;

    // Tool Mode
    [ObservableProperty]
    private ToolMode _selectedToolMode = ToolMode.SteamTools;

    [ObservableProperty]
    private GreenLumaMode _selectedGreenLumaMode = GreenLumaMode.Normal;

    // Theme
    [ObservableProperty]
    private string _selectedTheme = "Default";

    public ObservableCollection<string> AvailableThemes { get; } = new();
    public ObservableCollection<string> ToolModes { get; } = new();
    public ObservableCollection<string> GreenLumaModes { get; } = new();
    public ObservableCollection<string> AutoUpdateModes { get; } = new();

    // Download Settings
    [ObservableProperty]
    private bool _autoInstallAfterDownload;

    [ObservableProperty]
    private bool _deleteZipAfterInstall;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 8;

    [ObservableProperty]
    private bool _verifyFilesAfterDownload;

    // UI Settings
    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _showNotifications;

    [ObservableProperty]
    private bool _confirmBeforeDelete;

    [ObservableProperty]
    private bool _confirmBeforeUninstall;

    [ObservableProperty]
    private int _storePageSize = 20;

    [ObservableProperty]
    private int _libraryPageSize = 20;

    // Update Settings
    [ObservableProperty]
    private AutoUpdateMode _selectedAutoUpdateMode = AutoUpdateMode.CheckOnly;

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IDialogService dialogService,
        INotificationService notificationService,
        ISteamService steamService,
        ILoggerService logger,
        IManifestApiService manifestApiService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _steamService = steamService;
        _logger = logger;
        _manifestApiService = manifestApiService;

        InitializeCollections();
        _ = LoadSettingsAsync();
    }

    private void InitializeCollections()
    {
        // Load themes
        var themes = _themeService.GetAvailableThemes();
        foreach (var theme in themes)
        {
            AvailableThemes.Add(theme);
        }

        // Load enum values
        foreach (var mode in Enum.GetNames(typeof(ToolMode)))
        {
            ToolModes.Add(mode);
        }

        foreach (var mode in Enum.GetNames(typeof(GreenLumaMode)))
        {
            GreenLumaModes.Add(mode);
        }

        foreach (var mode in Enum.GetNames(typeof(AutoUpdateMode)))
        {
            AutoUpdateModes.Add(mode);
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            Settings = await _settingsService.LoadSettingsAsync<AppSettings>() ?? new AppSettings();

            // Map settings to properties
            ApiKey = Settings.ApiKey;
            SteamPath = Settings.SteamPath;
            DownloadsPath = Settings.DownloadsPath;
            AppListPath = Settings.AppListPath;
            DllInjectorPath = Settings.DLLInjectorPath;

            SelectedToolMode = Settings.Mode;
            SelectedGreenLumaMode = Settings.GreenLumaSubMode;
            SelectedTheme = Settings.Theme.ToString();
            SelectedAutoUpdateMode = Settings.AutoUpdate;

            AutoInstallAfterDownload = Settings.AutoInstallAfterDownload;
            DeleteZipAfterInstall = Settings.DeleteZipAfterInstall;
            MaxConcurrentDownloads = Settings.MaxConcurrentDownloads;
            VerifyFilesAfterDownload = Settings.VerifyFilesAfterDownload;

            MinimizeToTray = Settings.MinimizeToTray;
            StartMinimized = Settings.StartMinimized;
            ShowNotifications = Settings.ShowNotifications;
            ConfirmBeforeDelete = Settings.ConfirmBeforeDelete;
            ConfirmBeforeUninstall = Settings.ConfirmBeforeUninstall;

            StorePageSize = Settings.StorePageSize;
            LibraryPageSize = Settings.LibraryPageSize;

            HasUnsavedChanges = false;
            StatusMessage = "Settings loaded successfully";

            // Validate API key if present
            if (!string.IsNullOrEmpty(ApiKey))
            {
                _ = ValidateApiKeyAsync();
            }

            _logger.Info("Settings loaded in SettingsViewModel");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load settings: {ex.Message}");
            _notificationService.ShowError($"Failed to load settings: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            // Map properties back to settings
            Settings.ApiKey = ApiKey;
            Settings.SteamPath = SteamPath;
            Settings.DownloadsPath = DownloadsPath;
            Settings.AppListPath = AppListPath;
            Settings.DLLInjectorPath = DllInjectorPath;

            Settings.Mode = SelectedToolMode;
            Settings.GreenLumaSubMode = SelectedGreenLumaMode;

            if (Enum.TryParse<AppTheme>(SelectedTheme, out var theme))
            {
                Settings.Theme = theme;
            }

            Settings.AutoUpdate = SelectedAutoUpdateMode;

            Settings.AutoInstallAfterDownload = AutoInstallAfterDownload;
            Settings.DeleteZipAfterInstall = DeleteZipAfterInstall;
            Settings.MaxConcurrentDownloads = MaxConcurrentDownloads;
            Settings.VerifyFilesAfterDownload = VerifyFilesAfterDownload;

            Settings.MinimizeToTray = MinimizeToTray;
            Settings.StartMinimized = StartMinimized;
            Settings.ShowNotifications = ShowNotifications;
            Settings.ConfirmBeforeDelete = ConfirmBeforeDelete;
            Settings.ConfirmBeforeUninstall = ConfirmBeforeUninstall;

            Settings.StorePageSize = StorePageSize;
            Settings.LibraryPageSize = LibraryPageSize;

            await _settingsService.SaveSettingsAsync(Settings);

            HasUnsavedChanges = false;
            StatusMessage = "Settings saved successfully";
            _notificationService.ShowSuccess("Settings saved successfully", "Success");
            _logger.Info("Settings saved");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save settings: {ex.Message}");
            await _dialogService.ShowMessageAsync($"Failed to save settings: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Are you sure you want to reset all settings to defaults?",
            "Reset Settings");

        if (confirmed)
        {
            Settings = new AppSettings();
            await LoadSettingsAsync();
            HasUnsavedChanges = true;
            _notificationService.ShowInfo("Settings reset to defaults", "Reset");
            _logger.Info("Settings reset to defaults");
        }
    }

    [RelayCommand]
    private async Task ValidateApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKeyValid = false;
            ApiKeyStatus = "API key is empty";
            return;
        }

        ApiKeyStatus = "Validating...";

        try
        {
            var isValid = await _manifestApiService.TestApiKeyAsync(ApiKey);
            ApiKeyValid = isValid;
            ApiKeyStatus = isValid ? "✓ Valid" : "✗ Invalid";

            if (isValid)
            {
                _notificationService.ShowSuccess("API key is valid", "Success");
            }
            else
            {
                _notificationService.ShowWarning("API key validation failed", "Invalid");
            }

            _logger.Info($"API key validation: {(isValid ? "success" : "failed")}");
        }
        catch (Exception ex)
        {
            ApiKeyValid = false;
            ApiKeyStatus = $"Error: {ex.Message}";
            _logger.Error($"API key validation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DetectSteamPath()
    {
        try
        {
            var detectedPath = _steamService.GetSteamPath();
            if (!string.IsNullOrEmpty(detectedPath))
            {
                SteamPath = detectedPath;
                HasUnsavedChanges = true;
                _notificationService.ShowSuccess($"Steam detected at: {detectedPath}", "Success");
                _logger.Info($"Steam path detected: {detectedPath}");
            }
            else
            {
                _notificationService.ShowWarning("Steam installation not found", "Not Found");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to detect Steam path: {ex.Message}");
            _notificationService.ShowError($"Failed to detect Steam: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private async Task BrowseSteamPathAsync()
    {
        _notificationService.ShowInfo("Folder picker will be implemented", "Coming Soon");
        // TODO: Implement folder picker dialog
    }

    [RelayCommand]
    private async Task BrowseDownloadsPathAsync()
    {
        _notificationService.ShowInfo("Folder picker will be implemented", "Coming Soon");
        // TODO: Implement folder picker dialog
    }

    [RelayCommand]
    private async Task BrowseAppListPathAsync()
    {
        _notificationService.ShowInfo("File picker will be implemented", "Coming Soon");
        // TODO: Implement file picker dialog
    }

    [RelayCommand]
    private async Task BrowseDllInjectorPathAsync()
    {
        _notificationService.ShowInfo("File picker will be implemented", "Coming Soon");
        // TODO: Implement file picker dialog
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SolusManifestApp");

            if (Directory.Exists(settingsPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = settingsPath,
                    UseShellExecute = true
                });
            }
            else
            {
                _notificationService.ShowWarning("Settings folder not found", "Warning");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open settings folder: {ex.Message}");
            _notificationService.ShowError($"Failed to open folder: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            var logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SolusManifestApp",
                "logs");

            if (Directory.Exists(logsPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logsPath,
                    UseShellExecute = true
                });
            }
            else
            {
                _notificationService.ShowWarning("Logs folder not found", "Warning");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open logs folder: {ex.Message}");
            _notificationService.ShowError($"Failed to open folder: {ex.Message}", "Error");
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _themeService.ApplyTheme(value);
            HasUnsavedChanges = true;
            _logger.Info($"Theme changed to: {value}");
        }
    }

    partial void OnApiKeyChanged(string value)
    {
        HasUnsavedChanges = true;
        ApiKeyValid = false;
        ApiKeyStatus = "Not validated";
    }

    partial void OnSteamPathChanged(string value) => HasUnsavedChanges = true;
    partial void OnDownloadsPathChanged(string value) => HasUnsavedChanges = true;
    partial void OnAppListPathChanged(string value) => HasUnsavedChanges = true;
    partial void OnDllInjectorPathChanged(string value) => HasUnsavedChanges = true;
    partial void OnSelectedToolModeChanged(ToolMode value) => HasUnsavedChanges = true;
    partial void OnSelectedGreenLumaModeChanged(GreenLumaMode value) => HasUnsavedChanges = true;
    partial void OnSelectedAutoUpdateModeChanged(AutoUpdateMode value) => HasUnsavedChanges = true;
    partial void OnAutoInstallAfterDownloadChanged(bool value) => HasUnsavedChanges = true;
    partial void OnDeleteZipAfterInstallChanged(bool value) => HasUnsavedChanges = true;
    partial void OnMaxConcurrentDownloadsChanged(int value) => HasUnsavedChanges = true;
    partial void OnVerifyFilesAfterDownloadChanged(bool value) => HasUnsavedChanges = true;
    partial void OnMinimizeToTrayChanged(bool value) => HasUnsavedChanges = true;
    partial void OnStartMinimizedChanged(bool value) => HasUnsavedChanges = true;
    partial void OnShowNotificationsChanged(bool value) => HasUnsavedChanges = true;
    partial void OnConfirmBeforeDeleteChanged(bool value) => HasUnsavedChanges = true;
    partial void OnConfirmBeforeUninstallChanged(bool value) => HasUnsavedChanges = true;
    partial void OnStorePageSizeChanged(int value) => HasUnsavedChanges = true;
    partial void OnLibraryPageSizeChanged(int value) => HasUnsavedChanges = true;
}
