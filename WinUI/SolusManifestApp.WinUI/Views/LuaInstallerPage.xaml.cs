using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SolusManifestApp.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class LuaInstallerPage : Page
{
    public LuaInstallerViewModel ViewModel { get; }
    private SolidColorBrush? _originalBorderBrush;
    private Thickness _originalBorderThickness;

    public LuaInstallerPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<LuaInstallerViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshMode();
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        await BrowseFilesAsync();
    }

    private async System.Threading.Tasks.Task BrowseFilesAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".zip");
            picker.FileTypeFilter.Add(".lua");
            picker.FileTypeFilter.Add(".manifest");

            // Get the window handle
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                var filePaths = files.Select(f => f.Path).ToArray();
                if (ViewModel.ProcessDroppedFilesCommand.CanExecute(filePaths))
                {
                    ViewModel.ProcessDroppedFilesCommand.Execute(filePaths);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to browse files: {ex}");
        }
    }

    private void Page_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop to install";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;

            // Change border appearance
            if (_originalBorderBrush == null && FindName("DropZone") is Border dropZone)
            {
                _originalBorderBrush = dropZone.BorderBrush as SolidColorBrush;
                _originalBorderThickness = dropZone.BorderThickness;

                dropZone.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gold);
                dropZone.BorderThickness = new Thickness(3);
            }
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private void Page_DragLeave(object sender, DragEventArgs e)
    {
        // Restore original border
        if (_originalBorderBrush != null && FindName("DropZone") is Border dropZone)
        {
            dropZone.BorderBrush = _originalBorderBrush;
            dropZone.BorderThickness = _originalBorderThickness;
            _originalBorderBrush = null;
        }
    }

    private async void Page_Drop(object sender, DragEventArgs e)
    {
        // Restore original border
        if (_originalBorderBrush != null && FindName("DropZone") is Border dropZone)
        {
            dropZone.BorderBrush = _originalBorderBrush;
            dropZone.BorderThickness = _originalBorderThickness;
            _originalBorderBrush = null;
        }

        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var validFiles = new List<string>();

            foreach (var item in items)
            {
                if (item is StorageFile file)
                {
                    var ext = file.FileType.ToLower();
                    if (ext == ".lua" || ext == ".zip" || ext == ".manifest")
                    {
                        validFiles.Add(file.Path);
                    }
                }
                else if (item is StorageFolder folder)
                {
                    // Get all valid files from folder
                    var files = await GetFilesRecursiveAsync(folder);
                    validFiles.AddRange(files);
                }
            }

            if (validFiles.Any() && ViewModel.ProcessDroppedFilesCommand.CanExecute(validFiles.ToArray()))
            {
                ViewModel.ProcessDroppedFilesCommand.Execute(validFiles.ToArray());
            }
        }
    }

    private async System.Threading.Tasks.Task<List<string>> GetFilesRecursiveAsync(StorageFolder folder)
    {
        var result = new List<string>();

        try
        {
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
            {
                var ext = file.FileType.ToLower();
                if (ext == ".lua" || ext == ".zip" || ext == ".manifest")
                {
                    result.Add(file.Path);
                }
            }

            var folders = await folder.GetFoldersAsync();
            foreach (var subfolder in folders)
            {
                var subFiles = await GetFilesRecursiveAsync(subfolder);
                result.AddRange(subFiles);
            }
        }
        catch
        {
            // Ignore access denied errors
        }

        return result;
    }
}
