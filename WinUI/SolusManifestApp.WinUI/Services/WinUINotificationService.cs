using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using SolusManifestApp.Core.Interfaces;

namespace SolusManifestApp.WinUI.Services;

/// <summary>
/// WinUI 3 implementation of notification service using Windows App Notifications
/// </summary>
public class WinUINotificationService : INotificationService
{
    private readonly AppNotificationManager _notificationManager;
    private bool _isInitialized = false;

    public WinUINotificationService()
    {
        _notificationManager = AppNotificationManager.Default;
        Initialize();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            _notificationManager.NotificationInvoked += OnNotificationInvoked;
            _notificationManager.Register();
            _isInitialized = true;
        }
        catch
        {
            // Silently fail if notifications aren't supported
            _isInitialized = false;
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // Handle notification click if needed
    }

    public void ShowSuccess(string message, string? title = null)
    {
        ShowNotification(message, title ?? "Success", "✅");
    }

    public void ShowError(string message, string? title = null)
    {
        ShowNotification(message, title ?? "Error", "❌");
    }

    public void ShowInfo(string message, string? title = null)
    {
        ShowNotification(message, title ?? "Information", "ℹ️");
    }

    public void ShowWarning(string message, string? title = null)
    {
        ShowNotification(message, title ?? "Warning", "⚠️");
    }

    private void ShowNotification(string message, string title, string icon)
    {
        if (!_isInitialized)
        {
            return;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText($"{icon} {title}")
                .AddText(message);

            var notification = builder.BuildNotification();
            _notificationManager.Show(notification);
        }
        catch
        {
            // Silently fail if notification can't be shown
        }
    }
}
