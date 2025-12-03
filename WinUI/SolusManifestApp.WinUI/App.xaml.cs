using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Services;
using SolusManifestApp.ViewModels;
using SolusManifestApp.WinUI.Services;

namespace SolusManifestApp;

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

        // Build the DI container
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
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
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISteamService, SteamService>();

        // WinUI-Specific Services
        services.AddSingleton<IDialogService, WinUIDialogService>();
        services.AddSingleton<INotificationService, WinUINotificationService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();

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

        // Create and activate main window
        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();
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
