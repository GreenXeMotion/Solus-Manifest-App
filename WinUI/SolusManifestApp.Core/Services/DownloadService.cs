using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services
{
    /// <summary>
    /// Simplified download service for managing manifest downloads.
    /// Framework-agnostic with ObservableCollection support for UI binding.
    /// Note: Full implementation with retry logic, queue management, and status polling
    /// will be completed during ViewModel integration phase.
    /// </summary>
    public class DownloadService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Dictionary<string, CancellationTokenSource> _downloadCancellations;
        private readonly IManifestApiService _manifestApiService;
        private readonly ILoggerService _logger;

        public ObservableCollection<DownloadItem> ActiveDownloads { get; }
        public ObservableCollection<DownloadItem> QueuedDownloads { get; }
        public ObservableCollection<DownloadItem> CompletedDownloads { get; }
        public ObservableCollection<DownloadItem> FailedDownloads { get; }

        public event EventHandler<DownloadItem>? DownloadCompleted;
        public event EventHandler<DownloadItem>? DownloadFailed;
        public event EventHandler<DownloadItem>? DownloadStarted;

        private bool _isProcessingQueue = false;
        private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);

        public DownloadService(
            IHttpClientFactory httpClientFactory,
            IManifestApiService manifestApiService,
            ILoggerService logger)
        {
            _httpClientFactory = httpClientFactory;
            _downloadCancellations = new Dictionary<string, CancellationTokenSource>();
            _manifestApiService = manifestApiService;
            _logger = logger;

            ActiveDownloads = new ObservableCollection<DownloadItem>();
            QueuedDownloads = new ObservableCollection<DownloadItem>();
            CompletedDownloads = new ObservableCollection<DownloadItem>();
            FailedDownloads = new ObservableCollection<DownloadItem>();
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("Default");
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        /// <summary>
        /// Adds a download to the queue
        /// </summary>
        public void QueueDownload(DownloadItem downloadItem)
        {
            _logger.Info($"Queuing download: {downloadItem.GameName} ({downloadItem.AppId})");

            downloadItem.Status = DownloadStatus.Queued;
            downloadItem.StartTime = DateTime.Now;

            QueuedDownloads.Add(downloadItem);

            // Start processing queue if not already running
            if (!_isProcessingQueue)
            {
                _ = ProcessQueueAsync();
            }
        }

        /// <summary>
        /// Processes the download queue
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            await _queueLock.WaitAsync();
            try
            {
                if (_isProcessingQueue)
                    return;

                _isProcessingQueue = true;

                while (QueuedDownloads.Count > 0 && ActiveDownloads.Count < 3) // Max 3 concurrent
                {
                    var item = QueuedDownloads[0];
                    QueuedDownloads.RemoveAt(0);
                    ActiveDownloads.Add(item);

                    _ = DownloadItemAsync(item);
                }

                _isProcessingQueue = false;
            }
            finally
            {
                _queueLock.Release();
            }
        }

        /// <summary>
        /// Downloads a single item
        /// </summary>
        private async Task DownloadItemAsync(DownloadItem item)
        {
            var cts = new CancellationTokenSource();
            _downloadCancellations[item.Id] = cts;

            try
            {
                item.Status = DownloadStatus.Downloading;
                item.StatusMessage = "Starting download...";
                DownloadStarted?.Invoke(this, item);

                _logger.Info($"Starting download: {item.GameName}");

                // Wait for server to be ready (simplified - full implementation in Phase 4)
                await WaitForServerReadyAsync(item, cts.Token);

                // Download the file
                var client = CreateClient();
                using var response = await client.GetAsync(item.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                item.TotalBytes = totalBytes;

                // Ensure directory exists
                var directory = Path.GetDirectoryName(item.DestinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Download with progress tracking
                using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var fileStream = new FileStream(item.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                    totalRead += bytesRead;

                    item.DownloadedBytes = totalRead;
                    item.Progress = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;
                    item.StatusMessage = $"Downloading... {item.Progress:F1}%";
                }

                // Success
                item.Status = DownloadStatus.Completed;
                item.StatusMessage = "Download completed";
                item.EndTime = DateTime.Now;

                ActiveDownloads.Remove(item);
                CompletedDownloads.Add(item);

                _logger.Info($"✓ Download completed: {item.GameName}");
                DownloadCompleted?.Invoke(this, item);

                // Process next in queue
                _ = ProcessQueueAsync();
            }
            catch (OperationCanceledException)
            {
                item.Status = DownloadStatus.Cancelled;
                item.StatusMessage = "Cancelled";
                ActiveDownloads.Remove(item);
                _logger.Info($"Download cancelled: {item.GameName}");
            }
            catch (Exception ex)
            {
                item.Status = DownloadStatus.Failed;
                item.StatusMessage = $"Failed: {ex.Message}";
                item.EndTime = DateTime.Now;

                ActiveDownloads.Remove(item);
                FailedDownloads.Add(item);

                _logger.Error($"Download failed: {item.GameName} - {ex.Message}");
                DownloadFailed?.Invoke(this, item);

                // Process next in queue
                _ = ProcessQueueAsync();
            }
            finally
            {
                _downloadCancellations.Remove(item.Id);
            }
        }

        /// <summary>
        /// Waits for server to be ready before downloading (polls status API)
        /// </summary>
        private async Task WaitForServerReadyAsync(DownloadItem item, CancellationToken cancellationToken)
        {
            const int maxAttempts = 60; // 5 minutes max
            int attempt = 0;

            // Note: API key would need to be passed from settings
            // For now, this is a simplified implementation
            string apiKey = ""; // TODO: Get from settings in Phase 4

            while (attempt < maxAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                item.StatusMessage = "Checking server status...";

                var status = await _manifestApiService.GetGameStatusAsync(item.AppId, apiKey);

                if (status == null || status.UpdateInProgress != true)
                {
                    // Server is ready
                    return;
                }

                // Server is updating, wait and poll again
                item.StatusMessage = "Server updating manifest, waiting...";
                await Task.Delay(5000, cancellationToken);
                attempt++;
            }

            throw new Exception("Server status check timeout");
        }

        /// <summary>
        /// Cancels a download
        /// </summary>
        public void CancelDownload(string downloadId)
        {
            if (_downloadCancellations.TryGetValue(downloadId, out var cts))
            {
                cts.Cancel();
                _logger.Info($"Cancelling download: {downloadId}");
            }
        }

        /// <summary>
        /// Clears completed downloads
        /// </summary>
        public void ClearCompleted()
        {
            CompletedDownloads.Clear();
            _logger.Info("Cleared completed downloads");
        }

        /// <summary>
        /// Clears failed downloads
        /// </summary>
        public void ClearFailed()
        {
            FailedDownloads.Clear();
            _logger.Info("Cleared failed downloads");
        }

        /// <summary>
        /// Retries a failed download
        /// </summary>
        public void RetryDownload(DownloadItem item)
        {
            if (FailedDownloads.Contains(item))
            {
                FailedDownloads.Remove(item);
                item.Status = DownloadStatus.Queued;
                item.Progress = 0;
                item.DownloadedBytes = 0;
                item.StatusMessage = "Queued";
                QueueDownload(item);
            }
        }
    }
}
