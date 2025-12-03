using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SolusManifestApp.ViewModels;
using Windows.Storage.Pickers;

namespace SolusManifestApp.WinUI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;

        // Subscribe to Browse command requests from ViewModel
        ViewModel.BrowseSteamPathRequested += OnBrowseSteamPathRequested;
        ViewModel.BrowseDownloadsPathRequested += OnBrowseDownloadsPathRequested;
        ViewModel.BrowseAppListPathRequested += OnBrowseAppListPathRequested;
        ViewModel.BrowseDllInjectorPathRequested += OnBrowseDllInjectorPathRequested;
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        ViewModel.BrowseSteamPathRequested -= OnBrowseSteamPathRequested;
        ViewModel.BrowseDownloadsPathRequested -= OnBrowseDownloadsPathRequested;
        ViewModel.BrowseAppListPathRequested -= OnBrowseAppListPathRequested;
        ViewModel.BrowseDllInjectorPathRequested -= OnBrowseDllInjectorPathRequested;

        base.OnNavigatingFrom(e);
    }

    private async void OnBrowseSteamPathRequested(object? sender, EventArgs e)
    {
        var folder = await PickFolderAsync("Select Steam Installation Folder");
        if (folder != null)
        {
            ViewModel.SteamPath = folder;
        }
    }

    private async void OnBrowseDownloadsPathRequested(object? sender, EventArgs e)
    {
        var folder = await PickFolderAsync("Select Downloads Folder");
        if (folder != null)
        {
            ViewModel.DownloadsPath = folder;
        }
    }

    private async void OnBrowseAppListPathRequested(object? sender, EventArgs e)
    {
        var file = await PickFileAsync("Select AppList.txt", new[] { ".txt" });
        if (file != null)
        {
            ViewModel.AppListPath = file;
        }
    }

    private async void OnBrowseDllInjectorPathRequested(object? sender, EventArgs e)
    {
        var file = await PickFileAsync("Select DLL Injector", new[] { ".exe", ".dll" });
        if (file != null)
        {
            ViewModel.DllInjectorPath = file;
        }
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            ViewMode = PickerViewMode.List
        };

        // Must add at least one file type filter (required by WinUI)
        picker.FileTypeFilter.Add("*");

        // Initialize the picker with the window handle
        var app = App.Current as App;
        if (app?.MainWindow == null) return null;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task<string?> PickFileAsync(string title, string[] extensions)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            ViewMode = PickerViewMode.List
        };

        // Add file type filters
        foreach (var ext in extensions)
        {
            picker.FileTypeFilter.Add(ext);
        }

        // Initialize the picker with the window handle
        var app = App.Current as App;
        if (app?.MainWindow == null) return null;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
