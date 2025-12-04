using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using SolusManifestApp.ViewModels;
using System.ComponentModel;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class StorePage : Page
{
    public StorePageViewModel ViewModel { get; }

    public StorePage()
    {
        InitializeComponent();
        ViewModel = App.GetService<StorePageViewModel>();
        DataContext = ViewModel;

        // Subscribe to IsListView changes to update layout
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsListView))
        {
            UpdateViewLayout();
        }
    }

    private void UpdateViewLayout()
    {
        // Update the icon to show current view mode
        if (ViewIcon != null)
        {
            // If in list view, show grid icon (to switch back to grid)
            // If in grid view, show list icon (to switch to list)
            ViewIcon.Glyph = ViewModel.IsListView ? "\uE8A9" : "\uE8FD"; // Grid icon : List icon
        }

        // Switch between grid and list templates
        if (GamesItemsControl != null)
        {
            if (ViewModel.IsListView)
            {
                // Switch to list view
                GamesItemsControl.ItemTemplate = (DataTemplate)Resources["ListViewTemplate"];
                GamesItemsControl.ItemsPanel = (ItemsPanelTemplate)Resources["ListPanelTemplate"];
            }
            else
            {
                // Switch to grid view
                GamesItemsControl.ItemTemplate = (DataTemplate)Resources["GridViewTemplate"];
                GamesItemsControl.ItemsPanel = (ItemsPanelTemplate)Resources["GridPanelTemplate"];
            }
        }
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
