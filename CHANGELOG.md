# Changelog

## [v2025.11.24.01] - 2025-11-24

### 🎉 Major Release - UX Improvements & GBE Token Generator

This release focuses on improving user experience, fixing annoying notification spam, and adding the complete GBE Token Generator tool.

### ✨ New Features

#### GBE Token Generator Integration
- **NEW**: Complete GBE Token Generator tool integrated under Tools > Denuvo
- Generate Goldberg Emulator tokens directly from the app
- Automatic Steam ticket generation and configuration
- Fetches achievements, depots, DLCs, and language data from Steam APIs
- Creates complete token packages with all necessary files
- Real-time logging and progress feedback
- Credits: Detanup01, NotAndreh, Oureverday

### 🔧 Improvements

#### Store Tab Experience
- API key validation now only shows when clicking the Store tab (not on app launch)
- Much less annoying for users who don't use the API key feature
- Status message still shows in the Store tab when API key is missing

#### GreenLuma Mode
- Updated "Stealth (User32)" to just "Stealth" (following 006's DLL rename)
- Now uses Steam's `steam://uninstall/{appId}` protocol for uninstalling games
- Removed manual ACF file deletion - Steam handles cleanup properly now

#### Store Navigation
- Store listing now automatically scrolls to the top when paging through games
- No more staying stuck at the bottom when navigating pages

### 🐛 Bug Fixes

#### Auto-Update System
- **FIXED**: Notification spam on slow internet connections during updates
- Now shows only ONE notification when downloading updates
- Changed message to: "Downloading the latest version... This may take a few minutes."
- Removed progress reporting that was triggering hundreds of notifications on slow connections
- Auto-update now only shows notifications when updates are actually available
- No more "app is up to date" spam when auto-update is disabled

### 📝 Files Changed

**Modified:**
- App.xaml.cs - Fixed notification spam during updates
- ViewModels/StoreViewModel.cs - API key validation on navigation
- ViewModels/MainViewModel.cs - Store navigation handler
- ViewModels/SettingsViewModel.cs - Manual update check improvements
- ViewModels/HomeViewModel.cs - Updated mode descriptions
- Services/FileInstallService.cs - Steam uninstall protocol
- Views/StorePage.xaml - Named ScrollViewer
- Views/StorePage.xaml.cs - Scroll to top functionality
- Views/SettingsPage.xaml - Stealth mode rename
- Views/ToolsPage.xaml - Added GBE Token Generator tab

**Added:**
- Services/GBE/SteamApi.cs
- Services/GBE/GoldbergLogic.cs
- ViewModels/GBEDenuvoViewModel.cs
- Views/GBEDenuvoControl.xaml
- Views/GBEDenuvoControl.xaml.cs
- lib/gbe/ (Steam API DLLs)
- Resources/GBE/ (embedded resources)
- TODO.md

### 🙏 Credits

Special thanks to Detanup01, NotAndreh, and Oureverday for the GBE Token Generator implementation.

---

## [Unreleased]

### Added
- **Morrenus API Integration**: Switched from Steam API to Morrenus API for game data (better reliability and performance)
- **Library Pagination**: New pagination system with configurable page sizes (10, 20, 50, 100, or show all)
- **Image Caching System**: In-memory image caching for instant library image loading (ImageCacheService)
- **Auto-Update Manager**: New dialogs (UpdateEnablerDialog & UpdateDisablerDialog) for bulk auto-update management
- **Per-Game Auto-Update Toggle**: Enable/disable auto-updates for individual games from Library page
- **Pagination Navigation**: Previous/Next page buttons with page counter ("Page X of Y")
- **Library Page Size Setting**: Added to Settings page to control items per page
- **Null Value Converters**: Added InverseNullToVisibilityConverter for better handling of missing data
- **Better Status Messages**: Pagination info shows "Page X of Y: Showing N of M filtered items (Z total)"

### Changed
- **Library Layout**: Library cards now match Store card dimensions (280×350) for consistency
- **Library Card Structure**: Redesigned with proper 3-row Grid layout (Auto, *, Auto) for better content pinning
- **Image Display**: Reduced image size from 350×200 to 280×160 for memory optimization
- **Status Display**: Combined size and last updated date on single line in cards
- **Badge Positioning**: Type badges (ST/DD/GL) moved inside image container for proper positioning
- **Image Loading**: Added RenderOptions.BitmapScalingMode="LowQuality" for faster rendering
- **Library Default**: Library now displays 20 items per page by default for better performance
- **SteamAuth Pro UI**: Enhanced layout with better organization and spacing
- **Settings Page**: Reorganized with clearer sections and improved layout
- **Store Page**: Improved grid layout and card styling

### Fixed
- **Auto-Update Detection**: Now checks ALL setManifestid lines in lua files instead of just the first one
- **Fullscreen Pagination**: Pagination controls no longer hidden under taskbar in fullscreen/maximized mode
- **API Key Validation**: "Validate" button now properly validates API key instead of removing it
- **Version Display**: Now shows correct date-based version (2025.x) instead of hardcoded 1.0.0
- **Card Badge Overflow**: Type badges no longer go off-page in grid view
- **Image Placeholder**: Fixed visibility logic when no icon is available (proper MultiDataTrigger)
- **Store Page Layout**: Fixed grid spacing and card alignment issues
- **Theme Application**: Themes now apply correctly when changed in settings
- **Image Binding**: Added fallback binding chain (CachedBitmapImage → CachedIconPath → Placeholder)

### Performance
- **Dramatically Faster Library Loading**: Pagination reduces initial render time for large collections (100+ games)
- **Reduced Memory Usage**: Optimized image decoding at display size saves ~60% memory per image (~75KB per cached image)
- **Instant Image Loading**: Image cache enables instant loading after first view (7MB for 100 games)
- **Background Processing**: Async image loading and caching doesn't block UI thread
- **Smart Rendering**: Only renders visible page items instead of entire library (reduces WPF control overhead)
- **Cache Pre-loading**: Background pre-loading of all library images after database load

### Technical Details
- **New Files**:
  - `Services/ImageCacheService.cs` - In-memory BitmapImage caching with thread-safe operations
  - `Views/Dialogs/UpdateDisablerDialog.xaml(.cs)` - Batch auto-update disabler
  - `CHANGELOG.md` - This file

- **Major Refactors**:
  - `ViewModels/LibraryViewModel.cs` - Added pagination logic, image caching, auto-update controls
  - `Views/LibraryPage.xaml` - Complete redesign with pagination UI and optimized card layout
  - `Services/SteamApiService.cs` - Migrated to Morrenus API endpoints
  - `Services/LuaFileManager.cs` - Enhanced auto-update detection logic
  - `ViewModels/SettingsViewModel.cs` - Added LibraryPageSize property and logic
  - `Views/SettingsPage.xaml` - Added Library page size dropdown
  - `Models/LibraryItem.cs` - Added CachedBitmapImage property
  - `Models/AppSettings.cs` - Added LibraryPageSize (default: 20)

---

## Performance Tips
- **Large Libraries (100+ games)**: Use 20-50 items per page setting for best performance
- **Memory Usage**: Image cache uses ~7MB RAM for 100 games (optimized decoding at 280px width)
- **Loading Pattern**: First load caches all images in background, subsequent loads are instant
- **Show All Option**: Use "Show all" only for small libraries (<50 items) to avoid rendering overhead
