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
- **Visual muting** -- on forced home days where safe options still exist, plan buttons (grid mode) and plan badges (list mode) are visually muted with reduced opacity and partial grayscale. This distinguishes them from normal school days while keeping food items fully visible so parents can see what's available if plans change. The `.btn-forced-home` class (grid mode) and `.plan-section.forced-home` class (list mode) apply `opacity: 0.5; filter: grayscale(0.6)`. Days with no safe options still use `.btn-off` (full gray) regardless of forced home day status.
- **UI** -- five checkboxes (Monday–Friday) in the sidebar, visible when sessions are loaded. Selections persist independently per session.

## Separate Browser Launch

Generate Calendar only renders the preview in the in-app WebBrowser control. A separate "Open in Browser" button (green, in the status bar area) launches the file in the default browser:

- **Rationale** -- auto-launching the browser on every generation was disruptive during iterative configuration changes. The in-app preview is sufficient for quick checks, and users can explicitly open in the browser when ready to print.
- **Visibility** -- the button appears only after a calendar has been generated (bound to `GeneratedHtmlPath` being non-null).

## Holiday-Specific No-School Icons

No-school days display a holiday-specific emoji above the note text. The system checks user-configured overrides first, then falls back to hardcoded keyword matching.

### Configurable Overrides

Holiday overrides are stored in `HolidayOverrides` in settings, keyed by keyword. Each override specifies:
- **Emoji** -- the emoji to display
- **CustomMessage** -- optional replacement for the academic note text (null uses the original)

On first launch, defaults are pre-populated from the hardcoded patterns. Users can add, edit, or remove overrides via the collapsible "Holiday Icons" section in the sidebar. The `GetNoSchoolDisplay()` method iterates overrides first (case-insensitive substring match), then falls back to `GetNoSchoolEmoji()`.

### Default Patterns

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

The emoji is rendered in a centered `<div class="no-school-emoji">` with `font-size: 20px`. The note text below is also center-aligned. No-school cells use `vertical-align: middle; text-align: center;` to vertically center content within the table cell. Redundant " - No School" suffixes are automatically stripped from the note text before rendering, since the cell styling already indicates it's a no-school day (e.g., "President's Day - No School" becomes "President's Day").

## Day Cell Layout Modes

The calendar supports three layout modes, selected via a "Day Layout" ComboBox in the sidebar:

### List Mode

The classic stacked layout: each plan's items appear under a color-coded badge. No grid, no icon buttons.

### Icons Left / Icons Right (Grid Mode)

A CSS Grid layout where each plan gets its own row with a button cell and an items cell:

- **Grid structure** -- `.day-grid` uses CSS Grid. `buttons-left` sets `grid-template-columns: 38px 1fr`; `buttons-right` sets `1fr 38px`. Each plan row uses `display: contents` so its children participate in the parent grid.
- **Button cell** -- `.grid-btn` displays the plan's emoji icon (user-editable, defaults to ✅/⛔ based on safety) above a short label. Uses the plan's color for the background. Unsafe lines get `.btn-off` (gray, reduced opacity).
- **Items cell** -- `.grid-items` contains the entree list (same rendering as List mode items).
- **Home row** -- always present at the bottom: a home button + "From Home" text when the day is a home day.

### Per-Plan Emoji Icons

Users can set custom emoji icons per plan line via an editable ComboBox dropdown in the sidebar (visible in grid mode). The dropdown offers curated emoji suggestions but also accepts typed input. Icons are stored in `PlanIconOverrides` in settings. When a custom icon is set, it replaces the default ✅/⛔ status indicator on the grid button.

### Plan Display Order

Plans can be reordered via ▲/▼ buttons in the sidebar. The order is saved in `PlanDisplayOrder` and applied to both the legend and day cells. Plans not in the saved order appear alphabetically after saved ones.

### Label Overrides (Edit Mode)

The `PlanLabelOverrides` dictionary in settings maps original plan names to short labels (e.g., `"Big Cat Cafe - MS"` → `"Big Cat"`). Overrides apply to legend badges, day cell plan badges, and grid buttons.

In the sidebar, plan labels default to a compact **view mode** showing the display label (custom or full name) with a pencil (✏️) button. Clicking the pencil enters **edit mode**:

- The TextBox is prefilled with the current display label (full plan name if no custom label exists) and all text is auto-selected for easy replacement.
- Pressing **Enter** or clicking the **✓** button exits edit mode and saves.
- The **↩** button resets the label to the original plan name.
- A tooltip on the view-mode label always shows the full original plan name.
- Labels with custom overrides display in bold dark text; default labels display in muted gray.

### Show Unsafe Lines

When enabled via the "Show unsafe lines" checkbox (grid mode only), plan lines with no allergen-safe items are rendered with a grayed-out button (`.btn-off` class: gray background, reduced opacity) and a customizable message (default: "No safe options") in place of entree items. The message is editable in a TextBox that appears when the checkbox is checked. Settings: `ShowUnsafeLines` (bool) and `UnsafeLineMessage` (string).

### Row Tinted Backgrounds

In grid mode, each plan row's items cell has a tinted background using the plan's color at ~10% opacity (hex alpha `{color}1a`). This visually connects the button to its items and provides visual separation between rows. The home row uses the theme's `HomeBadgeBg` color at the same opacity.

### Home Row Styling

In grid mode, the home row is always present at the bottom of each day cell. It displays a 🏠 home button and a `<div class="safe-item">Home {sessionLabel}</div>` styled like a regular meal item (not a badge). The tint color matches the theme's home badge color.

### Settings

| Setting | Description |
|---|---|
| `LayoutMode` | `"List"`, `"IconsLeft"`, or `"IconsRight"` (replaces legacy `ShowMealButtons`) |
| `PlanLabelOverrides` | Custom short labels for plan line names |
| `PlanIconOverrides` | Custom emoji icons per plan line |
| `PlanDisplayOrder` | User-defined display order for plan lines |
| `ShowUnsafeLines` | Whether to show grayed-out buttons for plan lines with no allergen-safe items |
| `UnsafeLineMessage` | Custom message shown next to grayed-out unsafe line buttons (default: "No safe options") |

**Backward compatibility**: The legacy `ShowMealButtons` boolean is still deserialized. If `LayoutMode` is missing/empty and `ShowMealButtons` is true, the app defaults to `"IconsRight"`.

## Preview Zoom

The in-app WebBrowser preview supports zoom via **+/−** buttons in the status bar (5% increments, range 25%–200%, default 75%):

- **Preview-only** -- zoom is applied by injecting a CSS `zoom` property into the `<body>` tag of the preview HTML. The HTML file saved to disk has no zoom applied, so opening in a browser or printing produces a clean, unzoomed document.
- **Live update** -- changing zoom re-injects the style into the cached base HTML and updates the WebBrowser without regenerating the calendar.
- **Print isolation** -- `@media print { zoom: 1 !important; }` ensures the zoom never affects printed output.

## Source Link

A "View Menu Source" button in the sidebar opens the LINQ Connect public menu page in the default browser:

- **URL pattern**: `https://linqconnect.com/public/menu/{Identifier}?buildingId={BuildingId}`
- **Visibility** -- the button appears only when both an identifier code and a building are selected (bound to the computed `SourceUrl` property being non-null).
- **Rationale** -- gives parents quick access to the full menu website for cross-referencing or sharing.

## Forced Home Days Persistence Fix

The debounced settings save (500ms delay) could lose changes when switching sessions:

- **Root cause** -- `OnSelectedSessionChanged` called `ScheduleSettingsSave()` which cancelled the previous debounce timer. If session A had pending forced-home-day changes, they were lost.
- **Fix** -- `FlushPendingSave()` is called at the start of `OnSelectedSessionChanged`. It cancels the debounce timer and immediately runs `SaveSettingsAsync()` if a save was pending, ensuring session A's changes hit disk before session B's data loads.
- **Tracking** -- a `_savePending` flag tracks whether a debounced save is outstanding.

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

- **Debounced auto-save** -- settings save automatically 500ms after any change (allergens, preferences, building, session, theme, meal buttons, holiday overrides), batching rapid changes. A flush mechanism ensures pending saves complete before session switches.
- **Per-session preferences** -- not-preferred and favorite foods are stored per serving session (e.g., Lunch favorites are separate from Breakfast favorites).
- **Hidden themes** -- the `HiddenThemeNames` list is preserved across saves and only editable via the JSON file.
- **Simple and portable** -- the file travels with the application if copied to another machine.
- **Graceful degradation** -- if the file is missing or corrupt, the application falls back to sensible defaults (Lakeville Area Schools identifier pre-filled, Milk allergen selected by name on first API fetch, Icons Left layout, cross-out past days enabled, Red/White day label cycle).

## Cross Out Past Days

When enabled via the "Cross out past days" checkbox in the sidebar, past days are visually marked on the calendar:

- **Faded overlay** -- `opacity: 0.45` reduces the entire cell content to a muted appearance.
- **Diagonal X** -- a `::after` pseudo-element renders a large, subtle `✕` character centered over the cell (`font-size: 48px`, `color: rgba(0, 0, 0, 0.12)`).
- **Scope** -- applies to all past cells: regular days, favorite days, no-school days, and no-menu days.
- **Date comparison** -- a `GetPastDayCutoff()` helper is called at generation time: after 3 PM it returns today's date, before 3 PM it returns yesterday's date. This ensures the current day is not crossed out until the school day is over. The result is passed as `options.Today`. Days with `day.Date < options.Today` are marked.
- **CSS class** -- `past-day` is added to the `<td>` element alongside any existing classes (`favorite-day`, `no-school`).

## Rotating Day Labels (Corner Triangles)

The calendar supports rotating "day labels" displayed as colored corner triangles on school days. This is designed for schools that use alternating schedules (e.g., Red Day / White Day, Day A / Day B).

### Visual Design

Each label is rendered as a small colored triangle in the configurable corner of the day cell (default: top-right) using the CSS border trick:

- `.day-label` creates a zero-size element with a colored border that forms a triangle.
- `.day-label-text` positions short text over the triangle, rotated 45° to follow the diagonal.
- The corner position is configurable via `DayLabelCorner` setting: `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft`.
- The `<td>` gets `position: relative` when a label is present, enabling absolute positioning of the triangle.

### Cycle Algorithm

1. If `DayLabelCycle` is empty, the feature is off entirely.
2. Collect all **school days** (has menu, not no-school) in date order.
3. Determine the anchor date: `DayLabelStartDate` if set, otherwise the first school day in the month.
4. For each school day, compute `cycleIndex = (dayIndex - anchorIndex) % cycleLength`.
5. Store as `Dictionary<DateOnly, DayLabel>` lookup, used during cell rendering.
6. No-school days are skipped in the rotation (they don't get a label and don't consume a cycle slot).

### Configuration

Labels are configured in the collapsible "Day Labels (Red/White Day)" expander in the sidebar:

- Each entry has a **Label** text and a **Color** (editable ComboBox with preset CSS color suggestions).
- A **Start Date** text box anchors the cycle to a specific date (blank defaults to the first school day).
- A **Corner** dropdown selects which corner the triangle appears in.
- The **+ Add Label** button adds new entries; **✕** removes them.

### Settings

| Setting | Description |
|---|---|
| `CrossOutPastDays` | Whether to fade and X-out past days |
| `DayLabelCycle` | List of `{ Label, Color }` entries defining the rotation |
| `DayLabelStartDate` | Anchor date for the cycle (M/d/yyyy format, null = first school day) |
| `DayLabelCorner` | Corner for the triangle: `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft` |
| `DistrictName` | Persisted district display name for immediate display on launch |

## CMS Day Label Fetch

The "Fetch from CMS" button in the Day Labels expander scrapes Red Day / White Day entries from the ISD194 Finalsite CMS school calendar page (`https://cms.isd194.org/news-and-events/calendar`).

### Why HTML Scraping

The Finalsite CMS calendar does not expose an API, iCal feed, or RSS feed. The only option is to parse the rendered HTML. The page uses structured `fsCalendarDate` divs with `data-day`, `data-year`, and `data-month` attributes (months are 0-indexed), and `fsCalendarEventTitle` anchors with a `title` attribute containing the event name.

### Implementation

- `IDayLabelFetchService` / `DayLabelFetchService` -- a lightweight service registered with a typed `HttpClient` via `AddHttpClient<>`.
- Source-generated regex patterns (`[GeneratedRegex]`) extract date attributes and event titles from the HTML.
- The service matches event titles against known rotation label patterns (Red Day, White Day, and other color/letter variants).
- The ViewModel command (`FetchDayLabelsCommand`) maps fetched labels to colors (Red -> `#dc3545`, White -> `#adb5bd`), strips " Day" suffixes for compact display, sets the start date to the first entry's date, and populates `DayLabelEntries`.
- The CMS page only shows the current month; there is no URL parameter to navigate to other months.
- If the page is unreachable or returns no labels, an error message is shown in the status bar.

## District Name Persistence

`DistrictName` is now saved in `AppSettings` and restored on launch, making it visible immediately before the menu cache loads. `PopulateBuildings` will overwrite it with the cached value if available.

## Source Link in Status Bar

The "View Menu Source" button was moved from the sidebar to the status bar (next to "Open in Browser") as a blue-styled button. This makes it more visible and accessible without scrolling the sidebar.

## Share Footer (QR Code)

The "Share link on calendar" checkbox appends a footer below the calendar table containing up to two QR codes:

1. **Source menu QR** -- links to the LINQ Connect public menu page for the selected school (`https://linqconnect.com/public/menu/{Identifier}?buildingId={BuildingId}`). Only shown when a building is selected. The URL is passed via `CalendarRenderOptions.SourceUrl`.
2. **Project QR** -- links to `https://github.com/astoltz/school-lunch-menu` with the message "Want your own allergen-friendly lunch calendar? Scan to learn more!"

### QR Code Generation

QR codes are generated at runtime using the **QRCoder** NuGet package (`PngByteQRCode`) and embedded as base64-encoded PNG data URIs (`data:image/png;base64,...`). This keeps the HTML fully self-contained with no external image requests, consistent with the existing print CSS approach. URLs can be changed in the source code without manually regenerating QR images.

### Layout

The footer uses a flex container (`.share-footer`) centered below the table. Each QR code is wrapped in a `.share-group` div with the 64×64 image alongside descriptive text. Groups are separated by a 24px gap. A subtle top border visually separates the footer from the calendar. The footer prints cleanly on the same page as the calendar due to the existing `page-break-inside: avoid` on the table.

### Settings

| Setting | Description |
|---|---|
| `ShowShareFooter` | Whether to append the share footer with QR codes |

## Menu Cache

Menu data is cached to `menu-cache.json` for instant startup:

- On each successful data load (API or HAR), the full menu response, allergen list, and identifier response are saved.
- On next launch, the cache is loaded immediately, populating all dropdowns and checklists before the user interacts.
- Cache age is displayed in the status bar (e.g., "saved 2h 15m ago").
- The user can fetch fresh data at any time to replace the cache.

### Identifier Response Synthesis

The `IdentifierResponse` in the menu cache is essential for restoring the building dropdown on startup. If `_lastIdentifierResponse` is null when `FetchFromApiAsync` saves the cache (e.g., the user loaded from a cache that lacked it, then fetched fresh API data without clicking "Look Up"), the identifier response is synthesized from the current UI state: district ID, district name, identifier code, and the current buildings list. This ensures the cache always includes the identifier response going forward.

## ConfigureAwait Deadlock Fix

`FlushPendingSave()` calls `SaveSettingsAsync().GetAwaiter().GetResult()` synchronously on the UI thread to ensure pending settings hit disk before a session switch. `SaveSettingsAsync` uses `await _settingsService.LoadAsync()` and `await _settingsService.SaveAsync()` internally. Without `.ConfigureAwait(false)` on these awaits, the continuations capture the WPF synchronization context and try to resume on the UI thread -- which is blocked by `.GetAwaiter().GetResult()`, causing a classic async deadlock.

The fix adds `.ConfigureAwait(false)` to both awaits inside `SaveSettingsAsync`, so continuations run on the thread pool instead of marshaling back to the UI thread. The settings service methods (`LoadAsync`, `SaveAsync`) already use `.ConfigureAwait(false)` internally, but `SaveSettingsAsync` in the ViewModel did not.

## Default Settings

The `AppSettings` class provides sensible defaults for a fresh install without a `settings.json` file:

| Setting | Default | Rationale |
|---|---|---|
| `Identifier` | `"YVAM38"` | Lakeville Area Schools family menu code |
| `DistrictId` | `"47ce70b9-..."` | Lakeville Area Schools district UUID |
| `DistrictName` | `"Lakeville Area Schools"` | Shown immediately on launch |
| `BuildingId` | `null` | User picks their building from the dropdown |
| `SelectedSessionName` | `"Lunch"` | Most common use case |
| `LayoutMode` | `"IconsLeft"` | Grid mode with icon buttons on the left |
| `ShowUnsafeLines` | `true` | Show all plan lines even if unsafe |
| `CrossOutPastDays` | `true` | Visual aid for tracking the current week |
| `DayLabelCycle` | Red / White | ISD194 alternating schedule labels |

## About Window

Both platforms provide an About dialog following OS conventions:

### Windows (WPF)

- **Location**: Help > About School Lunch Menu (standard Windows menu bar placement)
- **Implementation**: `AboutWindow.xaml` / `AboutWindow.xaml.cs` -- a modal `Window` with `Owner = MainWindow`
- **Version info**: Parsed from `AssemblyInformationalVersionAttribute`, which CI sets to `1.0.0+abc1234 (branch)` format via `-p:InformationalVersion`. For local dev builds, `Directory.Build.props` runs `git rev-parse` to inject the same format.
- **Credits**: LINQ Connect (data source), ISD 194 CMS Calendar (day labels), GitHub project page -- all as clickable `Hyperlink` elements using `Process.Start` with `UseShellExecute = true`.
- **Menu bar**: The main window uses a `DockPanel` with a top-docked `Menu` containing Help > About. This follows Windows UX conventions (Help menu in the menu bar).

### macOS (SwiftUI)

- **Location**: App > About School Lunch Menu (standard macOS menu placement via `.openWindow`)
- **Implementation**: `AboutView.swift` -- a SwiftUI view using `BuildInfo` (injected by Xcode build phase)
- **Credits**: Same data sources as Windows, rendered as SwiftUI `Link` elements.

## Test Suite

The Windows project includes an xUnit test suite in `tests/SchoolLunchMenu.Tests/`:

### Design Choices

- **xUnit** -- the most widely used .NET test framework, with excellent IDE integration and parallel test execution.
- **FluentAssertions** -- provides readable, expressive assertions (e.g., `result.Should().BeTrue()`) that produce clear failure messages.
- **NSubstitute** -- lightweight mocking framework for interface substitution, used where services need to be isolated.
- **NullLogger** -- tests use `NullLogger<T>.Instance` to satisfy `ILogger<T>` dependencies without configuring logging infrastructure.
- **Real service instances** -- where possible, tests use actual service implementations (e.g., `MenuAnalyzer`, `CalendarHtmlGenerator`) rather than mocks, since these are pure functions with no external dependencies.

### Coverage Strategy

Tests focus on business logic correctness rather than UI behavior:

| Area | Key Scenarios |
|---|---|
| Menu analysis | Safe/unsafe classification, allergen flagging, not-preferred exclusion, favorite marking, no-school detection, multi-plan handling |
| HTML generation | Output structure, theme application, layout modes, past-day marking, day labels, share footer QR codes |
| Settings | Default values, round-trip persistence, graceful degradation |
| CMS parsing | Regex pattern validation against known HTML structure, label pattern matching |
| Models | Computed property correctness (IsNoSchool, AnyLineSafe, HasMenu) |
| Themes | Count verification, category distribution, auto-suggestion coverage |

### CI Integration

The `dotnet test` step runs between build and publish in `.github/workflows/dotnet.yml`, ensuring tests pass before artifacts are published.
