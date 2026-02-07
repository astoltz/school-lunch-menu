import Foundation
import SwiftUI
import Combine
import os
import AppKit

// MARK: - MainViewModel

/// Main view model for the School Lunch Menu application.
/// Manages UI state, API interactions, and calendar generation.
@MainActor
class MainViewModel: ObservableObject {
    private let logger = Logger(subsystem: "com.schoollunchmenu", category: "MainViewModel")

    // MARK: - Services

    private let apiService = LinqConnectApiService()
    private let settingsService = SettingsService()
    private let menuAnalyzer = MenuAnalyzer()
    private let calendarGenerator = CalendarHtmlGenerator()
    private let harFileService = HarFileService()
    private let dayLabelFetchService = DayLabelFetchService()

    // MARK: - Cached Responses

    private var lastMenuResponse: FamilyMenuResponse?
    private var lastIdentifierResponse: FamilyMenuIdentifierResponse?

    // MARK: - Debounce

    private var saveDebounceTask: Task<Void, Never>?
    private var savePending = false

    // MARK: - Recipe Storage

    private var allRecipes: [NotPreferredOption] = []
    private var allFavorites: [FavoriteOption] = []
    private var recipeAllergenMap: [String: Set<String>] = [:]

    // MARK: - Theme State

    private var themeManuallySelected = false
    private var suppressThemeManualFlag = false

    // MARK: - Preview State

    private var baseGeneratedHtml: String?

    // MARK: - Published Properties - Status

    @Published var isBusy = false
    @Published var statusText = "Ready. Enter an identifier code and click Look Up."

    // MARK: - Published Properties - School Lookup

    @Published var identifierCode = "" {
        didSet { objectWillChange.send() }
    }
    @Published var districtId: String?
    @Published var districtName: String?
    @Published var availableBuildings: [Building] = []
    @Published var selectedBuilding: Building? {
        didSet {
            objectWillChange.send()
            scheduleSettingsSave()
        }
    }
    @Published var hasBuildings = false

    // MARK: - Published Properties - Month/Year Selection

    @Published var selectedMonth: Int {
        didSet {
            if !themeManuallySelected {
                autoSuggestTheme(for: selectedMonth)
            }
        }
    }
    @Published var selectedYear: Int
    @Published var availableYears: [Int] = []

    // MARK: - Published Properties - Session

    @Published var availableSessions: [String] = []
    @Published var selectedSession: String? {
        didSet {
            // Flush any pending save for the previous session before loading the new one
            flushPendingSave()

            if let session = selectedSession, lastMenuResponse != nil {
                Task {
                    await populateRecipeNames(from: lastMenuResponse!)
                }
            }
            loadForcedHomeDays()
            scheduleSettingsSave()
        }
    }
    @Published var hasSessions = false

    // MARK: - Published Properties - Allergens

    @Published var availableAllergens: [AllergenOption] = []
    @Published var allergenSummary = "None selected"
    @Published var isAllergenExpanded = false

    // MARK: - Published Properties - Recipes

    @Published var hasRecipes = false
    @Published var recipeSearchText = "" {
        didSet { rebuildFilteredRecipes() }
    }
    @Published var favoriteSearchText = "" {
        didSet { rebuildFilteredFavorites() }
    }
    @Published var filteredRecipes: [NotPreferredOption] = []
    @Published var filteredFavorites: [FavoriteOption] = []

    // MARK: - Published Properties - Forced Home Days

    @Published var forcedHomeDays: [ForcedHomeDayOption] = []

    // MARK: - Published Properties - Theme

    @Published var themeListItems: [ThemeListItem] = []
    @Published var selectedTheme: CalendarTheme {
        didSet {
            if !suppressThemeManualFlag {
                themeManuallySelected = true
            }
            scheduleSettingsSave()
        }
    }
    @Published var selectedThemeItem: ThemeListItem? {
        didSet {
            guard let item = selectedThemeItem, !item.isHeader, let theme = item.theme else {
                return
            }
            selectedTheme = theme
        }
    }

    // MARK: - Published Properties - Layout

    @Published var selectedLayoutMode = "IconsLeft" {
        didSet {
            objectWillChange.send()
            scheduleSettingsSave()
        }
    }
    @Published var showUnsafeLines = true {
        didSet { scheduleSettingsSave() }
    }
    @Published var unsafeLineMessage = "No safe options" {
        didSet { scheduleSettingsSave() }
    }
    @Published var planLabelEntries: [PlanLabelEntry] = []

    // MARK: - Published Properties - Day Options

    @Published var crossOutPastDays = true {
        didSet { scheduleSettingsSave() }
    }
    @Published var showShareFooter = false {
        didSet { scheduleSettingsSave() }
    }

    // MARK: - Published Properties - Day Labels

    @Published var dayLabelEntries: [DayLabelEntry] = []
    @Published var dayLabelStartDate = "" {
        didSet { scheduleSettingsSave() }
    }
    @Published var dayLabelCorner = "TopRight" {
        didSet { scheduleSettingsSave() }
    }

    // MARK: - Published Properties - Holiday Overrides

    @Published var holidayOverrideEntries: [HolidayOverrideEntry] = []

    // MARK: - Published Properties - Preview

    @Published var generatedHtml: String?
    @Published var generatedHtmlPath: String?
    @Published var previewZoom = 75 {
        didSet { applyPreviewZoom() }
    }

    // MARK: - Static Options

    static let monthOptions: [MonthOption] = [
        MonthOption(id: 1, name: "January"), MonthOption(id: 2, name: "February"),
        MonthOption(id: 3, name: "March"), MonthOption(id: 4, name: "April"),
        MonthOption(id: 5, name: "May"), MonthOption(id: 6, name: "June"),
        MonthOption(id: 7, name: "July"), MonthOption(id: 8, name: "August"),
        MonthOption(id: 9, name: "September"), MonthOption(id: 10, name: "October"),
        MonthOption(id: 11, name: "November"), MonthOption(id: 12, name: "December")
    ]

    static let layoutModeOptions = [
        LayoutModeOption(id: "List", value: "List", displayText: "List"),
        LayoutModeOption(id: "IconsLeft", value: "IconsLeft", displayText: "Icons Left"),
        LayoutModeOption(id: "IconsRight", value: "IconsRight", displayText: "Icons Right")
    ]

    static let dayLabelCornerOptions = ["TopRight", "TopLeft", "BottomRight", "BottomLeft"]

    static let dayLabelColorSuggestions = [
        "#dc3545", "#adb5bd", "#0d6efd", "#198754", "#fd7e14",
        "#6f42c1", "#d63384", "#0dcaf0", "#ffc107", "#20c997"
    ]

    static let planIconSuggestions = [
        "", "🍽️", "🐱", "🐾", "🦅", "🐦", "🦁", "🐻", "🐺", "🐯",
        "🍕", "🌮", "🍔", "🥗", "🍎", "🥪", "🍝", "🍲",
        "⭐", "🔵", "🟢", "🟡", "🔴", "🟣", "❤️", "💙", "💚",
        "1️⃣", "2️⃣", "3️⃣", "🅰️", "🅱️"
    ]

    // MARK: - Computed Properties

    /// Whether the grid buttons are active (used for sidebar visibility of plan label entries).
    var isGridMode: Bool {
        selectedLayoutMode == "IconsLeft" || selectedLayoutMode == "IconsRight"
    }

    /// Computed URL to the LINQ Connect public menu page for the current identifier and building.
    var sourceUrl: String? {
        guard !identifierCode.trimmingCharacters(in: .whitespaces).isEmpty,
              let building = selectedBuilding else {
            return nil
        }
        return "https://linqconnect.com/public/menu/\(identifierCode.trimmingCharacters(in: .whitespaces))?buildingId=\(building.buildingId)"
    }

    /// Whether calendar generation can proceed.
    var canGenerate: Bool {
        !isBusy && lastMenuResponse != nil
    }

    /// Whether the generated HTML can be opened in a browser.
    var canOpenInBrowser: Bool {
        generatedHtmlPath != nil
    }

    // MARK: - Initialization

    init() {
        // Initialize month/year to current
        let today = Date()
        let calendar = Calendar.current
        _selectedMonth = Published(initialValue: calendar.component(.month, from: today))
        _selectedYear = Published(initialValue: calendar.component(.year, from: today))

        // Default theme (used before settings are loaded) - must be initialized before accessing self
        _selectedTheme = Published(initialValue: CalendarThemes.all.first { $0.name == "Default" } ?? CalendarThemes.all.last!)

        // Populate year options (after all stored properties are initialized)
        let currentYear = calendar.component(.year, from: today)
        for year in (currentYear - 1)...(currentYear + 1) {
            availableYears.append(year)
        }
    }

    // MARK: - Initialization Methods

    /// Loads persisted settings and attempts to preload cached menu data.
    /// If a HAR file path is provided (from command-line arg or drag-drop), loads that instead.
    func initialize(harFilePath: String? = nil) async {
        let settings = await settingsService.load()

        // Build categorized theme list (filtering hidden themes)
        buildThemeList(hiddenThemeNames: settings.hiddenThemeNames)

        // Restore theme selection
        if let savedThemeName = settings.selectedThemeName, !savedThemeName.isEmpty {
            if let savedItem = themeListItems.first(where: { $0.theme?.name == savedThemeName }) {
                suppressThemeManualFlag = true
                selectedThemeItem = savedItem
                if let theme = savedItem.theme {
                    selectedTheme = theme
                }
                suppressThemeManualFlag = false
                themeManuallySelected = true
            }
        }

        // Restore identifier and district from settings
        if let identifier = settings.identifier, !identifier.isEmpty {
            identifierCode = identifier
        }
        if let districtIdValue = settings.districtId, !districtIdValue.isEmpty {
            districtId = districtIdValue
        }

        // Migrate legacy ShowMealButtons to LayoutMode
        if settings.layoutMode.isEmpty || settings.layoutMode == "List" {
            selectedLayoutMode = settings.showMealButtons ? "IconsRight" : "List"
        } else {
            selectedLayoutMode = settings.layoutMode
        }

        showUnsafeLines = settings.showUnsafeLines
        if !settings.unsafeLineMessage.isEmpty {
            unsafeLineMessage = settings.unsafeLineMessage
        }

        crossOutPastDays = settings.crossOutPastDays
        showShareFooter = settings.showShareFooter
        loadDayLabelEntries(dayLabels: settings.dayLabelCycle, startDate: settings.dayLabelStartDate)
        dayLabelCorner = settings.dayLabelCorner.isEmpty ? "TopRight" : settings.dayLabelCorner

        // Restore district name from settings (visible before cache loads)
        if let name = settings.districtName, !name.isEmpty {
            districtName = name
        }

        // Load holiday overrides (pre-populate defaults if empty)
        var holidayOverrides = settings.holidayOverrides
        if holidayOverrides.isEmpty {
            holidayOverrides = Self.getDefaultHolidayOverrides()
        }
        loadHolidayOverrides(overrides: holidayOverrides)

        // If a HAR file was provided via command-line arg, load it directly
        if let harPath = harFilePath, !harPath.isEmpty {
            await loadFromHar(url: URL(fileURLWithPath: harPath))
            return
        }

        // Try to preload from disk cache
        await tryLoadMenuCache(settings: settings)
    }

    // MARK: - Lookup Methods

    /// Looks up district and buildings from the identifier code.
    func lookupIdentifier() async {
        let code = identifierCode.trimmingCharacters(in: .whitespaces)
        guard !code.isEmpty else {
            statusText = "Please enter an identifier code."
            return
        }

        isBusy = true
        statusText = "Looking up identifier \"\(code)\"..."

        do {
            let identifier = try await apiService.getMenuIdentifier(identifier: code)
            lastIdentifierResponse = identifier
            districtId = identifier.districtId
            districtName = identifier.districtName

            populateBuildings(from: identifier)

            // Load allergens for this district
            statusText = "Fetching allergen list..."
            let allergies = try await apiService.getAllergies(districtId: identifier.districtId)
            await populateAllergens(allergies: allergies)

            statusText = "Found \(identifier.buildings.count) building(s) in \(identifier.districtName). Select a building and fetch menu data."
            logger.info("Identifier lookup: \(identifier.districtName) with \(identifier.buildings.count) buildings")
        } catch {
            logger.error("Failed to look up identifier \(code): \(error.localizedDescription)")
            statusText = "Error looking up identifier: \(error.localizedDescription)"
        }

        isBusy = false
    }

    /// Fetches menu data from the LINQ Connect API.
    func fetchFromApi() async {
        guard let building = selectedBuilding, let districtIdValue = districtId else {
            statusText = "Please look up an identifier and select a building first."
            return
        }

        isBusy = true
        statusText = "Fetching menu data from API..."

        do {
            // Load allergens if not yet loaded
            if availableAllergens.isEmpty {
                statusText = "Fetching allergen list..."
                let allergies = try await apiService.getAllergies(districtId: districtIdValue)
                await populateAllergens(allergies: allergies)
            }

            // Fetch menu for selected month
            var components = DateComponents()
            components.year = selectedYear
            components.month = selectedMonth
            components.day = 1

            let calendar = Calendar.current
            guard let startDate = calendar.date(from: components),
                  let endDate = calendar.date(byAdding: DateComponents(month: 1, day: -1), to: startDate) else {
                statusText = "Failed to calculate date range."
                isBusy = false
                return
            }

            let formatter = DateFormatter()
            formatter.dateFormat = "MMMM yyyy"
            statusText = "Fetching menu for \(formatter.string(from: startDate))..."

            let menuResponse = try await apiService.getMenu(
                buildingId: building.buildingId,
                districtId: districtIdValue,
                startDate: startDate,
                endDate: endDate
            )

            // Set lastMenuResponse after populateSessions to prevent duplicate calls
            populateSessions(from: menuResponse)
            lastMenuResponse = menuResponse
            await populateRecipeNames(from: menuResponse)

            // Persist to disk cache for preloading on next launch
            let allergyCopy = availableAllergens.map { allergen in
                AllergyItem(allergyId: allergen.allergyId, sortOrder: 0, name: allergen.name)
            }

            // Ensure the identifier response is available for cache
            if lastIdentifierResponse == nil {
                lastIdentifierResponse = FamilyMenuIdentifierResponse(
                    districtId: districtIdValue,
                    districtName: districtName ?? "",
                    buildings: availableBuildings,
                    menuNotification: nil,
                    identifier: identifierCode.trimmingCharacters(in: .whitespaces)
                )
            }

            await settingsService.saveMenuCache(MenuCache(
                savedAtUtc: Date(),
                menuResponse: lastMenuResponse,
                allergies: allergyCopy,
                identifierResponse: lastIdentifierResponse
            ))

            statusText = "Menu data loaded from API. Click Generate to create calendar."
            logger.info("Menu data fetched from API for \(self.selectedMonth)/\(self.selectedYear)")
        } catch {
            logger.error("Failed to fetch menu data from API: \(error.localizedDescription)")
            statusText = "Error: \(error.localizedDescription)"
        }

        isBusy = false
    }

    /// Loads menu data from a HAR file.
    func loadFromHar(url: URL) async {
        guard !isBusy else { return }

        isBusy = true
        statusText = "Loading HAR file: \(url.lastPathComponent)..."

        do {
            let result = try await harFileService.loadFromHarFile(at: url)
            lastMenuResponse = result.menu
            lastIdentifierResponse = result.identifier

            // Populate buildings from identifier response
            populateBuildings(from: result.identifier)

            await populateAllergens(allergies: result.allergies)
            populateSessions(from: result.menu)
            detectMonthFromMenu(result.menu)

            // Persist to disk cache so it's available on next launch
            await settingsService.saveMenuCache(MenuCache(
                savedAtUtc: Date(),
                menuResponse: result.menu,
                allergies: result.allergies,
                identifierResponse: result.identifier
            ))

            statusText = "HAR file loaded (\(result.allergies.count) allergens, \(result.identifier.districtName)). Click Generate to create calendar."
            logger.info("Loaded HAR file \(url.path) for \(result.identifier.districtName)")
        } catch {
            logger.error("Failed to load HAR file \(url.path): \(error.localizedDescription)")
            statusText = "Error loading HAR file: \(error.localizedDescription)"
        }

        isBusy = false
    }

    // MARK: - Calendar Generation

    /// Generates the HTML calendar from loaded menu data.
    func generateCalendar() async {
        guard let menuResponse = lastMenuResponse else {
            statusText = "No menu data loaded. Fetch from API or load a HAR file first."
            return
        }

        guard let sessionName = selectedSession, !sessionName.isEmpty else {
            statusText = "Please select a serving session."
            return
        }

        isBusy = true
        statusText = "Analyzing menu and generating calendar..."

        do {
            // Capture UI state
            let selectedAllergenIds = Set(availableAllergens.filter { $0.isSelected }.map { $0.allergyId })
            let selectedAllergenNames = availableAllergens.filter { $0.isSelected }.map { $0.name }
            let notPreferredNames = Set(allRecipes.filter { $0.isNotPreferred }.map { $0.name })
            let favoriteNames = Set(allFavorites.filter { $0.isFavorite }.map { $0.name })
            let year = selectedYear
            let month = selectedMonth
            let forcedHomeDayValues = Set(forcedHomeDays.filter { $0.isForced }.map { $0.dayOfWeek })
            let buildingName = selectedBuilding?.name
            let theme = selectedTheme
            let layoutMode = selectedLayoutMode
            let showUnsafeLinesValue = showUnsafeLines
            let unsafeLineMessageValue = unsafeLineMessage
            let previewZoomValue = previewZoom
            let planLabelOverrides = buildPlanLabelOverridesFromUi()
            let planIconOverrides = buildPlanIconOverridesFromUi()
            let planDisplayOrder = buildPlanDisplayOrderFromUi()
            let holidayOverrides = buildHolidayOverridesFromUi()
            let crossOutPastDaysValue = crossOutPastDays
            let showShareFooterValue = showShareFooter
            let sourceUrlValue = sourceUrl
            let dayLabelCycle = buildDayLabelCycleFromUi()
            let dayLabelStartDateStr = dayLabelStartDate
            let dayLabelCornerValue = dayLabelCorner
            let todayValue = Self.getPastDayCutoff()

            // Run CPU-bound analysis and HTML generation on a background thread
            let (baseHtml, tempPath) = try await Task.detached(priority: .userInitiated) { [weak self] () -> (String, String) in
                guard let self = self else {
                    throw NSError(domain: "MainViewModel", code: -1, userInfo: [NSLocalizedDescriptionKey: "ViewModel deallocated"])
                }

                let processedMonth = self.menuAnalyzer.analyze(
                    menuResponse: menuResponse,
                    selectedAllergenIds: selectedAllergenIds,
                    notPreferredNames: notPreferredNames,
                    favoriteNames: favoriteNames,
                    year: year,
                    month: month,
                    sessionName: sessionName,
                    buildingName: buildingName
                )

                var parsedDayLabelStart: Date? = nil
                if !dayLabelStartDateStr.trimmingCharacters(in: .whitespaces).isEmpty {
                    let dateFormatter = DateFormatter()
                    dateFormatter.dateFormat = "M/d/yyyy"
                    dateFormatter.locale = Locale(identifier: "en_US_POSIX")
                    parsedDayLabelStart = dateFormatter.date(from: dayLabelStartDateStr)
                }

                var renderOptions = CalendarRenderOptions()
                renderOptions.layoutMode = layoutMode
                renderOptions.showUnsafeLines = showUnsafeLinesValue
                renderOptions.unsafeLineMessage = unsafeLineMessageValue
                renderOptions.planLabelOverrides = planLabelOverrides
                renderOptions.planIconOverrides = planIconOverrides
                renderOptions.planDisplayOrder = planDisplayOrder
                renderOptions.holidayOverrides = holidayOverrides
                renderOptions.crossOutPastDays = crossOutPastDaysValue
                renderOptions.today = todayValue
                renderOptions.showShareFooter = showShareFooterValue
                renderOptions.sourceUrl = sourceUrlValue
                renderOptions.dayLabelCycle = dayLabelCycle
                renderOptions.dayLabelStartDate = parsedDayLabelStart
                renderOptions.dayLabelCorner = dayLabelCornerValue

                let generatedHtml = self.calendarGenerator.generate(
                    month: processedMonth,
                    allergenNames: selectedAllergenNames,
                    forcedHomeDays: forcedHomeDayValues,
                    theme: theme,
                    options: renderOptions
                )

                // Save clean HTML (no zoom) to file for browser/print
                let dir = FileManager.default.temporaryDirectory.appendingPathComponent("SchoolLunchMenu", isDirectory: true)
                try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
                let path = dir.appendingPathComponent("LunchCalendar_\(year)-\(String(format: "%02d", month)).html")
                try generatedHtml.write(to: path, atomically: true, encoding: .utf8)

                return (generatedHtml, path.path)
            }.value

            generatedHtmlPath = tempPath
            baseGeneratedHtml = baseHtml
            generatedHtml = injectZoom(baseHtml, zoomPct: previewZoomValue)

            // Save settings after generation
            await saveSettings()

            statusText = "Calendar generated! Saved to \(tempPath)"
            logger.info("Calendar generated and saved to \(tempPath)")
        } catch {
            logger.error("Failed to generate calendar: \(error.localizedDescription)")
            statusText = "Error generating calendar: \(error.localizedDescription)"
        }

        isBusy = false
    }

    // MARK: - Settings Methods

    /// Saves current settings to disk.
    func saveSettings() async {
        var notPreferredBySession: [String: [String]] = [:]
        var favoritesBySession: [String: [String]] = [:]

        // Save current session's preferences
        let session = selectedSession ?? ""
        if !session.isEmpty {
            let notPreferred = allRecipes.filter { $0.isNotPreferred }.map { $0.name }
            if !notPreferred.isEmpty {
                notPreferredBySession[session] = notPreferred
            }

            let favorites = allFavorites.filter { $0.isFavorite }.map { $0.name }
            if !favorites.isEmpty {
                favoritesBySession[session] = favorites
            }
        }

        // Preserve other sessions' preferences from saved settings
        let existing = await settingsService.load()
        for (key, value) in existing.notPreferredBySession {
            if key.lowercased() != session.lowercased() {
                notPreferredBySession[key] = value
            }
        }
        for (key, value) in existing.favoritesBySession {
            if key.lowercased() != session.lowercased() {
                favoritesBySession[key] = value
            }
        }

        // Build forced home days per session
        var forcedHomeDaysBySession = existing.forcedHomeDaysBySession
        if !session.isEmpty {
            let checkedDays = forcedHomeDays.filter { $0.isForced }.map { String($0.dayOfWeek) }
            forcedHomeDaysBySession[session] = checkedDays
        }

        let settings = AppSettings()
        settings.selectedAllergenIds = availableAllergens.filter { $0.isSelected }.map { $0.allergyId }
        settings.forcedHomeDaysBySession = forcedHomeDaysBySession
        settings.notPreferredBySession = notPreferredBySession
        settings.favoritesBySession = favoritesBySession
        settings.identifier = identifierCode
        settings.districtId = districtId
        settings.districtName = districtName
        settings.buildingId = selectedBuilding?.buildingId
        settings.selectedSessionName = selectedSession
        settings.selectedThemeName = selectedTheme.name
        settings.hiddenThemeNames = existing.hiddenThemeNames
        settings.layoutMode = selectedLayoutMode
        settings.showUnsafeLines = showUnsafeLines
        settings.unsafeLineMessage = unsafeLineMessage
        settings.planLabelOverrides = mergePlanOverrides(existing: existing.planLabelOverrides, fromUi: buildPlanLabelOverridesFromUi())
        settings.planIconOverrides = mergePlanOverrides(existing: existing.planIconOverrides, fromUi: buildPlanIconOverridesFromUi())
        settings.planDisplayOrder = mergePlanDisplayOrder(existing: existing.planDisplayOrder, fromUi: buildPlanDisplayOrderFromUi())
        settings.holidayOverrides = buildHolidayOverridesFromUi()
        settings.crossOutPastDays = crossOutPastDays
        settings.showShareFooter = showShareFooter
        settings.dayLabelCycle = buildDayLabelCycleFromUi()
        settings.dayLabelStartDate = dayLabelStartDate.trimmingCharacters(in: .whitespaces).isEmpty ? nil : dayLabelStartDate.trimmingCharacters(in: .whitespaces)
        settings.dayLabelCorner = dayLabelCorner

        await settingsService.save(settings)
    }

    /// Schedules a debounced settings save (500ms delay to batch rapid changes).
    private func scheduleSettingsSave() {
        // Don't save during initial load before allergens are populated
        guard !availableAllergens.isEmpty else { return }

        saveDebounceTask?.cancel()
        savePending = true

        saveDebounceTask = Task {
            do {
                try await Task.sleep(nanoseconds: 500_000_000) // 500ms
                if !Task.isCancelled {
                    await saveSettings()
                    savePending = false
                }
            } catch {
                // Task was cancelled, expected
            }
        }
    }

    /// Flushes any pending debounced save synchronously.
    private func flushPendingSave() {
        guard savePending else { return }

        saveDebounceTask?.cancel()
        Task {
            await saveSettings()
        }
        savePending = false
    }

    // MARK: - Browser/Preview Methods

    /// Opens the generated HTML calendar in the default browser.
    func openInBrowser() {
        guard let path = generatedHtmlPath else { return }
        let url = URL(fileURLWithPath: path)
        NSWorkspace.shared.open(url)
    }

    /// Opens the LINQ Connect public menu page in the default browser.
    func openSourceLink() {
        guard let urlString = sourceUrl, let url = URL(string: urlString) else { return }
        NSWorkspace.shared.open(url)
    }

    /// Shows a file picker to select a HAR file and loads it.
    func showHarFilePicker() {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.init(filenameExtension: "har")!]
        panel.allowsMultipleSelection = false
        panel.canChooseDirectories = false
        panel.message = "Select a HAR file containing LINQ Connect API responses"
        panel.prompt = "Open"

        if panel.runModal() == .OK, let url = panel.url {
            Task {
                await loadFromHar(url: url)
            }
        }
    }

    /// Increases the preview zoom by 5%.
    func zoomIn() {
        if previewZoom < 200 {
            previewZoom = min(200, previewZoom + 5)
        }
    }

    /// Decreases the preview zoom by 5%.
    func zoomOut() {
        if previewZoom > 25 {
            previewZoom = max(25, previewZoom - 5)
        }
    }

    /// Applies the current zoom level to the preview HTML without regenerating the calendar.
    private func applyPreviewZoom() {
        guard let baseHtml = baseGeneratedHtml else { return }
        generatedHtml = injectZoom(baseHtml, zoomPct: previewZoom)
    }

    /// Injects a CSS zoom style into the HTML body tag for preview display.
    private func injectZoom(_ html: String, zoomPct: Int) -> String {
        if zoomPct == 100 {
            return html
        }
        return html.replacingOccurrences(
            of: "<body style=\"width:10.5in;\">",
            with: "<body style=\"width:10.5in;zoom:\(zoomPct)%;\">"
        )
    }

    // MARK: - Holiday Override Methods

    /// Adds a new empty holiday override entry.
    func addHolidayOverride() {
        let entry = HolidayOverrideEntry()
        entry.onChange = { [weak self] in self?.scheduleSettingsSave() }
        holidayOverrideEntries.append(entry)
    }

    /// Removes a holiday override entry.
    func removeHolidayOverride(_ entry: HolidayOverrideEntry) {
        holidayOverrideEntries.removeAll { $0.id == entry.id }
        scheduleSettingsSave()
    }

    // MARK: - Day Label Methods

    /// Adds a new empty day label entry.
    func addDayLabel() {
        let entry = DayLabelEntry()
        entry.onChange = { [weak self] in self?.scheduleSettingsSave() }
        dayLabelEntries.append(entry)
    }

    /// Removes a day label entry.
    func removeDayLabel(_ entry: DayLabelEntry) {
        dayLabelEntries.removeAll { $0.id == entry.id }
        scheduleSettingsSave()
    }

    /// Fetches day labels from the ISD 194 CMS calendar page and populates the day label entries.
    func fetchDayLabels() async {
        guard !isBusy else { return }

        isBusy = true
        statusText = "Fetching day labels from CMS..."

        do {
            let result = try await dayLabelFetchService.fetch()

            guard !result.entries.isEmpty else {
                statusText = "No day labels found on the CMS calendar page."
                isBusy = false
                return
            }

            // Map labels to colors
            let colorMap: [String: String] = [
                "red": "#dc3545",
                "white": "#adb5bd",
                "blue": "#0d6efd",
                "gold": "#ffc107",
                "green": "#198754",
                "silver": "#adb5bd",
                "black": "#212529",
                "orange": "#fd7e14",
                "purple": "#6f42c1"
            ]

            // Build new day label entries from distinct labels
            dayLabelEntries.removeAll()
            for label in result.distinctLabels {
                let entry = DayLabelEntry()
                // Strip " Day" suffix for compact display
                let compactLabel = label.replacingOccurrences(of: " Day", with: "", options: .caseInsensitive)
                entry.label = compactLabel
                entry.color = colorMap[compactLabel.lowercased()] ?? "#6c757d"
                entry.onChange = { [weak self] in self?.scheduleSettingsSave() }
                dayLabelEntries.append(entry)
            }

            // Set start date to first entry's date
            if let first = result.entries.first,
               let year = first.date.year, let month = first.date.month, let day = first.date.day {
                dayLabelStartDate = "\(month)/\(day)/\(year)"
            }

            scheduleSettingsSave()
            statusText = "Loaded \(result.entries.count) day label entries (\(result.distinctLabels.joined(separator: ", ")))."
            logger.info("Fetched \(result.entries.count) day labels from CMS")
        } catch {
            logger.error("Failed to fetch day labels: \(error.localizedDescription)")
            statusText = "Error fetching day labels: \(error.localizedDescription)"
        }

        isBusy = false
    }

    // MARK: - Plan Label Methods

    /// Moves a plan label entry up in the list.
    func movePlanUp(_ entry: PlanLabelEntry) {
        guard let index = planLabelEntries.firstIndex(where: { $0.id == entry.id }), index > 0 else { return }
        planLabelEntries.swapAt(index, index - 1)
        scheduleSettingsSave()
    }

    /// Moves a plan label entry down in the list.
    func movePlanDown(_ entry: PlanLabelEntry) {
        guard let index = planLabelEntries.firstIndex(where: { $0.id == entry.id }),
              index < planLabelEntries.count - 1 else { return }
        planLabelEntries.swapAt(index, index + 1)
        scheduleSettingsSave()
    }

    /// Toggles edit mode for a plan label entry.
    func editPlanLabel(_ entry: PlanLabelEntry) {
        if !entry.isEditing {
            // Prefill with current display label so the user has something to edit
            if entry.shortLabel.trimmingCharacters(in: .whitespaces).isEmpty {
                entry.shortLabel = entry.planName
            }
            entry.isEditing = true
        } else {
            entry.isEditing = false
            scheduleSettingsSave()
        }
    }

    /// Resets a plan label entry's short label to empty (reverts to full plan name).
    func resetPlanLabel(_ entry: PlanLabelEntry) {
        entry.shortLabel = ""
        entry.isEditing = false
        scheduleSettingsSave()
    }

    // MARK: - Helper Methods - Allergens

    /// Updates the allergen summary text shown when the expander is collapsed.
    func updateAllergenSummary() {
        let selected = availableAllergens.filter { $0.isSelected }.map { $0.name }
        allergenSummary = selected.isEmpty ? "None selected" : selected.joined(separator: ", ")
    }

    /// Called when an allergen selection changes.
    func onAllergenSelectionChanged() {
        updateAllergenSummary()
        rebuildFilteredRecipes()
        rebuildFilteredFavorites()
        scheduleSettingsSave()
    }

    /// Called when a not-preferred selection changes.
    func onNotPreferredSelectionChanged() {
        rebuildFilteredRecipes()
        scheduleSettingsSave()
    }

    /// Called when a favorite selection changes.
    func onFavoriteSelectionChanged() {
        rebuildFilteredFavorites()
        scheduleSettingsSave()
    }

    /// Returns the set of currently selected allergen IDs.
    private func getSelectedAllergenIds() -> Set<String> {
        Set(availableAllergens.filter { $0.isSelected }.map { $0.allergyId })
    }

    /// Rebuilds the filtered not-preferred list.
    func rebuildFilteredRecipes() {
        filteredRecipes.removeAll()
        let search = recipeSearchText.trimmingCharacters(in: .whitespaces)
        let selectedAllergenIds = getSelectedAllergenIds()

        // Always show checked items first (unless they contain a selected allergen)
        for item in allRecipes where item.isNotPreferred {
            if !item.allergenIds.isDisjoint(with: selectedAllergenIds) {
                continue
            }
            filteredRecipes.append(item)
        }

        // Show unchecked items that match search (if search has text)
        if !search.isEmpty {
            for item in allRecipes where !item.isNotPreferred && item.name.localizedCaseInsensitiveContains(search) {
                if !item.allergenIds.isDisjoint(with: selectedAllergenIds) {
                    continue
                }
                filteredRecipes.append(item)
            }
        }
    }

    /// Rebuilds the filtered favorites list.
    func rebuildFilteredFavorites() {
        filteredFavorites.removeAll()
        let search = favoriteSearchText.trimmingCharacters(in: .whitespaces)
        let selectedAllergenIds = getSelectedAllergenIds()

        // Always show checked items first (unless they contain a selected allergen)
        for item in allFavorites where item.isFavorite {
            if !item.allergenIds.isDisjoint(with: selectedAllergenIds) {
                continue
            }
            filteredFavorites.append(item)
        }

        // Show unchecked items that match search (if search has text)
        if !search.isEmpty {
            for item in allFavorites where !item.isFavorite && item.name.localizedCaseInsensitiveContains(search) {
                if !item.allergenIds.isDisjoint(with: selectedAllergenIds) {
                    continue
                }
                filteredFavorites.append(item)
            }
        }
    }

    // MARK: - Helper Methods - Population

    /// Populates the building list from an identifier response and restores saved selection.
    private func populateBuildings(from identifier: FamilyMenuIdentifierResponse) {
        availableBuildings = identifier.buildings
        hasBuildings = !availableBuildings.isEmpty
        districtId = identifier.districtId
        districtName = identifier.districtName

        // Try to restore saved building selection
        Task {
            let settings = await settingsService.load()
            if let savedBuildingId = settings.buildingId {
                selectedBuilding = availableBuildings.first { $0.buildingId == savedBuildingId }
            }
            // Default to first building if none selected
            if selectedBuilding == nil {
                selectedBuilding = availableBuildings.first
            }
        }
    }

    /// Populates the session dropdown from a menu response and restores saved selection.
    private func populateSessions(from menuResponse: FamilyMenuResponse) {
        availableSessions = menuResponse.familyMenuSessions
            .map { $0.servingSession }
            .filter { !$0.isEmpty }
        hasSessions = !availableSessions.isEmpty

        // Try to restore saved session selection
        Task {
            let settings = await settingsService.load()
            if let savedSession = settings.selectedSessionName, availableSessions.contains(savedSession) {
                selectedSession = savedSession
            } else {
                // Default to "Lunch" if available, otherwise first session
                selectedSession = availableSessions.contains("Lunch") ? "Lunch" : availableSessions.first
            }
        }
    }

    /// Populates the allergen list and applies saved selections or defaults.
    private func populateAllergens(allergies: [AllergyItem]) async {
        availableAllergens.removeAll()
        var savedIds: Set<String> = []

        // Load saved selections
        let settings = await settingsService.load()
        if !settings.selectedAllergenIds.isEmpty {
            savedIds = Set(settings.selectedAllergenIds)
        }

        for allergy in allergies.sorted(by: { $0.name < $1.name }) {
            let isSelected: Bool
            if !savedIds.isEmpty {
                isSelected = savedIds.contains(allergy.allergyId)
            } else {
                // Default to Milk by name on first launch
                isSelected = allergy.name.lowercased() == "milk"
            }

            let option = AllergenOption(id: allergy.allergyId, allergyId: allergy.allergyId, name: allergy.name, isSelected: isSelected)
            option.onChange = { [weak self] in self?.onAllergenSelectionChanged() }
            availableAllergens.append(option)
        }

        updateAllergenSummary()
    }

    /// Extracts unique entree recipe names from the current menu response and populates the not-preferred and favorites checklists.
    private func populateRecipeNames(from menuResponse: FamilyMenuResponse) async {
        allRecipes.removeAll()
        allFavorites.removeAll()
        filteredRecipes.removeAll()
        filteredFavorites.removeAll()
        recipeAllergenMap.removeAll()

        let settings = await settingsService.load()
        let sessionName = selectedSession ?? ""

        // Load per-session saved names
        let savedNotPreferred = Set(settings.notPreferredBySession[sessionName] ?? [])
        let savedFavorites = Set(settings.favoritesBySession[sessionName] ?? [])

        var uniqueNames: Set<String> = []
        var discoveredPlanNames: Set<String> = []
        recipeAllergenMap = [:]

        // Use selected session if available, otherwise scan all sessions
        let sessions: [MenuSession]
        if !sessionName.isEmpty,
           let match = menuResponse.familyMenuSessions.first(where: { $0.servingSession.lowercased() == sessionName.lowercased() }) {
            sessions = [match]
        } else {
            sessions = menuResponse.familyMenuSessions
        }

        for session in sessions {
            for plan in session.menuPlans {
                discoveredPlanNames.insert(plan.menuPlanName)
                for day in plan.days {
                    for meal in day.menuMeals {
                        for category in meal.recipeCategories {
                            guard category.isEntree else { continue }
                            for recipe in category.recipes {
                                uniqueNames.insert(recipe.recipeName)

                                // Build allergen mapping
                                if recipeAllergenMap[recipe.recipeName] == nil {
                                    recipeAllergenMap[recipe.recipeName] = []
                                }
                                for allergenId in recipe.allergens {
                                    recipeAllergenMap[recipe.recipeName]?.insert(allergenId)
                                }
                            }
                        }
                    }
                }
            }
        }

        for name in uniqueNames.sorted() {
            let recipeAllergens = recipeAllergenMap[name] ?? []

            let notPrefOption = NotPreferredOption(
                id: name,
                name: name,
                allergenIds: recipeAllergens,
                isNotPreferred: savedNotPreferred.contains(name)
            )
            notPrefOption.onChange = { [weak self] in self?.onNotPreferredSelectionChanged() }
            allRecipes.append(notPrefOption)

            let favOption = FavoriteOption(
                id: name,
                name: name,
                allergenIds: recipeAllergens,
                isFavorite: savedFavorites.contains(name)
            )
            favOption.onChange = { [weak self] in self?.onFavoriteSelectionChanged() }
            allFavorites.append(favOption)
        }

        hasRecipes = !allRecipes.isEmpty
        recipeSearchText = ""
        favoriteSearchText = ""
        rebuildFilteredRecipes()
        rebuildFilteredFavorites()
        populatePlanLabelEntries(
            planNames: discoveredPlanNames,
            savedOverrides: settings.planLabelOverrides,
            savedIcons: settings.planIconOverrides,
            savedOrder: settings.planDisplayOrder
        )
    }

    /// Populates plan label entries from discovered plan names, saved overrides, icons, and order.
    private func populatePlanLabelEntries(
        planNames: Set<String>,
        savedOverrides: [String: String],
        savedIcons: [String: String],
        savedOrder: [String]
    ) {
        planLabelEntries.removeAll()

        // Build ordered list: saved order first, then remaining alphabetically
        var allNames = planNames
        var ordered: [String] = []
        for name in savedOrder {
            if allNames.remove(name) != nil {
                ordered.append(name)
            }
        }
        ordered.append(contentsOf: allNames.sorted())

        for name in ordered {
            let savedLabel = savedOverrides[name]
            let savedIcon = savedIcons[name]
            let entry = PlanLabelEntry(
                planName: name,
                shortLabel: savedLabel ?? "",
                icon: savedIcon ?? ""
            )
            entry.onChange = { [weak self] in self?.scheduleSettingsSave() }
            planLabelEntries.append(entry)
        }
    }

    /// Loads forced home day selections for the current session from settings.
    private func loadForcedHomeDays() {
        forcedHomeDays.removeAll()

        let session = selectedSession ?? ""

        Task {
            let settings = await settingsService.load()

            var checkedDays: Set<Int> = []
            if let savedDays = settings.forcedHomeDaysBySession[session] {
                for dayString in savedDays {
                    if let dayInt = Int(dayString) {
                        checkedDays.insert(dayInt)
                    }
                }
            }

            // Weekdays: 2=Monday, 3=Tuesday, ..., 6=Friday
            let weekdays = [2, 3, 4, 5, 6]
            let dayNames = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]

            await MainActor.run {
                for (index, dow) in weekdays.enumerated() {
                    let option = ForcedHomeDayOption(
                        id: dow,
                        dayOfWeek: dow,
                        displayName: dayNames[index],
                        isForced: checkedDays.contains(dow)
                    )
                    option.onChange = { [weak self] in self?.scheduleSettingsSave() }
                    forcedHomeDays.append(option)
                }
            }
        }
    }

    /// Loads holiday override entries from settings into the collection.
    private func loadHolidayOverrides(overrides: [String: HolidayOverride]) {
        holidayOverrideEntries.removeAll()

        for (keyword, holidayOverride) in overrides {
            let entry = HolidayOverrideEntry()
            entry.keyword = keyword
            entry.emoji = holidayOverride.emoji
            entry.customMessage = holidayOverride.customMessage ?? ""
            entry.onChange = { [weak self] in self?.scheduleSettingsSave() }
            holidayOverrideEntries.append(entry)
        }
    }

    /// Loads day label entries from settings into the collection.
    private func loadDayLabelEntries(dayLabels: [DayLabel], startDate: String?) {
        dayLabelEntries.removeAll()

        for dayLabel in dayLabels {
            let entry = DayLabelEntry()
            entry.label = dayLabel.label
            entry.color = dayLabel.color
            entry.onChange = { [weak self] in self?.scheduleSettingsSave() }
            dayLabelEntries.append(entry)
        }

        dayLabelStartDate = startDate ?? ""
    }

    /// Attempts to load cached menu data from disk on startup.
    private func tryLoadMenuCache(settings: AppSettings) async {
        do {
            guard let cache = await settingsService.loadMenuCache(),
                  let menuResponse = cache.menuResponse,
                  let allergies = cache.allergies else {
                return
            }

            // Restore identifier response if cached
            if let identifierResponse = cache.identifierResponse {
                lastIdentifierResponse = identifierResponse
                populateBuildings(from: identifierResponse)
            }

            await populateAllergens(allergies: allergies)

            // Set lastMenuResponse after populateSessions
            populateSessions(from: menuResponse)
            lastMenuResponse = menuResponse
            await populateRecipeNames(from: menuResponse)

            // Apply saved allergen selections from settings (overrides defaults)
            if !settings.selectedAllergenIds.isEmpty {
                for allergen in availableAllergens {
                    allergen.isSelected = settings.selectedAllergenIds.contains(allergen.allergyId)
                }
            }

            // Restore saved session
            if let savedSession = settings.selectedSessionName, availableSessions.contains(savedSession) {
                selectedSession = savedSession
            }

            loadForcedHomeDays()
            detectMonthFromMenu(menuResponse)

            let age = Date().timeIntervalSince(cache.savedAtUtc)
            statusText = "Loaded cached menu data (saved \(formatAge(age)) ago). Ready to generate, or fetch fresh data."
            logger.info("Preloaded menu cache (age: \(age)s)")
        } catch {
            logger.warning("Failed to preload menu cache, continuing without it: \(error.localizedDescription)")
        }
    }

    /// Detects the month/year from menu data and updates the selectors.
    private func detectMonthFromMenu(_ menu: FamilyMenuResponse) {
        // Use selected session if available, otherwise try any session
        let sessionName = selectedSession
        let session: MenuSession?
        if let name = sessionName, !name.isEmpty {
            session = menu.familyMenuSessions.first { $0.servingSession.lowercased() == name.lowercased() }
        } else {
            session = menu.familyMenuSessions.first
        }

        guard let firstDateString = session?.menuPlans.flatMap({ $0.days }).first?.date else { return }

        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "M/d/yyyy"
        dateFormatter.locale = Locale(identifier: "en_US_POSIX")

        guard let parsed = dateFormatter.date(from: firstDateString) else { return }

        let calendar = Calendar.current
        selectedMonth = calendar.component(.month, from: parsed)
        selectedYear = calendar.component(.year, from: parsed)

        if !availableYears.contains(selectedYear) {
            availableYears.append(selectedYear)
            availableYears.sort()
        }
    }

    // MARK: - Helper Methods - Theme

    /// Auto-suggests a theme based on the selected month, if the user hasn't manually picked one.
    private func autoSuggestTheme(for month: Int) {
        // Search visible themes in the list (respects hidden themes filtering)
        let visibleThemes = themeListItems.compactMap { $0.theme }

        let suggested = visibleThemes.first {
            $0.suggestedMonth == month || $0.suggestedMonth2 == month
        }

        let target = suggested ?? visibleThemes.first { $0.name == "Default" } ?? CalendarThemes.all.last!
        if selectedTheme.name != target.name {
            suppressThemeManualFlag = true
            selectedTheme = target
            if let item = themeListItems.first(where: { $0.theme?.name == target.name }) {
                selectedThemeItem = item
            }
            suppressThemeManualFlag = false
        }
    }

    /// Builds the categorized theme list, filtering out hidden themes.
    private func buildThemeList(hiddenThemeNames: [String]) {
        themeListItems.removeAll()
        let hiddenSet = Set(hiddenThemeNames.map { $0.lowercased() })

        let categories = ["Seasonal", "Fun", "Basic"]
        for category in categories {
            let themes = CalendarThemes.all.filter {
                $0.category == category && !hiddenSet.contains($0.name.lowercased())
            }

            if themes.isEmpty {
                continue
            }

            themeListItems.append(ThemeListItem(headerText: category))
            for theme in themes {
                themeListItems.append(ThemeListItem(theme: theme))
            }
        }
    }

    // MARK: - Helper Methods - Build From UI

    /// Builds a dictionary of plan label overrides from the UI collection for saving.
    private func buildPlanLabelOverridesFromUi() -> [String: String] {
        var result: [String: String] = [:]
        for entry in planLabelEntries {
            let trimmed = entry.shortLabel.trimmingCharacters(in: .whitespaces)
            if !trimmed.isEmpty {
                result[entry.planName] = trimmed
            }
        }
        return result
    }

    /// Builds a dictionary of plan icon overrides from the UI collection for saving.
    private func buildPlanIconOverridesFromUi() -> [String: String] {
        var result: [String: String] = [:]
        for entry in planLabelEntries {
            let trimmed = entry.icon.trimmingCharacters(in: .whitespaces)
            if !trimmed.isEmpty {
                result[entry.planName] = trimmed
            }
        }
        return result
    }

    /// Builds the plan display order list from the UI collection for saving.
    private func buildPlanDisplayOrderFromUi() -> [String] {
        planLabelEntries.map { $0.planName }
    }

    /// Builds a dictionary of holiday overrides from the UI collection for saving.
    private func buildHolidayOverridesFromUi() -> [String: HolidayOverride] {
        var result: [String: HolidayOverride] = [:]
        for entry in holidayOverrideEntries {
            let keyword = entry.keyword.trimmingCharacters(in: .whitespaces).lowercased()
            guard !keyword.isEmpty else { continue }
            result[keyword] = HolidayOverride(
                emoji: entry.emoji,
                customMessage: entry.customMessage.trimmingCharacters(in: .whitespaces).isEmpty ? nil : entry.customMessage
            )
        }
        return result
    }

    /// Builds the day label cycle list from the UI collection for saving.
    private func buildDayLabelCycleFromUi() -> [DayLabel] {
        dayLabelEntries
            .filter { !$0.label.trimmingCharacters(in: .whitespaces).isEmpty }
            .map { DayLabel(label: $0.label.trimmingCharacters(in: .whitespaces), color: $0.color.trimmingCharacters(in: .whitespaces).isEmpty ? "#6c757d" : $0.color.trimmingCharacters(in: .whitespaces)) }
    }

    /// Merges plan overrides: current UI state takes priority, then existing saved entries for plans
    /// not currently visible in the UI are preserved.
    private func mergePlanOverrides(existing: [String: String], fromUi: [String: String]) -> [String: String] {
        let currentPlanNames = Set(planLabelEntries.map { $0.planName })
        var merged = fromUi

        for (key, value) in existing {
            if !currentPlanNames.contains(key) {
                merged[key] = value
            }
        }

        return merged
    }

    /// Merges plan display order: current UI order first, then appends saved entries for plans
    /// not currently visible.
    private func mergePlanDisplayOrder(existing: [String], fromUi: [String]) -> [String] {
        let currentPlanNames = Set(planLabelEntries.map { $0.planName })
        var merged = fromUi

        for name in existing {
            if !currentPlanNames.contains(name) && !merged.contains(name) {
                merged.append(name)
            }
        }

        return merged
    }

    // MARK: - Static Helper Methods

    /// Returns the default holiday override dictionary.
    private static func getDefaultHolidayOverrides() -> [String: HolidayOverride] {
        return [
            "winter break": HolidayOverride(emoji: "\u{2744}\u{FE0F}"), // ❄️
            "christmas": HolidayOverride(emoji: "\u{2744}\u{FE0F}"), // ❄️
            "thanksgiving": HolidayOverride(emoji: "\u{1F983}"), // 🦃
            "president": HolidayOverride(emoji: "\u{1F1FA}\u{1F1F8}"), // 🇺🇸
            "mlk": HolidayOverride(emoji: "\u{270A}"), // ✊
            "martin luther king": HolidayOverride(emoji: "\u{270A}"), // ✊
            "memorial": HolidayOverride(emoji: "\u{1F1FA}\u{1F1F8}"), // 🇺🇸
            "labor": HolidayOverride(emoji: "\u{1F1FA}\u{1F1F8}"), // 🇺🇸
            "spring break": HolidayOverride(emoji: "\u{1F338}"), // 🌸
            "teacher": HolidayOverride(emoji: "\u{1F4DA}") // 📚
        ]
    }

    /// Returns today's date for past-day comparison, but only after 3 PM (end of school day).
    private static func getPastDayCutoff() -> Date {
        let now = Date()
        let calendar = Calendar.current
        let hour = calendar.component(.hour, from: now)

        if hour >= 15 {
            return now
        } else {
            return calendar.date(byAdding: .day, value: -1, to: now) ?? now
        }
    }

    /// Formats a TimeInterval as a human-readable age string.
    private func formatAge(_ interval: TimeInterval) -> String {
        let totalMinutes = Int(interval / 60)
        if totalMinutes < 1 { return "just now" }
        if totalMinutes < 60 { return "\(totalMinutes)m" }
        let hours = totalMinutes / 60
        let minutes = totalMinutes % 60
        if hours < 24 { return "\(hours)h \(minutes)m" }
        let days = hours / 24
        return "\(days)d"
    }
}

// MARK: - Building Extension

extension Building: Identifiable, Hashable {
    var id: String { buildingId }

    func hash(into hasher: inout Hasher) {
        hasher.combine(buildingId)
    }

    static func == (lhs: Building, rhs: Building) -> Bool {
        lhs.buildingId == rhs.buildingId
    }
}

// MARK: - Supporting Types

/// Represents a month option for the month selector dropdown.
struct MonthOption: Identifiable {
    let id: Int
    let name: String
}

/// Represents an allergen with a selection state for the checklist UI.
class AllergenOption: ObservableObject, Identifiable {
    let id: String
    let allergyId: String
    let name: String
    var onChange: (() -> Void)?

    @Published var isSelected: Bool {
        didSet { onChange?() }
    }

    init(id: String, allergyId: String, name: String, isSelected: Bool = false) {
        self.id = id
        self.allergyId = allergyId
        self.name = name
        self.isSelected = isSelected
    }
}

/// Represents a recipe with a not-preferred selection state for the checklist UI.
class NotPreferredOption: ObservableObject, Identifiable {
    let id: String
    let name: String
    let allergenIds: Set<String>
    var onChange: (() -> Void)?

    @Published var isNotPreferred: Bool {
        didSet { onChange?() }
    }

    init(id: String, name: String, allergenIds: Set<String>, isNotPreferred: Bool = false) {
        self.id = id
        self.name = name
        self.allergenIds = allergenIds
        self.isNotPreferred = isNotPreferred
    }
}

/// Represents a recipe with a favorite selection state for the checklist UI.
class FavoriteOption: ObservableObject, Identifiable {
    let id: String
    let name: String
    let allergenIds: Set<String>
    var onChange: (() -> Void)?

    @Published var isFavorite: Bool {
        didSet { onChange?() }
    }

    init(id: String, name: String, allergenIds: Set<String>, isFavorite: Bool = false) {
        self.id = id
        self.name = name
        self.allergenIds = allergenIds
        self.isFavorite = isFavorite
    }
}

/// Represents a day-of-week option for the forced home days checklist.
class ForcedHomeDayOption: ObservableObject, Identifiable {
    let id: Int
    let dayOfWeek: Int // 2=Monday, 3=Tuesday, ..., 7=Saturday, 1=Sunday
    let displayName: String
    var onChange: (() -> Void)?

    @Published var isForced: Bool {
        didSet { onChange?() }
    }

    init(id: Int, dayOfWeek: Int, displayName: String, isForced: Bool = false) {
        self.id = id
        self.dayOfWeek = dayOfWeek
        self.displayName = displayName
        self.isForced = isForced
    }
}

/// Wrapper for theme ComboBox items: either a non-selectable section header or a selectable theme.
struct ThemeListItem: Identifiable {
    let id = UUID()
    var headerText: String?
    var theme: CalendarTheme?

    /// True if this item is a non-selectable category header.
    var isHeader: Bool { headerText != nil }

    /// Display text for the dropdown item.
    var displayText: String {
        if let header = headerText {
            return "── \(header) ──"
        }
        if let theme = theme {
            return "\(theme.emoji) \(theme.name)"
        }
        return ""
    }

    init(headerText: String? = nil, theme: CalendarTheme? = nil) {
        self.headerText = headerText
        self.theme = theme
    }
}

/// Represents a plan line with an editable short label for vending buttons.
class PlanLabelEntry: ObservableObject, Identifiable {
    let id = UUID()
    let planName: String
    var onChange: (() -> Void)?

    @Published var shortLabel: String {
        didSet { onChange?() }
    }
    @Published var icon: String {
        didSet { onChange?() }
    }
    @Published var isEditing: Bool = false

    /// The label shown in view mode: short label if set, otherwise the full plan name.
    var displayLabel: String {
        shortLabel.trimmingCharacters(in: .whitespaces).isEmpty ? planName : shortLabel
    }

    /// Whether this entry has a custom short label override.
    var hasCustomLabel: Bool {
        !shortLabel.trimmingCharacters(in: .whitespaces).isEmpty
    }

    init(planName: String, shortLabel: String = "", icon: String = "") {
        self.planName = planName
        self.shortLabel = shortLabel
        self.icon = icon
    }
}

/// Represents a layout mode option for the Day Layout dropdown.
struct LayoutModeOption: Identifiable {
    let id: String
    let value: String
    let displayText: String
}

/// Represents an editable holiday override entry in the UI.
class HolidayOverrideEntry: ObservableObject, Identifiable {
    let id = UUID()
    var onChange: (() -> Void)?

    @Published var keyword: String = "" {
        didSet { onChange?() }
    }
    @Published var emoji: String = "" {
        didSet { onChange?() }
    }
    @Published var customMessage: String = "" {
        didSet { onChange?() }
    }
}

/// Represents a day label entry in the rotating day label cycle.
class DayLabelEntry: ObservableObject, Identifiable {
    let id = UUID()
    var onChange: (() -> Void)?

    @Published var label: String = "" {
        didSet { onChange?() }
    }
    @Published var color: String = "#6c757d" {
        didSet { onChange?() }
    }
}
