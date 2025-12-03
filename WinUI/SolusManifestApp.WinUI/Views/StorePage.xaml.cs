using Microsoft.UI.Xaml.Controls;
using SolusManifestApp.ViewModels;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class StorePage : Page
{
    public StorePageViewModel ViewModel { get; }

    public StorePage()
    {
        InitializeComponent();
        ViewModel = App.GetService<StorePageViewModel>();
        DataContext = ViewModel;
    }
}
