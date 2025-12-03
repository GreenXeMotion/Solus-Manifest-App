using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SolusManifestApp.WinUI.Views;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace SolusManifestApp.WinUI;

/// <summary>
/// Main window with navigation
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow;

    // P/Invoke declarations to destroy caption controls
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    public MainWindow()
    {
        InitializeComponent();

        // Set up the custom title bar
        SetupTitleBar();

        // Set window size and position
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Resize(new SizeInt32(1280, 800));

        // Center window on screen
        CenterWindow();

        // Navigate to home page by default
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void CenterWindow()
    {
        if (_appWindow != null)
        {
            var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            var windowWidth = 1280;
            var windowHeight = 800;

            var x = (workArea.Width - windowWidth) / 2 + workArea.X;
            var y = (workArea.Height - windowHeight) / 2 + workArea.Y;

            _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }
    }

    private void SetupTitleBar()
    {
        // Get the AppWindow
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Check if title bar customization is supported (Collapse Launcher pattern)
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            // Extend content into title bar like Collapse Launcher
            ExtendsContentIntoTitleBar = true;

            // Set the title bar control (for drag region)
            SetTitleBar(AppTitleBar);

            // Hide icon and system menu (Collapse style)
            _appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            // Make title bar buttons completely transparent
            _appWindow.TitleBar.ButtonBackgroundColor = new Windows.UI.Color { A = 0, B = 0, G = 0, R = 0 };
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = new Windows.UI.Color { A = 0, B = 0, G = 0, R = 0 };

            // Destroy the caption controls window (Collapse Launcher's key trick)
            var controlsHwnd = FindWindowEx(hwnd, IntPtr.Zero, "ReunionWindowingCaptionControls", "ReunionCaptionControlsWindow");
            if (controlsHwnd != IntPtr.Zero)
            {
                DestroyWindow(controlsHwnd);
            }
        }
    }    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
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