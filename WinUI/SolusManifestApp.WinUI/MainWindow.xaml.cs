using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SolusManifestApp.WinUI.Views;
using System;
using WinRT.Interop;

namespace SolusManifestApp;

/// <summary>
/// Main window with navigation
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow;

    public MainWindow()
    {
        InitializeComponent();

        // Set up the custom title bar
        SetupTitleBar();

        // Set window size
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));

        // Navigate to home page by default
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void SetupTitleBar()
    {
        // Get the AppWindow
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Extend content into title bar
        ExtendsContentIntoTitleBar = true;

        // Set the title bar control
        SetTitleBar(AppTitleBar);

        // Customize title bar appearance
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = _appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            // Set button colors to match theme
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();

            switch (tag)
            {
                case "Home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "Store":
                    ContentFrame.Navigate(typeof(StorePage));
                    break;
                case "Library":
                    ContentFrame.Navigate(typeof(LibraryPage));
                    break;
                case "Downloads":
                    ContentFrame.Navigate(typeof(DownloadsPage));
                    break;
                case "Tools":
                    ContentFrame.Navigate(typeof(ToolsPage));
                    break;
            }
        }
        else if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
        }
    }
}