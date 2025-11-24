# TODO List

## GBE Denuvo Tool - ✓ COMPLETED

### Credits - ✓ DONE
- [x] Add proper credits for the GBE Token Generator tool
- [x] Update the GBEDenuvoControl.xaml to include the credits section with proper attribution
  - Credits added for: Detanup01, NotAndreh, Oureverday

### Implementation Notes - ✓ DONE
- [x] The UI and basic structure have been implemented
- [x] The core token generation logic has been fully ported:
  - [x] Port GoldbergLogic.cs functionality
  - [x] Port SteamApi.cs wrapper
  - [x] Include necessary Steam API DLLs from the lib folder
  - [x] Include sound resources and dependencies
  - [x] Implement the actual token generation workflow

### Files Created
- Views/GBEDenuvoControl.xaml - UI for the GBE Token Generator with credits
- Views/GBEDenuvoControl.xaml.cs - Code-behind with ViewModel initialization
- ViewModels/GBEDenuvoViewModel.cs - ViewModel with full GoldbergLogic integration
- Services/GBE/SteamApi.cs - Steam API wrapper with P/Invoke declarations
- Services/GBE/GoldbergLogic.cs - Complete token generation logic
- lib/gbe/ - Steam API DLL files for runtime
- Resources/GBE/ - Embedded resources (dependencies, sounds)

### Location
- Integrated under Tools > Denuvo > GBE Token Generator tab

### How It Works
1. User enters Steam App ID and selects output directory
2. Tool connects to Steam API and requests encrypted app ticket
3. Generates complete Goldberg configuration files:
   - configs.user.ini (with ticket and Steam ID)
   - steam_appid.txt
   - achievements.json (with achievement icons)
   - depots.txt
   - configs.app.ini (DLC configuration)
   - supported_languages.txt
   - controller/controls.txt
   - configs.overlay.ini
   - configs.main.ini
4. Copies Steam API DLLs (steam_api64.dll, steamclient64.dll)
5. Extracts sound resources
6. Creates a ZIP archive: "Token [AppID].zip"

### Requirements
- Steam must be running and user must be logged in
- The logged-in Steam account must own the game
- Steam API DLLs are included in lib/gbe/ directory
