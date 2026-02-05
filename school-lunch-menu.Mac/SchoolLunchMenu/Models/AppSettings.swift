import Foundation

// MARK: - AppSettings

/// Persisted application settings saved to settings.json.
class AppSettings: Codable {
    /// UUIDs of allergens selected for filtering.
    var selectedAllergenIds: [String] = []

    /// Weekdays forced as "from home" days per session, regardless of safe options.
    /// E.g., ["Breakfast": ["Thursday"], "Lunch": ["Thursday"]]
    var forcedHomeDaysBySession: [String: [String]] = [:]

    /// Recipe names the user has marked as not preferred, keyed by session name.
    /// E.g., ["Lunch": ["Corn Dog", ...], "Breakfast": ["Pancake", ...]]
    var notPreferredBySession: [String: [String]] = [:]

    /// Recipe names the user has marked as favorites, keyed by session name.
    /// E.g., ["Lunch": ["Pizza", ...], "Breakfast": ["Waffles", ...]]
    var favoritesBySession: [String: [String]] = [:]

    /// The identifier code used to look up the district (e.g., "YVAM38").
    var identifier: String? = "YVAM38"

    /// The selected district UUID.
    var districtId: String? = "47ce70b9-238e-ea11-bd68-f554d510c22b"

    /// The selected building UUID.
    var buildingId: String?

    /// The selected serving session name (e.g., "Lunch", "Breakfast").
    var selectedSessionName: String? = "Lunch"

    /// The name of the selected calendar theme (e.g., "Valentines", "Dinosaurs").
    var selectedThemeName: String?

    /// Theme names hidden from the dropdown. Users can edit settings.json to add entries.
    var hiddenThemeNames: [String] = []

    /// Legacy property kept for backward-compatible deserialization.
    /// If layoutMode is nil/empty and this is true, layoutMode defaults to "IconsRight".
    var showMealButtons: Bool = false

    /// Calendar layout mode: "List" (badge + items, no grid), "IconsLeft" (grid with buttons on left),
    /// "IconsRight" (grid with buttons on right). Default: "IconsLeft".
    var layoutMode: String = "IconsLeft"

    /// Custom short labels for plan line names displayed in meal buttons.
    /// E.g., ["Big Cat Cafe - MS": "Big Cat", "Lunch - MS": "Regular"]
    var planLabelOverrides: [String: String] = [:]

    /// User-editable emoji icons per plan line name.
    /// E.g., ["Big Cat Cafe - MS": "...", "Lunch - MS": "..."]
    var planIconOverrides: [String: String] = [:]

    /// User-defined display order for plan lines. Plan names listed here appear first (in order),
    /// followed by any remaining plans alphabetically.
    var planDisplayOrder: [String] = []

    /// Whether to show plan lines that have no allergen-safe items (grayed-out button with helper text).
    var showUnsafeLines: Bool = true

    /// Message shown next to grayed-out buttons for unsafe plan lines. Default: "No safe options".
    var unsafeLineMessage: String = "No safe options"

    /// Customizable emoji and message overrides for no-school holidays, keyed by keyword.
    /// E.g., ["thanksgiving": HolidayOverride(emoji: "...", customMessage: nil)]
    var holidayOverrides: [String: HolidayOverride] = [:]

    /// Whether to visually cross out past days on the calendar.
    var crossOutPastDays: Bool = true

    /// Rotating day label cycle definition. Each entry is a label with a color.
    /// Labels cycle in order across school days (skipping no-school days).
    /// E.g., [DayLabel(label: "Red", color: "#dc3545"), DayLabel(label: "White", color: "#adb5bd")]
    var dayLabelCycle: [DayLabel] = [
        DayLabel(label: "Red", color: "#dc3545"),
        DayLabel(label: "White", color: "#adb5bd")
    ]

    /// The date on which the day label cycle starts (anchors label assignment).
    /// Defaults to the first school day of the month if not set.
    var dayLabelStartDate: String?

    /// Which corner the day label triangle appears in: "TopRight" (default), "TopLeft", "BottomRight", "BottomLeft".
    var dayLabelCorner: String = "TopRight"

    /// Whether to append a shareable footer with QR code to the calendar.
    var showShareFooter: Bool = false

    /// The district display name (e.g., "Lakeville Area Schools").
    var districtName: String? = "Lakeville Area Schools"

    init() {}

    // MARK: - Coding Keys

    enum CodingKeys: String, CodingKey {
        case selectedAllergenIds
        case forcedHomeDaysBySession
        case notPreferredBySession
        case favoritesBySession
        case identifier
        case districtId
        case buildingId
        case selectedSessionName
        case selectedThemeName
        case hiddenThemeNames
        case showMealButtons
        case layoutMode
        case planLabelOverrides
        case planIconOverrides
        case planDisplayOrder
        case showUnsafeLines
        case unsafeLineMessage
        case holidayOverrides
        case crossOutPastDays
        case dayLabelCycle
        case dayLabelStartDate
        case dayLabelCorner
        case showShareFooter
        case districtName
    }
}

// MARK: - HolidayOverride

/// Custom emoji and optional message for a holiday keyword match.
struct HolidayOverride: Codable {
    /// The emoji to display for this holiday.
    var emoji: String = ""

    /// Optional custom message to display instead of the academic note. Nil uses the original note.
    var customMessage: String?

    init(emoji: String = "", customMessage: String? = nil) {
        self.emoji = emoji
        self.customMessage = customMessage
    }
}

// MARK: - DayLabel

/// A single label in the rotating day label cycle.
struct DayLabel: Codable {
    /// Short label text (e.g., "Red", "White", "A", "B").
    var label: String = ""

    /// CSS color for the corner triangle background.
    var color: String = "#6c757d"

    init(label: String = "", color: String = "#6c757d") {
        self.label = label
        self.color = color
    }
}
