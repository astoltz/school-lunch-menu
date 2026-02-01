# School Lunch Menu Calendar Generator

A Windows desktop application that generates allergen-aware, printable lunch calendars for Century Middle School in the Lakeville Area Schools district. It fetches menu data from the LINQ Connect API, analyzes each entree for allergen safety, and produces a color-coded HTML calendar designed for easy scanning and printing.

## Features

- **Live API and offline HAR file support** -- fetch menus directly from the LINQ Connect API, or load a previously captured HAR file for offline use.
- **17 allergen filters** -- select any combination of allergens; entrees containing them are flagged with strikethrough text.
- **ADHD-friendly color-coded calendar** -- green (Big Cat Line safe), blue (Regular Line safe), red (Lunch from Home), gray (No School), yellow (Thursday reminder). High-contrast colors chosen for quick visual scanning.
- **Printable landscape HTML** -- self-contained HTML with `@page landscape`, `print-color-adjust: exact`, and no external dependencies. Print directly from the browser.
- **Thursday reminders** -- configurable reminder text displayed on every Thursday cell (e.g., "Eat breakfast before the office").
- **Persisted settings** -- selected allergens, reminder preferences, and reminder text are saved to a JSON file and restored on next launch.
- **Two lunch lines** -- separately analyzes the Big Cat Line and Regular Lunch Line, showing safe entree options for each.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS, supported through November 2028)
- Windows 10 or later (WPF application)

## Build

```
dotnet build src/SchoolLunchMenu/SchoolLunchMenu.csproj
```

## Run

```
dotnet run --project src/SchoolLunchMenu/SchoolLunchMenu.csproj
```

## Usage

1. Select a **month** and **year** from the dropdowns.
2. Click **Fetch from API** to download menu data from LINQ Connect, or click **Load from HAR** to load a previously saved `.har` file.
3. Check the **allergens** you want to filter (Milk is selected by default).
4. Optionally configure the **Thursday reminder** text.
5. Click **Generate Calendar** to produce the HTML calendar. It opens in your default browser and is saved to `%TEMP%\SchoolLunchMenu\`.
6. Print the page in landscape orientation from the browser.

<!-- Screenshot placeholder: add a screenshot of the generated calendar here -->
<!-- ![Calendar screenshot](docs/screenshot.png) -->

## Project Structure

```
src/SchoolLunchMenu/
  App.xaml.cs              # DI container setup, Serilog configuration
  MainWindow.xaml          # WPF layout with sidebar and WebBrowser preview
  ViewModels/              # MainViewModel (MVVM with CommunityToolkit)
  Models/                  # ProcessedDay, ProcessedMonth, RecipeItem, AppSettings
  Models/Api/              # LINQ Connect API response DTOs
  Services/                # API client, HAR parser, menu analyzer, HTML generator, settings
```

## License

MIT
