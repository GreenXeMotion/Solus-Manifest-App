# Solus Manifest App - AI Coding Guide

## Project Overview

**Solus Manifest App** is a WPF desktop application for managing Steam game depots, manifests, and GreenLuma 2024 integration. Built with .NET 8, it provides depot downloading via the Morrenus API and direct Steam CDN access through integrated DepotDownloader and SteamKit2.

**Key Stack**: .NET 8 WPF, MVVM with CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, SteamKit2, protobuf-net

## Architecture & Patterns

### Dependency Injection & Service Layer
All services are singleton or transient registrations in `App.xaml.cs`. Services follow interface-based design for testability:
- Core services implement interfaces (e.g., `ISteamService`, `IManifestApiService`) registered twice: concrete and interface
- ViewModels are injected with service dependencies via constructor injection
- Use `IHttpClientFactory` for all HTTP operations (configured in `App.xaml.cs` with 30-min timeout)

Example service registration pattern:
```csharp
services.AddSingleton<SteamService>();
services.AddSingleton<ISteamService>(sp => sp.GetRequiredService<SteamService>());
```

### MVVM with CommunityToolkit
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `[ObservableProperty]` for bindable properties (auto-generates property changed notifications)
- Use `[RelayCommand]` for command bindings (auto-generates ICommand implementations)
- Views are cached in `MainViewModel._cachedViews` dictionary to prevent recreation on navigation

### Navigation Pattern
`MainViewModel` manages page navigation with a cached view dictionary. Navigation methods check `CanNavigateAway()` to prevent data loss from unsaved changes. Views are created once and reused.

### Settings & Configuration
`AppSettings.cs` is the single source of truth for all app configuration (API keys, paths, modes, themes). Managed by `SettingsService` which handles JSON serialization to `%AppData%\SolusManifestApp\settings.json`. Settings are injected into services as needed.

**Important modes**:
- `ToolMode`: `SteamTools` (depot cache only) vs `GreenLuma` (AppList.txt) vs `DepotDownloader` (full downloads)
- `GreenLumaMode`: `Normal`, `StealthAnyFolder`, `StealthUser32` (different DLL injection methods)
- `AutoUpdateMode`: `Disabled`, `CheckOnly`, `AutoDownloadAndInstall`

## Critical Subsystems

### Steam Integration
- **SteamService**: Finds Steam via registry (`HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam`), validates paths, manages Steam process restart
- **SteamGamesService**: Parses `appmanifest_*.acf` files from `steamapps/` folders using `VdfParser` (custom Valve Data Format parser)
- **SteamLibraryService**: Detects multiple library folders via `libraryfolders.vdf`
- Steam path is cached in `_cachedSteamPath` after first lookup

### Manifest & Depot Management
- **ManifestApiService**: Hits Morrenus API (`manifest.morrenus.xyz/api/v1`) for game metadata, search, and status
  - All API calls require `api_key` parameter (stored in settings, validated with `smm` prefix)
  - Implements `CacheService` integration for 5-minute status caching
- **DepotDownloadService**: Wraps SteamKit2 for direct Steam CDN downloads without credentials
- **DepotDownloaderWrapperService**: Integrates the full DepotDownloader tool (from `DepotDownloader/` namespace) for authenticated downloads

### Embedded DepotDownloader Integration
The `DepotDownloader/` namespace contains the full DepotDownloader codebase (by SteamRE). Key classes:
- `ContentDownloader`: Main download logic, manifest retrieval
- `Steam3Session`: Manages anonymous or authenticated Steam connections
- `ProtoManifest`: Protobuf manifest deserialization
- Integrated into `DepotDownloaderWrapperService` which provides async/await wrappers and progress reporting

### GreenLuma Profile System
`ProfileService` manages multiple GreenLuma profiles stored in `%AppData%\SolusManifestApp\greenluma_profiles.json`:
- Each profile tracks AppList entries separately
- Active profile ID stored in `ProfileData.ActiveProfileId`
- Switching profiles updates `AppList.txt` and triggers Steam library refresh via `LibraryRefreshService`

### File Installation Patterns
`FileInstallService` handles three file types:
1. **Manifest files** → `{steampath}/depotcache/{depotid}_{manifestid}.manifest`
2. **Lua scripts** → `{steampath}/config/stplug-in/AppList.txt` (GreenLuma mode)
3. **Zip archives** → Extracts manifests to depotcache, optionally creates ACF files for fake installs

**ACF Generation**: Creates fake `appmanifest_{appid}.acf` files to make games appear installed in Steam without actual files.

## Build & Development

### Build Commands
- **Build**: `dotnet build` (or run task `build` from tasks.json)
- **Publish**: `dotnet publish -c Release -r win-x64` (creates single-file executable)
- **Watch**: `dotnet watch run --project MorrenusApp.csproj` (hot reload during development)

### Embedded Resources
All files in `Resources/` and `lib/` are embedded as `EmbeddedResource` (see `.csproj`). Access via:
```csharp
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("SolusManifestApp.Resources.{path}");
```

Example: `TrayIconService` loads icon from embedded resource `"SolusManifestApp.icon.ico"`

### Release Process
Push a version tag (e.g., `git tag v2025.12.3.1` and `git push origin v2025.12.3.1`). GitHub Actions workflow (`.github/workflows/release.yml`) auto-builds and creates release with `SolusManifestApp.exe`.

**Version format**: `YYYY.MM.DD.Build` (e.g., `2025.11.24.03`) - stored in `.csproj` as `<Version>` property.

## Common Workflows

### Adding a New Service
1. Create service class in `Services/`
2. Define interface in `Interfaces/` if needed for testing
3. Register in `App.xaml.cs` DI container:
   ```csharp
   services.AddSingleton<MyService>();
   services.AddSingleton<IMyService>(sp => sp.GetRequiredService<MyService>());
   ```
4. Inject into ViewModels or other services via constructor

### Adding a New ViewModel Page
1. Create ViewModel in `ViewModels/` inheriting `ObservableObject`
2. Create View in `Views/` (UserControl)
3. Register ViewModel in `App.xaml.cs` (singleton for main pages, transient for dialogs)
4. Add navigation method in `MainViewModel`
5. Add navigation button/menu item in MainWindow.xaml

### Working with VDF Files
Use `VdfParser.Parse(filePath)` or `VdfParser.ParseContent(string)` to parse Steam's `.vdf`/`.acf` files:
```csharp
var data = VdfParser.Parse(manifestPath);
var appId = VdfParser.GetValue(data, "appid");
var name = VdfParser.GetValue(data, "name");
```

### Protocol Handler (`solus://`)
`ProtocolHandlerService` processes `solus://` URLs registered in Windows Registry. Format: `solus://install/{appid}` triggers automatic manifest download and installation. Registration happens in `ProtocolRegistrationHelper` on app startup.

## Testing & Debugging

**No formal test suite exists** - manual testing workflow:
1. Build with `dotnet build`
2. Run executable or use `dotnet run`
3. Test Steam integration by checking registry detection
4. Test depot downloads with valid API key from settings

**Common debug scenarios**:
- **Steam path not found**: Check Windows Registry, ensure Steam is installed
- **API errors**: Validate API key format (`smm` prefix), check network connectivity
- **Manifest parsing fails**: Use `VdfParser` with try-catch, Steam VDF format is strict about quotes and braces

## Important Conventions

### Error Handling
- Services should catch exceptions and return null/false rather than throwing
- ViewModels display errors via `NotificationService.ShowError()` or `MessageBoxHelper.Show()`
- Use `LoggerService` for diagnostic logging (writes to `%AppData%\SolusManifestApp\logs\`)

### Async Patterns
- All I/O operations are async (HTTP, file access, Steam connections)
- Use `async Task` for commands that do I/O
- Progress reporting via `IProgress<T>` passed to service methods

### Theme System
`ThemeService` dynamically loads XAML resource dictionaries from `Resources/Themes/`. 8 themes available (Default, Dark, Light, Cherry, Sunset, Forest, Grape, Cyberpunk). Apply via `ThemeService.ApplyTheme(theme)`.

### Notification System
Two notification types:
1. **Toast notifications**: `NotificationService` (Windows 10/11 native toasts via `Microsoft.Toolkit.Uwp.Notifications`)
2. **Message boxes**: `MessageBoxHelper.Show()` with `forceShow` parameter to override tray icon minimization

## External Tools Integration

### DepotDumper
Located in `Tools/DepotDumper/` - integrated tool for extracting depot keys from Steam. Requires 2FA authentication. Dialog shows QR code for Steam Guard.

### SteamAuth Pro
Located in `Tools/SteamAuthPro/` - generates encrypted Steam tickets. Uses P/Invoke to load `steam_api64.dll` from embedded resources (see `Services/GBE/SteamApi.cs`).

### Config VDF Key Extractor
Parses `config.vdf` to extract depot decryption keys. Uses `VdfParser` to navigate nested VDF structure to `Software > Valve > Steam > depots` section.

## Key Files Reference

- **App.xaml.cs**: DI container setup, single-instance logic, protocol handling, auto-update checks
- **MainViewModel.cs**: Navigation hub, view caching
- **SteamService.cs**: Steam installation detection and path management
- **ManifestApiService.cs**: Morrenus API client with caching
- **FileInstallService.cs**: File installation logic (manifests, Lua, ACF generation)
- **ProfileService.cs**: GreenLuma profile management
- **VdfParser.cs**: Steam VDF/ACF file parser
- **AppSettings.cs**: Complete settings schema

## Gotchas & Edge Cases

- **Steam must be restarted** after installing manifests/Lua scripts for changes to take effect
- **Single-file publish** includes all DLLs; `Resources/` and `lib/` folders are embedded, not extracted at runtime
- **VDF parsing** is case-insensitive but whitespace-sensitive - `VdfParser` handles both quoted and unquoted values
- **DepotDownloader integration** runs synchronously in background - wrap in `Task.Run()` for UI responsiveness
- **API key validation** checks `smm` prefix but doesn't verify format - actual validation happens on first API call
- **Tray icon** persists even when window is closed if `MinimizeToTray` is enabled - app only exits via tray menu or `Exit()` call
- **Profile switching** must call `LibraryRefreshService.RefreshLibrary()` to update Steam's game list cache

## Useful Context for AI Agents

When implementing features:
1. **Check settings first**: Most behavior is configurable in `AppSettings`
2. **Use DI**: Never `new` up services - always inject dependencies
3. **Follow MVVM**: Keep business logic in services, UI logic in ViewModels, zero code-behind in Views
4. **Cache aggressively**: Views, API responses (via `CacheService`), Steam paths all use caching
5. **Notify users**: Use `NotificationService` for background operations, `MessageBoxHelper` for blocking confirmations
6. **Log everything**: `LoggerService` writes to files - useful for debugging user issues
