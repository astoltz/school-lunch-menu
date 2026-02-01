namespace SchoolLunchMenu.Models;

using SchoolLunchMenu.Models.Api;

/// <summary>
/// Disk-persisted cache of the last fetched menu data and allergens,
/// allowing the app to preload data on startup without an API call.
/// </summary>
public class MenuCache
{
    /// <summary>When this cache was written (UTC).</summary>
    public DateTime SavedAtUtc { get; set; }

    /// <summary>The cached menu response.</summary>
    public FamilyMenuResponse? MenuResponse { get; set; }

    /// <summary>The cached allergen list.</summary>
    public List<AllergyItem>? Allergies { get; set; }

    /// <summary>The cached identifier response (district + buildings).</summary>
    public FamilyMenuIdentifierResponse? IdentifierResponse { get; set; }
}
