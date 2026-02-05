# School Lunch Menu Calendar Generator

[![.NET Build](https://github.com/YOUR_USERNAME/school-lunch-menu/actions/workflows/dotnet.yml/badge.svg)](https://github.com/YOUR_USERNAME/school-lunch-menu/actions/workflows/dotnet.yml)
[![Swift Build](https://github.com/YOUR_USERNAME/school-lunch-menu/actions/workflows/swift.yml/badge.svg)](https://github.com/YOUR_USERNAME/school-lunch-menu/actions/workflows/swift.yml)

A multi-platform desktop application that generates allergen-aware, printable lunch calendars for any school in the LINQ Connect system. Available for both Windows (WPF/.NET) and macOS (SwiftUI), it fetches menu data from the LINQ Connect API, analyzes each entree for allergen safety, and produces a color-coded HTML calendar designed for easy scanning and printing. Defaults are pre-configured for Lakeville Area Schools.

## Features

- **Live API and offline HAR file support** -- fetch menus directly from the LINQ Connect API, or load a previously captured HAR file for offline use.
- **17 allergen filters** -- select any combination of allergens; entrees containing them are flagged with strikethrough text.
- **ADHD-friendly color-coded calendar** -- per-plan color-coded badges, "From Home" badges, gray no-school days, favorite highlighting. High-contrast colors chosen for quick visual scanning.
- **21 visual themes** -- seasonal, fun, and basic themes with auto-suggestion by month. Themes can be hidden via settings.
- **Three calendar layout modes** -- List (classic stacked badges), Icons Left, or Icons Right. Grid modes show per-plan icon buttons alongside food items in a CSS Grid layout with tinted row backgrounds. Per-plan emoji icons, short labels (click-to-edit), and display order are user-customizable. Optionally show grayed-out buttons for unsafe plan lines with a customizable message.
- **Customizable holiday icons** -- configure emoji and optional messages for no-school days by keyword. Defaults pre-populated from common US holidays.
- **Cross out past days** -- optional faded overlay with a subtle X on past days, like crossing off a wall calendar. Days are marked past only after 3 PM (end of school day).
- **Rotating day labels** -- configurable corner triangles for alternating schedules (Red/White Day, Day A/B). Labels cycle across school days, skipping no-school days. Corner position is customizable.
- **Share link on calendar** -- optional footer with two QR codes: one linking to the school's LINQ Connect menu page and one to the project GitHub page, both embedded as self-contained base64 PNGs.
- **Source link** -- "View Source" button in the status bar opens the LINQ Connect public menu page for your school in the default browser.
- **Preview zoom** -- adjustable +/- zoom in the status bar (5% increments, default 75%). Zoom applies only to the in-app preview; the saved HTML file and browser view are unaffected.
- **Printable landscape HTML** -- self-contained HTML with `@page landscape`, `print-color-adjust: exact`, and no external dependencies. Print directly from the browser.
- **Forced home days** -- per-session configurable weekdays that always show a "From Home" badge, regardless of safe options.
- **Persisted settings** -- selected allergens, forced home days, not-preferred/favorite foods, theme, layout mode, plan icons/order, and holiday overrides are saved to a JSON file and restored on next launch.
- **Dynamic school support** -- enter any LINQ Connect identifier code to look up a district and its buildings. All menu plans are discovered dynamically.

## Screenshots

<!-- Screenshot placeholder: add screenshots of the generated calendar here -->
<!-- ![Windows screenshot](docs/screenshot-windows.png) -->
<!-- ![macOS screenshot](docs/screenshot-macos.png) -->

## Getting Started

### Prerequisites

**Windows:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS, supported through November 2028)
- Windows 10 or later

**macOS:**
- macOS 13 (Ventura) or later
- Xcode 15+ with Swift 5.9+

### Building the Windows Version

```bash
# Build
dotnet build school-lunch-menu.Windows/SchoolLunchMenu.csproj

# Run
dotnet run --project school-lunch-menu.Windows/SchoolLunchMenu.csproj

# Publish self-contained executable
dotnet publish school-lunch-menu.Windows/SchoolLunchMenu.csproj --configuration Release --self-contained -r win-x64 -p:PublishSingleFile=true
```

### Building the macOS Version

```bash
# Build from command line
cd school-lunch-menu.Mac
xcodebuild -project SchoolLunchMenu.xcodeproj -scheme SchoolLunchMenu -configuration Release build

# Or open in Xcode
open school-lunch-menu.Mac/SchoolLunchMenu.xcodeproj
```

Then build and run using Xcode (Cmd+R).

## Usage

1. Enter an **identifier code** and click **Look Up** to find your school district.
2. Select a **building** from the dropdown.
3. Select a **month** and **year** from the dropdowns.
4. Click **Fetch from API** to download menu data from LINQ Connect, or click **Load from HAR** to load a previously saved `.har` file.
5. Check the **allergens** you want to filter (Milk is selected by default).
6. Optionally mark foods as **not preferred** or **favorites**.
7. Configure **forced home days** (default: Thursday) per serving session.
8. Optionally change **Day Layout** to "Icons Left" or "Icons Right" to see per-line icon buttons. Set custom emoji icons, click to edit short labels, and reorder plans.
9. Optionally customize **holiday icons** in the Holiday Icons section.
10. Optionally check **Cross out past days** to fade and X-out past dates.
11. Optionally check **Share link on calendar** to add a QR code footer linking to the project page.
12. Optionally expand **Day Labels** to set up rotating labels (e.g., Red Day / White Day). Add labels with colors, choose a corner position, and optionally set a start date.
13. Click **Generate Calendar** to produce the HTML calendar. It's displayed in the preview pane (use +/- to zoom) and saved to a temporary directory.
14. Click **Open in Browser** to launch in your default browser, or **View Source** in the status bar to see the LINQ Connect public menu page.
15. Print the page in landscape orientation from the browser.

## Configuration

Settings are persisted to a JSON file and restored on next launch:

- **Windows:** `%APPDATA%\SchoolLunchMenu\settings.json`
- **macOS:** `~/Library/Application Support/SchoolLunchMenu/settings.json`

### Settings Structure

```json
{
  "identifierCode": "string",
  "selectedBuildingId": "string",
  "selectedAllergens": ["Milk", "Eggs", "Peanuts"],
  "notPreferredFoods": ["food1", "food2"],
  "favoriteFoods": ["food3"],
  "selectedTheme": "Default",
  "layoutMode": "List",
  "forcedHomeDays": {
    "Lunch": ["Thursday"]
  },
  "planSettings": {
    "PlanName": {
      "icon": "emoji",
      "shortLabel": "Label",
      "displayOrder": 0
    }
  },
  "holidayIcons": {
    "Christmas": { "icon": "tree-emoji", "message": "Happy Holidays!" }
  },
  "crossOutPastDays": false,
  "showShareLink": false,
  "dayLabels": [],
  "dayLabelCorner": "TopLeft"
}
```

## Development

### Repository Structure

```
school-lunch-menu/
├── .github/
│   └── workflows/
│       ├── dotnet.yml           # Windows CI/CD pipeline
│       └── swift.yml            # macOS CI/CD pipeline
├── school-lunch-menu.Windows/   # Windows WPF application (.NET 10)
│   ├── App.xaml.cs              # DI container setup, Serilog configuration
│   ├── MainWindow.xaml          # WPF layout with sidebar and WebBrowser preview
│   ├── ViewModels/              # MainViewModel (MVVM with CommunityToolkit)
│   ├── Models/                  # ProcessedDay, ProcessedMonth, RecipeItem, AppSettings
│   ├── Models/Api/              # LINQ Connect API response DTOs
│   ├── Services/                # API client, HAR parser, menu analyzer, HTML generator
│   └── Converters/              # WPF value converters
├── school-lunch-menu.Mac/       # macOS SwiftUI application
│   ├── SchoolLunchMenu.xcodeproj
│   └── SchoolLunchMenu/
│       ├── App/                 # SwiftUI app entry point
│       ├── Views/               # SwiftUI views (MainView, SidebarView, PreviewView)
│       ├── ViewModels/          # MainViewModel with @Observable
│       ├── Models/              # Swift model types matching Windows DTOs
│       ├── Models/Api/          # LINQ Connect API response Codable types
│       ├── Services/            # API client, HAR parser, menu analyzer, HTML generator
│       └── Resources/           # Asset catalogs
├── docs/                        # Project documentation
├── SchoolLunchMenu.slnx         # Visual Studio solution file
└── README.md
```

### Architecture Overview

Both platforms follow the MVVM (Model-View-ViewModel) pattern:

- **Models** define data structures for API responses, processed menu data, and application settings
- **ViewModels** contain business logic, state management, and commands
- **Views** handle UI rendering and user interaction
- **Services** encapsulate external concerns (API calls, file I/O, HTML generation)

The Windows version uses:
- WPF with XAML for UI
- CommunityToolkit.Mvvm for MVVM infrastructure
- Microsoft.Extensions.DependencyInjection for DI
- Serilog for logging

The macOS version uses:
- SwiftUI with the Observation framework
- Native Swift Codable for JSON serialization
- URLSession for networking
- WebKit for HTML preview

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request
