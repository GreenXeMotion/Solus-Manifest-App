# WinUI 3 Migration Plan for Solus Manifest App

**Document Version:** 1.1
**Created:** December 3, 2025
**Updated:** December 3, 2025 (Added CollapseLauncher reference)
**Project:** Solus Manifest App (WPF → WinUI 3)
**Current Framework:** .NET 8 WPF
**Target Framework:** .NET 8-10 WinUI 3

> **Note:** This plan is informed by real-world production WinUI 3 applications, specifically CollapseLauncher, a complex game launcher with AOT compilation, advanced optimizations, and Windows App SDK integration.

---

## Executive Summary

This document provides a comprehensive, phased migration plan to transition the Solus Manifest App from WPF to WinUI 3. The migration is **technically feasible** but requires significant effort due to fundamental architectural differences between WPF and WinUI 3. Estimated timeline: **8-12 weeks** for complete migration with testing.

### Key Benefits of Migration
- ✅ Modern Windows 11 native UI with Fluent Design
- ✅ Better performance and GPU acceleration
- ✅ Native support for modern Windows features (Mica, Acrylic)
- ✅ Future-proof technology stack (WPF is in maintenance mode)
- ✅ Better touch and pen input support

### Key Challenges
- ❌ Complete XAML rewrite required (30+ files)
- ❌ System.Windows.Forms dependencies (NotifyIcon, FolderBrowserDialog)
- ❌ Custom control templates incompatible
- ❌ Breaking changes in data binding syntax
- ❌ Theme system requires complete rewrite
- ❌ No direct WPF control equivalents for some features

---

## Table of Contents

1. [Architecture Analysis](#1-architecture-analysis)
2. [Breaking Changes Assessment](#2-breaking-changes-assessment)
3. [Migration Strategy](#3-migration-strategy)
4. [Phased Implementation Plan](#4-phased-implementation-plan)
5. [Technical Specifications](#5-technical-specifications)
6. [Risk Assessment](#6-risk-assessment)
7. [Testing Strategy](#7-testing-strategy)
8. [Rollback Plan](#8-rollback-plan)

---

## 1. Architecture Analysis

### 1.1 Current Project Structure

```
SolusManifestApp (WPF)
├── App.xaml / App.xaml.cs          ← Windows.Application
├── MorrenusApp.csproj              ← WPF SDK project
├── Views/                          ← 10 XAML pages, 10 dialogs
│   ├── MainWindow.xaml             ← Custom chrome, borderless window
│   ├── *Page.xaml (9 pages)
│   └── Dialogs/ (10 dialogs)
├── ViewModels/                     ← 10 ViewModels (MVVM)
├── Services/                       ← 28 services (DI-based)
├── Models/                         ← 9 model classes
├── Converters/                     ← 15 WPF value converters
├── Resources/
│   ├── Themes/ (8 themes)          ← ResourceDictionary-based
│   └── Styles/SteamTheme.xaml      ← Control templates
└── Helpers/                        ← 4 helper classes
```

### 1.2 Core Dependencies

| Package | Current | WinUI 3 Equivalent | Migration Complexity |
|---------|---------|-------------------|---------------------|
| `Microsoft.NET.Sdk` | WPF | `Microsoft.WindowsAppSDK` | **High** - Complete rewrite |
| `CommunityToolkit.Mvvm` | 8.2.2 | ✅ Same | **None** - Fully compatible |
| `Microsoft.Extensions.DependencyInjection` | 8.0.0 | ✅ Same | **None** - Fully compatible |
| `SteamKit2` | 3.2.0 | ✅ Same | **None** - Fully compatible |
| `Newtonsoft.Json` | 13.0.3 | ✅ Same | **None** - Fully compatible |
| `protobuf-net` | 3.2.52 | ✅ Same | **None** - Fully compatible |
| `QRCoder` | 1.6.0 | ✅ Same | **None** - Fully compatible |
| `Microsoft.Toolkit.Uwp.Notifications` | 7.1.3 | ⚠️ `Microsoft.Windows.AppNotifications` | **Medium** - API changes |
| `System.Windows.Forms` (NotifyIcon) | Built-in | ❌ No equivalent | **High** - Needs custom solution |

### 1.3 XAML Files Inventory

**Total XAML files requiring migration: 36**

#### Main Window & Pages (10 files)
1. `MainWindow.xaml` - Custom title bar, navigation frame
2. `HomePage.xaml` - Landing page with cards
3. `StorePage.xaml` - Complex grid/list view with pagination
4. `LibraryPage.xaml` - Game library with filtering
5. `LuaInstallerPage.xaml` - Drag-drop installer
6. `DownloadsPage.xaml` - Download queue manager
7. `ToolsPage.xaml` - Tool launcher page
8. `SettingsPage.xaml` - Settings form with hyperlinks
9. `SupportPage.xaml` - Support/about page
10. `GBEDenuvoControl.xaml` - Embedded control

#### Dialogs (10 files)
11. `CustomMessageBox.xaml`
12. `InputDialog.xaml`
13. `DepotSelectionDialog.xaml`
14. `ProfileManagerDialog.xaml`
15. `ProfileGamesDialog.xaml`
16. `ProfileSelectionDialog.xaml`
17. `LanguageSelectionDialog.xaml`
18. `UpdateEnablerDialog.xaml`
19. `UpdateDisablerDialog.xaml`
20. `TwoFactorDialog.xaml` (DepotDumper)

#### Tool Subpages (6 files)
21. `DepotDumperControl.xaml`
22. `ConfigVdfKeyExtractorControl.xaml`
23. `SteamAuthProControl.xaml`
24. `AboutWindow.xaml` (SteamAuthPro)
25. `GameSearchWindow.xaml` (SteamAuthPro)
26. `SettingsWindow.xaml` (SteamAuthPro)

#### Theme Resources (8 files)
27-34. Theme XAML files (DefaultTheme, DarkTheme, LightTheme, Cherry, Sunset, Forest, Grape, Cyberpunk)

#### Style Resources (2 files)
35. `SteamTheme.xaml` - Control templates & styles
36. `App.xaml` - Resource dictionaries

---

## 2. Breaking Changes Assessment

### 2.1 Critical Breaking Changes

#### 2.1.1 XAML Namespace Changes
```xml
<!-- WPF -->
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

<!-- WinUI 3 -->
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:muxc="using:Microsoft.UI.Xaml.Controls">
```

**Impact:** All 36 XAML files require namespace updates.

#### 2.1.2 Window Customization
**WPF:**
```csharp
WindowStyle = WindowStyle.None;
AllowsTransparency = true;
ResizeMode = ResizeMode.CanResize;
```

**WinUI 3:**
```csharp
// AppWindow API required
var appWindow = GetAppWindowForCurrentWindow();
appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
```

**Impact:** Complete `MainWindow` rewrite needed for custom title bar.

#### 2.1.3 System Tray (NotifyIcon)
**WPF:**
```csharp
using System.Windows.Forms; // Built-in
var notifyIcon = new NotifyIcon();
```

**WinUI 3:**
- ❌ No direct equivalent
- Requires COM interop or H.NotifyIcon.WinUI package
- Context menu requires separate implementation

**Impact:** `TrayIconService` requires complete rewrite (~300 lines).

#### 2.1.4 Dialogs & MessageBoxes
**WPF:**
```csharp
MessageBox.Show("Text", "Title", MessageBoxButton.YesNo);
```

**WinUI 3:**
```csharp
var dialog = new ContentDialog {
    Title = "Title",
    Content = "Text",
    PrimaryButtonText = "Yes",
    CloseButtonText = "No",
    XamlRoot = this.Content.XamlRoot
};
await dialog.ShowAsync();
```

**Impact:**
- `MessageBoxHelper` rewrite required
- All 10 custom dialogs need API changes
- Async/await required for all dialog calls

#### 2.1.5 Value Converters
**WPF:**
```csharp
public class BoolToVisibilityConverter : IValueConverter {
    public object Convert(...) => value ? Visibility.Visible : Visibility.Collapsed;
}
```

**WinUI 3:**
```csharp
// Must use Microsoft.UI.Xaml.Data.IValueConverter
public class BoolToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter {
    public object Convert(...) => (bool)value ? Visibility.Visible : Visibility.Collapsed;
}
```

**Impact:** All 15 converters in `ValueConverters.cs` need namespace changes.

#### 2.1.6 Resource Dictionary Loading
**WPF:**
```csharp
Application.Current.Resources.MergedDictionaries.Clear();
Application.Current.Resources.MergedDictionaries.Add(newTheme);
```

**WinUI 3:**
```csharp
// Similar but requires different threading model
DispatcherQueue.TryEnqueue(() => {
    Application.Current.Resources.MergedDictionaries.Clear();
    Application.Current.Resources.MergedDictionaries.Add(newTheme);
});
```

**Impact:** `ThemeService` needs async updates.

#### 2.1.7 Control Template Differences

| WPF Control | WinUI 3 Equivalent | Notes |
|-------------|-------------------|-------|
| `UserControl` | ✅ `UserControl` | Same |
| `Window` | ✅ `Window` | Different APIs |
| `ScrollViewer` | ✅ `ScrollViewer` | Same |
| `TextBox` | ✅ `TextBox` | Style properties differ |
| `ComboBox` | ✅ `ComboBox` | Template structure differs |
| `ProgressBar` | ✅ `ProgressBar` | Template differs |
| `Button` | ✅ `Button` | Style properties differ |
| `Hyperlink` | ⚠️ `HyperlinkButton` | Different element |
| `WrapPanel` | ❌ Not built-in | Requires CommunityToolkit.WinUI |

**Impact:** All control templates in `SteamTheme.xaml` need rewrite (~800 lines).

### 2.2 Medium Impact Changes

#### 2.2.1 DPI Awareness
**WPF:**
```csharp
SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
```

**WinUI 3:**
- Built-in per-monitor DPI handling
- No manual configuration needed

**Impact:** Remove DPI P/Invoke code from `App.xaml.cs`.

#### 2.2.2 Single Instance Management
**WPF:**
```csharp
// Custom named mutex implementation
```

**WinUI 3:**
```csharp
// Use AppInstance.GetCurrent() and Activated events
```

**Impact:** `SingleInstanceHelper` needs rewrite (~150 lines).

#### 2.2.3 File Pickers
**WPF:**
```csharp
var dialog = new System.Windows.Forms.FolderBrowserDialog();
dialog.ShowDialog();
```

**WinUI 3:**
```csharp
var picker = new Windows.Storage.Pickers.FolderPicker();
picker.FileTypeFilter.Add("*");
var folder = await picker.PickSingleFolderAsync();
```

**Impact:** All file/folder picker usages need async rewrite.

### 2.3 Low Impact Changes

#### 2.3.1 Services Layer
- ✅ All 28 services are framework-agnostic
- ✅ No changes needed for DI container setup
- ✅ SteamKit2, HTTP, and file operations unchanged

#### 2.3.2 ViewModels
- ✅ CommunityToolkit.Mvvm works identically
- ✅ `[ObservableProperty]` and `[RelayCommand]` unchanged
- ⚠️ Only need to update System.Windows references

#### 2.3.3 Models
- ✅ Completely framework-agnostic
- ✅ No changes required

---

## 3. Migration Strategy

### 3.1 Recommended Approach: **Gradual Parallel Development**

**Strategy:** Create a separate WinUI 3 project alongside WPF, sharing service/model layers.

#### Phase 1: Setup & Infrastructure (Week 1-2)
- Create new WinUI 3 project structure
- Configure shared projects for services/models/viewmodels
- Set up build configurations
- Implement basic window shell

#### Phase 2: Core UI Migration (Week 3-6)
- Migrate main pages one by one
- Convert dialogs
- Rebuild theme system
- Implement custom controls

#### Phase 3: Integration & Polish (Week 7-9)
- System tray implementation
- Protocol handler
- Auto-update integration
- Notification system

#### Phase 4: Testing & Validation (Week 10-12)
- Comprehensive testing
- Bug fixes
- Performance optimization
- Documentation

### 3.2 Alternative Approach: **Big Bang Migration**

**Not Recommended** due to:
- High risk of prolonged broken state
- Difficult to maintain both versions
- No fallback during development

---

## 4. Phased Implementation Plan

### Phase 1: Project Setup (Week 1-2)

#### Week 1: Solution Restructure
**Tasks:**
1. Create new WinUI 3 project
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <OutputType>WinExe</OutputType>
       <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
       <UseWinUI>true</UseWinUI>
       <Platforms>x64</Platforms>
     </PropertyGroup>
   </Project>
   ```

2. Restructure solution:
   ```
   SolusManifestApp.sln
   ├── SolusManifestApp.Core (Class Library)
   │   ├── Services/
   │   ├── Models/
   │   └── Interfaces/
   ├── SolusManifestApp.ViewModels (Class Library)
   │   └── ViewModels/
   ├── SolusManifestApp.WPF (WPF App) [Keep for now]
   └── SolusManifestApp.WinUI (WinUI 3 App) [New]
   ```

3. Move framework-agnostic code to Core:
   - All services (28 files)
   - All models (9 files)
   - All interfaces (6 files)
   - Helpers (VdfParser, ProtocolRegistrationHelper)

4. Move ViewModels to shared library:
   - Update System.Windows references to abstraction
   - Create IWindowService interface for window operations

**Deliverables:**
- ✅ WinUI 3 project compiles
- ✅ Core services accessible from both projects
- ✅ Basic app launches (empty window)

#### Week 2: Infrastructure Services
**Tasks:**
1. Implement WinUI 3-specific services:
   - `WinUINotificationService` (replace `NotificationService`)
   - `WinUITrayIconService` (H.NotifyIcon.WinUI wrapper)
   - `WinUIDialogService` (ContentDialog wrapper)
   - `WinUIThemeService` (WinUI 3 theme switching)

2. Create abstraction interfaces:
   ```csharp
   public interface INotificationService {
       void ShowNotification(string title, string message, NotificationType type);
   }

   public interface IDialogService {
       Task<DialogResult> ShowDialogAsync(string title, string message, DialogButtons buttons);
   }
   ```

3. Set up dependency injection for WinUI 3:
   ```csharp
   services.AddSingleton<INotificationService, WinUINotificationService>();
   services.AddSingleton<IDialogService, WinUIDialogService>();
   services.AddSingleton<ITrayIconService, WinUITrayIconService>();
   ```

**Deliverables:**
- ✅ All core services injected correctly
- ✅ Basic notification system working
- ✅ Dialog abstraction layer functional

### Phase 2: Theme System (Week 3)

**Tasks:**
1. Convert theme colors to WinUI 3 format:
   ```xml
   <!-- WPF: DefaultTheme.xaml -->
   <Color x:Key="PrimaryDark">#1b2838</Color>
   <SolidColorBrush x:Key="PrimaryDarkBrush" Color="{StaticResource PrimaryDark}"/>

   <!-- WinUI 3: Same structure works -->
   ```

2. Rebuild control styles:
   - Convert Button templates
   - Convert TextBox templates
   - Convert ComboBox templates
   - Convert ProgressBar templates
   - Convert ScrollBar templates

3. Implement dynamic theme switching:
   ```csharp
   public class WinUIThemeService {
       public void ApplyTheme(AppTheme theme) {
           var themeUri = new Uri($"ms-appx:///Resources/Themes/{GetFileName(theme)}");
           DispatcherQueue.TryEnqueue(() => {
               Application.Current.Resources.MergedDictionaries.Clear();
               var dict = new ResourceDictionary { Source = themeUri };
               Application.Current.Resources.MergedDictionaries.Add(dict);
           });
       }
   }
   ```

**Deliverables:**
- ✅ All 8 themes converted
- ✅ Theme switching functional
- ✅ Styles applied consistently

### Phase 3: Main Window & Navigation (Week 4)

**Tasks:**
1. Implement custom title bar:
   ```csharp
   public sealed partial class MainWindow : Window {
       public MainWindow() {
           InitializeComponent();

           // Extend into title bar
           var appWindow = GetAppWindowForCurrentWindow();
           appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
           appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
       }

       private AppWindow GetAppWindowForCurrentWindow() {
           var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
           var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
           return AppWindow.GetFromWindowId(windowId);
       }
   }
   ```

2. Create navigation shell:
   ```xml
   <NavigationView x:Name="NavView"
                   PaneDisplayMode="Left"
                   IsBackButtonVisible="Collapsed">
       <NavigationView.MenuItems>
           <NavigationViewItem Content="Home" Icon="Home"/>
           <NavigationViewItem Content="Store" Icon="Shop"/>
           <NavigationViewItem Content="Library" Icon="Library"/>
       </NavigationView.MenuItems>
       <Frame x:Name="ContentFrame"/>
   </NavigationView>
   ```

3. Implement cached navigation:
   ```csharp
   private readonly Dictionary<Type, Page> _pageCache = new();

   private void NavigateTo<T>() where T : Page, new() {
       if (!_pageCache.ContainsKey(typeof(T))) {
           _pageCache[typeof(T)] = new T();
       }
       ContentFrame.Content = _pageCache[typeof(T)];
   }
   ```

**Deliverables:**
- ✅ Custom title bar with window controls
- ✅ Navigation menu functional
- ✅ Page caching working

### Phase 4: Core Pages Migration (Week 5-6)

#### Week 5: Simple Pages
**Tasks:**
1. **HomePage** (simple layout, cards)
   - Convert StackPanels and Borders
   - Update button styles
   - Test launch commands

2. **SupportPage** (static content)
   - Convert hyperlinks to HyperlinkButton
   - Update text formatting

3. **DownloadsPage** (list view)
   - Convert ItemsControl to ListView
   - Update progress bar bindings
   - Test download queue display

**Estimated effort per page:** 4-6 hours

#### Week 6: Complex Pages
**Tasks:**
1. **StorePage** (grid/list toggle, pagination)
   - Convert WrapPanel to GridView/ListView toggle
   - Implement pagination controls
   - Update item templates
   - Test search and filtering

2. **LibraryPage** (similar complexity)
   - Convert item templates
   - Implement filtering UI
   - Test drag-drop interactions

3. **LuaInstallerPage** (drag-drop)
   - Implement WinUI 3 drag-drop:
     ```csharp
     DragArea.DragOver += (s, e) => {
         e.AcceptedOperation = DataPackageOperation.Copy;
     };
     DragArea.Drop += async (s, e) => {
         if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
             var items = await e.DataView.GetStorageItemsAsync();
             // Process files
         }
     };
     ```

**Estimated effort per complex page:** 8-12 hours

### Phase 5: Dialogs Migration (Week 7)

**Tasks:**
1. Convert each dialog to ContentDialog:
   ```csharp
   public sealed partial class ProfileManagerDialog : ContentDialog {
       public ProfileManagerDialog() {
           InitializeComponent();
           XamlRoot = App.MainWindow.Content.XamlRoot;
           PrimaryButtonText = "Save";
           CloseButtonText = "Cancel";
       }
   }
   ```

2. Update all dialog calls to async:
   ```csharp
   // Before (WPF)
   var dialog = new ProfileManagerDialog();
   var result = dialog.ShowDialog();

   // After (WinUI 3)
   var dialog = new ProfileManagerDialog();
   var result = await dialog.ShowAsync();
   ```

3. Dialogs to convert:
   - CustomMessageBox → ContentDialog with custom template
   - InputDialog → ContentDialog with TextBox
   - DepotSelectionDialog → ContentDialog with ListView
   - ProfileManagerDialog
   - ProfileGamesDialog
   - ProfileSelectionDialog
   - LanguageSelectionDialog
   - UpdateEnablerDialog
   - UpdateDisablerDialog
   - TwoFactorDialog

**Deliverables:**
- ✅ All 10 dialogs converted
- ✅ DialogService abstraction complete
- ✅ All dialog calls updated to async

### Phase 6: System Tray Integration (Week 8)

**Tasks:**
1. Add H.NotifyIcon.WinUI package:
   ```xml
   <PackageReference Include="H.NotifyIcon.WinUI" Version="2.0.131" />
   ```

2. Implement TrayIconService:
   ```csharp
   using H.NotifyIcon;

   public class WinUITrayIconService {
       private TaskbarIcon? _trayIcon;

       public void Initialize() {
           _trayIcon = new TaskbarIcon {
               IconSource = new BitmapImage(new Uri("ms-appx:///icon.ico")),
               ToolTipText = "Solus Manifest App"
           };

           var menu = new MenuFlyout();
           menu.Items.Add(new MenuFlyoutItem { Text = "Show", Command = ShowCommand });
           menu.Items.Add(new MenuFlyoutItem { Text = "Exit", Command = ExitCommand });

           _trayIcon.ContextFlyout = menu;
       }
   }
   ```

3. Implement dynamic menu updates:
   - Recent games list
   - Quick actions
   - Theme switcher

**Deliverables:**
- ✅ System tray icon visible
- ✅ Context menu functional
- ✅ Recent games displayed

### Phase 7: Tools Integration (Week 9)

**Tasks:**
1. **ToolsPage** - Launcher page for embedded tools

2. **DepotDumper** integration:
   - Convert DepotDumperControl.xaml
   - Update QR code display (use Microsoft.UI.Xaml.Controls.Image)
   - Test authentication flow

3. **ConfigVdfKeyExtractor** integration:
   - Convert ConfigVdfKeyExtractorControl.xaml
   - Test VDF parsing display

4. **SteamAuthPro** integration:
   - Convert SteamAuthProControl.xaml
   - Convert AboutWindow, GameSearchWindow, SettingsWindow
   - Test Steam API integration

**Deliverables:**
- ✅ All tool controls functional
- ✅ Tool windows display correctly
- ✅ Integration with services working

### Phase 8: Settings & Protocol Handler (Week 10)

**Tasks:**
1. **SettingsPage** migration:
   - Convert all ComboBoxes
   - Update file/folder pickers to async:
     ```csharp
     var picker = new Windows.Storage.Pickers.FolderPicker();
     picker.FileTypeFilter.Add("*");
     var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
     WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
     var folder = await picker.PickSingleFolderAsync();
     ```
   - Test all settings persistence

2. **Protocol Handler**:
   - Update registry registration for WinUI 3 executable
   - Test `solus://install/{appid}` URL handling

**Deliverables:**
- ✅ Settings page fully functional
- ✅ File pickers working
- ✅ Protocol handler registered

### Phase 9: Testing & Bug Fixes (Week 11)

**Tasks:**
1. **Functional Testing:**
   - Test all navigation flows
   - Test all CRUD operations
   - Test download functionality
   - Test installation flows
   - Test profile management

2. **Integration Testing:**
   - Test Steam detection and integration
   - Test API communication
   - Test file operations
   - Test manifest installation

3. **UI/UX Testing:**
   - Test all themes
   - Test window resizing
   - Test DPI scaling
   - Test accessibility

4. **Performance Testing:**
   - Profile app startup time
   - Test memory usage
   - Test large library handling

**Deliverables:**
- ✅ Bug tracking list created
- ✅ All critical bugs fixed
- ✅ Performance benchmarks met

### Phase 10: Polish & Release (Week 12)

**Tasks:**
1. **Final polish:**
   - Animation tuning
   - Visual refinements
   - Icon updates

2. **Documentation:**
   - Update README for WinUI 3
   - Migration notes for users
   - Known issues documentation

3. **Release preparation:**
   - Update CI/CD for WinUI 3 builds
   - Create installer package (MSIX)
   - Test MSIX installation

4. **Deprecation plan for WPF:**
   - Archive WPF project
   - Update download links
   - Announce transition

**Deliverables:**
- ✅ WinUI 3 app released
- ✅ MSIX package available
- ✅ Documentation updated

---

## 5. Technical Specifications

### 5.1 Project Configuration

#### WinUI 3 Project File (Based on CollapseLauncher Best Practices)

**Key Changes from Initial Plan:**
- Updated to latest Windows SDK (26100) for Windows 11 24H2
- Added AOT compilation support for 30-50% performance boost
- Included advanced trimming configuration
- Added security hardening options
- Configured for reproducible builds with package lock files

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- General Properties -->
    <OutputType>WinExe</OutputType>
    <ApplicationIcon>icon.ico</ApplicationIcon>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <Configurations>Debug;Release;Publish</Configurations>

    <!-- Assembly Info -->
    <AssemblyName>SolusManifestApp</AssemblyName>
    <ProductName>Solus Manifest App</ProductName>
    <Product>Solus Manifest App</Product>
    <Description>Steam Depot Manager &amp; Tools</Description>
    <Company>Solus</Company>
    <Authors>$(Company)</Authors>
    <Copyright>Copyright 2025 $(Company)</Copyright>

    <!-- Versioning -->
    <Version>3.0.0</Version>
    <LangVersion>preview</LangVersion>

    <!-- Target Settings (Use latest SDK for best performance) -->
    <Platforms>x64</Platforms>
    <TargetFramework>net8.0-windows10.0.26100.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <CsWinRTAotOptimizerEnabled>true</CsWinRTAotOptimizerEnabled>

    <!-- Debug Settings -->
    <DebugType>portable</DebugType>

    <!-- WinUI Properties -->
    <UseWinUI>true</UseWinUI>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>

    <!-- Analyzers Settings -->
    <EnableAOTAnalyzer>true</EnableAOTAnalyzer>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>

    <!-- Other Settings -->
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <EnablePreviewMsixTooling>true</EnablePreviewMsixTooling>
    <BuiltInComInteropSupport>false</BuiltInComInteropSupport>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <InvariantGlobalization>false</InvariantGlobalization>
    <ShouldComputeInputPris>true</ShouldComputeInputPris>
    <DisableImplicitNuGetFallbackFolder>true</DisableImplicitNuGetFallbackFolder>
    <GenerateNeutralResourcesLanguageAttribute>false</GenerateNeutralResourcesLanguageAttribute>
    <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
    <WebView2EnableCsWinRTProjection>true</WebView2EnableCsWinRTProjection>
  </PropertyGroup>

  <PropertyGroup>
    <NoWarn>$(NoWarn);TA101;TA100</NoWarn>
    <WarningLevel>5</WarningLevel>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>

  <!-- Configuration-specific settings -->
  <PropertyGroup Condition="'$(Configuration)'=='Debug'">
    <DefineConstants>DISABLE_XAML_GENERATED_MAIN;DEBUG</DefineConstants>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)'=='Release'">
    <DefineConstants>DISABLE_XAML_GENERATED_MAIN;RELEASE</DefineConstants>
    <Optimize>True</Optimize>
  </PropertyGroup>

  <!-- AOT Publishing Configuration (Optional but Recommended) -->
  <PropertyGroup Condition="'$(PublishAot)' == 'true'">
    <DefineConstants>$(DefineConstants);AOT</DefineConstants>
    <RunAOTCompilation>true</RunAOTCompilation>

    <!-- Compilation and Optimization -->
    <Deterministic>true</Deterministic>
    <CsWinRTAotOptimizerEnabled>true</CsWinRTAotOptimizerEnabled>
    <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
    <IlcDisableReflection>false</IlcDisableReflection>
    <OptimizationPreference>Speed</OptimizationPreference>
    <IlcTrimMetadata>true</IlcTrimMetadata>
    <TrimMetadata>true</TrimMetadata>
    <IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>
    <FoldIdenticalMethodBodies>true</FoldIdenticalMethodBodies>

    <!-- Debugging and Metadata -->
    <DebuggerSupport>false</DebuggerSupport>
    <MetadataUpdaterSupport>false</MetadataUpdaterSupport>
    <StackTraceSupport>true</StackTraceSupport>
    <TrimmerRemoveSymbols>true</TrimmerRemoveSymbols>

    <!-- Security and Resource Management -->
    <EnableUnsafeBinaryFormatterSerialization>false</EnableUnsafeBinaryFormatterSerialization>
    <EnableUnsafeUTF7Encoding>false</EnableUnsafeUTF7Encoding>
    <EventSourceSupport>false</EventSourceSupport>
    <HttpActivityPropagationSupport>false</HttpActivityPropagationSupport>
    <MetricsSupport>false</MetricsSupport>
    <UseSystemResourceKeys>false</UseSystemResourceKeys>

    <!-- Instruction Sets (x86-64-v2: SSE, SSE2, SSE3, SSSE3, SSE4.1, SSE4.2) -->
    <IlcInstructionSet>x86-64-v2</IlcInstructionSet>
  </PropertyGroup>

  <ItemGroup>
    <Manifest Include="$(ApplicationManifest)" />
  </ItemGroup>

  <ItemGroup>
    <!-- Windows App SDK (Latest stable version) -->
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251106002" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.6901" />
    <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.3.0-prerelease.250720.1" />

    <!-- Community Toolkit (Latest versions) -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="CommunityToolkit.Common" Version="8.4.0" />
    <PackageReference Include="CommunityToolkit.WinUI.Behaviors" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Controls.Primitives" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Media" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Converters" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Extensions" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Controls.Sizers" Version="8.2.250402" />

    <!-- Dependency Injection -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />

    <!-- Existing packages (unchanged) -->
    <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.9" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="protobuf-net" Version="3.2.52" />
    <PackageReference Include="QRCoder" Version="1.6.0" />
    <PackageReference Include="SteamKit2" Version="3.2.0" />
    <PackageReference Include="System.Text.Json" Version="10.0.0" />
    <PackageReference Include="System.Text.Encoding.CodePages" Version="10.0.0" />

    <!-- WinUI 3 specific -->
    <PackageReference Include="H.NotifyIcon.WinUI" Version="2.0.131" />
    <PackageReference Include="Microsoft.Graphics.Win2D" Version="1.3.2" />
    <PackageReference Include="Microsoft.Xaml.Behaviors.WinUI.Managed" Version="3.0.0" />

    <!-- Update Manager (Velopack recommended over Squirrel) -->
    <PackageReference Include="Velopack" Version="0.0.1298" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SolusManifestApp.Core\SolusManifestApp.Core.csproj" />
    <ProjectReference Include="..\SolusManifestApp.ViewModels\SolusManifestApp.ViewModels.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Update="Assets\**">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
    <Content Include="icon.ico">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <!-- Trimming Configuration -->
  <Target Name="ConfigureTrimming" BeforeTargets="PrepareForILLink">
    <ItemGroup>
      <!-- Descriptor for all classes that cannot be trimmed -->
      <TrimmerRootDescriptor Include="NonTrimmableRoots.xml" />
    </ItemGroup>
  </Target>

  <!-- Post-build cleanup for unnecessary files -->
  <Target Name="PostBuild" AfterTargets="Publish">
    <ItemGroup>
      <FilesToDelete Include="$(PublishDir)*.winmd;" />
      <FilesToDelete Include="$(PublishDir)*.xml;" />
      <FilesToDelete Include="$(PublishDir)Microsoft.Windows.Vision*.dll;" />
      <FilesToDelete Include="$(PublishDir)Microsoft.Windows.AI*.dll;" />
      <FilesToDelete Include="$(PublishDir)Microsoft.Windows.Widgets*.dll;" />
    </ItemGroup>
    <Delete Files="@(FilesToDelete)" />
  </Target>
</Project>
```

**Key Improvements from CollapseLauncher:**
1. **Latest SDK Targets**: Uses `net8.0-windows10.0.26100.0` for Windows 11 24H2 features
2. **AOT Compilation Support**: Optional but provides 30-50% performance improvement
3. **Advanced Optimizations**: `IlcOptimizationPreference`, `IlcFoldIdenticalMethodBodies`
4. **Security Hardening**: Disabled unsafe serialization and UTF7
5. **Trimming Configuration**: Reduces final binary size by 40-60%
6. **Post-Build Cleanup**: Removes unnecessary Windows SDK components (AI, Vision, Widgets DLLs)
7. **Package Lock File**: `RestorePackagesWithLockFile` for reproducible builds
8. **Latest Community Toolkit**: Version 8.4.0 with bug fixes and improvements
9. **Velopack Integration**: Modern replacement for Squirrel update framework

### 5.2 App.xaml Changes

#### WPF App.xaml
```xml
<Application x:Class="SolusManifestApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Themes/DefaultTheme.xaml"/>
                <ResourceDictionary Source="Resources/Styles/SteamTheme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

#### WinUI 3 App.xaml
```xml
<Application x:Class="SolusManifestApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <ResourceDictionary Source="Resources/Themes/DefaultTheme.xaml"/>
                <ResourceDictionary Source="Resources/Styles/SteamTheme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 5.3 App.xaml.cs Changes

#### Key differences:
```csharp
// WPF
public partial class App : Application {
    private readonly IHost _host;

    protected override async void OnStartup(StartupEventArgs e) {
        await _host.StartAsync();
        _mainWindow.Show();
    }
}

// WinUI 3
public partial class App : Application {
    private readonly IHost _host;
    private Window? _mainWindow;

    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        _host.Start();
        _mainWindow = _host.Services.GetRequiredService<MainWindow>();
        _mainWindow.Activate();
    }
}
```

### 5.4 MainWindow Migration

#### WPF MainWindow.xaml (Current)
```xml
<Window WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ResizeMode="CanResize">
    <Border BorderBrush="{DynamicResource BorderBrush}" BorderThickness="1">
        <Grid>
            <!-- Custom title bar -->
            <Grid Height="35" VerticalAlignment="Top">
                <TextBlock Text="Solus Manifest App"/>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <Button Content="—" Command="{Binding MinimizeCommand}"/>
                    <Button Content="□" Command="{Binding MaximizeCommand}"/>
                    <Button Content="✕" Command="{Binding CloseCommand}"/>
                </StackPanel>
            </Grid>
            <!-- Content -->
        </Grid>
    </Border>
</Window>
```

#### WinUI 3 MainWindow.xaml (Target)
```xml
<Window x:Class="SolusManifestApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:muxc="using:Microsoft.UI.Xaml.Controls">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="32"/> <!-- Title bar -->
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Custom title bar -->
        <Grid x:Name="AppTitleBar" Grid.Row="0">
            <Image Source="Assets/icon.png" Width="16" Height="16"
                   HorizontalAlignment="Left" Margin="12,0,0,0"/>
            <TextBlock Text="Solus Manifest App"
                       Style="{StaticResource CaptionTextBlockStyle}"
                       VerticalAlignment="Center" Margin="32,0,0,0"/>
        </Grid>

        <!-- Content -->
        <NavigationView x:Name="NavView" Grid.Row="1"
                        PaneDisplayMode="Left"
                        IsBackButtonVisible="Collapsed">
            <NavigationView.MenuItems>
                <NavigationViewItem Content="Home" Icon="Home" Tag="Home"/>
                <NavigationViewItem Content="Installer" Icon="Download" Tag="Installer"/>
                <NavigationViewItem Content="Library" Icon="Library" Tag="Library"/>
                <NavigationViewItem Content="Store" Icon="Shop" Tag="Store"/>
                <NavigationViewItem Content="Downloads" Icon="Download" Tag="Downloads"/>
                <NavigationViewItem Content="Tools" Icon="Repair" Tag="Tools"/>
            </NavigationView.MenuItems>

            <NavigationView.FooterMenuItems>
                <NavigationViewItem Content="Settings" Icon="Setting" Tag="Settings"/>
                <NavigationViewItem Content="Support" Icon="Help" Tag="Support"/>
            </NavigationView.FooterMenuItems>

            <Frame x:Name="ContentFrame"/>
        </NavigationView>
    </Grid>
</Window>
```

#### WinUI 3 MainWindow.xaml.cs
```csharp
public sealed partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Configure navigation
        NavView.SelectionChanged += NavView_SelectionChanged;
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender,
                                         NavigationViewSelectionChangedEventArgs args) {
        if (args.SelectedItemContainer != null) {
            var tag = args.SelectedItemContainer.Tag.ToString();
            NavigateToPage(tag);
        }
    }
}
```

### 5.5 Value Converter Migration

#### Before (WPF)
```csharp
using System.Windows;
using System.Windows.Data;

public class BoolToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }
}
```

#### After (WinUI 3)
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

public class BoolToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }
}
```

**Changes needed:**
- Namespace: `System.Windows.Data` → `Microsoft.UI.Xaml.Data`
- Parameter: `CultureInfo culture` → `string language`

### 5.6 NonTrimmableRoots.xml Configuration

**Important:** Create `NonTrimmableRoots.xml` in the project root to prevent aggressive trimming (inspired by CollapseLauncher's approach):

```xml
<linker>
  <!-- Preserve all types in the main assembly -->
  <assembly fullname="SolusManifestApp" preserve="all" />

  <!-- Preserve ViewModels (required for XAML binding) -->
  <assembly fullname="SolusManifestApp.ViewModels" preserve="all" />

  <!-- Preserve types used via reflection -->
  <type fullname="SolusManifestApp.Services.*" preserve="all" />
  <type fullname="SolusManifestApp.Models.*" preserve="all" />

  <!-- Preserve CommunityToolkit types -->
  <assembly fullname="CommunityToolkit.Mvvm" preserve="all" />
  <assembly fullname="CommunityToolkit.WinUI.*" preserve="all" />

  <!-- Preserve SteamKit2 (uses reflection) -->
  <assembly fullname="SteamKit2" preserve="all" />
</linker>
```

**Note:** CollapseLauncher uses this pattern to prevent issues with XAML binding and reflection-based APIs. Start conservative, then optimize later.

### 5.7 App Manifest Configuration

Create `app.manifest` for proper Windows integration (based on CollapseLauncher):

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="3.0.0.0" name="SolusManifestApp.app"/>

  <!-- Enable Windows 10/11 compatibility -->
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <!-- Windows 10 and Windows 11 -->
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>

  <!-- DPI Awareness (automatic in WinUI 3, but explicit is better) -->
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">permonitorv2,permonitor</dpiAwareness>
    </windowsSettings>
  </application>

  <!-- Request administrator execution level if needed for Steam integration -->
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

### 5.8 Package Publishing (MSIX)

WinUI 3 apps should be packaged as MSIX for deployment:

```xml
<!-- Package.appxmanifest -->
<Package>
  <Identity Name="SolusManifestApp"
            Publisher="CN=YourPublisher"
            Version="2025.12.3.0" />

  <Properties>
    <DisplayName>Solus Manifest App</DisplayName>
    <PublisherDisplayName>Your Name</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Applications>
    <Application Id="App" Executable="SolusManifestApp.exe"
                 EntryPoint="SolusManifestApp.App">
      <uap:VisualElements DisplayName="Solus Manifest App"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png"
                          Description="Steam Depot Manager"
                          BackgroundColor="transparent">
      </uap:VisualElements>

      <Extensions>
        <uap:Extension Category="windows.protocol">
          <uap:Protocol Name="solus"/>
        </uap:Extension>
      </Extensions>
    </Application>
  </Applications>
</Package>
```

---

## 6. Risk Assessment

### 6.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| System tray integration issues | **High** | **High** | Use proven H.NotifyIcon.WinUI library, extensive testing |
| Performance degradation | **Medium** | **Medium** | Profile early, optimize hot paths |
| XAML conversion errors | **High** | **Medium** | Incremental migration, thorough testing |
| Async/await cascade | **Medium** | **Low** | Update all dialog calls systematically |
| Theme switching breaks | **Low** | **Low** | Test each theme individually |
| File picker async issues | **Medium** | **Medium** | Wrap in try-catch, provide fallbacks |

### 6.2 Schedule Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Underestimated XAML conversion | **High** | **High** | Add 20% buffer to timeline |
| Tool integration complexity | **Medium** | **Medium** | Prioritize core functionality first |
| Testing bottleneck | **Medium** | **High** | Automated UI testing, parallel testing |
| Third-party package issues | **Low** | **High** | Verify package compatibility early |

### 6.3 User Impact Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking changes in settings | **Low** | **Medium** | Settings migration tool |
| Different UI feels "wrong" | **Medium** | **Low** | Beta testing, gather feedback |
| Loss of features | **Low** | **High** | Feature parity checklist |
| Installation issues (MSIX) | **Medium** | **High** | Provide portable alternative |

---

## 7. Testing Strategy

### 7.1 Unit Testing
- Test all service layer functions
- Test ViewModels in isolation
- Mock UI interactions

### 7.2 Integration Testing
- Test navigation flows
- Test Steam integration
- Test API communication
- Test file operations

### 7.3 UI Testing
```csharp
[TestMethod]
public async Task StorePage_Search_DisplaysResults() {
    var page = new StorePage();
    var viewModel = new StoreViewModel(/* dependencies */);
    page.DataContext = viewModel;

    viewModel.SearchQuery = "Portal";
    await viewModel.SearchGamesCommand.ExecuteAsync(null);

    Assert.IsTrue(viewModel.Games.Count > 0);
}
```

### 7.4 Manual Testing Checklist
- [ ] All pages navigate correctly
- [ ] All dialogs display properly
- [ ] All themes apply correctly
- [ ] System tray menu works
- [ ] Protocol handler responds
- [ ] File pickers function
- [ ] Downloads complete successfully
- [ ] Installations work properly
- [ ] Settings persist correctly
- [ ] Window resizing works
- [ ] DPI scaling correct
- [ ] Accessibility features work

---

## 8. Rollback Plan

### 8.1 Version Control Strategy
- Maintain WPF in `main` branch
- Develop WinUI 3 in `feature/winui3-migration` branch
- Keep both branches buildable during migration

### 8.2 Dual Release Strategy
- Release WinUI 3 version as v3.0.0
- Maintain WPF version as v2.x (security fixes only)
- Allow users to choose version for 6 months

### 8.3 Rollback Triggers
- Critical bugs affecting >25% of users
- Performance degradation >50%
- Loss of critical functionality
- MSIX deployment failures >10%

---

## 9. Cost-Benefit Analysis

### 9.1 Development Cost
| Item | Estimated Hours | Cost (@ $50/hr) |
|------|----------------|-----------------|
| Planning & Setup | 40 | $2,000 |
| XAML Conversion | 120 | $6,000 |
| Service Refactoring | 40 | $2,000 |
| Dialog Migration | 30 | $1,500 |
| Testing | 60 | $3,000 |
| Bug Fixes | 40 | $2,000 |
| Documentation | 20 | $1,000 |
| **Total** | **350** | **$17,500** |

### 9.2 Benefits
- ✅ **Future-proof:** WinUI 3 is actively developed, WPF is maintenance-only
- ✅ **Performance:** GPU acceleration, better rendering (30-50% faster with AOT)
- ✅ **Modern UI:** Fluent Design, native Windows 11 look with Mica/Acrylic
- ✅ **Features:** Mica, Acrylic, better animations, native backdrop effects
- ✅ **Accessibility:** Better screen reader support, improved keyboard navigation
- ✅ **Touch:** Native touch and pen support with better gesture recognition
- ✅ **Binary Size:** Trimming can reduce size by 40-60% (CollapseLauncher achieves 50%+)
- ✅ **Startup Time:** AOT compilation reduces cold start by 2-3x
- ✅ **Native AOT:** Optional native compilation for maximum performance
- ✅ **Better Packaging:** MSIX provides better install/uninstall experience

### 9.3 Opportunity Cost
- ⚠️ **3 months** of halted feature development
- ⚠️ **Distraction** from core product improvements
- ⚠️ **Learning curve** for development team

---

## 10. Recommendations

### 10.1 Should You Migrate?

**YES, if:**
- ✅ You want to modernize the app for Windows 11
- ✅ You have 3 months of development time available
- ✅ You want to leverage future Windows features
- ✅ You're okay with MSIX packaging

**NO, if:**
- ❌ WPF version is working perfectly
- ❌ You need rapid feature development
- ❌ Team lacks WinUI 3 experience
- ❌ Budget is constrained (<$15k)

### 10.2 Recommended Timeline

**Conservative Estimate:** 12 weeks (3 months)
- Assumes 1 full-time developer
- Includes buffer for unexpected issues
- Includes comprehensive testing

**Aggressive Estimate:** 8 weeks (2 months)
- Assumes 2 developers working in parallel
- Requires WinUI 3 experience
- Higher risk of bugs

### 10.3 Success Criteria

Migration is successful when:
- ✅ 100% feature parity with WPF version
- ✅ All automated tests passing
- ✅ Manual testing checklist complete
- ✅ Performance equal or better than WPF
- ✅ Zero critical bugs in production
- ✅ User satisfaction maintained

---

## 11. Next Steps

### Immediate Actions (This Week)
1. **Decision:** Management approval for migration
2. **Budget:** Allocate $15-20k for development
3. **Timeline:** Block 12 weeks on roadmap
4. **Team:** Assign 1-2 developers
5. **Training:** WinUI 3 tutorials and documentation

### Phase 1 Kickoff (Week 1)
1. Create feature branch: `feature/winui3-migration`
2. Set up WinUI 3 project structure
3. Create shared Core/ViewModels projects
4. Configure build system
5. First commit: "WinUI 3 migration - initial setup"

### Weekly Check-ins
- Monday: Sprint planning, task assignment
- Wednesday: Mid-week progress review
- Friday: Demo to stakeholders, retrospective

---

## 12. References & Resources

### Official Documentation
- [WinUI 3 Migration Guide](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [WPF to WinUI 3 Differences](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-migrate)

### Libraries
- [H.NotifyIcon.WinUI](https://github.com/HavenDV/H.NotifyIcon) - System tray for WinUI 3
- [CommunityToolkit.WinUI](https://learn.microsoft.com/en-us/windows/communitytoolkit/windows/) - Additional controls
- [WinUIEx](https://github.com/dotMorten/WinUIEx) - Extended window features
- [Velopack](https://github.com/velopack/velopack) - Modern update framework (recommended over Squirrel)

### Sample Projects
- [WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery) - Official samples
- [Template Studio](https://github.com/microsoft/TemplateStudio) - Project templates
- **[CollapseLauncher](https://github.com/CollapseLauncher/Collapse) - Production WinUI 3 game launcher** ⭐
  - AOT compilation examples
  - Advanced trimming configuration
  - Complex navigation patterns
  - Native library integration

### Performance & Optimization
- [.NET Native AOT Guide](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trimming Options](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
- [WinUI 3 Performance Best Practices](https://learn.microsoft.com/en-us/windows/apps/performance/)

---

## Appendix A: File-by-File Migration Checklist

### XAML Files
- [ ] App.xaml
- [ ] MainWindow.xaml
- [ ] HomePage.xaml
- [ ] StorePage.xaml
- [ ] LibraryPage.xaml
- [ ] LuaInstallerPage.xaml
- [ ] DownloadsPage.xaml
- [ ] ToolsPage.xaml
- [ ] SettingsPage.xaml
- [ ] SupportPage.xaml
- [ ] GBEDenuvoControl.xaml
- [ ] CustomMessageBox.xaml
- [ ] InputDialog.xaml
- [ ] DepotSelectionDialog.xaml
- [ ] ProfileManagerDialog.xaml
- [ ] ProfileGamesDialog.xaml
- [ ] ProfileSelectionDialog.xaml
- [ ] LanguageSelectionDialog.xaml
- [ ] UpdateEnablerDialog.xaml
- [ ] UpdateDisablerDialog.xaml
- [ ] TwoFactorDialog.xaml
- [ ] DepotDumperControl.xaml
- [ ] ConfigVdfKeyExtractorControl.xaml
- [ ] SteamAuthProControl.xaml
- [ ] AboutWindow.xaml
- [ ] GameSearchWindow.xaml
- [ ] SettingsWindow.xaml
- [ ] DefaultTheme.xaml
- [ ] DarkTheme.xaml
- [ ] LightTheme.xaml
- [ ] CherryTheme.xaml
- [ ] SunsetTheme.xaml
- [ ] ForestTheme.xaml
- [ ] GrapeTheme.xaml
- [ ] CyberpunkTheme.xaml
- [ ] SteamTheme.xaml

### Code Files
- [ ] App.xaml.cs
- [ ] NotificationService.cs
- [ ] TrayIconService.cs
- [ ] ThemeService.cs
- [ ] MessageBoxHelper.cs
- [ ] SingleInstanceHelper.cs
- [ ] ValueConverters.cs
- [ ] All ViewModels (10 files) - Update System.Windows references

---

## Appendix B: Testing Scenarios

### Smoke Tests
1. App launches without crash
2. Main window displays correctly
3. Navigation between pages works
4. Settings persist on restart
5. Downloads can be initiated
6. System tray icon appears

### Regression Tests
1. Install .lua file → Verify in stplug-in folder
2. Install .manifest file → Verify in depotcache folder
3. Install .zip archive → Verify extraction and installation
4. Search games → Verify results display
5. Download game → Verify download completes
6. Switch theme → Verify all UI updates
7. Create profile → Verify in profiles.json
8. Switch profile → Verify AppList.txt updated
9. Protocol handler → Verify `solus://install/480` works
10. Auto-update → Verify update detection and download

---

## Appendix C: Known Issues & Workarounds

### Issue 1: WrapPanel Not Available
**Problem:** WinUI 3 doesn't have WrapPanel built-in
**Workaround:** Use CommunityToolkit.WinUI.UI.Controls.WrapPanel

### Issue 2: Hyperlink Different Behavior
**Problem:** WPF Hyperlink doesn't exist in WinUI 3
**Workaround:** Use HyperlinkButton instead

### Issue 3: System.Windows.Forms Dependencies
**Problem:** NotifyIcon, FolderBrowserDialog not available
**Workaround:** H.NotifyIcon.WinUI for tray, Windows.Storage.Pickers for files

### Issue 4: AllowsTransparency Not Supported
**Problem:** Transparent windows not supported in WinUI 3
**Workaround:** Use Mica or Acrylic backdrop materials

### Issue 5: Pack URIs Different
**Problem:** `pack://application:,,,/` syntax doesn't work
**Workaround:** Use `ms-appx:///` for embedded resources

---

**END OF MIGRATION PLAN**

---

*This document should be treated as a living document and updated as the migration progresses. Any deviations from the plan should be documented with rationale.*
