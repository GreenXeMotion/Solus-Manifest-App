namespace SolusManifestApp.Core.Models;

/// <summary>
/// Application settings model
/// Placeholder for settings that will be migrated from WPF version
/// </summary>
public class AppSettings
{
    // TODO: Migrate from original AppSettings.cs
    public string? ApiKey { get; set; }
    public string? SteamPath { get; set; }
    public string? ToolMode { get; set; }
    public string? Theme { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool AutoUpdate { get; set; }
}
