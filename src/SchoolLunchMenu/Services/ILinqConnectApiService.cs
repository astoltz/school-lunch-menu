namespace SchoolLunchMenu.Services;

using SchoolLunchMenu.Models.Api;

/// <summary>
/// Fetches menu data from the LINQ Connect API.
/// </summary>
public interface ILinqConnectApiService
{
    /// <summary>
    /// Fetches district and building information for the given identifier.
    /// </summary>
    Task<FamilyMenuIdentifierResponse> GetMenuIdentifierAsync(string identifier);

    /// <summary>
    /// Fetches the list of allergens for a district.
    /// </summary>
    Task<List<AllergyItem>> GetAllergiesAsync(string districtId);

    /// <summary>
    /// Fetches the full menu for a building and date range.
    /// </summary>
    Task<FamilyMenuResponse> GetMenuAsync(string buildingId, string districtId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Fetches meal definitions for a district.
    /// </summary>
    Task<List<MealItem>> GetMenuMealsAsync(string districtId);
}
