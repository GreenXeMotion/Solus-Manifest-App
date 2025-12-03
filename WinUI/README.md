# WinUI 3 Migration

This directory contains the WinUI 3 version of Solus Manifest App.

## Project Structure

```
WinUI/
├── SolusManifestApp.sln              # Solution file
├── SolusManifestApp.WinUI/           # Main WinUI 3 application
│   ├── App.xaml / App.xaml.cs        # Application entry point
│   ├── MainWindow.xaml / .cs         # Main window with custom title bar
│   ├── app.manifest                  # Windows manifest for DPI awareness
│   └── Assets/                       # Images and resources
├── SolusManifestApp.Core/            # Framework-agnostic services & models
│   ├── Interfaces/                   # Service interfaces
│   ├── Services/                     # Core business logic
│   └── Models/                       # Data models
└── SolusManifestApp.ViewModels/      # Shared ViewModels using MVVM Toolkit
    └── *.cs                          # ViewModel classes
```

## Building

1. Open `SolusManifestApp.sln` in Visual Studio 2022
2. Select x64 or ARM64 platform
3. Build the solution (Ctrl+Shift+B)
4. Run the application (F5)

## Current Status

✅ **Phase 1 - Week 1 Complete:**
- WinUI 3 project structure created
- Core library with interfaces and basic services
- ViewModels library with MVVM Toolkit
- Solution file linking all projects
- Basic MainWindow with custom title bar

🚧 **Next Steps (Phase 1 - Week 2):**
- Implement WinUI-specific services (NotificationService, DialogService, TrayIconService)
- Migrate services from WPF version to Core library
- Set up theme system
- Implement navigation framework

## Dependencies

- .NET 8.0
- Windows App SDK 1.8
- WinUI 3
- CommunityToolkit.Mvvm 8.2.2
- H.NotifyIcon.WinUI 2.1.4
- SteamKit2 3.2.0

## Notes

- This is a gradual migration - the WPF version in the parent directory remains functional
- Service layer is framework-agnostic and shared between projects
- ViewModels use CommunityToolkit.Mvvm which works identically in WPF and WinUI 3
