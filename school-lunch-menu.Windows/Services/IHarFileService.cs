namespace SchoolLunchMenu.Services;

using SchoolLunchMenu.Models.Api;

/// <summary>
/// Parses LINQ Connect API responses from a HAR (HTTP Archive) file.
/// </summary>
public interface IHarFileService
{
    /// <summary>
    /// Loads and parses API data from a HAR file.
    /// </summary>
    /// <param name="harFilePath">Path to the HAR file.</param>
    /// <returns>Tuple of menu response, allergen list, and identifier response.</returns>
    Task<(FamilyMenuResponse Menu, List<AllergyItem> Allergies, FamilyMenuIdentifierResponse Identifier)> LoadFromHarFileAsync(string harFilePath);
}
