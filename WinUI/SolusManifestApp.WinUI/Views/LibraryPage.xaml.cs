using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SolusManifestApp.ViewModels;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class LibraryPage : Page
{
    public LibraryPageViewModel ViewModel { get; }

    public LibraryPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<LibraryPageViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Refresh library when navigating to this page
        if (!ViewModel.IsLoading)
        {            await ViewModel.RefreshLibraryCommand.ExecuteAsync(null);
        }
    }

    private void GameItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Core.Models.LibraryItem item)
        {
            ViewModel.LaunchGameCommand.Execute(item);
        }
    }
}
