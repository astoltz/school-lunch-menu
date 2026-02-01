namespace SchoolLunchMenu.ViewModels;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SchoolLunchMenu.Models;
using SchoolLunchMenu.Models.Api;
using SchoolLunchMenu.Services;

/// <summary>
/// Main view model for the School Lunch Menu application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILinqConnectApiService _apiService;
    private readonly IHarFileService _harFileService;
    private readonly IMenuAnalyzer _menuAnalyzer;
    private readonly ICalendarHtmlGenerator _calendarGenerator;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MainViewModel> _logger;

    private FamilyMenuResponse? _lastMenuResponse;
    private FamilyMenuIdentifierResponse? _lastIdentifierResponse;

    /// <summary>Debounce timer for auto-saving settings after user changes.</summary>
    private CancellationTokenSource? _saveDebounce;

    /// <summary>Master list of all not-preferred options (unfiltered).</summary>
    private readonly List<NotPreferredOption> _allRecipes = [];

    /// <summary>Master list of all favorite options (unfiltered).</summary>
    private readonly List<FavoriteOption> _allFavorites = [];

    /// <summary>Mapping from recipe name to its allergen IDs, built during PopulateRecipeNamesAsync.</summary>
    private Dictionary<string, HashSet<string>> _recipeAllergenMap = [];

    /// <summary>
    /// Initializes a new instance of <see cref="MainViewModel"/>.
    /// </summary>
    public MainViewModel(
        ILinqConnectApiService apiService,
        IHarFileService harFileService,
        IMenuAnalyzer menuAnalyzer,
        ICalendarHtmlGenerator calendarGenerator,
        ISettingsService settingsService,
        ILogger<MainViewModel> logger)
    {
        _apiService = apiService;
        _harFileService = harFileService;
        _menuAnalyzer = menuAnalyzer;
        _calendarGenerator = calendarGenerator;
        _settingsService = settingsService;
        _logger = logger;

        // Initialize month/year to current
        var today = DateTime.Today;
        _selectedMonth = today.Month;
        _selectedYear = today.Year;

        // Populate year options
        for (var y = today.Year - 1; y <= today.Year + 1; y++)
            AvailableYears.Add(y);

        // Default theme (used before settings are loaded)
        _selectedTheme = CalendarThemes.All.First(t => t.Name == "Default");
    }

    /// <summary>
    /// Available months for selection (1-12 with display names).
    /// </summary>
    public ObservableCollection<MonthOption> AvailableMonths { get; } =
    [
        new(1, "January"), new(2, "February"), new(3, "March"),
        new(4, "April"), new(5, "May"), new(6, "June"),
        new(7, "July"), new(8, "August"), new(9, "September"),
        new(10, "October"), new(11, "November"), new(12, "December")
    ];

    /// <summary>
    /// Available years for selection.
    /// </summary>
    public ObservableCollection<int> AvailableYears { get; } = [];

    /// <summary>
    /// Available allergens loaded from the API or HAR file.
    /// </summary>
    public ObservableCollection<AllergenOption> AvailableAllergens { get; } = [];

    /// <summary>
    /// Filtered not-preferred recipes shown in the sidebar (checked items + search matches).
    /// </summary>
    public ObservableCollection<NotPreferredOption> FilteredRecipes { get; } = [];

    /// <summary>
    /// Filtered favorite recipes shown in the sidebar (checked items + search matches).
    /// </summary>
    public ObservableCollection<FavoriteOption> FilteredFavorites { get; } = [];

    /// <summary>
    /// Available buildings from the identifier lookup.
    /// </summary>
    public ObservableCollection<Building> AvailableBuildings { get; } = [];

    /// <summary>
    /// Available serving sessions from the menu response.
    /// </summary>
    public ObservableCollection<string> AvailableSessions { get; } = [];

    /// <summary>
    /// Theme list items (headers + selectable themes) for the categorized dropdown.
    /// </summary>
    public ObservableCollection<ThemeListItem> ThemeListItems { get; } = [];

    /// <summary>
    /// Forced home day options (Mon–Fri) for the current session.
    /// </summary>
    public ObservableCollection<ForcedHomeDayOption> ForcedHomeDays { get; } = [];

    [ObservableProperty]
    private bool _hasRecipes;

    [ObservableProperty]
    private bool _hasBuildings;

    [ObservableProperty]
    private bool _hasSessions;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private string _statusText = "Ready. Enter an identifier code and click Look Up.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FetchFromApiCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadFromHarCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCalendarCommand))]
    [NotifyCanExecuteChangedFor(nameof(LookupIdentifierCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _generatedHtmlPath;

    [ObservableProperty]
    private string? _generatedHtml;

    [ObservableProperty]
    private string _identifierCode = string.Empty;

    [ObservableProperty]
    private Building? _selectedBuilding;

    [ObservableProperty]
    private string? _selectedSession;

    [ObservableProperty]
    private string? _districtId;

    [ObservableProperty]
    private string? _districtName;

    [ObservableProperty]
    private string _recipeSearchText = string.Empty;

    [ObservableProperty]
    private string _favoriteSearchText = string.Empty;

    [ObservableProperty]
    private bool _isAllergenExpanded;

    [ObservableProperty]
    private string _allergenSummary = "None selected";

    [ObservableProperty]
    private CalendarTheme _selectedTheme;

    private ThemeListItem? _selectedThemeItem;

    /// <summary>
    /// The currently selected item in the theme ComboBox. Rejects header selections.
    /// </summary>
    public ThemeListItem? SelectedThemeItem
    {
        get => _selectedThemeItem;
        set
        {
            if (value is null || value.IsHeader)
                return; // Reject header selections
            if (SetProperty(ref _selectedThemeItem, value) && value.Theme is not null)
                SelectedTheme = value.Theme;
        }
    }

    private bool _themeManuallySelected;
    private bool _suppressThemeManualFlag;

    partial void OnSelectedBuildingChanged(Building? value) => ScheduleSettingsSave();

    partial void OnSelectedSessionChanged(string? value)
    {
        if (value is not null && _lastMenuResponse is not null)
        {
            _ = PopulateRecipeNamesAsync(_lastMenuResponse);
        }
        LoadForcedHomeDays();
        ScheduleSettingsSave();
    }

    partial void OnSelectedMonthChanged(int value)
    {
        if (!_themeManuallySelected)
            AutoSuggestTheme(value);
    }

    partial void OnSelectedThemeChanged(CalendarTheme value)
    {
        if (!_suppressThemeManualFlag)
            _themeManuallySelected = true;
        ScheduleSettingsSave();
    }

    partial void OnRecipeSearchTextChanged(string value) => RebuildFilteredRecipes();

    partial void OnFavoriteSearchTextChanged(string value) => RebuildFilteredFavorites();

    /// <summary>
    /// Loads persisted settings and attempts to preload cached menu data.
    /// If a HAR file path is provided (from command-line arg or drag-drop), loads that instead.
    /// </summary>
    public async Task InitializeAsync(string? harFilePath = null)
    {
        var settings = await _settingsService.LoadAsync();

        // Build categorized theme list (filtering hidden themes)
        BuildThemeList(settings.HiddenThemeNames);

        // Restore theme selection
        if (!string.IsNullOrEmpty(settings.SelectedThemeName))
        {
            var saved = ThemeListItems.FirstOrDefault(t => t.Theme?.Name == settings.SelectedThemeName);
            if (saved is not null)
            {
                _suppressThemeManualFlag = true;
                _selectedThemeItem = saved;
                OnPropertyChanged(nameof(SelectedThemeItem));
                SelectedTheme = saved.Theme!;
                _suppressThemeManualFlag = false;
                _themeManuallySelected = true;
            }
        }

        // Restore identifier and district from settings
        if (!string.IsNullOrEmpty(settings.Identifier))
            IdentifierCode = settings.Identifier;
        if (!string.IsNullOrEmpty(settings.DistrictId))
            DistrictId = settings.DistrictId;

        // If a HAR file was provided via command-line arg, load it directly
        if (!string.IsNullOrEmpty(harFilePath))
        {
            await LoadHarFileAsync(harFilePath);
            return;
        }

        // Try to preload from disk cache
        await TryLoadMenuCacheAsync(settings);
    }

    /// <summary>
    /// Loads a HAR file from a specific path (for drag-drop and command-line arg support).
    /// </summary>
    public async Task LoadHarFileAsync(string harFilePath)
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusText = $"Loading HAR file: {Path.GetFileName(harFilePath)}...";

        try
        {
            var (menu, allergies, identifier) = await _harFileService.LoadFromHarFileAsync(harFilePath);
            _lastMenuResponse = menu;
            _lastIdentifierResponse = identifier;

            // Populate buildings from identifier response
            PopulateBuildings(identifier);

            await PopulateAllergensAsync(allergies);
            PopulateSessions(menu);
            DetectMonthFromMenu(menu);

            // Persist to disk cache so it's available on next launch
            await _settingsService.SaveMenuCacheAsync(new MenuCache
            {
                SavedAtUtc = DateTime.UtcNow,
                MenuResponse = menu,
                Allergies = allergies,
                IdentifierResponse = identifier
            });

            GenerateCalendarCommand.NotifyCanExecuteChanged();
            StatusText = $"HAR file loaded ({allergies.Count} allergens, {identifier.DistrictName}). Click Generate to create calendar.";
            _logger.LogInformation("Loaded HAR file {File} for {District}", harFilePath, identifier.DistrictName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load HAR file {File}", harFilePath);
            StatusText = $"Error loading HAR file: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Saves current settings to disk.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        var notPreferredBySession = new Dictionary<string, List<string>>();
        var favoritesBySession = new Dictionary<string, List<string>>();

        // Save current session's preferences
        var session = SelectedSession;
        if (!string.IsNullOrEmpty(session))
        {
            var notPreferred = _allRecipes
                .Where(r => r.IsNotPreferred)
                .Select(r => r.Name)
                .ToList();
            if (notPreferred.Count > 0)
                notPreferredBySession[session] = notPreferred;

            var favorites = _allFavorites
                .Where(f => f.IsFavorite)
                .Select(f => f.Name)
                .ToList();
            if (favorites.Count > 0)
                favoritesBySession[session] = favorites;
        }

        // Preserve other sessions' preferences from saved settings
        var existing = await _settingsService.LoadAsync();
        foreach (var (key, value) in existing.NotPreferredBySession)
        {
            if (!key.Equals(session, StringComparison.OrdinalIgnoreCase))
                notPreferredBySession[key] = value;
        }
        foreach (var (key, value) in existing.FavoritesBySession)
        {
            if (!key.Equals(session, StringComparison.OrdinalIgnoreCase))
                favoritesBySession[key] = value;
        }

        // Build forced home days per session
        var forcedHomeDaysBySession = new Dictionary<string, List<string>>(existing.ForcedHomeDaysBySession);
        if (!string.IsNullOrEmpty(session))
        {
            var checkedDays = ForcedHomeDays
                .Where(d => d.IsForced)
                .Select(d => d.DayOfWeek.ToString())
                .ToList();
            if (checkedDays.Count > 0)
                forcedHomeDaysBySession[session] = checkedDays;
            else
                forcedHomeDaysBySession.Remove(session);
        }

        var settings = new AppSettings
        {
            SelectedAllergenIds = AvailableAllergens
                .Where(a => a.IsSelected)
                .Select(a => a.AllergyId)
                .ToList(),
            ForcedHomeDaysBySession = forcedHomeDaysBySession,
            NotPreferredBySession = notPreferredBySession,
            FavoritesBySession = favoritesBySession,
            Identifier = IdentifierCode,
            DistrictId = DistrictId,
            BuildingId = SelectedBuilding?.BuildingId,
            SelectedSessionName = SelectedSession,
            SelectedThemeName = SelectedTheme.Name,
            HiddenThemeNames = existing.HiddenThemeNames
        };
        await _settingsService.SaveAsync(settings);
    }

    /// <summary>
    /// Schedules a debounced settings save (500ms delay to batch rapid changes).
    /// </summary>
    private void ScheduleSettingsSave()
    {
        // Don't save during initial load before allergens are populated
        if (AvailableAllergens.Count == 0) return;

        _saveDebounce?.Cancel();
        _saveDebounce = new CancellationTokenSource();
        var token = _saveDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                    await SaveSettingsAsync();
            }
            catch (TaskCanceledException)
            {
                // Debounce cancelled by a newer change, expected
            }
        }, token);
    }

    /// <summary>
    /// Looks up district and buildings from the identifier code.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LookupIdentifierAsync()
    {
        if (string.IsNullOrWhiteSpace(IdentifierCode))
        {
            StatusText = "Please enter an identifier code.";
            return;
        }

        IsBusy = true;
        StatusText = $"Looking up identifier \"{IdentifierCode}\"...";

        try
        {
            var identifier = await _apiService.GetMenuIdentifierAsync(IdentifierCode.Trim());
            _lastIdentifierResponse = identifier;
            DistrictId = identifier.DistrictId;
            DistrictName = identifier.DistrictName;

            PopulateBuildings(identifier);

            // Load allergens for this district
            StatusText = "Fetching allergen list...";
            var allergies = await _apiService.GetAllergiesAsync(identifier.DistrictId);
            await PopulateAllergensAsync(allergies);

            StatusText = $"Found {identifier.Buildings.Count} building(s) in {identifier.DistrictName}. Select a building and fetch menu data.";
            _logger.LogInformation("Identifier lookup: {District} with {Count} buildings", identifier.DistrictName, identifier.Buildings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up identifier {Code}", IdentifierCode);
            StatusText = $"Error looking up identifier: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Fetches menu data from the LINQ Connect API.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task FetchFromApiAsync()
    {
        if (SelectedBuilding is null || string.IsNullOrEmpty(DistrictId))
        {
            StatusText = "Please look up an identifier and select a building first.";
            return;
        }

        IsBusy = true;
        StatusText = "Fetching menu data from API...";

        try
        {
            // Load allergens if not yet loaded
            if (AvailableAllergens.Count == 0)
            {
                StatusText = "Fetching allergen list...";
                var allergies = await _apiService.GetAllergiesAsync(DistrictId);
                await PopulateAllergensAsync(allergies);
            }

            // Fetch menu for selected month
            var startDate = new DateOnly(SelectedYear, SelectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            StatusText = $"Fetching menu for {startDate:MMMM yyyy}...";
            var menuResponse = await _apiService.GetMenuAsync(SelectedBuilding.BuildingId, DistrictId, startDate, endDate);

            // Set _lastMenuResponse after PopulateSessions to prevent OnSelectedSessionChanged
            // from firing a duplicate PopulateRecipeNamesAsync via the fire-and-forget path.
            PopulateSessions(menuResponse);
            _lastMenuResponse = menuResponse;
            await PopulateRecipeNamesAsync(menuResponse);

            // Persist to disk cache for preloading on next launch
            var allergyCopy = AvailableAllergens.Select(a => new AllergyItem
            {
                AllergyId = a.AllergyId,
                Name = a.Name,
                SortOrder = 0
            }).ToList();

            await _settingsService.SaveMenuCacheAsync(new MenuCache
            {
                SavedAtUtc = DateTime.UtcNow,
                MenuResponse = _lastMenuResponse,
                Allergies = allergyCopy,
                IdentifierResponse = _lastIdentifierResponse
            });

            GenerateCalendarCommand.NotifyCanExecuteChanged();
            StatusText = "Menu data loaded from API. Click Generate to create calendar.";
            _logger.LogInformation("Menu data fetched from API for {Month}/{Year}", SelectedMonth, SelectedYear);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch menu data from API");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads menu data from a HAR file via file picker dialog.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task LoadFromHarAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select HAR File",
            Filter = "HAR Files (*.har)|*.har|All Files (*.*)|*.*",
            DefaultExt = ".har"
        };

        if (dialog.ShowDialog() != true)
            return;

        await LoadHarFileAsync(dialog.FileName);
    }

    /// <summary>
    /// Generates the HTML calendar from loaded menu data.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateCalendarAsync()
    {
        if (_lastMenuResponse is null)
        {
            StatusText = "No menu data loaded. Fetch from API or load a HAR file first.";
            return;
        }

        if (string.IsNullOrEmpty(SelectedSession))
        {
            StatusText = "Please select a serving session.";
            return;
        }

        IsBusy = true;
        StatusText = "Analyzing menu and generating calendar...";

        try
        {
            // Capture UI state before moving to background thread
            var selectedAllergenIds = AvailableAllergens
                .Where(a => a.IsSelected)
                .Select(a => a.AllergyId)
                .ToHashSet();

            var selectedAllergenNames = AvailableAllergens
                .Where(a => a.IsSelected)
                .Select(a => a.Name)
                .ToList();

            var notPreferredNames = _allRecipes
                .Where(r => r.IsNotPreferred)
                .Select(r => r.Name)
                .ToHashSet();

            var favoriteNames = _allFavorites
                .Where(f => f.IsFavorite)
                .Select(f => f.Name)
                .ToHashSet();

            var year = SelectedYear;
            var month = SelectedMonth;
            var forcedHomeDays = ForcedHomeDays
                .Where(d => d.IsForced)
                .Select(d => d.DayOfWeek)
                .ToHashSet();
            var menuResponse = _lastMenuResponse;
            var sessionName = SelectedSession;
            var buildingName = SelectedBuilding?.Name;
            var theme = SelectedTheme;

            // Run CPU-bound analysis and HTML generation on a background thread
            var (html, tempPath) = await Task.Run(() =>
            {
                var processedMonth = _menuAnalyzer.Analyze(menuResponse, selectedAllergenIds, notPreferredNames, favoriteNames, year, month, sessionName, buildingName);

                var generatedHtml = _calendarGenerator.Generate(
                    processedMonth,
                    selectedAllergenNames,
                    forcedHomeDays,
                    theme);

                var dir = Path.Combine(Path.GetTempPath(), "SchoolLunchMenu");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"LunchCalendar_{year}-{month:D2}.html");
                File.WriteAllText(path, generatedHtml);

                return (generatedHtml, path);
            });

            GeneratedHtmlPath = tempPath;
            GeneratedHtml = html;

            // Save settings after generation
            await SaveSettingsAsync();

            StatusText = $"Calendar generated! Saved to {tempPath}";
            _logger.LogInformation("Calendar generated and saved to {Path}", tempPath);
            OpenInBrowserCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate calendar");
            StatusText = $"Error generating calendar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteCommands() => !IsBusy;
    private bool CanGenerate() => !IsBusy && _lastMenuResponse is not null;
    private bool CanOpenInBrowser() => GeneratedHtmlPath is not null;

    /// <summary>
    /// Opens the generated HTML calendar in the default browser.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenInBrowser))]
    private void OpenInBrowser()
    {
        Process.Start(new ProcessStartInfo(GeneratedHtmlPath!) { UseShellExecute = true });
    }

    /// <summary>
    /// Loads forced home day selections for the current session from settings, defaulting to Thursday.
    /// </summary>
    private void LoadForcedHomeDays()
    {
        // Unsubscribe from existing events
        foreach (var option in ForcedHomeDays)
            option.PropertyChanged -= OnForcedHomeDayChanged;

        ForcedHomeDays.Clear();

        var session = SelectedSession ?? "";
        var settings = _settingsService.LoadAsync().GetAwaiter().GetResult();

        HashSet<DayOfWeek> checkedDays;
        if (settings.ForcedHomeDaysBySession.TryGetValue(session, out var savedDays))
        {
            checkedDays = savedDays
                .Where(d => Enum.TryParse<DayOfWeek>(d, out _))
                .Select(d => Enum.Parse<DayOfWeek>(d))
                .ToHashSet();
        }
        else
        {
            // Default to Thursday when no saved entry exists
            checkedDays = [DayOfWeek.Thursday];
        }

        DayOfWeek[] weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
        foreach (var dow in weekdays)
        {
            var option = new ForcedHomeDayOption(dow, checkedDays.Contains(dow));
            option.PropertyChanged += OnForcedHomeDayChanged;
            ForcedHomeDays.Add(option);
        }
    }

    /// <summary>
    /// Handles forced home day checkbox changes to auto-save settings.
    /// </summary>
    private void OnForcedHomeDayChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForcedHomeDayOption.IsForced))
            ScheduleSettingsSave();
    }

    /// <summary>
    /// Populates the building list from an identifier response and restores saved selection.
    /// </summary>
    private void PopulateBuildings(FamilyMenuIdentifierResponse identifier)
    {
        AvailableBuildings.Clear();
        foreach (var building in identifier.Buildings)
            AvailableBuildings.Add(building);

        HasBuildings = AvailableBuildings.Count > 0;
        DistrictId = identifier.DistrictId;
        DistrictName = identifier.DistrictName;

        // Try to restore saved building selection
        var settings = _settingsService.LoadAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(settings.BuildingId))
        {
            SelectedBuilding = AvailableBuildings.FirstOrDefault(b => b.BuildingId == settings.BuildingId);
        }

        // Default to first building if none selected
        SelectedBuilding ??= AvailableBuildings.FirstOrDefault();
    }

    /// <summary>
    /// Populates the session dropdown from a menu response and restores saved selection.
    /// </summary>
    private void PopulateSessions(FamilyMenuResponse menuResponse)
    {
        AvailableSessions.Clear();
        foreach (var session in menuResponse.FamilyMenuSessions)
        {
            if (!string.IsNullOrEmpty(session.ServingSession))
                AvailableSessions.Add(session.ServingSession);
        }

        HasSessions = AvailableSessions.Count > 0;

        // Try to restore saved session selection
        var settings = _settingsService.LoadAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(settings.SelectedSessionName) && AvailableSessions.Contains(settings.SelectedSessionName))
        {
            SelectedSession = settings.SelectedSessionName;
        }
        else
        {
            // Default to "Lunch" if available, otherwise first session
            SelectedSession = AvailableSessions.Contains("Lunch") ? "Lunch" : AvailableSessions.FirstOrDefault();
        }
    }

    /// <summary>
    /// Populates the allergen list and applies saved selections or defaults.
    /// </summary>
    private async Task PopulateAllergensAsync(List<AllergyItem> allergies)
    {
        // Unsubscribe from any existing allergen change events
        foreach (var existing in AvailableAllergens)
            existing.PropertyChanged -= OnAllergenSelectionChanged;

        AvailableAllergens.Clear();
        var savedIds = new HashSet<string>();

        // Load saved selections without blocking the UI thread
        var settings = await _settingsService.LoadAsync();
        if (settings.SelectedAllergenIds.Count > 0)
            savedIds = settings.SelectedAllergenIds.ToHashSet();

        foreach (var allergy in allergies.OrderBy(a => a.Name))
        {
            var isSelected = savedIds.Count > 0
                ? savedIds.Contains(allergy.AllergyId)
                : allergy.Name.Equals("Milk", StringComparison.OrdinalIgnoreCase); // Default to Milk by name on first launch

            var option = new AllergenOption(allergy.AllergyId, allergy.Name, isSelected);
            option.PropertyChanged += OnAllergenSelectionChanged;
            AvailableAllergens.Add(option);
        }

        UpdateAllergenSummary();
        GenerateCalendarCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Handles allergen checkbox changes to auto-save settings and update filtered lists.
    /// </summary>
    private void OnAllergenSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AllergenOption.IsSelected))
        {
            UpdateAllergenSummary();
            RebuildFilteredRecipes();
            RebuildFilteredFavorites();
            ScheduleSettingsSave();
        }
    }

    /// <summary>
    /// Updates the allergen summary text shown when the expander is collapsed.
    /// </summary>
    private void UpdateAllergenSummary()
    {
        var selected = AvailableAllergens
            .Where(a => a.IsSelected)
            .Select(a => a.Name)
            .ToList();

        AllergenSummary = selected.Count > 0
            ? string.Join(", ", selected)
            : "None selected";
    }

    /// <summary>
    /// Handles not-preferred checkbox changes to auto-save settings and rebuild filtered list.
    /// </summary>
    private void OnNotPreferredSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotPreferredOption.IsNotPreferred))
        {
            RebuildFilteredRecipes();
            ScheduleSettingsSave();
        }
    }

    /// <summary>
    /// Handles favorite checkbox changes to auto-save settings and rebuild filtered list.
    /// </summary>
    private void OnFavoriteSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FavoriteOption.IsFavorite))
        {
            RebuildFilteredFavorites();
            ScheduleSettingsSave();
        }
    }

    /// <summary>
    /// Returns the set of currently selected allergen IDs.
    /// </summary>
    private HashSet<string> GetSelectedAllergenIds()
    {
        return AvailableAllergens
            .Where(a => a.IsSelected)
            .Select(a => a.AllergyId)
            .ToHashSet();
    }

    /// <summary>
    /// Rebuilds the filtered not-preferred list: checked items always shown, plus unchecked items matching search.
    /// Items containing selected allergens are hidden.
    /// </summary>
    private void RebuildFilteredRecipes()
    {
        FilteredRecipes.Clear();
        var search = RecipeSearchText.Trim();
        var selectedAllergenIds = GetSelectedAllergenIds();

        // Always show checked items first (unless they contain a selected allergen)
        foreach (var item in _allRecipes.Where(r => r.IsNotPreferred))
        {
            if (item.AllergenIds.Overlaps(selectedAllergenIds))
                continue;
            FilteredRecipes.Add(item);
        }

        // Show unchecked items that match search (if search has text)
        if (search.Length > 0)
        {
            foreach (var item in _allRecipes.Where(r => !r.IsNotPreferred && r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.AllergenIds.Overlaps(selectedAllergenIds))
                    continue;
                FilteredRecipes.Add(item);
            }
        }
    }

    /// <summary>
    /// Rebuilds the filtered favorites list: checked items always shown, plus unchecked items matching search.
    /// Items containing selected allergens are hidden.
    /// </summary>
    private void RebuildFilteredFavorites()
    {
        FilteredFavorites.Clear();
        var search = FavoriteSearchText.Trim();
        var selectedAllergenIds = GetSelectedAllergenIds();

        // Always show checked items first (unless they contain a selected allergen)
        foreach (var item in _allFavorites.Where(f => f.IsFavorite))
        {
            if (item.AllergenIds.Overlaps(selectedAllergenIds))
                continue;
            FilteredFavorites.Add(item);
        }

        // Show unchecked items that match search (if search has text)
        if (search.Length > 0)
        {
            foreach (var item in _allFavorites.Where(f => !f.IsFavorite && f.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.AllergenIds.Overlaps(selectedAllergenIds))
                    continue;
                FilteredFavorites.Add(item);
            }
        }
    }

    /// <summary>
    /// Extracts unique entree recipe names from the current menu response and populates the not-preferred and favorites checklists.
    /// Also builds the recipe-to-allergen mapping for filtering.
    /// </summary>
    private async Task PopulateRecipeNamesAsync(FamilyMenuResponse menuResponse)
    {
        // Unsubscribe from existing events
        foreach (var existing in _allRecipes)
            existing.PropertyChanged -= OnNotPreferredSelectionChanged;
        foreach (var existing in _allFavorites)
            existing.PropertyChanged -= OnFavoriteSelectionChanged;

        _allRecipes.Clear();
        _allFavorites.Clear();
        FilteredRecipes.Clear();
        FilteredFavorites.Clear();
        _recipeAllergenMap.Clear();

        var settings = await _settingsService.LoadAsync();
        var sessionName = SelectedSession ?? "";

        // Load per-session saved names
        settings.NotPreferredBySession.TryGetValue(sessionName, out var savedNotPreferred);
        var notPreferredSet = savedNotPreferred?.ToHashSet() ?? [];

        settings.FavoritesBySession.TryGetValue(sessionName, out var savedFavorites);
        var favoritesSet = savedFavorites?.ToHashSet() ?? [];

        var uniqueNames = new HashSet<string>();
        _recipeAllergenMap = new Dictionary<string, HashSet<string>>();

        // Use selected session if available, otherwise scan all sessions
        IEnumerable<MenuSession> sessions;
        if (!string.IsNullOrEmpty(sessionName))
        {
            var match = menuResponse.FamilyMenuSessions
                .FirstOrDefault(s => s.ServingSession.Equals(sessionName, StringComparison.OrdinalIgnoreCase));
            sessions = match is not null ? [match] : [];
        }
        else
        {
            sessions = menuResponse.FamilyMenuSessions;
        }

        foreach (var session in sessions)
        {
            foreach (var plan in session.MenuPlans)
            {
                foreach (var day in plan.Days)
                {
                    foreach (var meal in day.MenuMeals ?? [])
                    {
                        foreach (var category in meal.RecipeCategories ?? [])
                        {
                            if (!category.IsEntree) continue;
                            foreach (var recipe in category.Recipes ?? [])
                            {
                                uniqueNames.Add(recipe.RecipeName);

                                // Build allergen mapping: merge allergen IDs per recipe name
                                if (!_recipeAllergenMap.TryGetValue(recipe.RecipeName, out var allergenIds))
                                {
                                    allergenIds = [];
                                    _recipeAllergenMap[recipe.RecipeName] = allergenIds;
                                }
                                foreach (var allergenId in recipe.Allergens)
                                    allergenIds.Add(allergenId);
                            }
                        }
                    }
                }
            }
        }

        foreach (var name in uniqueNames.OrderBy(n => n))
        {
            _recipeAllergenMap.TryGetValue(name, out var recipeAllergens);
            IReadOnlySet<string> allergenIds = recipeAllergens is not null
                ? recipeAllergens
                : new HashSet<string>();

            var notPrefOption = new NotPreferredOption(name, notPreferredSet.Contains(name), allergenIds);
            notPrefOption.PropertyChanged += OnNotPreferredSelectionChanged;
            _allRecipes.Add(notPrefOption);

            var favOption = new FavoriteOption(name, favoritesSet.Contains(name), allergenIds);
            favOption.PropertyChanged += OnFavoriteSelectionChanged;
            _allFavorites.Add(favOption);
        }

        HasRecipes = _allRecipes.Count > 0;
        RecipeSearchText = string.Empty;
        FavoriteSearchText = string.Empty;
        RebuildFilteredRecipes();
        RebuildFilteredFavorites();
    }

    /// <summary>
    /// Attempts to load cached menu data from disk on startup.
    /// </summary>
    private async Task TryLoadMenuCacheAsync(AppSettings settings)
    {
        try
        {
            var cache = await _settingsService.LoadMenuCacheAsync();
            if (cache?.MenuResponse is null || cache.Allergies is null)
                return;

            // Restore identifier response if cached
            if (cache.IdentifierResponse is not null)
            {
                _lastIdentifierResponse = cache.IdentifierResponse;
                PopulateBuildings(cache.IdentifierResponse);
            }

            await PopulateAllergensAsync(cache.Allergies);

            // Set _lastMenuResponse after PopulateSessions to prevent OnSelectedSessionChanged
            // from firing a duplicate PopulateRecipeNamesAsync via the fire-and-forget path.
            PopulateSessions(cache.MenuResponse);
            _lastMenuResponse = cache.MenuResponse;
            await PopulateRecipeNamesAsync(cache.MenuResponse);

            // Apply saved allergen selections from settings (overrides defaults)
            if (settings.SelectedAllergenIds.Count > 0)
            {
                foreach (var allergen in AvailableAllergens)
                    allergen.IsSelected = settings.SelectedAllergenIds.Contains(allergen.AllergyId);
            }

            // Restore saved session
            if (!string.IsNullOrEmpty(settings.SelectedSessionName) && AvailableSessions.Contains(settings.SelectedSessionName))
                SelectedSession = settings.SelectedSessionName;

            LoadForcedHomeDays();
            DetectMonthFromMenu(cache.MenuResponse);
            GenerateCalendarCommand.NotifyCanExecuteChanged();

            var age = DateTime.UtcNow - cache.SavedAtUtc;
            StatusText = $"Loaded cached menu data (saved {FormatAge(age)} ago). Ready to generate, or fetch fresh data.";
            _logger.LogInformation("Preloaded menu cache (age: {Age})", age);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload menu cache, continuing without it");
        }
    }

    /// <summary>
    /// Detects the month/year from menu data and updates the selectors.
    /// </summary>
    private void DetectMonthFromMenu(FamilyMenuResponse menu)
    {
        // Use selected session if available, otherwise try any session
        var sessionName = SelectedSession;
        MenuSession? session;
        if (!string.IsNullOrEmpty(sessionName))
        {
            session = menu.FamilyMenuSessions
                .FirstOrDefault(s => s.ServingSession.Equals(sessionName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            session = menu.FamilyMenuSessions.FirstOrDefault();
        }

        var firstDate = session?.MenuPlans
            .SelectMany(p => p.Days)
            .Select(d => d.Date)
            .FirstOrDefault();

        if (firstDate is not null && DateTime.TryParse(firstDate, out var parsed))
        {
            SelectedMonth = parsed.Month;
            SelectedYear = parsed.Year;

            if (!AvailableYears.Contains(parsed.Year))
                AvailableYears.Add(parsed.Year);
        }
    }

    /// <summary>
    /// Auto-suggests a theme based on the selected month, if the user hasn't manually picked one.
    /// </summary>
    private void AutoSuggestTheme(int month)
    {
        // Search visible themes in the list (respects hidden themes filtering)
        var visibleThemes = ThemeListItems
            .Where(t => t.Theme is not null)
            .Select(t => t.Theme!);

        var suggested = visibleThemes.FirstOrDefault(t =>
            t.SuggestedMonth == month || t.SuggestedMonth2 == month);

        var target = suggested ?? visibleThemes.FirstOrDefault(t => t.Name == "Default") ?? CalendarThemes.All.Last();
        if (SelectedTheme != target)
        {
            _suppressThemeManualFlag = true;
            SelectedTheme = target;
            var item = ThemeListItems.FirstOrDefault(t => t.Theme == target);
            if (item is not null)
            {
                _selectedThemeItem = item;
                OnPropertyChanged(nameof(SelectedThemeItem));
            }
            _suppressThemeManualFlag = false;
        }
    }

    /// <summary>
    /// Builds the categorized theme list, filtering out hidden themes.
    /// </summary>
    private void BuildThemeList(List<string> hiddenThemeNames)
    {
        ThemeListItems.Clear();
        var hiddenSet = hiddenThemeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var categories = new[] { "Seasonal", "Fun", "Basic" };
        foreach (var category in categories)
        {
            var themes = CalendarThemes.All
                .Where(t => t.Category == category && !hiddenSet.Contains(t.Name))
                .ToList();

            if (themes.Count == 0)
                continue;

            ThemeListItems.Add(new ThemeListItem { HeaderText = category });
            foreach (var theme in themes)
                ThemeListItems.Add(new ThemeListItem { Theme = theme });
        }
    }

    /// <summary>
    /// Formats a TimeSpan as a human-readable age string.
    /// </summary>
    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h {age.Minutes}m";
        return $"{(int)age.TotalDays}d";
    }
}

/// <summary>
/// Represents a month option for the month selector dropdown.
/// </summary>
/// <param name="Number">Month number (1-12).</param>
/// <param name="Name">Display name of the month.</param>
public record MonthOption(int Number, string Name)
{
    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
/// Represents an allergen with a selection state for the checklist UI.
/// </summary>
public partial class AllergenOption : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of <see cref="AllergenOption"/>.
    /// </summary>
    public AllergenOption(string allergyId, string name, bool isSelected)
    {
        AllergyId = allergyId;
        Name = name;
        _isSelected = isSelected;
    }

    /// <summary>The allergen UUID.</summary>
    public string AllergyId { get; }

    /// <summary>The display name of the allergen.</summary>
    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Represents a recipe with a not-preferred selection state for the checklist UI.
/// </summary>
public partial class NotPreferredOption : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotPreferredOption"/>.
    /// </summary>
    public NotPreferredOption(string name, bool isNotPreferred, IReadOnlySet<string> allergenIds)
    {
        Name = name;
        _isNotPreferred = isNotPreferred;
        AllergenIds = allergenIds;
    }

    /// <summary>The display name of the recipe.</summary>
    public string Name { get; }

    /// <summary>The allergen IDs associated with this recipe.</summary>
    public IReadOnlySet<string> AllergenIds { get; }

    [ObservableProperty]
    private bool _isNotPreferred;
}

/// <summary>
/// Represents a recipe with a favorite selection state for the checklist UI.
/// </summary>
public partial class FavoriteOption : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of <see cref="FavoriteOption"/>.
    /// </summary>
    public FavoriteOption(string name, bool isFavorite, IReadOnlySet<string> allergenIds)
    {
        Name = name;
        _isFavorite = isFavorite;
        AllergenIds = allergenIds;
    }

    /// <summary>The display name of the recipe.</summary>
    public string Name { get; }

    /// <summary>The allergen IDs associated with this recipe.</summary>
    public IReadOnlySet<string> AllergenIds { get; }

    [ObservableProperty]
    private bool _isFavorite;
}

/// <summary>
/// Wrapper for theme ComboBox items: either a non-selectable section header or a selectable theme.
/// </summary>
public class ThemeListItem
{
    /// <summary>Non-null for section headers (e.g., "Seasonal", "Fun").</summary>
    public string? HeaderText { get; init; }

    /// <summary>Non-null for selectable theme entries.</summary>
    public CalendarTheme? Theme { get; init; }

    /// <summary>True if this item is a non-selectable category header.</summary>
    public bool IsHeader => HeaderText is not null;

    /// <summary>Display text for the ComboBox item.</summary>
    public string DisplayText => IsHeader
        ? $"── {HeaderText} ──"
        : Theme is not null ? $"{Theme.Emoji} {Theme.Name}" : "";
}

/// <summary>
/// Represents a day-of-week option for the forced home days checklist.
/// </summary>
public partial class ForcedHomeDayOption : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForcedHomeDayOption"/>.
    /// </summary>
    public ForcedHomeDayOption(DayOfWeek dayOfWeek, bool isForced)
    {
        DayOfWeek = dayOfWeek;
        DisplayName = dayOfWeek.ToString();
        _isForced = isForced;
    }

    /// <summary>The day of the week.</summary>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>The display name (e.g., "Monday").</summary>
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isForced;
}
