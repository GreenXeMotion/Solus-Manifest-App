using Microsoft.UI.Xaml.Controls;
using SolusManifestApp.ViewModels;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsPageViewModel ViewModel { get; }

    public ToolsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<ToolsPageViewModel>();
        DataContext = ViewModel;
    }
}
