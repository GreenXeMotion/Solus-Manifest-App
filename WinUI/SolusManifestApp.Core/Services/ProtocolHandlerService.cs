using SolusManifestApp.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SolusManifestApp.Core.Services;

/// <summary>
/// Service to handle custom protocol (solus://) for installing games
/// </summary>
public class ProtocolHandlerService
{
    private readonly ILoggerService _logger;
    private readonly INotificationService _notificationService;

    public ProtocolHandlerService(ILoggerService logger, INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Parse protocol URL (e.g., solus://install/480)
    /// </summary>
    public bool TryParseProtocolUrl(string url, out string action, out string parameter)
    {
        action = "";
        parameter = "";

        try
        {
            if (!url.StartsWith("solus://", StringComparison.OrdinalIgnoreCase))
                return false;

            var parts = url.Substring(8).Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1)
            {
                action = parts[0].ToLowerInvariant();
                parameter = parts.Length >= 2 ? parts[1] : "";
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse protocol URL: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Register the solus:// protocol in Windows Registry
    /// </summary>
    public bool RegisterProtocol()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                _logger.Error("Failed to get executable path");
                return false;
            }

            // Use reg add command for protocol registration
            var regCommand = $@"reg add ""HKCU\Software\Classes\solus"" /ve /d ""URL:Solus Protocol"" /f";
            var regCommand2 = $@"reg add ""HKCU\Software\Classes\solus"" /v ""URL Protocol"" /d """" /f";
            var regCommand3 = $@"reg add ""HKCU\Software\Classes\solus\shell\open\command"" /ve /d ""\""{exePath}\"" \""%1\"""" /f";

            ExecuteCommand(regCommand);
            ExecuteCommand(regCommand2);
            ExecuteCommand(regCommand3);

            _logger.Info("Protocol handler registered successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to register protocol: {ex.Message}");
            return false;
        }
    }

    private void ExecuteCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();
    }

    /// <summary>
    /// Handle protocol action
    /// </summary>
    public async Task<bool> HandleProtocolActionAsync(string action, string parameter)
    {
        try
        {
            _logger.Info($"Handling protocol action: {action} with parameter: {parameter}");

            switch (action)
            {
                case "install":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        _notificationService.ShowInfo($"Install requested for App ID: {parameter}");
                        // Actual installation would be handled by the UI layer
                        return true;
                    }
                    break;

                case "launch":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        _notificationService.ShowInfo($"Launch requested for App ID: {parameter}");
                        return true;
                    }
                    break;

                default:
                    _logger.Warning($"Unknown protocol action: {action}");
                    return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Protocol handler error: {ex.Message}");
            return false;
        }
    }
}
