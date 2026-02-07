namespace SchoolLunchMenu.Models;

/// <summary>
/// Persisted application settings saved to settings.json.
/// </summary>
public class AppSettings
{
    /// <summary>UUIDs of allergens selected for filtering.</summary>
    public List<string> SelectedAllergenIds { get; set; } = [];

    /// <summary>
    /// Weekdays forced as "from home" days per session, regardless of safe options.
    /// E.g., { "Breakfast": ["Thursday"], "Lunch": ["Thursday"] }
    /// </summary>
    public Dictionary<string, List<string>> ForcedHomeDaysBySession { get; set; } = [];

    /// <summary>
    /// Recipe names the user has marked as not preferred, keyed by session name.
    /// E.g., { "Lunch": ["Corn Dog", ...], "Breakfast": ["Pancake", ...] }
    /// </summary>
    public Dictionary<string, List<string>> NotPreferredBySession { get; set; } = [];

    /// <summary>
    /// Recipe names the user has marked as favorites, keyed by session name.
    /// E.g., { "Lunch": ["Pizza", ...], "Breakfast": ["Waffles", ...] }
    /// </summary>
    public Dictionary<string, List<string>> FavoritesBySession { get; set; } = [];

    /// <summary>The identifier code used to look up the district (e.g., "YVAM38").</summary>
    public string? Identifier { get; set; } = "YVAM38";

    /// <summary>The selected district UUID.</summary>
    public string? DistrictId { get; set; } = "47ce70b9-238e-ea11-bd68-f554d510c22b";

    /// <summary>The selected building UUID.</summary>
    public string? BuildingId { get; set; }

    /// <summary>The selected serving session name (e.g., "Lunch", "Breakfast").</summary>
    public string? SelectedSessionName { get; set; } = "Lunch";

    /// <summary>The name of the selected calendar theme (e.g., "Valentines", "Dinosaurs").</summary>
    public string? SelectedThemeName { get; set; }

    /// <summary>Theme names hidden from the dropdown. Users can edit settings.json to add entries.</summary>
    public List<string> HiddenThemeNames { get; set; } = [];

    /// <summary>
    /// Legacy property kept for backward-compatible deserialization.
    /// If LayoutMode is null/empty and this is true, LayoutMode defaults to "IconsRight".
    /// </summary>
    public bool ShowMealButtons { get; set; }

    /// <summary>
    /// Calendar layout mode: "List" (badge + items, no grid), "IconsLeft" (grid with buttons on left),
    /// "IconsRight" (grid with buttons on right). Default: "List".
    /// </summary>
    public string LayoutMode { get; set; } = "IconsLeft";

    /// <summary>
    /// Custom short labels for plan line names displayed in meal buttons.
    /// E.g., { "Big Cat Cafe - MS": "Big Cat", "Lunch - MS": "Regular" }
    /// </summary>
    public Dictionary<string, string> PlanLabelOverrides { get; set; } = [];

    /// <summary>
    /// User-editable emoji icons per plan line name.
    /// E.g., { "Big Cat Cafe - MS": "🐱", "Lunch - MS": "🍽️" }
    /// </summary>
    public Dictionary<string, string> PlanIconOverrides { get; set; } = [];

    /// <summary>
    /// User-defined display order for plan lines. Plan names listed here appear first (in order),
    /// followed by any remaining plans alphabetically.
    /// </summary>
    public List<string> PlanDisplayOrder { get; set; } = [];

    /// <summary>Whether to show plan lines that have no allergen-safe items (grayed-out button with helper text).</summary>
    public bool ShowUnsafeLines { get; set; } = true;

    /// <summary>
    /// Message shown next to grayed-out buttons for unsafe plan lines. Default: "No safe options".
    /// </summary>
    public string UnsafeLineMessage { get; set; } = "No safe options";

    /// <summary>
    /// Customizable emoji and message overrides for no-school holidays, keyed by keyword.
    /// E.g., { "thanksgiving": { Emoji: "🦃", CustomMessage: null } }
    /// </summary>
    public Dictionary<string, HolidayOverride> HolidayOverrides { get; set; } = [];

    /// <summary>Whether to visually cross out past days on the calendar.</summary>
    public bool CrossOutPastDays { get; set; } = true;

    /// <summary>Whether to show day labels (e.g., Red/White Day) on the calendar.</summary>
    public bool DayLabelsEnabled { get; set; } = true;

    /// <summary>
    /// Rotating day label cycle definition. Each entry is a label with a color.
    /// Labels cycle in order across school days (skipping no-school days).
    /// E.g., [{ Label: "Red", Color: "#dc3545" }, { Label: "White", Color: "#6c757d" }]
    /// </summary>
    public List<DayLabel> DayLabelCycle { get; set; } =
    [
        new() { Label = "Red", Color = "#dc3545" },
        new() { Label = "White", Color = "#adb5bd" }
    ];

    /// <summary>
    /// The date on which the day label cycle starts (anchors label assignment).
    /// Defaults to the first school day of the month if not set.
    /// </summary>
    public string? DayLabelStartDate { get; set; }

    /// <summary>
    /// Which corner the day label triangle appears in: "TopRight" (default), "TopLeft", "BottomRight", "BottomLeft".
    /// </summary>
    public string DayLabelCorner { get; set; } = "TopRight";

    /// <summary>Whether to append a shareable footer with QR code to the calendar.</summary>
    public bool ShowShareFooter { get; set; }

    /// <summary>The district display name (e.g., "Lakeville Area Schools").</summary>
    public string? DistrictName { get; set; } = "Lakeville Area Schools";

    /// <summary>Custom User-Agent string for HTTP requests. Auto-populated from HAR file or uses Firefox default.</summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Custom emoji and optional message for a holiday keyword match.
/// </summary>
public class HolidayOverride
{
    /// <summary>The emoji to display for this holiday.</summary>
    public string Emoji { get; set; } = "";

    /// <summary>Optional custom message to display instead of the academic note. Null uses the original note.</summary>
    public string? CustomMessage { get; set; }
}

/// <summary>
/// A single label in the rotating day label cycle.
/// </summary>
public class DayLabel
{
    /// <summary>Short label text (e.g., "Red", "White", "A", "B").</summary>
    public string Label { get; set; } = "";

    /// <summary>CSS color for the corner triangle background.</summary>
    public string Color { get; set; } = "#6c757d";
}
