using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SolusManifestApp.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Linq;
using System;

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