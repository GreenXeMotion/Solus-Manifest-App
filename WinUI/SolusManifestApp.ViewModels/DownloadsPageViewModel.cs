using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using SolusManifestApp.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Downloads Page - manages download queue, active downloads, and completed downloads
/// </summary>
public partial class DownloadsPageViewModel : ObservableObject
{
    private readonly DownloadService _downloadService;
    private readonly FileInstallService _fileInstallService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly ILoggerService _logger;

    private AppSettings? _currentSettings;

    [ObservableProperty]
    private string _statusMessage = "No active downloads";

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _queuedCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private DownloadItem? _selectedDownload;

    [ObservableProperty]
    private bool _autoInstallAfterDownload = true;

    // Direct references to DownloadService collections for UI binding
    public ObservableCollection<DownloadItem> ActiveDownloads => _downloadService.ActiveDownloads;
    public ObservableCollection<DownloadItem> QueuedDownloads => _downloadService.QueuedDownloads;
    public ObservableCollection<DownloadItem> CompletedDownloads => _downloadService.CompletedDownloads;
    public ObservableCollection<DownloadItem> FailedDownloads => _downloadService.FailedDownloads;

    // Computed properties for UI visibility
    public bool HasActiveDownloads => ActiveCount > 0;
    public bool HasQueuedDownloads => QueuedCount > 0;
    public bool HasCompletedDownloads => CompletedCount > 0;
    public bool HasFailedDownloads => FailedCount > 0;
    public bool HasAnyDownloads => ActiveCount > 0 || QueuedCount > 0 || CompletedCount > 0 || FailedCount > 0;

    public DownloadsPageViewModel(
        DownloadService downloadService,
        FileInstallService fileInstallService,
        ISettingsService settingsService,
        INotificationService notificationService,
        ILoggerService logger)
    {
        _downloadService = downloadService;
        _fileInstallService = fileInstallService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _logger = logger;

        // Subscribe to download events
        _downloadService.DownloadStarted += OnDownloadStarted;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _downloadService.DownloadFailed += OnDownloadFailed;

        // Subscribe to collection changes
        ActiveDownloads.CollectionChanged += (s, e) => UpdateStatistics();
        QueuedDownloads.CollectionChanged += (s, e) => UpdateStatistics();
        CompletedDownloads.CollectionChanged += (s, e) => UpdateStatistics();
        FailedDownloads.CollectionChanged += (s, e) => UpdateStatistics();

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _currentSettings = await _settingsService.LoadSettingsAsync<AppSettings>();
            AutoInstallAfterDownload = _currentSettings?.AutoInstallAfterDownload ?? true;

            UpdateStatistics();
            _logger.Info("Downloads page initialized");
        }
        catch (Exception ex)
        {
            _logger.Error($"Downloads initialization failed: {ex.Message}");
            _notificationService.ShowError($"Failed to initialize downloads: {ex.Message}", "Error");
        }
    }

    private void UpdateStatistics()
    {
        ActiveCount = ActiveDownloads.Count;
        QueuedCount = QueuedDownloads.Count;
        CompletedCount = CompletedDownloads.Count;
        FailedCount = FailedDownloads.Count;

        // Notify computed properties
        OnPropertyChanged(nameof(HasActiveDownloads));
        OnPropertyChanged(nameof(HasQueuedDownloads));
        OnPropertyChanged(nameof(HasCompletedDownloads));
        OnPropertyChanged(nameof(HasFailedDownloads));
        OnPropertyChanged(nameof(HasAnyDownloads));

        if (ActiveCount > 0)
        {
            StatusMessage = $"{ActiveCount} download(s) in progress, {QueuedCount} queued";
        }
        else if (QueuedCount > 0)
        {
            StatusMessage = $"{QueuedCount} download(s) queued";
        }
        else
        {
            StatusMessage = "No active downloads";
        }
    }

    [RelayCommand]
    private void CancelDownload(DownloadItem download)
    {
        if (download == null) return;

        try
        {
            _downloadService.CancelDownload(download.Id);
            _notificationService.ShowInfo($"Cancelled download: {download.GameName}", "Download Cancelled");
            _logger.Info($"Cancelled download: {download.GameName} ({download.AppId})");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to cancel download: {ex.Message}");
            _notificationService.ShowError($"Failed to cancel download: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void RetryDownload(DownloadItem download)
    {
        if (download == null) return;

        try
        {
            // Remove from failed list and re-queue
            FailedDownloads.Remove(download);
            download.Status = DownloadStatus.Queued;
            download.StatusMessage = "Queued for retry";
            download.Progress = 0;

            _downloadService.QueueDownload(download);

            _notificationService.ShowInfo($"Retrying download: {download.GameName}", "Retry");
            _logger.Info($"Retrying download: {download.GameName} ({download.AppId})");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to retry download: {ex.Message}");
            _notificationService.ShowError($"Failed to retry download: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private async Task InstallDownload(DownloadItem download)
    {
        if (download == null || download.Status != DownloadStatus.Completed) return;

        try
        {
            _logger.Info($"Installing downloaded file: {download.DestinationPath}");

            var result = await _fileInstallService.InstallFromZipAsync(download.DestinationPath);

            if (result)
            {
                _notificationService.ShowSuccess($"Installed: {download.GameName}", "Success");
                _logger.Info($"Successfully installed: {download.GameName}");

                // TODO: Optionally move to "Installed" list or remove from completed
            }
            else
            {
                _notificationService.ShowWarning($"Installation completed with warnings for: {download.GameName}", "Warning");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install: {ex.Message}");
            _notificationService.ShowError($"Failed to install: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void RemoveCompleted(DownloadItem download)
    {
        if (download == null) return;

        try
        {
            CompletedDownloads.Remove(download);
            _logger.Info($"Removed completed download: {download.GameName}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to remove download: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveFailed(DownloadItem download)
    {
        if (download == null) return;

        try
        {
            FailedDownloads.Remove(download);
            _logger.Info($"Removed failed download: {download.GameName}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to remove download: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var count = CompletedDownloads.Count;
        CompletedDownloads.Clear();
        _notificationService.ShowInfo($"Cleared {count} completed download(s)", "Cleared");
        _logger.Info($"Cleared {count} completed downloads");
    }

    [RelayCommand]
    private void ClearFailed()
    {
        var count = FailedDownloads.Count;
        FailedDownloads.Clear();
        _notificationService.ShowInfo($"Cleared {count} failed download(s)", "Cleared");
        _logger.Info($"Cleared {count} failed downloads");
    }

    [RelayCommand]
    private void OpenDownloadFolder(DownloadItem download)
    {
        if (download == null || string.IsNullOrEmpty(download.DestinationPath)) return;

        try
        {
            var folder = System.IO.Path.GetDirectoryName(download.DestinationPath);
            if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open folder: {ex.Message}");
            _notificationService.ShowError($"Failed to open folder: {ex.Message}", "Error");
        }
    }

    private void OnDownloadStarted(object? sender, DownloadItem download)
    {
        _logger.Info($"Download started: {download.GameName} ({download.AppId})");
        UpdateStatistics();
    }

    private void OnDownloadCompleted(object? sender, DownloadItem download)
    {
        _notificationService.ShowSuccess($"Download completed: {download.GameName}", "Success");
        _logger.Info($"Download completed: {download.GameName} ({download.AppId})");

        UpdateStatistics();

        // Auto-install if enabled
        if (AutoInstallAfterDownload)
        {
            _ = InstallDownload(download);
        }
    }

    private void OnDownloadFailed(object? sender, DownloadItem download)
    {
        _notificationService.ShowError($"Download failed: {download.GameName}", "Failed");
        _logger.Error($"Download failed: {download.GameName} - {download.StatusMessage}");

        UpdateStatistics();
    }

    partial void OnAutoInstallAfterDownloadChanged(bool value)
    {
        if (_currentSettings != null)
        {
            _currentSettings.AutoInstallAfterDownload = value;
            _ = _settingsService.SaveSettingsAsync(_currentSettings);
            _logger.Info($"Auto-install after download: {value}");
        }
    }

    public void Dispose()
    {
        _downloadService.DownloadStarted -= OnDownloadStarted;
        _downloadService.DownloadCompleted -= OnDownloadCompleted;
        _downloadService.DownloadFailed -= OnDownloadFailed;
    }
}
