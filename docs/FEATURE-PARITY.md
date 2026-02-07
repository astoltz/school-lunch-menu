# Feature Parity: Windows (WPF) vs macOS (SwiftUI)

This document tracks feature parity between the two platform implementations.

Legend: Y = Implemented, N = Not implemented, P = Partial

## Core Features

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| LINQ Connect API fetch | Y | Y | |
| HAR file loading | Y | Y | WPF supports drag-drop + command-line arg |
| Menu analysis (allergen safety) | Y | Y | |
| HTML calendar generation | Y | Y | |
| In-app HTML preview | Y | Y | WPF uses WebBrowser, Mac uses WKWebView |
| Open in browser | Y | Y | |
| Printable landscape HTML | Y | Y | Self-contained, no external dependencies |

## School Lookup

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| Identifier code lookup | Y | Y | |
| Building selection | Y | Y | |
| Session selection | Y | Y | |
| Dynamic school support | Y | Y | |

## Allergen & Food Preferences

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| 17 allergen filters | Y | Y | |
| Not-preferred foods | Y | Y | |
| Favorite foods | Y | Y | |
| Allergen-based recipe filtering | Y | Y | |
| Collapsible allergen panel | Y | Y | |
| Allergen summary text | Y | Y | |

## Calendar Themes

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| 21 visual themes | Y | Y | |
| Categorized dropdown (Seasonal/Fun/Basic) | Y | Y | |
| Auto-suggestion by month | Y | Y | |
| Hidden themes (settings.json) | Y | Y | |

## Layout Modes

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| List mode | Y | Y | |
| Icons Left mode | Y | Y | |
| Icons Right mode | Y | Y | |
| Per-plan emoji icons | Y | Y | |
| Plan label editing | Y | Y | |
| Plan reordering | Y | Y | |
| Show unsafe lines option | Y | Y | |

## Day Options

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| Cross out past days (3 PM cutoff) | Y | Y | |
| Share link with QR codes | Y | Y | |
| Forced home days (per-session) | Y | Y | |
| Holiday icon overrides | Y | Y | |

## Day Labels (Rotating Schedule)

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| Rotating day labels | Y | Y | |
| Configurable corner position | Y | Y | |
| Start date anchor | Y | Y | |
| Add/remove labels | Y | Y | |
| Fetch from CMS | Y | Y | |

## Settings Persistence

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| JSON settings file | Y | Y | |
| Debounced auto-save | Y | Y | |
| Menu cache (instant startup) | Y | Y | |
| Per-session preferences | Y | Y | |

## UI Chrome

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| Preview zoom (+/-) | Y | Y | |
| View Source link | Y | Y | |
| About dialog/window | Y | Y | WPF: Help > About menu; Mac: App > About |
| Credits in About | Y | Y | LINQ Connect, ISD 194 CMS, GitHub |
| Version/git info in About | Y | Y | |
| Help menu bar | Y | N/A | Mac uses standard App menu |
| Drag-drop HAR file | Y | N | Mac uses file picker only |
| Command-line HAR arg | Y | N | |

## Development & CI

| Feature | WPF | Mac | Notes |
|---|---|---|---|
| CI/CD pipeline | Y | Y | GitHub Actions |
| Git info injection | Y | Y | WPF: MSBuild; Mac: Xcode build phase |
| Test suite | Y | N | xUnit + FluentAssertions |
| MIT License | Y | Y | |

## Known Gaps

1. **Mac: Drag-drop HAR** -- WPF supports drag-dropping HAR files onto the window; Mac only supports the file picker.
2. **Mac: Command-line HAR arg** -- WPF accepts a HAR file path as a command-line argument; Mac does not.
3. **Mac: Test suite** -- No Swift test suite exists yet. The WPF test suite covers the shared business logic.
