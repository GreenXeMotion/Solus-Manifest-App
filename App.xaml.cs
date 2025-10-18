using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SolusManifestApp.Helpers;
using SolusManifestApp.Services;
using SolusManifestApp.ViewModels;
using SolusManifestApp.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SolusManifestApp
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private SingleInstanceHelper? _singleInstance;
        private TrayIconService? _trayIconService;
        private MainWindow? _mainWindow;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<LoggerService>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<SteamService>();
                    services.AddSingleton<SteamGamesService>();
                    services.AddSingleton<SteamApiService>();
                    services.AddSingleton<ManifestApiService>();
                    services.AddSingleton<DownloadService>();
                    services.AddSingleton<FileInstallService>();
                    services.AddSingleton<UpdateService>();
                    services.AddSingleton<NotificationService>();
                    services.AddSingleton<CacheService>();
                    services.AddSingleton<BackupService>();
                    services.AddSingleton<DepotDownloadService>();
                    services.AddSingleton<SteamLibraryService>();
                    services.AddSingleton<ThemeService>();
                    services.AddSingleton<ProtocolHandlerService>();
                    services.AddSingleton<LibraryDatabaseService>();
                    services.AddSingleton<LibraryRefreshService>();
                    services.AddSingleton<RecentGamesService>();
                    services.AddSingleton<ConfigKeysUploadService>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<HomeViewModel>();
                    services.AddTransient<LuaInstallerViewModel>();
                    services.AddTransient<LibraryViewModel>();
                    services.AddTransient<StoreViewModel>();
                    services.AddTransient<DownloadsViewModel>();
                    services.AddSingleton<ToolsViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<SupportViewModel>();

                    // Views
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Register protocol (will update if path changed)
            ProtocolRegistrationHelper.RegisterProtocol();

            // Check for single instance
            _singleInstance = new SingleInstanceHelper();
            if (!_singleInstance.TryAcquire())
            {
                // Not the first instance, notify user and send args to first instance
                MessageBox.Show(
                    "Solus Manifest App is already running.\n\nThe existing instance has been brought to the foreground.",
                    "Already Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var args = string.Join(" ", e.Args);
                if (!string.IsNullOrEmpty(args))
                {
                    SingleInstanceHelper.SendArgumentsToFirstInstance(args);
                }
                Shutdown();
                return;
            }

            // This is the first instance, set up IPC listener
            _singleInstance.ArgumentsReceived += async (sender, args) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    // Show and activate the main window
                    if (_mainWindow != null)
                    {
                        _mainWindow.Show();
                        _mainWindow.WindowState = WindowState.Normal;
                        _mainWindow.Activate();
                    }

                    // Handle protocol URL if provided
                    if (!string.IsNullOrEmpty(args))
                    {
                        HandleProtocolUrl(args);
                    }
                });
            };

            await _host.StartAsync();

            // Load and apply theme
            var settingsService = _host.Services.GetRequiredService<SettingsService>();
            var themeService = _host.Services.GetRequiredService<ThemeService>();
            var settings = settingsService.LoadSettings();
            themeService.ApplyTheme(settings.Theme);

            _mainWindow = _host.Services.GetRequiredService<MainWindow>();

            // Initialize tray icon service with all dependencies
            var recentGamesService = _host.Services.GetRequiredService<RecentGamesService>();
            var steamService = _host.Services.GetRequiredService<SteamService>();
            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();

            _trayIconService = new TrayIconService(_mainWindow, settingsService, recentGamesService, steamService, mainViewModel, themeService);
            _trayIconService.Initialize();

            _mainWindow.Show();

            // Handle protocol URL if passed as argument
            if (e.Args.Length > 0)
            {
                HandleProtocolUrl(string.Join(" ", e.Args));
            }

            // Check for updates based on mode
            if (settings.AutoUpdate != Models.AutoUpdateMode.Disabled)
            {
                _ = CheckForUpdatesAsync(settings.AutoUpdate);
            }

            // Start background config keys upload service
            var configKeysUploadService = _host.Services.GetRequiredService<ConfigKeysUploadService>();
            configKeysUploadService.Start();

            base.OnStartup(e);
        }

        private async Task CheckForUpdatesAsync(Models.AutoUpdateMode mode)
        {
            try
            {
                var updateService = _host.Services.GetRequiredService<UpdateService>();
                var notificationService = _host.Services.GetRequiredService<NotificationService>();

                var (hasUpdate, updateInfo) = await updateService.CheckForUpdatesAsync();

                if (hasUpdate && updateInfo != null)
                {
                    if (mode == Models.AutoUpdateMode.AutoDownloadAndInstall)
                    {
                        // Auto download and install without asking
                        await DownloadAndInstallUpdateAsync(updateInfo);
                    }
                    else // CheckOnly mode
                    {
                        // Ask user if they want to update
                        await Dispatcher.InvokeAsync(() =>
                        {
                            var result = MessageBoxHelper.Show(
                                $"A new version ({updateInfo.TagName}) is available!\n\nWould you like to download and install it now?\n\nCurrent version: {updateService.GetCurrentVersion()}",
                                "Update Available",
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Information);

                            if (result == System.Windows.MessageBoxResult.Yes)
                            {
                                _ = DownloadAndInstallUpdateAsync(updateInfo);
                            }
                        });
                    }
                }
            }
            catch
            {
                // Silently fail if update check fails
            }
        }

        private async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                var updateService = _host.Services.GetRequiredService<UpdateService>();
                var notificationService = _host.Services.GetRequiredService<NotificationService>();

                notificationService.ShowNotification("Downloading Update", "Downloading the latest version...", NotificationType.Info);

                var updatePath = await updateService.DownloadUpdateAsync(updateInfo);

                if (!string.IsNullOrEmpty(updatePath))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBoxHelper.Show(
                            "Update downloaded successfully!\n\nThe app will now restart to install the update.",
                            "Update Ready",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        updateService.InstallUpdate(updatePath);
                    });
                }
                else
                {
                    notificationService.ShowError("Failed to download update. Please try again later.", "Update Failed");
                }
            }
            catch
            {
                var notificationService = _host.Services.GetRequiredService<NotificationService>();
                notificationService.ShowError("An error occurred while updating. Please try again later.", "Update Error");
            }
        }

        private async void HandleProtocolUrl(string url)
        {
            var protocolPath = ProtocolRegistrationHelper.ParseProtocolUrl(url);
            if (!string.IsNullOrEmpty(protocolPath))
            {
                var protocolHandler = _host.Services.GetRequiredService<ProtocolHandlerService>();
                await protocolHandler.HandleProtocolAsync(protocolPath);
            }
        }

        public TrayIconService? GetTrayIconService()
        {
            return _trayIconService;
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            // Save critical state before Windows shuts down/reboots
            try
            {
                var mainWindow = _host.Services.GetService<MainWindow>();
                if (mainWindow != null)
                {
                    var settingsService = _host.Services.GetRequiredService<SettingsService>();
                    var settings = settingsService.LoadSettings();
                    settings.WindowWidth = mainWindow.Width;
                    settings.WindowHeight = mainWindow.Height;
                    settingsService.SaveSettings(settings);
                }
            }
            catch
            {
                // Fail silently - don't block shutdown
            }

            base.OnSessionEnding(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _trayIconService?.Dispose();
            _singleInstance?.Dispose();

            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}
