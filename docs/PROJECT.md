# Project Overview (AI Agent Onboarding)

This document is intended for AI agents and new contributors. It describes the architecture, file layout, data flow, and extension points of the School Lunch Menu Calendar Generator.

## Architecture

The application follows **WPF + MVVM + Dependency Injection**:

- **View**: `MainWindow.xaml` / `MainWindow.xaml.cs` -- WPF window with a settings sidebar and a `WebBrowser` preview pane.
- **ViewModel**: `MainViewModel` -- orchestrates data fetching, analysis, and generation. Uses `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
- **Services**: interface-based services registered in `App.xaml.cs` via `Microsoft.Extensions.DependencyInjection`.
- **Models**: immutable records for API responses and processed output.

## Technology Stack

| Component | Package / Version |
|---|---|
| Runtime | .NET 10 (`net10.0-windows`), C# 14 |
| UI framework | WPF |
| MVVM toolkit | CommunityToolkit.Mvvm 8.x |
| Dependency injection | Microsoft.Extensions.DependencyInjection 10.x |
| HTTP | Microsoft.Extensions.Http 10.x (typed `HttpClient`) |
| Logging | Serilog.Extensions.Logging 9.x, Serilog.Sinks.File 7.x |
| QR code generation | QRCoder 1.x |
| Serialization | System.Text.Json (built-in) |

## File Map

### Entry Point
| File | Purpose |
|---|---|
| `App.xaml.cs` | Configures DI container, Serilog file logging (14-day rolling), registers all services and views. HTTP clients: `LinqConnectApiService`, `DayLabelFetchService`. |

### Views
| File | Purpose |
|---|---|
| `MainWindow.xaml` | WPF layout: DockPanel with Help menu bar, 300px left sidebar (school lookup, month/year selectors, session selector, categorized theme dropdown with headers, collapsible allergen filter, not-preferred/favorite checklists, forced home days, collapsible holiday icons editor, day layout mode selector with show-unsafe-lines option, plan icons/reorder/edit-mode labels, cross-out past days checkbox, share link on calendar checkbox, collapsible day labels editor with corner/start-date/entries/Fetch from CMS button, action buttons), right pane (WebBrowser preview, status bar with +/− zoom buttons, Open in Browser button, and View Source button). Default size 1100×800, opens maximized. App icon via `Icon="Assets/logo.ico"`. |
| `MainWindow.xaml.cs` | Code-behind: wires DataContext, navigates WebBrowser on HTML change, handles drag-drop for HAR files, plan label TextBox auto-focus/select-all on edit mode enter, Enter key to confirm label edit, About click handler |
| `AboutWindow.xaml` | About dialog: app icon, name, version, git commit/branch info, credits (LINQ Connect, ISD 194 CMS Calendar, GitHub), MIT license mention, clickable hyperlinks, OK button |
| `AboutWindow.xaml.cs` | Code-behind: parses `AssemblyInformationalVersionAttribute` for version/commit/branch, hyperlink navigation via `Process.Start` |
| `Converters/BoolToVisibilityConverter.cs` | `bool` to `Visibility`: true=Visible, false=Collapsed |
| `Converters/BoolToCollapsedConverter.cs` | Inverse `bool` to `Visibility`: true=Collapsed, false=Visible (used for view/edit mode toggle) |
| `Converters/NullToVisibilityConverter.cs` | `null` to `Visible`/`Collapsed` for preview placeholder vs. WebBrowser |

### View Models
| File | Purpose |
|---|---|
| `ViewModels/MainViewModel.cs` | Coordinates fetch/load/generate workflow. Contains helper types: `AllergenOption`, `MonthOption`, `NotPreferredOption`, `FavoriteOption`, `ThemeListItem`, `ForcedHomeDayOption`, `HolidayOverrideEntry`, `PlanLabelEntry` (with view/edit mode, display label, icon), `LayoutModeOption`, `DayLabelEntry`. Manages categorized theme list with headers, hidden theme filtering, theme selection, allergen expander state, allergen-based recipe filtering, per-session forced home days, layout mode selection (List/IconsLeft/IconsRight), plan icon editing, plan label edit/reset commands, plan reorder commands, show unsafe lines toggle, cross-out past days toggle (3 PM cutoff via `GetPastDayCutoff()`), share footer toggle, day labels enable/disable toggle, rotating day label cycle editing (add/remove entries, start date, corner position, CMS fetch via `FetchDayLabelsCommand`), configurable User-Agent (Firefox default, auto-populated from HAR), preview zoom (+/− commands, preview-only injection), holiday override editing, district name persistence, source link, and debounced settings persistence with flush-on-session-switch (uses `ConfigureAwait(false)` to avoid deadlock). |

### Models -- API DTOs
| File | Purpose |
|---|---|
| `Models/Api/FamilyMenuIdentifierResponse.cs` | District info, buildings, menu identifier code |
| `Models/Api/FamilyAllergyResponse.cs` | `AllergyItem` record: AllergyId (UUID), Name, SortOrder |
| `Models/Api/FamilyMenuResponse.cs` | Full menu tree: `FamilyMenuResponse` > `MenuSession` > `MenuPlan` > `MenuDay` > `MenuMeal` > `RecipeCategory` > `Recipe` > `Nutrient`. Also `AcademicCalendar` > `AcademicCalendarDay`. |
| `Models/Api/FamilyMenuMealsResponse.cs` | `MealItem` record: MealId, Name, SortOrder |

### Models -- Application
| File | Purpose |
|---|---|
| `Models/ProcessedDay.cs` | One analyzed school day: date, list of `ProcessedLine` (one per menu plan), academic note. Computed properties: `IsNoSchool`, `HasSpecialNote`, `AnyLineSafe`, `HasMenu`. Also defines `ProcessedLine`: plan name, safety flag, entrees. |
| `Models/ProcessedMonth.cs` | Collection of `ProcessedDay` for a calendar month, with `BuildingName`, `SessionName`, and computed `DisplayName`. |
| `Models/RecipeItem.cs` | `record RecipeItem(string Name, bool ContainsAllergen, bool IsNotPreferred, bool IsFavorite)` |
| `Models/CalendarTheme.cs` | `CalendarTheme` record with color/emoji/category properties for themed calendar output. `CalendarThemes.All` provides 21 built-in themes across 3 categories (Seasonal, Fun, Basic) with optional month auto-suggestion. |
| `Models/AppSettings.cs` | Persisted settings: selected allergen IDs, per-session forced home days, per-session not-preferred/favorites, school identifiers, district name, selected theme name, hidden theme names, layout mode (`LayoutMode`), plan label overrides, plan icon overrides (`PlanIconOverrides`), plan display order (`PlanDisplayOrder`), holiday overrides, `CrossOutPastDays`, `ShowShareFooter`, `DayLabelsEnabled`/`DayLabelCycle`/`DayLabelStartDate`/`DayLabelCorner` for rotating day labels, `UserAgent` for configurable HTTP User-Agent. Legacy `ShowMealButtons` kept for migration. Also defines `HolidayOverride` and `DayLabel` classes. |
| `Models/MenuCache.cs` | Disk cache for menu response, allergen list, and identifier response with timestamp |

### Services
| File | Purpose |
|---|---|
| `Services/ILinqConnectApiService.cs` | Interface for LINQ Connect API calls |
| `Services/LinqConnectApiService.cs` | HTTP client calling 4 API endpoints with 1-hour in-memory response caching. Date format: `M-d-yyyy`. |
| `Services/IHarFileService.cs` | Interface for HAR file loading |
| `Services/HarFileService.cs` | Parses `FamilyMenu`, `FamilyAllergy`, and `FamilyMenuIdentifier` responses from HAR JSON entries. Extracts User-Agent from the first linqconnect.com request headers. |
| `Services/IMenuAnalyzer.cs` | Interface for menu analysis |
| `Services/MenuAnalyzer.cs` | Finds the selected session, iterates all menu plans dynamically, extracts entree-category recipes, checks allergen UUIDs, and applies not-preferred/favorite flags |
| `Services/ICalendarHtmlGenerator.cs` | Interface for HTML generation. Also defines `CalendarRenderOptions` with layout mode, plan label/icon overrides, plan display order, show-unsafe-lines option, unsafe line message, home badge color (set internally from theme), holiday overrides, cross-out past days toggle with today's date, rotating day label cycle with anchor date and corner position, share footer toggle, and source URL for QR code. |
| `Services/CalendarHtmlGenerator.cs` | Builds self-contained HTML with themed inline CSS, per-plan color-coded badges, Monday-Friday calendar grid, favorite highlighting, configurable holiday no-school emoji (vertically centered), 🏠 "From Home" badges (forced home days + no safe options), three layout modes (List/IconsLeft/IconsRight grid with tinted row backgrounds), optional unsafe line display, past-day overlay (opacity fade + X), rotating corner triangle day labels, optional share footer with dual QR codes (source menu + GitHub, generated via QRCoder as base64 PNGs), and print styles. Zoom is not embedded -- applied externally by the view model for preview only. |
| `Services/IDayLabelFetchService.cs` | Interface for fetching day labels from external calendar. Also defines `DayLabelFetchResult`. |
| `Services/DayLabelFetchService.cs` | Scrapes Red/White Day entries from the ISD194 Finalsite CMS calendar HTML using source-generated regex. Typed `HttpClient` via DI. |
| `Services/ISettingsService.cs` | Interface for settings and cache persistence |
| `Services/SettingsService.cs` | Reads/writes `settings.json` and `menu-cache.json` next to the executable |

### Tests
| File | Purpose |
|---|---|
| `tests/SchoolLunchMenu.Tests/SchoolLunchMenu.Tests.csproj` | xUnit test project with FluentAssertions, NSubstitute. Targets `net10.0-windows`. |
| `tests/SchoolLunchMenu.Tests/Services/MenuAnalyzerTests.cs` | Tests for `MenuAnalyzer`: safe/unsafe entrees, allergen flagging, not-preferred, favorites, no-school days, multi-plan, session matching |
| `tests/SchoolLunchMenu.Tests/Services/CalendarHtmlGeneratorTests.cs` | Tests for `CalendarHtmlGenerator`: month title, safe items, favorites, home badge, themes, grid mode, past days, day labels, share footer, self-contained HTML |
| `tests/SchoolLunchMenu.Tests/Services/SettingsServiceTests.cs` | Tests for `SettingsService`: default load, round-trip save/load, default values |
| `tests/SchoolLunchMenu.Tests/Services/DayLabelFetchServiceTests.cs` | Tests for `DayLabelFetchService`: regex parsing of CMS HTML, label pattern matching, 0-indexed months |
| `tests/SchoolLunchMenu.Tests/Models/ProcessedDayTests.cs` | Tests for `ProcessedDay`: IsNoSchool, HasSpecialNote, AnyLineSafe, HasMenu computed properties |
| `tests/SchoolLunchMenu.Tests/Models/CalendarThemeTests.cs` | Tests for `CalendarThemes`: 21 themes, categories, auto-suggestion, required properties |

### Assets
| File | Purpose |
|---|---|
| `Assets/logo.svg` | Application logo SVG source: lunch tray/plate with calendar grid overlay |
| `Assets/logo.ico` | Application icon (ICO format, 16/32/48px) used for window title bar and taskbar |

## Data Flow

```
1. User enters identifier code and clicks "Look Up"
   LinqConnectApiService.GetMenuIdentifierAsync()   --> FamilyMenuIdentifierResponse
   LinqConnectApiService.GetAllergiesAsync()         --> List<AllergyItem>
   User selects building and clicks "Fetch from API"
   LinqConnectApiService.GetMenuAsync()              --> FamilyMenuResponse

   -- OR --

   User clicks "Load from HAR" (or drags HAR file onto window)
   HarFileService.LoadFromHarFileAsync()             --> (FamilyMenuResponse, List<AllergyItem>, FamilyMenuIdentifierResponse)

   -- OR --

   On startup, TryLoadMenuCacheAsync() preloads from disk cache

2. User configures: allergens, not-preferred foods, favorites, theme (categorized dropdown), forced home days

3. User clicks "Generate Calendar"
   MenuAnalyzer.Analyze(menuResponse, allergenIds, notPreferred, favorites, year, month, session, building)
       --> ProcessedMonth (list of ProcessedDay, each with per-plan ProcessedLines)

4. CalendarHtmlGenerator.Generate(processedMonth, allergenNames, forcedHomeDays, theme, renderOptions)
       --> Self-contained themed HTML string with:
           - Configurable holiday-specific emoji on no-school days (vertically centered, user overrides checked first)
           - 🏠 "From Home" badges (on forced home days or when no safe options)
           - Three layout modes: List (stacked badges), IconsLeft/IconsRight (CSS grid with icon buttons)
           - Optional past-day overlay (faded + X) when CrossOutPastDays is enabled
           - Optional rotating corner triangle day labels (cycle assigned across school days, skipping no-school)
           - Optional share footer with dual QR codes (source menu page + GitHub project)

5. HTML is saved to %TEMP%/SchoolLunchMenu/ (clean, no zoom) and displayed in the WPF WebBrowser
   control with preview zoom injected (CSS zoom on body tag, default 75%). Zoom is adjustable via
   +/− buttons without regenerating. User can click "Open in Browser" to launch the clean file.
```

## Settings Persistence

Settings are saved to `settings.json` with 500ms debounce on any change:

| Setting | Description |
|---|---|
| `SelectedAllergenIds` | List of allergen UUIDs |
| `ForcedHomeDaysBySession` | Per-session map of weekday names forced as "from home" days (default: Thursday) |
| `NotPreferredBySession` | Per-session map of not-preferred recipe names |
| `FavoritesBySession` | Per-session map of favorite recipe names |
| `Identifier` / `DistrictId` / `BuildingId` | School lookup state |
| `SelectedSessionName` | Last selected serving session |
| `SelectedThemeName` | Last selected calendar theme name |
| `HiddenThemeNames` | List of theme names to hide from dropdown (edit settings.json directly) |
| `LayoutMode` | Calendar layout mode: `"List"`, `"IconsLeft"`, or `"IconsRight"` |
| `PlanLabelOverrides` | Custom short labels for plan line names |
| `PlanIconOverrides` | Custom emoji icons per plan line |
| `PlanDisplayOrder` | User-defined display order for plan lines |
| `ShowUnsafeLines` | Whether to show grayed-out buttons for unsafe plan lines (grid mode) |
| `UnsafeLineMessage` | Custom message shown next to grayed-out unsafe line buttons (default: "No safe options") |
| `HolidayOverrides` | Custom emoji and messages for no-school day keywords (editable in UI or settings.json) |
| `CrossOutPastDays` | Whether to fade and X-out past days on the calendar |
| `DayLabelsEnabled` | Whether to show day label triangles on the calendar (default: true) |
| `DayLabelCycle` | List of `{ Label, Color }` entries defining the rotating day label cycle |
| `DayLabelStartDate` | Anchor date for the day label cycle (M/d/yyyy, null = first school day) |
| `DayLabelCorner` | Corner for the day label triangle: `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft` |
| `ShowShareFooter` | Whether to append a share footer with dual QR codes to the calendar |
| `DistrictName` | Persisted district display name for immediate display on launch |
| `UserAgent` | Custom User-Agent string for HTTP requests (null = Firefox default, auto-populated from HAR) |

Menu data is cached separately in `menu-cache.json` for instant startup preloading.

## Calendar Themes

The `CalendarTheme` record defines visual properties (header colors, title color, safe item color, favorite star/border/background, home badge color, accent borders, body background, optional CSS cell pattern, category). The `CalendarThemes.All` list provides 21 built-in themes in 3 categories:

### Seasonal
| Theme | Emoji | Auto-suggest Month |
|---|---|---|
| New Year | 🎆 | January |
| Valentines | 💕 | February |
| St. Patrick's | ☘️ | March |
| Easter | 🐣 | April |
| Spring | 🌸 | May (secondary) |
| Summer | ☀️ | June |
| Fourth of July | 🎆 | July |
| Back to School | 🎒 | August |
| Fall | 🍂 | September |
| Spooky | 🎃 | October |
| Thanksgiving | 🦃 | November |
| Winter | ❄️ | December |

### Fun
| Theme | Emoji |
|---|---|
| Cardinal | 🐦 |
| Blue Jay | 🐦‍⬛ |
| Cats | 🐱 |
| Dinosaurs | 🦕 |
| Princess | 👑 |
| Robots | 🤖 |
| Unicorn | 🦄 |

### Basic
| Theme | Emoji |
|---|---|
| Default | 📅 |

The theme dropdown displays items grouped by category with non-selectable header rows. When the user changes the month selector and hasn't manually picked a theme, the app auto-suggests a matching seasonal theme. Themes can be hidden by adding their names to `HiddenThemeNames` in `settings.json`.

## How to Extend

### Adding a new data source
1. Create a new service implementing the relevant interface (or a new interface if the data shape differs).
2. Register it in `App.xaml.cs` `ConfigureServices`.
3. Add a new command in `MainViewModel` to load data from the source.
4. The rest of the pipeline (analyzer, generator) works unchanged as long as you produce a `FamilyMenuResponse`.

### Adding a new output format
1. Create a new generator service (e.g., `ICalendarPdfGenerator`).
2. Implement it to consume `ProcessedMonth` and produce the desired output.
3. Wire it into `MainViewModel` with a new command.

### Modifying allergen analysis logic
- `MenuAnalyzer` controls which recipe categories are examined. Currently it only checks categories where `IsEntree == true`.
- Allergen matching is a simple UUID set intersection: `recipe.Allergens.Any(a => allergenIds.Contains(a))`.
- To add severity levels or cross-contamination logic, extend `RecipeItem` and update the analyzer.

### Adding a new school
- The application now supports dynamic school lookup via identifier codes. No code changes needed -- just enter a different identifier.
- Menu plan names are discovered dynamically from the API response (no hardcoded plan name prefixes).

### Adding a new calendar theme
1. Add a new `CalendarTheme` entry to `CalendarThemes.All` in `Models/CalendarTheme.cs`.
2. Set `Category` to "Seasonal", "Fun", or "Basic".
3. Set `SuggestedMonth` / `SuggestedMonth2` if the theme should auto-suggest for specific months.
4. The theme will automatically appear in the categorized UI dropdown with no other changes needed.
