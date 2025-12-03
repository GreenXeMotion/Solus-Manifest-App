using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
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

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Load games when navigating to this page
        if (!ViewModel.IsLoading && ViewModel.Games.Count == 0)
        {
            await ViewModel.LoadGamesCommand.ExecuteAsync(null);
        }
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.SearchCommand.ExecuteAsync(null);
        }
    }

    private async void SearchBox_EnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await ViewModel.SearchCommand.ExecuteAsync(null);
        args.Handled = true;
    }

    private void GameItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Core.Models.LibraryGame game)
        {
            ViewModel.ViewGameDetailsCommand.Execute(game);
        }
    }

    private async void PageNumber_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(args.NewValue) && args.NewValue >= 1 && args.NewValue <= ViewModel.TotalPages)
        {
            await ViewModel.GoToPageCommand.ExecuteAsync((int)args.NewValue);
        }
    }
}
