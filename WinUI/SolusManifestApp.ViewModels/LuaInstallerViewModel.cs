using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using SolusManifestApp.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Lua Installer Page - handles file installation
/// </summary>
public partial class LuaInstallerViewModel : ObservableObject
{
    private readonly FileInstallService _fileInstallService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ISteamService _steamService;
    private readonly ILoggerService _logger;
    private readonly ProfileService _profileService;

    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private string _selectedFileName = "No file selected";

    [ObservableProperty]
    private bool _hasFileSelected = false;

    [ObservableProperty]
    private bool _isInstalling = false;

    [ObservableProperty]
    private string _statusMessage = "Drop a .zip, .lua, or .manifest file here to install";

    [ObservableProperty]
    private bool _isGreenLumaMode;

    [ObservableProperty]
    private List<GreenLumaProfile> _profiles = new();

    [ObservableProperty]
    private GreenLumaProfile? _selectedProfile;

    [ObservableProperty]
    private List<string> _selectedFiles = new();

    public LuaInstallerViewModel(
        FileInstallService fileInstallService,
        INotificationService notificationService,
        ISettingsService settingsService,
        ISteamService steamService,
        ILoggerService logger,
        ProfileService profileService)
    {
        _fileInstallService = fileInstallService;
        _notificationService = notificationService;
        _settingsService = settingsService;
        _steamService = steamService;
        _logger = logger;
        _profileService = profileService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync<AppSettings>();
            if (settings != null)
            {
                IsGreenLumaMode = settings.Mode == ToolMode.GreenLuma;
            }
            LoadProfiles();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize LuaInstallerViewModel: {ex.Message}");
        }
    }

    public void RefreshMode()
    {
        _ = Task.Run(async () =>
        {
            var settings = await _settingsService.LoadSettingsAsync<AppSettings>();
            if (settings != null)
            {
                IsGreenLumaMode = settings.Mode == ToolMode.GreenLuma;
            }
            LoadProfiles();
        });
    }

    private void LoadProfiles()
    {
        try
        {
            var profileData = _profileService.LoadProfiles();
            Profiles = profileData.Profiles.ToList();

            var activeProfileId = profileData.ActiveProfileId;
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == activeProfileId) ?? Profiles.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load profiles: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ProcessDroppedFiles(string[] files)
    {
        if (files == null || files.Length == 0)
            return;

        var validFiles = files.Where(f =>
            f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)).ToList();

        if (validFiles.Count == 0)
        {
            _notificationService.ShowError("Please drop a .zip, .lua, or .manifest file", "Invalid File");
            return;
        }

        if (validFiles.Count > 1)
        {
            SelectedFiles = validFiles;
            SelectedFilePath = string.Join(";", validFiles);
            SelectedFileName = $"{validFiles.Count} files selected";
            HasFileSelected = true;

            var luaCount = validFiles.Count(f => f.EndsWith(".lua", StringComparison.OrdinalIgnoreCase));
            var zipCount = validFiles.Count(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            var manifestCount = validFiles.Count(f => f.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase));

            var parts = new List<string>();
            if (zipCount > 0) parts.Add($"{zipCount} zip(s)");
            if (luaCount > 0) parts.Add($"{luaCount} lua(s)");
            if (manifestCount > 0) parts.Add($"{manifestCount} manifest(s)");

            StatusMessage = $"Ready to install: {string.Join(", ", parts)}";
        }
        else
        {
            var file = validFiles.First();
            SelectedFiles = new List<string> { file };
            SelectedFilePath = file;
            SelectedFileName = Path.GetFileName(file);
            HasFileSelected = true;

            if (file.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Ready to install manifest: {SelectedFileName}";
            }
            else if (file.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Ready to install Lua file: {SelectedFileName}";
            }
            else
            {
                StatusMessage = $"Ready to install: {SelectedFileName}";
            }
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        // File picker logic is handled in code-behind (Page.xaml.cs)
        // This command just exists for binding purposes
    }

    [RelayCommand]
    private async Task InstallFile()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            _notificationService.ShowError("Please select a valid file first", "No File Selected");
            return;
        }

        IsInstalling = true;
        StatusMessage = $"Installing {SelectedFileName}...";

        try
        {
            if (SelectedFiles.Count > 1)
            {
                await InstallMultipleFilesAsync();
            }
            else if (SelectedFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _notificationService.ShowInfo("Full .zip installation with depot selection coming soon. Basic extraction will be performed.", "Feature In Development");
                await _fileInstallService.InstallFromZipAsync(SelectedFilePath, msg => StatusMessage = msg);
                _notificationService.ShowSuccess($"{SelectedFileName} installed successfully!\n\nRestart Steam for changes to take effect.", "Success");
            }
            else if (SelectedFilePath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
                     SelectedFilePath.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
            {
                // For .lua and .manifest files, show not implemented message
                _notificationService.ShowInfo("Direct .lua and .manifest installation coming soon. Please use .zip archives for now.", "Feature In Development");
            }

            StatusMessage = "Installation complete! Restart Steam for changes to take effect.";

            // Clear selection
            SelectedFilePath = string.Empty;
            SelectedFileName = "No file selected";
            SelectedFiles.Clear();
            HasFileSelected = false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Installation failed: {ex.Message}");
            _notificationService.ShowError($"Installation failed: {ex.Message}", "Error");
            StatusMessage = $"Installation failed: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private async Task InstallMultipleFilesAsync()
    {
        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < SelectedFiles.Count; i++)
        {
            var file = SelectedFiles[i];
            if (!File.Exists(file)) continue;

            StatusMessage = $"Installing {i + 1}/{SelectedFiles.Count}: {Path.GetFileName(file)}...";

            try
            {
                if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    await _fileInstallService.InstallFromZipAsync(file, msg => StatusMessage = msg);
                    successCount++;
                }
                else
                {
                    // Skip .lua and .manifest files for now
                    _logger.Info($"Skipping {Path.GetFileName(file)} - only .zip files supported");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to install {Path.GetFileName(file)}: {ex.Message}");
                failCount++;
            }
        }

        if (failCount == 0)
        {
            _notificationService.ShowSuccess($"All {successCount} files installed successfully!\n\nRestart Steam for changes to take effect.", "Success");
            StatusMessage = "Installation complete! Restart Steam for changes to take effect.";
        }
        else
        {
            _notificationService.ShowWarning($"Installed {successCount} files, {failCount} failed.\n\nRestart Steam for changes to take effect.", "Partial Success");
            StatusMessage = $"Partial installation: {successCount} succeeded, {failCount} failed";
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedFilePath = string.Empty;
        SelectedFileName = "No file selected";
        SelectedFiles.Clear();
        HasFileSelected = false;
        StatusMessage = "Drop a .zip, .lua, or .manifest file here to install";
    }
}
