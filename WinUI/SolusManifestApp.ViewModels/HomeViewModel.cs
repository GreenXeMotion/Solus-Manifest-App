using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for the Home page
/// Displays quick actions and recent games
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Solus Manifest App";

    [RelayCommand]
    private void LaunchSteam()
    {
        // TODO: Implement Steam launch
    }

    [RelayCommand]
    private void RefreshLibrary()
    {
        // TODO: Implement library refresh
    }
}
