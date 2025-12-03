# Next Steps Checklist - Phase 1 Week 2

## Overview
Implement WinUI 3-specific services and begin migrating core functionality from the WPF version.

---

## Priority 1: WinUI-Specific Services

### Task 1.1: Implement WinUIDialogService
**Location:** `SolusManifestApp.WinUI/Services/WinUIDialogService.cs`

**Interface to implement:**
```csharp
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message, string title);
    Task ShowMessageAsync(string message, string title);
    Task<string?> ShowInputAsync(string message, string title, string? defaultValue = null);
}
```

**Key WinUI 3 API:**
- Use `ContentDialog` instead of MessageBox
- Requires `XamlRoot` reference from current window
- All methods are async

**Example:**
```csharp
var dialog = new ContentDialog
{
    Title = title,
    Content = message,
    CloseButtonText = "Cancel",
    PrimaryButtonText = "OK",
    XamlRoot = GetXamlRoot()
};
var result = await dialog.ShowAsync();
return result == ContentDialogResult.Primary;
```

---

### Task 1.2: Implement WinUINotificationService
**Location:** `SolusManifestApp.WinUI/Services/WinUINotificationService.cs`

**Interface to implement:**
```csharp
public interface INotificationService
{
    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
}
```

**Key WinUI 3 API:**
- Use `Windows.UI.Notifications` for toast notifications
- Or use `Microsoft.Windows.AppNotifications` (Windows App SDK)
- Requires app registration in manifest

**Package needed:**
```xml
<PackageReference Include="Microsoft.Windows.AppNotifications" Version="1.1.4" />
```

---

### Task 1.3: Implement WinUITrayIconService
**Location:** `SolusManifestApp.WinUI/Services/WinUITrayIconService.cs`

**Use H.NotifyIcon.WinUI** (already installed)

**Features needed:**
- Show/hide tray icon
- Context menu with recent games
- Click to show/hide window
- Exit command

**Reference from WPF version:**
- Original: `Services/TrayIconService.cs` (~300 lines)
- Convert System.Windows.Forms.NotifyIcon → H.NotifyIcon.WinUI

---

## Priority 2: Copy Core Services from WPF

### Task 2.1: Copy Framework-Agnostic Services
**Copy these files from WPF to Core:**

```
Source: ../Services/
Target: SolusManifestApp.Core/Services/

Files to copy:
- ✅ SettingsService.cs (already created)
- ⬜ SteamService.cs (needs ISteamService interface)
- ⬜ ManifestApiService.cs
- ⬜ DepotDownloadService.cs
- ⬜ CacheService.cs
- ⬜ LoggerService.cs
- ⬜ ProfileService.cs
- ⬜ LibraryRefreshService.cs
- ⬜ SteamGamesService.cs
- ⬜ SteamLibraryService.cs
- ⬜ FileInstallService.cs
- ⬜ DownloadService.cs
- ⬜ BackupService.cs
- ⬜ ImageCacheService.cs
- ⬜ ArchiveExtractionService.cs
- ⬜ DepotFilterService.cs
- ⬜ RecentGamesService.cs
- ⬜ LibraryDatabaseService.cs
- ⬜ LuaFileManager.cs
- ⬜ LuaParser.cs
- ⬜ ConfigKeysUploadService.cs
- ⬜ SteamApiService.cs
- ⬜ SteamCmdApiService.cs
- ⬜ SteamKitAppInfoService.cs
- ⬜ DepotDownloaderWrapperService.cs
```

**Note:** Most services are already framework-agnostic. Just copy and adjust namespaces.

---

### Task 2.2: Copy Helper Classes
**Copy these from WPF to Core:**

```
Source: ../Helpers/
Target: SolusManifestApp.Core/Helpers/

Files to copy:
- ⬜ VdfParser.cs (Steam VDF file parser)
- ⬜ ProtocolRegistrationHelper.cs (might need platform-specific adjustments)
```

**Note:** `SingleInstanceHelper` needs WinUI-specific implementation.

---

### Task 2.3: Copy Models
**Copy these from WPF to Core:**

```
Source: ../Models/
Target: SolusManifestApp.Core/Models/

Files to copy:
- ✅ AppSettings.cs (placeholder created, needs full copy)
- ⬜ DownloadItem.cs
- ⬜ Game.cs
- ⬜ GameStatus.cs
- ⬜ GreenLumaGame.cs
- ⬜ GreenLumaProfile.cs
- ⬜ LibraryItem.cs
- ⬜ LibraryResponse.cs
- ⬜ Manifest.cs
- ⬜ SteamGame.cs
```

---

## Priority 3: Update Dependency Injection

### Task 3.1: Register Services in App.xaml.cs
**Location:** `SolusManifestApp.WinUI/App.xaml.cs`

**Add to ConfigureServices method:**

```csharp
// WinUI-specific services
services.AddSingleton<IDialogService, WinUIDialogService>();
services.AddSingleton<INotificationService, WinUINotificationService>();
services.AddSingleton<ITrayIconService, WinUITrayIconService>();

// Core services (once copied)
services.AddSingleton<ISteamService, SteamService>();
services.AddSingleton<IManifestApiService, ManifestApiService>();
services.AddSingleton<ISettingsService, SettingsService>();
// ... etc for all 28 services

// ViewModels
services.AddSingleton<MainViewModel>();
services.AddTransient<HomeViewModel>();
services.AddTransient<StoreViewModel>();
services.AddTransient<LibraryViewModel>();
// ... etc for all ViewModels
```

---

## Priority 4: Basic Navigation Implementation

### Task 4.1: Create HomePage
**Location:** `SolusManifestApp.WinUI/Views/HomePage.xaml`

**Features:**
- Welcome message
- Quick action cards (Launch Steam, Refresh Library, etc.)
- Recent games list (if available)

**Bind to:** `HomeViewModel`

---

### Task 4.2: Update MainWindow for Navigation
**Location:** `SolusManifestApp.WinUI/MainWindow.xaml`

**Changes needed:**
1. Add NavigationView control
2. Add Frame for page content
3. Wire up navigation items
4. Implement page caching (like WPF version)

**Example structure:**
```xml
<NavigationView>
    <NavigationView.MenuItems>
        <NavigationViewItem Icon="Home" Content="Home" />
        <NavigationViewItem Icon="Shop" Content="Store" />
        <NavigationViewItem Icon="Library" Content="Library" />
        <NavigationViewItem Icon="Setting" Content="Settings" />
    </NavigationView.MenuItems>

    <Frame x:Name="ContentFrame"/>
</NavigationView>
```

---

## Priority 5: Theme Foundation

### Task 5.1: Create Default Theme
**Location:** `SolusManifestApp.WinUI/Resources/Themes/DefaultTheme.xaml`

**Convert from WPF:** `../Resources/Themes/DefaultTheme.xaml`

**Key changes:**
- WPF `Color` attributes → WinUI 3 `Color` attributes (same format)
- Control styles need complete rewrite
- Focus on colors first, styles later

---

### Task 5.2: Implement Theme Service
**Location:** `SolusManifestApp.WinUI/Services/ThemeService.cs`

**Interface:**
```csharp
public interface IThemeService
{
    void ApplyTheme(string themeName);
    string[] GetAvailableThemes();
    string CurrentTheme { get; }
}
```

**Features:**
- Load ResourceDictionary from Themes folder
- Apply to Application.Resources.MergedDictionaries
- Persist theme choice in settings

---

## Estimated Time

| Task | Estimated Hours |
|------|----------------|
| WinUI-specific services | 8h |
| Copy core services | 4h |
| Copy helpers & models | 2h |
| Update DI container | 2h |
| Basic navigation | 6h |
| Theme foundation | 6h |
| Testing & debugging | 8h |
| **Total** | **36h** |

---

## Success Criteria

- [ ] App launches and shows MainWindow
- [ ] Custom title bar displays correctly
- [ ] Basic navigation works (Home, Settings)
- [ ] HomePage displays withViewModel binding
- [ ] Settings can be loaded and saved
- [ ] At least one theme can be applied
- [ ] Notifications display (toast or dialog)
- [ ] No build errors or warnings

---

## Notes

- Focus on getting one feature working end-to-end first
- Test frequently as you build
- Use the WPF version as reference but don't copy blindly
- WinUI 3 APIs are often similar but not identical
- Async/await is required for many WinUI operations

---

**When complete, move to:** Phase 2 - Theme System Migration
