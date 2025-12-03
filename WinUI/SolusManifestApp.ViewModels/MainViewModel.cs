using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// Handles navigation and top-level UI state
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _title = "Solus Manifest App";

    [ObservableProperty]
    private string _currentPageTitle = "Home";

    public MainViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [RelayCommand]
    private void NavigateHome()
    {
        CurrentPageTitle = "Home";
        // TODO: Implement navigation
    }

    [RelayCommand]
    private void NavigateStore()
    {
        CurrentPageTitle = "Store";
        // TODO: Implement navigation
    }

    [RelayCommand]
    private void NavigateLibrary()
    {
        CurrentPageTitle = "Library";
        // TODO: Implement navigation
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        CurrentPageTitle = "Settings";
        // TODO: Implement navigation
    }
}
