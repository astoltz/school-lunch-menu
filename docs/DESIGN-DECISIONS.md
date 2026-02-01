# Design Decisions

This document explains the rationale behind key architectural and design choices in the School Lunch Menu Calendar Generator.

## Why WPF

WPF was chosen because the application is Windows-only and benefits from:

- **WebBrowser control** -- provides an in-app HTML preview without bundling a browser engine like Chromium (as Electron or MAUI BlazorWebView would require).
- **Native print dialog integration** -- the generated HTML opens in the user's default browser for printing, but the WPF WebBrowser serves as a quick preview.
- **Mature MVVM ecosystem** -- CommunityToolkit.Mvvm provides source-generated observable properties and relay commands with minimal boilerplate.
- **No cross-platform requirement** -- the target user runs Windows. A cross-platform UI framework would add complexity without benefit.

## Why .NET 10

.NET 10 is the current Long-Term Support (LTS) release, supported through November 2028. This ensures the application remains on a supported runtime for several years without requiring framework upgrades. C# 14 language features (used via `<LangVersion>14</LangVersion>`) reduce boilerplate.

## Dynamic School Support

The application supports any school in the LINQ Connect system via identifier code lookup:

- **Identifier codes** (e.g., "YVAM38") are entered in the sidebar to look up a district and its buildings.
- **Building selection** is presented in a dropdown after lookup.
- **Serving sessions** (Breakfast, Lunch, etc.) are discovered from the menu response.
- **Menu plan names** are discovered dynamically -- no hardcoded plan name prefixes. All plans within the selected session are rendered with color-coded badges.
- **Settings persist** the last used identifier, district, building, and session across restarts.

## Per-Plan Line Rendering

The calendar renders each menu plan as a separate section within each day cell:

- Each plan gets a **color-coded badge** from a palette of 10 colors, assigned alphabetically by plan name.
- Plan badges appear in both the calendar cells and the legend.
- This approach handles any number of plans (not just the original two-line Big Cat / Regular model).
- Entrees are listed under their plan badge, preserving the association between food items and serving lines.

## Color Scheme and Themes

The calendar supports 21 built-in visual themes organized into 3 categories (Seasonal, Fun, Basic) via the `CalendarTheme` model:

| Property | Controls |
|---|---|
| `HeaderBg` / `HeaderFg` | Table header (day-of-week row) |
| `TitleColor` | Calendar title (h1) |
| `SafeColor` | Safe entree text color |
| `FavoriteStar` / `FavoriteBorder` / `FavoriteBg` | Favorite item highlighting |
| `HomeBadgeBg` | "From Home" badge |
| `AccentBorder` | Cell borders |
| `BodyBg` | Page background |
| `CellPattern` | Optional CSS background pattern on cells |
| `Emoji` | Decorative emoji in title |
| `Category` | Grouping category: "Seasonal", "Fun", or "Basic" |

### Categorized Theme Dropdown

Themes are displayed in a ComboBox grouped by category with non-selectable header rows:

1. **Seasonal** -- 12 themes covering every month, from New Year (January) to Winter (December)
2. **Fun** -- 7 character/interest themes (Cardinal, Blue Jay, Cats, Dinosaurs, Princess, Robots, Unicorn)
3. **Basic** -- Default theme

Header items use `ThemeListItem` with `IsHeader = true` and are disabled in the ComboBox via `DataTrigger`. The `SelectedThemeItem` property rejects header selections. Each selectable item displays the theme's emoji and name (e.g., "🦃 Thanksgiving").

### Hidden Themes

Users can hide themes from the dropdown by adding theme names to the `HiddenThemeNames` list in `settings.json`. This is useful for removing themes that aren't relevant (e.g., hiding "Spooky" for families who prefer not to see Halloween themes). The `BuildThemeList()` method filters hidden themes when constructing the categorized list. A UI for managing hidden themes may be added later; for now, users edit the JSON directly.

**Theme auto-suggestion**: Themes with `SuggestedMonth` (or `SuggestedMonth2`) are automatically selected when the user changes the month selector, unless they've manually picked a different theme. The `_themeManuallySelected` flag tracks this. The list is ordered so that specific holiday themes (e.g., St. Patrick's for March) take priority over broader seasonal themes (e.g., Spring).

The Default theme preserves the original ADHD-friendly, high-contrast color scheme:

| Color | Hex | Meaning |
|---|---|---|
| Dark green | `#155724` | Safe entree text |
| Orange | `#ff8c00` | Favorite star and day highlight border |
| Red | `#dc3545` | "From Home" badge (no safe options or forced home day) |
| Gray | `#e9ecef` | No school |

Colors are chosen to be distinct from each other and readable at small font sizes. The `print-color-adjust: exact` CSS property ensures colors print correctly.

## Classification Algorithm

Menu analysis follows these rules:

1. **Session selection** -- the user selects which serving session to analyze (Lunch, Breakfast, etc.).
2. **All plan lines** -- the analyzer iterates all menu plans within the selected session. Each plan produces a separate `ProcessedLine`.
3. **Entree categories only** -- only recipe categories where `IsEntree == true` are checked. Side dishes, beverages, and condiments are excluded.
4. **Union allergen logic** -- a recipe is flagged if it contains *any* of the user's selected allergens.
5. **Not-preferred items** -- allergen-free recipes marked as not preferred are flagged separately. They don't count toward line safety.
6. **Favorite items** -- allergen-free recipes marked as favorites are flagged. Days with favorites get a highlighted border.
7. **Per-line safety** -- a line is "safe" if it has at least one allergen-free, non-not-preferred entree.
8. **Lunch from Home** -- if no line is safe, the day gets a "🏠 {Session} from Home" badge.

## Forced Home Days

Rather than a hardcoded Thursday reminder, the application supports per-session "forced home days":

- **Per-session configuration** -- each serving session (Breakfast, Lunch, etc.) has its own set of forced weekdays, stored in `ForcedHomeDaysBySession` in settings.
- **Default to Thursday** -- when no saved entry exists for a session, Thursday is checked by default (preserving the original behavior for existing users).
- **Unified badge logic** -- the "🏠 {Session} from Home" badge appears if `!day.AnyLineSafe || forcedHomeDays.Contains(day.Date.DayOfWeek)`. This single check replaces the old separate "no safe options" badge and Thursday-specific logic.
- **UI** -- five checkboxes (Monday–Friday) in the sidebar, visible when sessions are loaded. Selections persist independently per session.

## Separate Browser Launch

Generate Calendar only renders the preview in the in-app WebBrowser control. A separate "Open in Browser" button (green, in the status bar area) launches the file in the default browser:

- **Rationale** -- auto-launching the browser on every generation was disruptive during iterative configuration changes. The in-app preview is sufficient for quick checks, and users can explicitly open in the browser when ready to print.
- **Visibility** -- the button appears only after a calendar has been generated (bound to `GeneratedHtmlPath` being non-null).

## Holiday-Specific No-School Icons

No-school days display a holiday-specific emoji above the note text, determined by keyword matching on the `AcademicNote`:

| Keywords | Emoji |
|---|---|
| "winter break", "christmas" | ❄️ |
| "thanksgiving" | 🦃 |
| "president" | 🇺🇸 |
| "mlk", "martin luther king" | ✊ |
| "memorial" | 🇺🇸 |
| "labor" | 🇺🇸 |
| "spring break" | 🌸 |
| "teacher" | 📚 |
| (fallback) | 🏠 |

The `GetNoSchoolEmoji()` method performs case-insensitive substring matching. The emoji is rendered in a centered `<div class="no-school-emoji">` with `font-size: 20px`.

## Application Logo

The app uses a custom icon (`Assets/logo.ico`) displayed in the window title bar and Windows taskbar. The icon depicts a lunch tray/plate with a calendar grid overlay, incorporating the app's signature colors (green for safe items, orange for favorites, dark header). The SVG source is stored alongside the ICO for future editing.

## Allergen-Based Recipe Filtering

The not-preferred and favorites checklists in the sidebar hide items that contain selected allergens:

- During `PopulateRecipeNamesAsync`, a `_recipeAllergenMap` is built mapping each recipe name to its set of allergen IDs.
- `NotPreferredOption` and `FavoriteOption` carry an `AllergenIds` property.
- `RebuildFilteredRecipes()` and `RebuildFilteredFavorites()` skip items whose allergen IDs overlap with selected allergens.
- This prevents the user from marking allergen-containing items as not-preferred or favorite, since those items are already filtered out by the allergen analysis.

## Collapsible Allergen Filter

The allergen checkbox list is wrapped in a WPF `Expander`, collapsed by default:

- An `AllergenSummary` text (e.g., "Milk", "Milk, Egg") is always visible below the expander, showing the current selection at a glance.
- This reduces sidebar clutter since allergen selection is a one-time setup for most users.
- The summary updates immediately when allergens are toggled.

## Print CSS Approach

The generated HTML is fully self-contained with no external stylesheets, scripts, or images. Key print CSS choices:

- **`@page { size: landscape; margin: 0.25in; }`** -- forces landscape orientation and minimal margins to maximize calendar space.
- **`print-color-adjust: exact` / `-webkit-print-color-adjust: exact`** -- ensures background colors and badges print as shown on screen.
- **`table { page-break-inside: avoid; }`** -- prevents the calendar table from splitting across pages.
- **Fixed table layout with `table-layout: fixed`** -- ensures equal column widths for Monday through Friday.
- **No JavaScript** -- the calendar is pure HTML/CSS, compatible with any browser or print workflow.

## HAR File Support

HAR (HTTP Archive) files provide an offline fallback:

- **Use case**: when the LINQ Connect API is temporarily unavailable, slow, or when developing/testing without network access.
- **How it works**: the user captures network traffic in Chrome DevTools while browsing the LINQ Connect menu site, saves as `.har`, and loads it into the application.
- **Drag-drop**: HAR files can be dragged onto the window or passed as a command-line argument.
- **Parsing**: `HarFileService` iterates HAR log entries, matches URLs by substring (`FamilyMenu?`, `FamilyAllergy`, `FamilyMenuIdentifier`), and deserializes the first matching response body for each endpoint.
- **Month detection**: when loading from HAR, the application inspects the first date in the menu data and auto-selects the corresponding month/year.

## Settings Persistence

Application settings are stored in a `settings.json` file next to the executable:

- **Debounced auto-save** -- settings save automatically 500ms after any change (allergens, preferences, building, session, theme), batching rapid changes.
- **Per-session preferences** -- not-preferred and favorite foods are stored per serving session (e.g., Lunch favorites are separate from Breakfast favorites).
- **Hidden themes** -- the `HiddenThemeNames` list is preserved across saves and only editable via the JSON file.
- **Simple and portable** -- the file travels with the application if copied to another machine.
- **Graceful degradation** -- if the file is missing or corrupt, the application falls back to defaults (Milk allergen selected, Thursday forced home day, Default theme).

## Menu Cache

Menu data is cached to `menu-cache.json` for instant startup:

- On each successful data load (API or HAR), the full menu response, allergen list, and identifier response are saved.
- On next launch, the cache is loaded immediately, populating all dropdowns and checklists before the user interacts.
- Cache age is displayed in the status bar (e.g., "saved 2h 15m ago").
- The user can fetch fresh data at any time to replace the cache.
