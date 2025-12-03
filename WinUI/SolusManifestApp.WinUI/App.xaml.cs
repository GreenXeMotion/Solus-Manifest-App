using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using SolusManifestApp.Core.Services;
using SolusManifestApp.ViewModels;
using SolusManifestApp.WinUI.Services;

namespace SolusManifestApp.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    /// <summary>
    /// Main window instance accessible to services
    /// </summary>
    public MainWindow? MainWindow { get; private set; }

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        // Handle unhandled exceptions
        this.UnhandledException += App_UnhandledException;

        // Build the DI container
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception
        System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {e.Exception}");
        try
        {
            System.IO.File.WriteAllText(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SolusManifestApp_Crash.txt"),
                $"Exception: {e.Exception}\r\n\r\nStackTrace: {e.Exception.StackTrace}\r\n\r\nInnerException: {e.Exception.InnerException}"
            );
        }
        catch { }
        e.Handled = true;
    }

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // Configure HttpClient
        services.AddHttpClient("Default", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(30);
        });

        // Core Services
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISteamService, SteamService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IManifestApiService, ManifestApiService>();
        services.AddSingleton<SteamGamesService>();
        services.AddSingleton<SteamLibraryService>();
        services.AddSingleton<ProfileService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<LibraryRefreshService>();
        services.AddSingleton<FileInstallService>();
        services.AddSingleton<LuaFileManager>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<ArchiveExtractionService>();
        services.AddSingleton<RecentGamesService>();
        services.AddSingleton<SteamApiService>();
        services.AddSingleton<SteamCmdApiService>();
        services.AddSingleton<ProtocolHandlerService>();
        services.AddSingleton<DepotFilterService>();
        services.AddSingleton<LuaParser>();
        services.AddSingleton<ImageCacheService>();
        services.AddSingleton<DepotDownloadService>();
        services.AddSingleton<ConfigKeysUploadService>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<LibraryDatabaseService>();
        services.AddSingleton<SteamKitAppInfoService>();
        services.AddSingleton<DepotDownloaderWrapperService>();

        // WinUI-Specific Services
        services.AddSingleton<IDialogService, WinUIDialogService>();
        services.AddSingleton<INotificationService, WinUINotificationService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomePageViewModel>();
        services.AddTransient<StorePageViewModel>();
        services.AddTransient<ToolsPageViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Register MainWindow
        services.AddTransient<MainWindow>();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Start the host
        await _host.StartAsync();

        // Create and activate main window first
        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();

        // Apply saved theme AFTER window is shown (with error handling)
        /*
        try
        {
            var themeService = _host.Services.GetRequiredService<IThemeService>();
            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            var settings = await settingsService.LoadSettingsAsync<AppSettings>();
            if (settings != null)
            {
                themeService.ApplyTheme(settings.Theme.ToString());
            }
        }
        catch
        {
            // If theme loading fails, continue with default theme
        }
        */
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if ((Current as App)?._host?.Services is IServiceProvider services)
        {
            return services.GetRequiredService<T>();
        }
        throw new InvalidOperationException("Service provider not initialized");
    }
}
