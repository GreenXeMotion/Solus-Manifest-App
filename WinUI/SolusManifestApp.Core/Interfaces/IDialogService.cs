namespace SolusManifestApp.Core.Interfaces;

/// <summary>
/// Interface for platform-agnostic dialog service
/// WPF implementation uses MessageBox, WinUI 3 uses ContentDialog
/// </summary>
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message, string title);
    Task ShowMessageAsync(string message, string title);
    Task<string?> ShowInputAsync(string message, string title, string? defaultValue = null);
}
