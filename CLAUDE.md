# School Lunch Menu Calendar Generator

Dual-platform app (WPF .NET 10 + SwiftUI) that generates printable, allergen-aware school lunch calendars from LINQ Connect menu data. Both platforms follow MVVM architecture.

## Documentation

- `docs/PROJECT.md` — full architecture, file map, data flow, extension points (Windows-focused)
- `docs/DESIGN-DECISIONS.md` — rationale for all major design choices
- `docs/FEATURE-PARITY.md` — feature comparison between Windows and Mac platforms

## Mac (SwiftUI) Build Commands

Build (Debug):
```sh
xcodebuild -project school-lunch-menu.Mac/SchoolLunchMenu.xcodeproj \
  -scheme SchoolLunchMenu \
  -configuration Debug \
  -destination 'platform=macOS' \
  build
```

Build (Release) + Archive:
```sh
cd school-lunch-menu.Mac && \
xcodebuild -project SchoolLunchMenu.xcodeproj \
  -scheme SchoolLunchMenu \
  -configuration Release \
  -destination 'platform=macOS' \
  -archivePath build/SchoolLunchMenu.xcarchive \
  archive
```

Export archived app:
```sh
cd school-lunch-menu.Mac && \
xcodebuild -exportArchive \
  -archivePath build/SchoolLunchMenu.xcarchive \
  -exportPath build/export \
  -exportOptionsPlist ExportOptions.plist
```

Open in Xcode: `open school-lunch-menu.Mac/SchoolLunchMenu.xcodeproj`

Build output goes to `DerivedData` by default. Use `-derivedDataPath path/` to override.

**No test target exists for the Mac project.** This is a known gap tracked in `docs/FEATURE-PARITY.md`.

## Windows (.NET) Build Commands

Build:
```sh
dotnet build school-lunch-menu.Windows/SchoolLunchMenu.csproj
```

Test:
```sh
dotnet test tests/SchoolLunchMenu.Tests/SchoolLunchMenu.Tests.csproj
```

Publish (self-contained single file):
```sh
dotnet publish school-lunch-menu.Windows/SchoolLunchMenu.csproj \
  --self-contained -r win-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

**Note:** Tests target `net10.0-windows` and require Windows Desktop runtime. They will not run on macOS or Linux.

## Project Structure

```
school-lunch-menu.Mac/          SwiftUI macOS app
  SchoolLunchMenu/
    App/                        App entry point, BuildInfo
    Models/                     Domain models + API DTOs
    Services/                   API, analysis, HTML generation, settings, CMS fetch
    ViewModels/                 MainViewModel (@Observable, @MainActor)
    Views/                      SwiftUI views (Main, Sidebar, About)
    Resources/                  Assets (app icon)

school-lunch-menu.Windows/      WPF .NET 10 app
  Models/                       Domain models + API DTOs
  Services/                     Interface-based services (DI registered)
  ViewModels/                   MainViewModel (CommunityToolkit.Mvvm)
  Views/ (implied by xaml)      MainWindow, AboutWindow
  Assets/                       logo.ico, logo.svg

tests/                          .NET xUnit test suite
  SchoolLunchMenu.Tests/
    Services/                   MenuAnalyzer, CalendarHtmlGenerator, Settings, DayLabelFetch tests
    Models/                     ProcessedDay, CalendarTheme tests

docs/                           Project documentation
.github/workflows/              CI/CD (swift.yml, dotnet.yml)
```

## Architecture & Conventions

### Mac (SwiftUI)
- SwiftUI with `@Observable` macro and `@MainActor` for UI-bound state
- `async/await` for all async work
- `os.Logger` for logging (subsystem: `com.schoollunchmenu`)
- Services are plain Swift classes (no DI container — instantiated directly in the ViewModel)
- Build info injected via Xcode build phase (or CI sed replacement into `BuildInfo.swift`)

### Windows (WPF / .NET 10)
- `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`)
- `Microsoft.Extensions.DependencyInjection` for service registration in `App.xaml.cs`
- Serilog for file logging (14-day rolling)
- Typed `HttpClient` via `AddHttpClient<>`
- `Directory.Build.props` injects git commit/branch info for local dev builds

### Both Platforms
- MVVM pattern
- JSON settings persistence (debounced auto-save, 500ms)
- Self-contained HTML output (inline CSS, no external dependencies, no JavaScript)
- QR codes generated at runtime as base64 PNGs (QRCoder on .NET)
- Menu plan names discovered dynamically from API response (no hardcoded plan names)

## Settings & Data Paths

**Mac:** `~/Library/Application Support/SchoolLunchMenu/settings.json` and `menu-cache.json`

**Windows:** `settings.json` and `menu-cache.json` next to the executable

## CI/CD

| Workflow | File | Runner | Toolchain |
|---|---|---|---|
| Swift Build | `.github/workflows/swift.yml` | `macos-14` | Xcode 15.2 |
| .NET Build | `.github/workflows/dotnet.yml` | `windows-latest` | .NET 10 SDK |

Both workflows build Debug + Release configurations. The .NET workflow runs tests between build and publish. The Swift workflow archives and uploads the Release build as an artifact.

## Manual Testing Checklist

When making changes, verify these key features:

- [ ] API fetch: enter identifier code, look up district, select building, fetch menu
- [ ] HAR file loading: load a .har file as an alternative data source
- [ ] Allergen filtering: select allergens, verify unsafe items are flagged
- [ ] Not-preferred / favorites: mark items, verify visual treatment in calendar
- [ ] Theme switching: change themes, verify colors and emoji update
- [ ] Layout modes: test List, Icons Left, Icons Right
- [ ] Plan label editing: rename plan labels, verify in legend and calendar cells
- [ ] Day labels: configure rotating labels (Red/White), verify corner triangles
- [ ] Fetch from CMS: fetch day labels from ISD194 calendar
- [ ] Forced home days: toggle weekday checkboxes, verify home badge
- [ ] Cross out past days: enable, verify 3 PM cutoff logic
- [ ] Holiday icons: verify emoji on no-school days, test custom overrides
- [ ] Share footer: enable, verify dual QR codes appear below calendar
- [ ] About dialog: verify version, git commit, credits, clickable links
- [ ] Open in Browser: verify clean (no zoom) HTML opens in default browser
- [ ] Print: verify landscape layout, colors preserved, no page breaks mid-table
