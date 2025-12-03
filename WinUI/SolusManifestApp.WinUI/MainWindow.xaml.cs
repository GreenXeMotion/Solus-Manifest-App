using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SolusManifestApp.WinUI.Views;
using System;
using Windows.Graphics;
using WinRT.Interop;

namespace SolusManifestApp.WinUI;

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
        _appWindow.Resize(new SizeInt32(1280, 800));

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

        // Set the title bar control (for drag region)
        SetTitleBar(AppTitleBar);

        // Customize title bar appearance - hide default buttons
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = _appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            // Make default buttons invisible since we're using custom ones
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.Transparent;
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Minimize();
        }
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }
            else
            {
                presenter.Maximize();
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // Debug output
        System.Diagnostics.Debug.WriteLine($"Navigation triggered - IsSettingsSelected: {args.IsSettingsSelected}");

        if (args.IsSettingsSelected)
        {
            System.Diagnostics.Debug.WriteLine("Navigating to Settings page");
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            System.Diagnostics.Debug.WriteLine($"Navigation triggered - Tag: {tag}");

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
    }
}