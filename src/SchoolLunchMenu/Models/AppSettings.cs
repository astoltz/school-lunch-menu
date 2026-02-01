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
    public string? Identifier { get; set; }

    /// <summary>The selected district UUID.</summary>
    public string? DistrictId { get; set; }

    /// <summary>The selected building UUID.</summary>
    public string? BuildingId { get; set; }

    /// <summary>The selected serving session name (e.g., "Lunch", "Breakfast").</summary>
    public string? SelectedSessionName { get; set; }

    /// <summary>The name of the selected calendar theme (e.g., "Valentines", "Dinosaurs").</summary>
    public string? SelectedThemeName { get; set; }

    /// <summary>Theme names hidden from the dropdown. Users can edit settings.json to add entries.</summary>
    public List<string> HiddenThemeNames { get; set; } = [];
}
