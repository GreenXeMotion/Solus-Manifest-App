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
/// ViewModel for Library Page - manages Steam games, GreenLuma games, and profiles
/// </summary>
public partial class LibraryPageViewModel : ObservableObject
{
    private readonly SteamGamesService _steamGamesService;
    private readonly ProfileService _profileService;
    private readonly ISettingsService _settingsService;
    private readonly ISteamService _steamService;
    private readonly INotificationService _notificationService;
    private readonly ILoggerService _logger;

    private AppSettings? _currentSettings;

    [ObservableProperty]
    private ObservableCollection<LibraryItem> _libraryItems = new();

    [ObservableProperty]
    private ObservableCollection<GreenLumaProfile> _profiles = new();

    [ObservableProperty]
    private GreenLumaProfile? _activeProfile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Loading library...";

    [ObservableProperty]
    private string _filterMode = "All";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _totalGames;

    [ObservableProperty]
    private int _steamGamesCount;

    [ObservableProperty]
    private int _greenLumaGamesCount;

    [ObservableProperty]
    private int _luaScriptsCount;

    [ObservableProperty]
    private LibraryItem? _selectedItem;

    // Computed properties for UI
    public bool HasGames => LibraryItems.Count > 0;
    public bool CanDeleteProfile => ActiveProfile != null && Profiles.Count > 1;

    public ObservableCollection<string> FilterOptions { get; } = new()
    {
        "All",
        "Steam Games",
        "GreenLuma",
        "Lua Only"
    };

    public LibraryPageViewModel(
        SteamGamesService steamGamesService,
        ProfileService profileService,
        ISettingsService settingsService,
        ISteamService steamService,
        INotificationService notificationService,
        ILoggerService logger)
    {
        _steamGamesService = steamGamesService;
        _profileService = profileService;
        _settingsService = settingsService;
        _steamService = steamService;
        _notificationService = notificationService;
        _logger = logger;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _currentSettings = await _settingsService.LoadSettingsAsync<AppSettings>();

            // Load profiles
            await LoadProfilesAsync();

            // Load library
            await RefreshLibraryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"Library initialization failed: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            _notificationService.ShowError($"Failed to initialize library: {ex.Message}", "Error");
        }
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            var profileData = _profileService.LoadProfiles();

            Profiles.Clear();
            foreach (var profile in profileData.Profiles)
            {
                Profiles.Add(profile);
            }

            var activeProfileId = profileData.ActiveProfileId;
            ActiveProfile = Profiles.FirstOrDefault(p => p.Id == activeProfileId) ?? Profiles.FirstOrDefault();

            _logger.Info($"Loaded {Profiles.Count} profiles, active: {ActiveProfile?.Name ?? "None"}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load profiles: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshLibraryAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing library...";

            var steamPath = _steamService.GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                StatusMessage = "Steam not found. Please install Steam or configure path in Settings.";
                _notificationService.ShowWarning("Steam installation not found", "Warning");
                IsLoading = false;
                return;
            }

            // Get Steam games
            var steamGames = _steamGamesService.GetInstalledGames();

            // Get GreenLuma games from active profile
            var greenLumaAppIds = ActiveProfile?.Games.Select(g => g.AppId).ToList() ?? new System.Collections.Generic.List<string>();

            // Combine into LibraryItems
            LibraryItems.Clear();

            // Add Steam games
            foreach (var game in steamGames)
            {
                var item = LibraryItem.FromSteamGame(game);
                // Check if it's also in GreenLuma
                if (greenLumaAppIds.Contains(game.AppId))
                {
                    item.ItemType = LibraryItemType.GreenLuma;
                }
                LibraryItems.Add(item);
            }

            // Add GreenLuma-only games (not in Steam library)
            foreach (var greenLumaGame in ActiveProfile?.Games ?? new System.Collections.Generic.List<ProfileGame>())
            {
                if (!LibraryItems.Any(i => i.AppId == greenLumaGame.AppId))
                {
                    var item = new LibraryItem
                    {
                        AppId = greenLumaGame.AppId,
                        Name = greenLumaGame.Name,
                        ItemType = LibraryItemType.Lua
                    };
                    LibraryItems.Add(item);
                }
            }

            UpdateStatistics();
            ApplyFilter();

            StatusMessage = $"Library loaded: {TotalGames} total games";
            _logger.Info($"Library refreshed: {TotalGames} games ({SteamGamesCount} Steam, {GreenLumaGamesCount} GreenLuma)");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to refresh library: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            _notificationService.ShowError($"Failed to refresh library: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchLibrary()
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    [RelayCommand]
    private void ChangeFilter(string filter)
    {
        FilterMode = filter;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var allItems = LibraryItems.ToList();
        LibraryItems.Clear();

        var filtered = allItems.AsEnumerable();

        // Apply filter mode
        filtered = FilterMode switch
        {
            "Steam Games" => filtered.Where(i => i.ItemType == LibraryItemType.SteamGame),
            "GreenLuma" => filtered.Where(i => i.ItemType == LibraryItemType.GreenLuma),
            "Lua Only" => filtered.Where(i => i.ItemType == LibraryItemType.Lua),
            _ => filtered // "All"
        };

        // Apply search
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var searchLower = SearchQuery.ToLower();
            filtered = filtered.Where(i =>
                i.Name.ToLower().Contains(searchLower) ||
                i.AppId.Contains(SearchQuery));
        }

        foreach (var item in filtered)
        {
            LibraryItems.Add(item);
        }

        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        TotalGames = LibraryItems.Count;
        SteamGamesCount = LibraryItems.Count(i => i.ItemType == LibraryItemType.SteamGame);
        GreenLumaGamesCount = LibraryItems.Count(i => i.ItemType == LibraryItemType.GreenLuma);
        LuaScriptsCount = LibraryItems.Count(i => i.ItemType == LibraryItemType.Lua);

        // Notify computed properties
        OnPropertyChanged(nameof(HasGames));
        OnPropertyChanged(nameof(CanDeleteProfile));
    }

    [RelayCommand]
    private async Task SwitchProfile(GreenLumaProfile profile)
    {
        if (profile == null || profile == ActiveProfile) return;

        try
        {
            _profileService.SetActiveProfile(profile.Id);
            ActiveProfile = profile;

            await RefreshLibraryAsync();

            _notificationService.ShowSuccess($"Switched to profile: {profile.Name}", "Success");
            _logger.Info($"Switched to profile: {profile.Name} ({profile.Id})");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to switch profile: {ex.Message}");
            _notificationService.ShowError($"Failed to switch profile: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void CreateProfile()
    {
        _notificationService.ShowInfo("Profile creation dialog will be implemented", "Coming Soon");
        // TODO: Show create profile dialog
    }

    [RelayCommand]
    private void DeleteProfile(GreenLumaProfile profile)
    {
        if (profile == null) return;

        _notificationService.ShowInfo("Profile deletion will be implemented", "Coming Soon");
        // TODO: Show confirmation dialog and delete profile
    }

    [RelayCommand]
    private void AddToGreenLuma(LibraryItem item)
    {
        if (item == null || ActiveProfile == null) return;

        try
        {
            // Add to active profile as a ProfileGame
            if (!ActiveProfile.Games.Any(g => g.AppId == item.AppId))
            {
                var profileGame = new ProfileGame
                {
                    AppId = item.AppId,
                    Name = item.Name,
                    AddedAt = DateTime.UtcNow
                };
                ActiveProfile.Games.Add(profileGame);
                _profileService.SaveProfiles();

                item.ItemType = LibraryItemType.GreenLuma;

                _notificationService.ShowSuccess($"Added {item.Name} to GreenLuma", "Success");
                _logger.Info($"Added {item.AppId} to GreenLuma profile {ActiveProfile.Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to add to GreenLuma: {ex.Message}");
            _notificationService.ShowError($"Failed to add to GreenLuma: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void RemoveFromGreenLuma(LibraryItem item)
    {
        if (item == null || ActiveProfile == null) return;

        try
        {
            var existingGame = ActiveProfile.Games.FirstOrDefault(g => g.AppId == item.AppId);
            if (existingGame != null)
            {
                ActiveProfile.Games.Remove(existingGame);
                _profileService.SaveProfiles();

                // Check if it's a Steam game, if so set to SteamGame, otherwise remove
                if (LibraryItems.Any(i => i.AppId == item.AppId && i.ItemType == LibraryItemType.SteamGame))
                {
                    item.ItemType = LibraryItemType.SteamGame;
                }
                else
                {
                    LibraryItems.Remove(item);
                }

                _notificationService.ShowSuccess($"Removed {item.Name} from GreenLuma", "Success");
                _logger.Info($"Removed {item.AppId} from GreenLuma profile {ActiveProfile.Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to remove from GreenLuma: {ex.Message}");
            _notificationService.ShowError($"Failed to remove: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void LaunchGame(LibraryItem item)
    {
        if (item == null) return;

        try
        {
            var steamPath = _steamService.GetSteamPath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"steam://run/{item.AppId}",
                    UseShellExecute = true
                });

                _logger.Info($"Launched game: {item.Name} ({item.AppId})");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to launch game: {ex.Message}");
            _notificationService.ShowError($"Failed to launch: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void UninstallGame(LibraryItem item)
    {
        if (item == null) return;

        _notificationService.ShowInfo("Uninstall functionality will be implemented", "Coming Soon");
        // TODO: Implement uninstall with confirmation dialog
    }
}
