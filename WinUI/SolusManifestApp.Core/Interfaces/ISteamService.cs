namespace SolusManifestApp.Core.Interfaces;

/// <summary>
/// Interface for Steam service
/// Framework-agnostic implementation for Steam path detection and management
/// </summary>
public interface ISteamService
{
    string? GetSteamPath();
    bool IsSteamInstalled();
    bool IsSteamRunning();
    Task<bool> RestartSteamAsync();
    string? FindSteamExecutable();
}
