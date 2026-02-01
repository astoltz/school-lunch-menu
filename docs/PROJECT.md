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
| Serialization | System.Text.Json (built-in) |

## File Map

### Entry Point
| File | Purpose |
|---|---|
| `App.xaml.cs` | Configures DI container, Serilog file logging (14-day rolling), registers all services and views |

### Views
| File | Purpose |
|---|---|
| `MainWindow.xaml` | WPF layout: left sidebar (school lookup, month/year selectors, session selector, categorized theme dropdown with headers, collapsible allergen filter, not-preferred/favorite checklists, forced home days, action buttons), right pane (WebBrowser preview, status bar with Open in Browser button). App icon via `Icon="Assets/logo.ico"`. |
| `MainWindow.xaml.cs` | Code-behind: wires DataContext, navigates WebBrowser on HTML change, handles drag-drop for HAR files |
| `Converters/BoolToVisibilityConverter.cs` | `bool` to `Visibility` for progress bar |
| `Converters/NullToVisibilityConverter.cs` | `null` to `Visible`/`Collapsed` for preview placeholder vs. WebBrowser |

### View Models
| File | Purpose |
|---|---|
| `ViewModels/MainViewModel.cs` | Coordinates fetch/load/generate workflow. Contains helper types: `AllergenOption`, `MonthOption`, `NotPreferredOption`, `FavoriteOption`, `ThemeListItem`, `ForcedHomeDayOption`. Manages categorized theme list with headers, hidden theme filtering, theme selection, allergen expander state, allergen-based recipe filtering, per-session forced home days, and debounced settings persistence. |

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
| `Models/AppSettings.cs` | Persisted settings: selected allergen IDs, per-session forced home days, per-session not-preferred/favorites, school identifiers, selected theme name, hidden theme names |
| `Models/MenuCache.cs` | Disk cache for menu response, allergen list, and identifier response with timestamp |

### Services
| File | Purpose |
|---|---|
| `Services/ILinqConnectApiService.cs` | Interface for LINQ Connect API calls |
| `Services/LinqConnectApiService.cs` | HTTP client calling 4 API endpoints with 1-hour in-memory response caching. Date format: `M-d-yyyy`. |
| `Services/IHarFileService.cs` | Interface for HAR file loading |
| `Services/HarFileService.cs` | Parses `FamilyMenu`, `FamilyAllergy`, and `FamilyMenuIdentifier` responses from HAR JSON entries |
| `Services/IMenuAnalyzer.cs` | Interface for menu analysis |
| `Services/MenuAnalyzer.cs` | Finds the selected session, iterates all menu plans dynamically, extracts entree-category recipes, checks allergen UUIDs, and applies not-preferred/favorite flags |
| `Services/ICalendarHtmlGenerator.cs` | Interface for HTML generation |
| `Services/CalendarHtmlGenerator.cs` | Builds self-contained HTML with themed inline CSS, per-plan color-coded badges, Monday-Friday calendar grid, favorite highlighting, holiday-specific no-school emoji, 🏠 "From Home" badges (forced home days + no safe options), and print styles |
| `Services/ISettingsService.cs` | Interface for settings and cache persistence |
| `Services/SettingsService.cs` | Reads/writes `settings.json` and `menu-cache.json` next to the executable |

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

4. CalendarHtmlGenerator.Generate(processedMonth, allergenNames, forcedHomeDays, theme)
       --> Self-contained themed HTML string with:
           - Holiday-specific emoji on no-school days
           - 🏠 "From Home" badges (on forced home days or when no safe options)

5. HTML is saved to %TEMP%/SchoolLunchMenu/ and displayed in the WPF WebBrowser control.
   User can click "Open in Browser" to launch the file in their default browser.
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
