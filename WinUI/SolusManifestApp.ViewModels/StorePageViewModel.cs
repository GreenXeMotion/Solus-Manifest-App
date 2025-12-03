using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Store Page - demonstrates service integration
/// Full implementation will be added incrementally in Phase 4
/// </summary>
public partial class StorePageViewModel : ObservableObject
{
    private readonly IManifestApiService _manifestApiService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<string> _statusItems = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Store page - Service integration working";

    [ObservableProperty]
    private string _apiKeyStatus = "Checking...";

    public StorePageViewModel(
        IManifestApiService manifestApiService,
        INotificationService notificationService,
        ISettingsService settingsService)
    {
        _manifestApiService = manifestApiService;
        _notificationService = notificationService;
        _settingsService = settingsService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;

        var settings = await _settingsService.LoadSettingsAsync<AppSettings>();

        StatusItems.Clear();
        StatusItems.Add("✓ ManifestApiService: Injected");
        StatusItems.Add("✓ SettingsService: Injected");
        StatusItems.Add("✓ NotificationService: Injected");

        if (!string.IsNullOrEmpty(settings?.ApiKey))
        {
            ApiKeyStatus = "API Key: Configured ✓";
            StatusMessage = $"Connected to Morrenus API - Ready to browse games";
            StatusItems.Add($"✓ API Key: {settings.ApiKey.Substring(0, 8)}...");
        }
        else
        {
            ApiKeyStatus = "API Key: Not configured";
            StatusMessage = "Please configure API key in Settings to browse games";
            StatusItems.Add("⚠ API Key: Not configured");
        }

        IsLoading = false;
    }

    [RelayCommand]
    private void TestNotification()
    {
        _notificationService.ShowSuccess("Store page services are working correctly!", "Success");
    }

    [RelayCommand]
    private async Task RefreshStatus()
    {
        await InitializeAsync();
        _notificationService.ShowInfo("Status refreshed", "Info");
    }
}
