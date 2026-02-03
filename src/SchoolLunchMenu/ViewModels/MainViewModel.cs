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
    private readonly IDayLabelFetchService _dayLabelFetchService;
    private readonly ILogger<MainViewModel> _logger;

    private FamilyMenuResponse? _lastMenuResponse;
    private FamilyMenuIdentifierResponse? _lastIdentifierResponse;

    /// <summary>Debounce timer for auto-saving settings after user changes.</summary>
    private CancellationTokenSource? _saveDebounce;

    /// <summary>Tracks whether a debounced save is pending and needs flushing.</summary>
    private bool _savePending;

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
        IDayLabelFetchService dayLabelFetchService,
        ILogger<MainViewModel> logger)
    {
        _apiService = apiService;
        _harFileService = harFileService;
        _menuAnalyzer = menuAnalyzer;
        _calendarGenerator = calendarGenerator;
        _settingsService = settingsService;
        _dayLabelFetchService = dayLabelFetchService;
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

    /// <summary>
    /// Plan label override entries for customizing vending button labels.
    /// </summary>
    public ObservableCollection<PlanLabelEntry> PlanLabelEntries { get; } = [];

    /// <summary>
    /// Holiday override entries for customizing no-school day icons and messages.
    /// </summary>
    public ObservableCollection<HolidayOverrideEntry> HolidayOverrideEntries { get; } = [];

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
    [NotifyCanExecuteChangedFor(nameof(FetchDayLabelsCommand))]
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
    private string _selectedLayoutMode = "IconsLeft";

    [ObservableProperty]
    private bool _showUnsafeLines;

    [ObservableProperty]
    private string _unsafeLineMessage = "No safe options";

    [ObservableProperty]
    private string _allergenSummary = "None selected";

    [ObservableProperty]
    private bool _crossOutPastDays;

    [ObservableProperty]
    private bool _showShareFooter;

    [ObservableProperty]
    private string _dayLabelStartDate = "";

    [ObservableProperty]
    private string _dayLabelCorner = "TopRight";

    /// <summary>Observable collection of day label cycle entries for the sidebar.</summary>
    public ObservableCollection<DayLabelEntry> DayLabelEntries { get; } = [];

    /// <summary>Curated color suggestions for day label color selection.</summary>
    public static IReadOnlyList<string> DayLabelColorSuggestions { get; } =
    [
        "#dc3545", "#adb5bd", "#0d6efd", "#198754", "#fd7e14",
        "#6f42c1", "#d63384", "#0dcaf0", "#ffc107", "#20c997"
    ];

    /// <summary>Available corner positions for the day label triangle.</summary>
    public static IReadOnlyList<string> DayLabelCornerOptions { get; } =
    [
        "TopRight", "TopLeft", "BottomRight", "BottomLeft"
    ];

    [ObservableProperty]
    private int _previewZoom = 75;

    /// <summary>The base HTML without zoom, saved to file and used as the source for preview zoom injection.</summary>
    private string? _baseGeneratedHtml;

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

    partial void OnSelectedBuildingChanged(Building? value)
    {
        OnPropertyChanged(nameof(SourceUrl));
        ScheduleSettingsSave();
    }

    partial void OnSelectedSessionChanged(string? value)
    {
        // Flush any pending save for the previous session before loading the new one
        FlushPendingSave();

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

    partial void OnIdentifierCodeChanged(string value) => OnPropertyChanged(nameof(SourceUrl));

    partial void OnSelectedLayoutModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsGridMode));
        ScheduleSettingsSave();
    }

    partial void OnPreviewZoomChanged(int value) => ApplyPreviewZoom();

    partial void OnShowUnsafeLinesChanged(bool value) => ScheduleSettingsSave();

    partial void OnUnsafeLineMessageChanged(string value) => ScheduleSettingsSave();

    partial void OnCrossOutPastDaysChanged(bool value) => ScheduleSettingsSave();

    partial void OnShowShareFooterChanged(bool value) => ScheduleSettingsSave();

    partial void OnDayLabelStartDateChanged(string value) => ScheduleSettingsSave();

    partial void OnDayLabelCornerChanged(string value) => ScheduleSettingsSave();

    /// <summary>Whether the grid buttons are active (used for sidebar visibility of plan label entries).</summary>
    public bool IsGridMode => SelectedLayoutMode is "IconsLeft" or "IconsRight";

    /// <summary>Available layout mode options for the ComboBox.</summary>
    public static IReadOnlyList<LayoutModeOption> LayoutModeOptions { get; } =
    [
        new("List", "📋 List"),
        new("IconsLeft", "⬅️ Icons Left"),
        new("IconsRight", "➡️ Icons Right"),
    ];

    /// <summary>Curated emoji suggestions for plan icon selection.</summary>
    public static IReadOnlyList<string> PlanIconSuggestions { get; } =
    [
        "", "🍽️", "🐱", "🐾", "🦅", "🐦", "🦁", "🐻", "🐺", "🐯",
        "🍕", "🌮", "🍔", "🥗", "🍎", "🥪", "🍝", "🍲",
        "⭐", "🔵", "🟢", "🟡", "🔴", "🟣", "❤️", "💙", "💚",
        "1️⃣", "2️⃣", "3️⃣", "🅰️", "🅱️",
    ];

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

        // Migrate legacy ShowMealButtons to LayoutMode
        if (string.IsNullOrEmpty(settings.LayoutMode) || settings.LayoutMode == "List")
        {
            SelectedLayoutMode = settings.ShowMealButtons ? "IconsRight" : "List";
        }
        else
        {
            SelectedLayoutMode = settings.LayoutMode;
        }

        ShowUnsafeLines = settings.ShowUnsafeLines;
        if (!string.IsNullOrEmpty(settings.UnsafeLineMessage))
            UnsafeLineMessage = settings.UnsafeLineMessage;

        CrossOutPastDays = settings.CrossOutPastDays;
        ShowShareFooter = settings.ShowShareFooter;
        LoadDayLabelEntries(settings.DayLabelCycle, settings.DayLabelStartDate);
        DayLabelCorner = string.IsNullOrEmpty(settings.DayLabelCorner) ? "TopRight" : settings.DayLabelCorner;

        // Restore district name from settings (visible before cache loads)
        if (!string.IsNullOrEmpty(settings.DistrictName))
            DistrictName = settings.DistrictName;

        // Load holiday overrides (pre-populate defaults if empty)
        if (settings.HolidayOverrides.Count == 0)
        {
            settings.HolidayOverrides = GetDefaultHolidayOverrides();
        }
        LoadHolidayOverrides(settings.HolidayOverrides);

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
        var existing = await _settingsService.LoadAsync().ConfigureAwait(false);
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

        // Build forced home days per session (always write the key so we can
        // distinguish "never configured" from "user explicitly unchecked all days")
        var forcedHomeDaysBySession = new Dictionary<string, List<string>>(existing.ForcedHomeDaysBySession);
        if (!string.IsNullOrEmpty(session))
        {
            var checkedDays = ForcedHomeDays
                .Where(d => d.IsForced)
                .Select(d => d.DayOfWeek.ToString())
                .ToList();
            forcedHomeDaysBySession[session] = checkedDays;
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
            DistrictName = DistrictName,
            BuildingId = SelectedBuilding?.BuildingId,
            SelectedSessionName = SelectedSession,
            SelectedThemeName = SelectedTheme.Name,
            HiddenThemeNames = existing.HiddenThemeNames,
            LayoutMode = SelectedLayoutMode,
            ShowUnsafeLines = ShowUnsafeLines,
            UnsafeLineMessage = UnsafeLineMessage,
            PlanLabelOverrides = MergePlanOverrides(existing.PlanLabelOverrides, BuildPlanLabelOverridesFromUi()),
            PlanIconOverrides = MergePlanOverrides(existing.PlanIconOverrides, BuildPlanIconOverridesFromUi()),
            PlanDisplayOrder = MergePlanDisplayOrder(existing.PlanDisplayOrder, BuildPlanDisplayOrderFromUi()),
            HolidayOverrides = BuildHolidayOverridesFromUi(),
            CrossOutPastDays = CrossOutPastDays,
            ShowShareFooter = ShowShareFooter,
            DayLabelCycle = BuildDayLabelCycleFromUi(),
            DayLabelStartDate = string.IsNullOrWhiteSpace(DayLabelStartDate) ? null : DayLabelStartDate.Trim(),
            DayLabelCorner = DayLabelCorner
        };
        await _settingsService.SaveAsync(settings).ConfigureAwait(false);
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
        _savePending = true;
        var token = _saveDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await SaveSettingsAsync();
                    _savePending = false;
                }
            }
            catch (TaskCanceledException)
            {
                // Debounce cancelled by a newer change, expected
            }
        }, token);
    }

    /// <summary>
    /// Flushes any pending debounced save synchronously. Called before switching sessions
    /// to ensure the previous session's changes are persisted before loading new data.
    /// </summary>
    private void FlushPendingSave()
    {
        if (!_savePending) return;

        _saveDebounce?.Cancel();
        SaveSettingsAsync().GetAwaiter().GetResult();
        _savePending = false;
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

            // Ensure the identifier response is available for cache (synthesize from UI state if needed)
            _lastIdentifierResponse ??= new FamilyMenuIdentifierResponse
            {
                DistrictId = DistrictId!,
                DistrictName = DistrictName ?? "",
                Identifier = IdentifierCode.Trim(),
                Buildings = AvailableBuildings.ToList()
            };

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
            var layoutMode = SelectedLayoutMode;
            var showUnsafeLines = ShowUnsafeLines;
            var unsafeLineMessage = UnsafeLineMessage;
            var previewZoom = PreviewZoom;

            // Build overrides from live UI state (not disk, which may lag behind)
            var planLabelOverrides = BuildPlanLabelOverridesFromUi();
            var planIconOverrides = BuildPlanIconOverridesFromUi();
            var planDisplayOrder = BuildPlanDisplayOrderFromUi();
            var currentSettings = await _settingsService.LoadAsync();
            var holidayOverrides = currentSettings.HolidayOverrides;
            var crossOutPastDays = CrossOutPastDays;
            var showShareFooter = ShowShareFooter;
            var sourceUrl = SourceUrl;
            var dayLabelCycle = BuildDayLabelCycleFromUi();
            var dayLabelStartDateStr = DayLabelStartDate;
            var dayLabelCorner = DayLabelCorner;

            // Run CPU-bound analysis and HTML generation on a background thread
            var (baseHtml, tempPath) = await Task.Run(() =>
            {
                var processedMonth = _menuAnalyzer.Analyze(menuResponse, selectedAllergenIds, notPreferredNames, favoriteNames, year, month, sessionName, buildingName);

                DateOnly? parsedDayLabelStart = null;
                if (!string.IsNullOrWhiteSpace(dayLabelStartDateStr) &&
                    DateOnly.TryParse(dayLabelStartDateStr, out var dlsd))
                    parsedDayLabelStart = dlsd;

                var renderOptions = new CalendarRenderOptions
                {
                    LayoutMode = layoutMode,
                    ShowUnsafeLines = showUnsafeLines,
                    UnsafeLineMessage = unsafeLineMessage,
                    PlanLabelOverrides = planLabelOverrides,
                    PlanIconOverrides = planIconOverrides,
                    PlanDisplayOrder = planDisplayOrder,
                    HolidayOverrides = holidayOverrides,
                    CrossOutPastDays = crossOutPastDays,
                    ShowShareFooter = showShareFooter,
                    SourceUrl = sourceUrl,
                    Today = GetPastDayCutoff(),
                    DayLabelCycle = dayLabelCycle,
                    DayLabelStartDate = parsedDayLabelStart,
                    DayLabelCorner = dayLabelCorner
                };

                var generatedHtml = _calendarGenerator.Generate(
                    processedMonth,
                    selectedAllergenNames,
                    forcedHomeDays,
                    theme,
                    renderOptions);

                // Save clean HTML (no zoom) to file for browser/print
                var dir = Path.Combine(Path.GetTempPath(), "SchoolLunchMenu");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"LunchCalendar_{year}-{month:D2}.html");
                File.WriteAllText(path, generatedHtml);

                return (generatedHtml, path);
            });

            GeneratedHtmlPath = tempPath;
            _baseGeneratedHtml = baseHtml;
            GeneratedHtml = InjectZoom(baseHtml, previewZoom);

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
    /// Computed URL to the LINQ Connect public menu page for the current identifier and building.
    /// </summary>
    public string? SourceUrl
    {
        get
        {
            if (string.IsNullOrEmpty(IdentifierCode) || SelectedBuilding is null)
                return null;
            return $"https://linqconnect.com/public/menu/{IdentifierCode.Trim()}?buildingId={SelectedBuilding.BuildingId}";
        }
    }

    /// <summary>
    /// Opens the generated HTML calendar in the default browser.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenInBrowser))]
    private void OpenInBrowser()
    {
        Process.Start(new ProcessStartInfo(GeneratedHtmlPath!) { UseShellExecute = true });
    }

    /// <summary>
    /// Increases the preview zoom by 5%.
    /// </summary>
    [RelayCommand]
    private void ZoomIn()
    {
        if (PreviewZoom < 200)
            PreviewZoom = Math.Min(200, PreviewZoom + 5);
    }

    /// <summary>
    /// Decreases the preview zoom by 5%.
    /// </summary>
    [RelayCommand]
    private void ZoomOut()
    {
        if (PreviewZoom > 25)
            PreviewZoom = Math.Max(25, PreviewZoom - 5);
    }

    /// <summary>
    /// Applies the current zoom level to the preview HTML without regenerating the calendar.
    /// </summary>
    private void ApplyPreviewZoom()
    {
        if (_baseGeneratedHtml is null) return;

        GeneratedHtml = InjectZoom(_baseGeneratedHtml, PreviewZoom);
    }

    /// <summary>
    /// Injects a CSS zoom style into the HTML body tag for preview display.
    /// </summary>
    private static string InjectZoom(string html, int zoomPct)
    {
        if (zoomPct == 100)
            return html;

        return html.Replace(
            "<body style=\"width:10.5in;\">",
            $"<body style=\"width:10.5in;zoom:{zoomPct}%;\">");
    }

    /// <summary>
    /// Opens the LINQ Connect public menu page in the default browser.
    /// </summary>
    [RelayCommand]
    private void OpenSourceLink()
    {
        if (SourceUrl is not null)
            Process.Start(new ProcessStartInfo(SourceUrl) { UseShellExecute = true });
    }

    /// <summary>
    /// Adds a new empty holiday override entry.
    /// </summary>
    [RelayCommand]
    private void AddHolidayOverride()
    {
        var entry = new HolidayOverrideEntry();
        entry.PropertyChanged += OnHolidayOverrideChanged;
        HolidayOverrideEntries.Add(entry);
    }

    /// <summary>
    /// Removes a holiday override entry.
    /// </summary>
    [RelayCommand]
    private void RemoveHolidayOverride(HolidayOverrideEntry entry)
    {
        entry.PropertyChanged -= OnHolidayOverrideChanged;
        HolidayOverrideEntries.Remove(entry);
        SaveHolidayOverridesToSettings();
    }

    /// <summary>
    /// Adds a new empty day label entry.
    /// </summary>
    [RelayCommand]
    private void AddDayLabel()
    {
        var entry = new DayLabelEntry();
        entry.PropertyChanged += OnDayLabelEntryChanged;
        DayLabelEntries.Add(entry);
    }

    /// <summary>
    /// Removes a day label entry.
    /// </summary>
    [RelayCommand]
    private void RemoveDayLabel(DayLabelEntry entry)
    {
        entry.PropertyChanged -= OnDayLabelEntryChanged;
        DayLabelEntries.Remove(entry);
        ScheduleSettingsSave();
    }

    /// <summary>
    /// Fetches day labels (Red/White Day) from the ISD194 CMS school calendar and populates the day label cycle.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task FetchDayLabelsAsync()
    {
        IsBusy = true;
        StatusText = "Fetching day labels from school calendar...";

        try
        {
            var result = await _dayLabelFetchService.FetchAsync();

            if (result.Entries.Count == 0)
            {
                StatusText = "No day labels found on the calendar page.";
                return;
            }

            // Clear existing entries
            foreach (var entry in DayLabelEntries)
                entry.PropertyChanged -= OnDayLabelEntryChanged;
            DayLabelEntries.Clear();

            // Map known label names to colors
            var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Red Day"] = "#dc3545",
                ["White Day"] = "#adb5bd"
            };

            // Build entries for each distinct label
            var colorIndex = 0;
            foreach (var label in result.DistinctLabels)
            {
                if (!colorMap.TryGetValue(label, out var color))
                {
                    // Assign sequential colors from the suggestion palette
                    color = DayLabelColorSuggestions[colorIndex % DayLabelColorSuggestions.Count];
                    colorIndex++;
                }

                var entry = new DayLabelEntry
                {
                    Label = label.Replace(" Day", "").Trim(),
                    Color = color
                };
                entry.PropertyChanged += OnDayLabelEntryChanged;
                DayLabelEntries.Add(entry);
            }

            // Set start date to the first entry's date
            DayLabelStartDate = result.Entries[0].Date.ToString("M/d/yyyy");

            ScheduleSettingsSave();
            StatusText = $"Fetched {result.Entries.Count} day labels ({string.Join(", ", result.DistinctLabels)}) starting {DayLabelStartDate}.";
            _logger.LogInformation("Fetched {Count} day labels from CMS: {Labels}",
                result.Entries.Count, string.Join(", ", result.DistinctLabels));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch day labels from CMS");
            StatusText = $"Error fetching day labels: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnDayLabelEntryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        ScheduleSettingsSave();
    }

    private void OnHolidayOverrideChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SaveHolidayOverridesToSettings();
    }

    /// <summary>
    /// Persists holiday overrides from the UI collection back to settings.
    /// </summary>
    private void SaveHolidayOverridesToSettings()
    {
        ScheduleSettingsSave();
    }

    private void OnPlanLabelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlanLabelEntry.ShortLabel) or nameof(PlanLabelEntry.Icon))
            ScheduleSettingsSave();
    }

    /// <summary>
    /// Populates plan label entries from discovered plan names, saved overrides, icons, and order.
    /// </summary>
    private void PopulatePlanLabelEntries(
        IEnumerable<string> planNames,
        Dictionary<string, string> savedOverrides,
        Dictionary<string, string> savedIcons,
        List<string> savedOrder)
    {
        foreach (var entry in PlanLabelEntries)
            entry.PropertyChanged -= OnPlanLabelChanged;

        PlanLabelEntries.Clear();

        // Build ordered list: saved order first, then remaining alphabetically
        var allNames = planNames.ToHashSet();
        var ordered = new List<string>();
        foreach (var name in savedOrder)
        {
            if (allNames.Remove(name))
                ordered.Add(name);
        }
        ordered.AddRange(allNames.OrderBy(n => n));

        foreach (var name in ordered)
        {
            savedOverrides.TryGetValue(name, out var savedLabel);
            savedIcons.TryGetValue(name, out var savedIcon);
            var entry = new PlanLabelEntry
            {
                PlanName = name,
                ShortLabel = savedLabel ?? "",
                Icon = savedIcon ?? ""
            };
            entry.PropertyChanged += OnPlanLabelChanged;
            PlanLabelEntries.Add(entry);
        }
    }

    /// <summary>
    /// Moves a plan label entry up in the list.
    /// </summary>
    [RelayCommand]
    private void MovePlanUp(PlanLabelEntry entry)
    {
        var index = PlanLabelEntries.IndexOf(entry);
        if (index > 0)
        {
            PlanLabelEntries.Move(index, index - 1);
            ScheduleSettingsSave();
        }
    }

    /// <summary>
    /// Moves a plan label entry down in the list.
    /// </summary>
    [RelayCommand]
    private void MovePlanDown(PlanLabelEntry entry)
    {
        var index = PlanLabelEntries.IndexOf(entry);
        if (index >= 0 && index < PlanLabelEntries.Count - 1)
        {
            PlanLabelEntries.Move(index, index + 1);
            ScheduleSettingsSave();
        }
    }

    /// <summary>
    /// Toggles edit mode for a plan label entry.
    /// </summary>
    [RelayCommand]
    private void EditPlanLabel(PlanLabelEntry entry)
    {
        if (!entry.IsEditing)
        {
            // Prefill with current display label so the user has something to edit
            if (string.IsNullOrWhiteSpace(entry.ShortLabel))
                entry.ShortLabel = entry.PlanName;
            entry.IsEditing = true;
        }
        else
        {
            entry.IsEditing = false;
            ScheduleSettingsSave();
        }
    }

    /// <summary>
    /// Resets a plan label entry's short label to empty (reverts to full plan name).
    /// </summary>
    [RelayCommand]
    private void ResetPlanLabel(PlanLabelEntry entry)
    {
        entry.ShortLabel = "";
        entry.IsEditing = false;
        ScheduleSettingsSave();
    }

    /// <summary>
    /// Builds a dictionary of plan label overrides from the UI collection for saving.
    /// </summary>
    private Dictionary<string, string> BuildPlanLabelOverridesFromUi()
    {
        var result = new Dictionary<string, string>();
        foreach (var entry in PlanLabelEntries)
        {
            if (!string.IsNullOrWhiteSpace(entry.ShortLabel))
                result[entry.PlanName] = entry.ShortLabel.Trim();
        }
        return result;
    }

    /// <summary>
    /// Builds a dictionary of plan icon overrides from the UI collection for saving.
    /// </summary>
    private Dictionary<string, string> BuildPlanIconOverridesFromUi()
    {
        var result = new Dictionary<string, string>();
        foreach (var entry in PlanLabelEntries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Icon))
                result[entry.PlanName] = entry.Icon.Trim();
        }
        return result;
    }

    /// <summary>
    /// Builds the plan display order list from the UI collection for saving.
    /// </summary>
    private List<string> BuildPlanDisplayOrderFromUi()
    {
        return PlanLabelEntries.Select(e => e.PlanName).ToList();
    }

    /// <summary>
    /// Merges plan overrides: current UI state takes priority, then existing saved entries for plans
    /// not currently visible in the UI are preserved. This prevents session switches from wiping
    /// out customizations for plans belonging to other sessions.
    /// </summary>
    private Dictionary<string, string> MergePlanOverrides(
        Dictionary<string, string> existing, Dictionary<string, string> fromUi)
    {
        var currentPlanNames = PlanLabelEntries.Select(e => e.PlanName).ToHashSet();
        var merged = new Dictionary<string, string>(fromUi);

        foreach (var (key, value) in existing)
        {
            // Preserve saved entries for plans not currently in the UI
            if (!currentPlanNames.Contains(key))
                merged[key] = value;
        }

        return merged;
    }

    /// <summary>
    /// Merges plan display order: current UI order first, then appends saved entries for plans
    /// not currently visible (preserving their relative order).
    /// </summary>
    private List<string> MergePlanDisplayOrder(
        List<string> existing, List<string> fromUi)
    {
        var currentPlanNames = PlanLabelEntries.Select(e => e.PlanName).ToHashSet();
        var merged = new List<string>(fromUi);

        foreach (var name in existing)
        {
            if (!currentPlanNames.Contains(name) && !merged.Contains(name))
                merged.Add(name);
        }

        return merged;
    }

    /// <summary>
    /// Builds a dictionary of holiday overrides from the UI collection for saving.
    /// </summary>
    private Dictionary<string, HolidayOverride> BuildHolidayOverridesFromUi()
    {
        var result = new Dictionary<string, HolidayOverride>();
        foreach (var entry in HolidayOverrideEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Keyword)) continue;
            result[entry.Keyword.Trim().ToLowerInvariant()] = new HolidayOverride
            {
                Emoji = entry.Emoji,
                CustomMessage = string.IsNullOrWhiteSpace(entry.CustomMessage) ? null : entry.CustomMessage
            };
        }
        return result;
    }

    /// <summary>
    /// Builds the day label cycle list from the UI collection for saving.
    /// </summary>
    private List<DayLabel> BuildDayLabelCycleFromUi()
    {
        return DayLabelEntries
            .Where(e => !string.IsNullOrWhiteSpace(e.Label))
            .Select(e => new DayLabel
            {
                Label = e.Label.Trim(),
                Color = string.IsNullOrWhiteSpace(e.Color) ? "#6c757d" : e.Color.Trim()
            })
            .ToList();
    }

    /// <summary>
    /// Loads day label entries from settings into the observable collection.
    /// </summary>
    private void LoadDayLabelEntries(List<DayLabel> dayLabels, string? startDate)
    {
        foreach (var entry in DayLabelEntries)
            entry.PropertyChanged -= OnDayLabelEntryChanged;

        DayLabelEntries.Clear();

        foreach (var dl in dayLabels)
        {
            var entry = new DayLabelEntry
            {
                Label = dl.Label,
                Color = dl.Color
            };
            entry.PropertyChanged += OnDayLabelEntryChanged;
            DayLabelEntries.Add(entry);
        }

        DayLabelStartDate = startDate ?? "";
    }

    /// <summary>
    /// Loads holiday override entries from settings into the observable collection.
    /// </summary>
    private void LoadHolidayOverrides(Dictionary<string, HolidayOverride> overrides)
    {
        foreach (var entry in HolidayOverrideEntries)
            entry.PropertyChanged -= OnHolidayOverrideChanged;

        HolidayOverrideEntries.Clear();

        foreach (var (keyword, holidayOverride) in overrides)
        {
            var entry = new HolidayOverrideEntry
            {
                Keyword = keyword,
                Emoji = holidayOverride.Emoji,
                CustomMessage = holidayOverride.CustomMessage ?? ""
            };
            entry.PropertyChanged += OnHolidayOverrideChanged;
            HolidayOverrideEntries.Add(entry);
        }
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
            checkedDays = [];
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
        var discoveredPlanNames = new HashSet<string>();
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
                discoveredPlanNames.Add(plan.MenuPlanName);
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
        PopulatePlanLabelEntries(discoveredPlanNames, settings.PlanLabelOverrides, settings.PlanIconOverrides, settings.PlanDisplayOrder);
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
    /// Returns the default holiday override dictionary matching the hardcoded GetNoSchoolEmoji patterns.
    /// </summary>
    private static Dictionary<string, HolidayOverride> GetDefaultHolidayOverrides() => new()
    {
        ["winter break"] = new HolidayOverride { Emoji = "❄️" },
        ["christmas"] = new HolidayOverride { Emoji = "❄️" },
        ["thanksgiving"] = new HolidayOverride { Emoji = "🦃" },
        ["president"] = new HolidayOverride { Emoji = "🇺🇸" },
        ["mlk"] = new HolidayOverride { Emoji = "✊" },
        ["martin luther king"] = new HolidayOverride { Emoji = "✊" },
        ["memorial"] = new HolidayOverride { Emoji = "🇺🇸" },
        ["labor"] = new HolidayOverride { Emoji = "🇺🇸" },
        ["spring break"] = new HolidayOverride { Emoji = "🌸" },
        ["teacher"] = new HolidayOverride { Emoji = "📚" }
    };

    /// <summary>
    /// Returns today's date for past-day comparison, but only after 3 PM (end of school day).
    /// Before 3 PM, returns yesterday's date so that today is not crossed out.
    /// </summary>
    private static DateOnly GetPastDayCutoff()
    {
        var now = DateTime.Now;
        return now.Hour >= 15
            ? DateOnly.FromDateTime(now)
            : DateOnly.FromDateTime(now).AddDays(-1);
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
/// Represents a plan line with an editable short label for vending buttons.
/// </summary>
public partial class PlanLabelEntry : ObservableObject
{
    /// <summary>The original plan name from the menu data (read-only).</summary>
    public string PlanName { get; init; } = "";

    [ObservableProperty]
    private string _shortLabel = "";

    partial void OnShortLabelChanged(string value) => OnPropertyChanged(nameof(DisplayLabel));

    [ObservableProperty]
    private string _icon = "";

    [ObservableProperty]
    private bool _isEditing;

    /// <summary>The label shown in view mode: short label if set, otherwise the full plan name.</summary>
    public string DisplayLabel => string.IsNullOrWhiteSpace(ShortLabel) ? PlanName : ShortLabel;

    /// <summary>Whether this entry has a custom short label override.</summary>
    public bool HasCustomLabel => !string.IsNullOrWhiteSpace(ShortLabel);
}

/// <summary>
/// Represents a layout mode option for the Day Layout ComboBox.
/// </summary>
/// <param name="Value">The internal value (e.g., "List", "IconsLeft", "IconsRight").</param>
/// <param name="DisplayText">The display text shown in the dropdown.</param>
public record LayoutModeOption(string Value, string DisplayText);

/// <summary>
/// Represents an editable holiday override entry in the UI.
/// </summary>
public partial class HolidayOverrideEntry : ObservableObject
{
    [ObservableProperty]
    private string _keyword = "";

    [ObservableProperty]
    private string _emoji = "";

    [ObservableProperty]
    private string _customMessage = "";
}

/// <summary>
/// Represents a day label entry in the rotating day label cycle.
/// </summary>
public partial class DayLabelEntry : ObservableObject
{
    [ObservableProperty]
    private string _label = "";

    [ObservableProperty]
    private string _color = "#6c757d";
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
