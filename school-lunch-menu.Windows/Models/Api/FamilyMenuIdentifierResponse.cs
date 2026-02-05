using System.Text.Json.Serialization;

namespace SchoolLunchMenu.Models.Api;

/// <summary>
/// Response from the LINQ Connect family menu identifier endpoint,
/// containing district info, buildings, and the menu identifier code.
/// </summary>
public record FamilyMenuIdentifierResponse
{
    /// <summary>
    /// The unique identifier for the school district.
    /// </summary>
    [JsonPropertyName("DistrictId")]
    public string DistrictId { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the school district.
    /// </summary>
    [JsonPropertyName("DistrictName")]
    public string DistrictName { get; init; } = string.Empty;

    /// <summary>
    /// The list of buildings within the district.
    /// </summary>
    [JsonPropertyName("Buildings")]
    public List<Building> Buildings { get; init; } = [];

    /// <summary>
    /// A notification message displayed alongside the menu.
    /// </summary>
    [JsonPropertyName("MenuNotification")]
    public string? MenuNotification { get; init; }

    /// <summary>
    /// The short alphanumeric identifier code for the family menu.
    /// </summary>
    [JsonPropertyName("Identifier")]
    public string Identifier { get; init; } = string.Empty;
}

/// <summary>
/// A school building within a district.
/// </summary>
public record Building
{
    /// <summary>
    /// The unique identifier for the building.
    /// </summary>
    [JsonPropertyName("BuildingId")]
    public string BuildingId { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the building.
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;
}
