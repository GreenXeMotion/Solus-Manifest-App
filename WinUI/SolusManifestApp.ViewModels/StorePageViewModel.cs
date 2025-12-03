using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SolusManifestApp.Core.Interfaces;
using SolusManifestApp.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SolusManifestApp.ViewModels;

/// <summary>
/// ViewModel for Store Page - full game browsing with search, pagination, and download
/// </summary>
public partial class StorePageViewModel : ObservableObject
{
    private readonly IManifestApiService _manifestApiService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ICacheService _cacheService;
    private readonly ILoggerService _logger;

    private AppSettings? _currentSettings;
    private string _lastSearch = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LibraryGame> _games = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready to browse games";

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private string _sortBy = "updated";

    [ObservableProperty]
    private bool _isApiKeyConfigured;

    [ObservableProperty]
    private LibraryGame? _selectedGame;

    [ObservableProperty]
    private bool _isListView = false;

    // Computed properties for UI
    public bool HasGames => Games.Count > 0;
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "updated",
        "name",
        "added"
    };

    public StorePageViewModel(
        IManifestApiService manifestApiService,
        INotificationService notificationService,
        ISettingsService settingsService,
        ICacheService cacheService,
        ILoggerService logger)
    {
        _manifestApiService = manifestApiService;
        _notificationService = notificationService;
        _settingsService = settingsService;
        _cacheService = cacheService;
        _logger = logger;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _currentSettings = await _settingsService.LoadSettingsAsync<AppSettings>();

            if (_currentSettings != null && !string.IsNullOrEmpty(_currentSettings.ApiKey))
            {
                IsApiKeyConfigured = true;
                await LoadGamesAsync();
            }
            else
            {
                IsApiKeyConfigured = false;
                StatusMessage = "Please configure API key in Settings to browse games";
                _notificationService.ShowWarning("API key not configured. Please add it in Settings.", "Configuration Required");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Store initialization failed: {ex.Message}");
            StatusMessage = $"Initialization error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        if (_currentSettings == null || string.IsNullOrEmpty(_currentSettings.ApiKey))
        {
            _notificationService.ShowWarning("API key not configured", "Error");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Loading games...";

            var offset = (CurrentPage - 1) * PageSize;
            var result = await _manifestApiService.GetLibraryAsync(
                _currentSettings.ApiKey,
                PageSize,
                offset,
                string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                SortBy);

            if (result != null && result.Games != null)
            {
                Games.Clear();
                foreach (var game in result.Games)
                {
                    Games.Add(game);
                    // Start icon caching in background
                    _ = LoadGameIconAsync(game);
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                StatusMessage = $"Showing {Games.Count} of {TotalCount} games (Page {CurrentPage}/{TotalPages})";

                // Notify computed properties
                OnPropertyChanged(nameof(HasGames));
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));

                _logger.Info($"Loaded {Games.Count} games from library API");
            }
            else
            {
                StatusMessage = "No games found";
                _notificationService.ShowWarning("No games returned from API", "No Results");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load games: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            _notificationService.ShowError($"Failed to load games: {ex.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (SearchQuery != _lastSearch)
        {
            CurrentPage = 1; // Reset to first page on new search
            _lastSearch = SearchQuery;
        }
        await LoadGamesAsync();
    }

    [RelayCommand]
    private async Task ClearSearch()
    {
        SearchQuery = string.Empty;
        _lastSearch = string.Empty;
        CurrentPage = 1;
        await LoadGamesAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            await LoadGamesAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            await LoadGamesAsync();
        }
    }

    [RelayCommand]
    private async Task GoToPage(int page)
    {
        if (page > 0 && page <= TotalPages && page != CurrentPage)
        {
            CurrentPage = page;
            await LoadGamesAsync();
        }
    }

    [RelayCommand]
    private async Task ChangeSortBy(string sortOption)
    {
        if (SortBy != sortOption)
        {
            SortBy = sortOption;
            CurrentPage = 1; // Reset to first page on sort change
            await LoadGamesAsync();
        }
    }

    [RelayCommand]
    private async Task SortByUpdated()
    {
        await ChangeSortBy("updated");
    }

    [RelayCommand]
    private async Task SortByName()
    {
        await ChangeSortBy("name");
    }

    [RelayCommand]
    private async Task SearchGames()
    {
        await SearchAsync();
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsListView = !IsListView;
        _logger.Info($"Toggled view mode to: {(IsListView ? "List" : "Grid")}");
    }

    [RelayCommand]
    private async Task RefreshLibrary()
    {
        await LoadGamesAsync();
        _notificationService.ShowSuccess("Library refreshed", "Success");
    }

    [RelayCommand]
    private void DownloadGame(LibraryGame game)
    {
        if (game == null) return;

        try
        {
            _notificationService.ShowInfo($"Download functionality will be integrated with DownloadService", "Coming Soon");
            _logger.Info($"Download requested for: {game.GameName} ({game.GameId})");

            // TODO: Integrate with DownloadService when DownloadsPageViewModel is created
            // var downloadItem = new DownloadItem
            // {
            //     AppId = game.GameId,
            //     GameName = game.GameName,
            //     DownloadUrl = $"https://manifest.morrenus.xyz/api/v1/manifest/{game.GameId}?api_key={_currentSettings.ApiKey}"
            // };
            // _downloadService.QueueDownload(downloadItem);
        }
        catch (Exception ex)
        {
            _logger.Error($"Download failed for {game.GameName}: {ex.Message}");
            _notificationService.ShowError($"Failed to start download: {ex.Message}", "Error");
        }
    }

    [RelayCommand]
    private void ViewGameDetails(LibraryGame game)
    {
        if (game == null) return;

        SelectedGame = game;
        _logger.Info($"Viewing details for: {game.GameName} ({game.GameId})");
        // TODO: Show game details dialog or navigate to details page
    }

    private async Task LoadGameIconAsync(LibraryGame game)
    {
        try
        {
            if (string.IsNullOrEmpty(game.HeaderImage)) return;

            // Check if already cached
            if (_cacheService.HasCachedIcon(game.GameId))
            {
                var cachedPath = _cacheService.GetCachedIconPath(game.GameId);
                game.CachedIconPath = cachedPath;
                return;
            }

            // Cache will be implemented when ImageCacheService is fully integrated
            // For now, just use the URL directly
            game.CachedIconPath = game.HeaderImage;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load icon for {game.GameName}: {ex.Message}");
        }
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1; // Reset to first page when page size changes
        _ = LoadGamesAsync();
    }
}
