namespace SolusManifestApp.Core.Interfaces;

/// <summary>
/// Interface for platform-agnostic notification service
/// Will be implemented differently for WPF and WinUI 3
/// </summary>
public interface INotificationService
{
    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
}
