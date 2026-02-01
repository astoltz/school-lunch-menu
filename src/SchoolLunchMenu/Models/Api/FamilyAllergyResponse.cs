using System.Text.Json.Serialization;

namespace SchoolLunchMenu.Models.Api;

/// <summary>
/// An allergen item returned by the LINQ Connect family allergy endpoint.
/// The response is a JSON array of these items.
/// </summary>
public record AllergyItem
{
    /// <summary>
    /// The unique identifier for the allergen.
    /// </summary>
    [JsonPropertyName("AllergyId")]
    public string AllergyId { get; init; } = string.Empty;

    /// <summary>
    /// The sort order used for display purposes.
    /// </summary>
    [JsonPropertyName("SortOrder")]
    public int SortOrder { get; init; }

    /// <summary>
    /// The display name of the allergen.
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;
}
