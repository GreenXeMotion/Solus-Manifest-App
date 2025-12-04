using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SolusManifestApp.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Linq;
using System;
using System.ComponentModel;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class LibraryPage : Page
{
    public LibraryPageViewModel ViewModel { get; }

    public LibraryPage()
    {
        try
        {
            InitializeComponent();
            ViewModel = App.GetService<LibraryPageViewModel>();

            // Subscribe to IsListView changes to update layout
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LibraryPage initialization error: {ex}");
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LibraryPage_Error.txt"),
                $"Exception: {ex}\r\n\r\nStackTrace: {ex.StackTrace}\r\n\r\nInnerException: {ex.InnerException}"
            );
            throw;
        }
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
        try
        {
            base.OnNavigatedTo(e);

            // Refresh library when navigating to this page
            if (!ViewModel.IsLoading)
            {
                await ViewModel.RefreshLibraryCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LibraryPage navigation error: {ex}");
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to load library: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void Page_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Drop .lua or .zip files to install";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void Page_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var files = items.OfType<StorageFile>()
                .Where(f => f.FileType.Equals(".lua", System.StringComparison.OrdinalIgnoreCase) ||
                           f.FileType.Equals(".zip", System.StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .ToList();

            if (files.Any())
            {
                // TODO: Add file installation logic when available in ViewModel
                // For now, show a notification
                var dialog = new ContentDialog
                {
                    Title = "Drag & Drop",
                    Content = $"Dropped {files.Count} file(s). Installation feature coming soon!",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}