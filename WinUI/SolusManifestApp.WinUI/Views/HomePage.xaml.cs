using Microsoft.UI.Xaml.Controls;
using SolusManifestApp.ViewModels;

namespace SolusManifestApp.WinUI.Views;

/// <summary>
/// Home page showing welcome message and quick actions
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePageViewModel ViewModel { get; }

    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HomePageViewModel>();
        DataContext = ViewModel;
    }
}
