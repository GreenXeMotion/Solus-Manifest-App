using Microsoft.UI.Xaml.Controls;
using SolusManifestApp.ViewModels;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class DownloadsPage : Page
{
    public DownloadsPageViewModel ViewModel { get; }

    public DownloadsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<DownloadsPageViewModel>();
        DataContext = ViewModel;
    }
}
