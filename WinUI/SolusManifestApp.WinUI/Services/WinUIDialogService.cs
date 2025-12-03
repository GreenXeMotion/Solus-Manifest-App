using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SolusManifestApp.Core.Interfaces;

namespace SolusManifestApp.WinUI.Services;

/// <summary>
/// WinUI 3 implementation of dialog service using ContentDialog
/// </summary>
public class WinUIDialogService : IDialogService
{
    private XamlRoot? GetXamlRoot()
    {
        // Get the current window's XamlRoot
        var window = (Application.Current as App)?.MainWindow;
        return window?.Content?.XamlRoot;
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title)
    {
        var xamlRoot = GetXamlRoot();
        if (xamlRoot == null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task ShowMessageAsync(string message, string title)
    {
        var xamlRoot = GetXamlRoot();
        if (xamlRoot == null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    public async Task<string?> ShowInputAsync(string message, string title, string? defaultValue = null)
    {
        var xamlRoot = GetXamlRoot();
        if (xamlRoot == null)
        {
            return null;
        }

        var inputTextBox = new TextBox
        {
            Text = defaultValue ?? string.Empty,
            AcceptsReturn = false,
            Height = 32
        };

        var stackPanel = new StackPanel
        {
            Spacing = 12
        };
        stackPanel.Children.Add(new TextBlock { Text = message });
        stackPanel.Children.Add(inputTextBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stackPanel,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? inputTextBox.Text : null;
    }
}
