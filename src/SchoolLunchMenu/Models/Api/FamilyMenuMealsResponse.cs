using System.Text.Json.Serialization;

namespace SchoolLunchMenu.Models.Api;

/// <summary>
/// A meal item returned by the LINQ Connect family menu meals endpoint.
/// The response is a JSON array of these items.
/// </summary>
public record MealItem
{
    /// <summary>
    /// The unique identifier for the meal.
    /// </summary>
    [JsonPropertyName("MealId")]
    public string MealId { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the meal (e.g., "Monday 1").
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The sort order used for display purposes.
    /// </summary>
    [JsonPropertyName("SortOrder")]
    public int SortOrder { get; init; }
}
