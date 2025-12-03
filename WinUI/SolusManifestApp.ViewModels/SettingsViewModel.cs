using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Settings page
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private string[] _availableThemes = Array.Empty<string>();

    [ObservableProperty]
    private string _selectedTheme = string.Empty;

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IDialogService dialogService,
        INotificationService notificationService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _dialogService = dialogService;
        _notificationService = notificationService;

        Settings = _settingsService.GetSettings<AppSettings>();
        AvailableThemes = _themeService.GetAvailableThemes();
        SelectedTheme = Settings.Theme.ToString();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(Settings);
            _notificationService.ShowSuccess("Settings saved successfully");
        }
        catch (Exception ex)
        {
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
            SelectedTheme = Settings.Theme.ToString();
            _notificationService.ShowInfo("Settings reset to defaults");
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (Enum.TryParse<AppTheme>(value, out var theme))
        {
            Settings.Theme = theme;
            _themeService.ApplyTheme(value);
        }
    }
}
