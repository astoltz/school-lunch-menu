namespace SchoolLunchMenu.Services;

using SchoolLunchMenu.Models;
using SchoolLunchMenu.Models.Api;

/// <summary>
/// Analyzes menu data to classify days based on allergen safety.
/// </summary>
public interface IMenuAnalyzer
{
    /// <summary>
    /// Processes a menu response into a month of classified days.
    /// </summary>
    /// <param name="menuResponse">The full API menu response.</param>
    /// <param name="selectedAllergenIds">Set of allergen UUIDs to filter against.</param>
    /// <param name="notPreferredNames">Recipe names the user has marked as not preferred.</param>
    /// <param name="favoriteNames">Recipe names the user has marked as favorites.</param>
    /// <param name="year">The target year.</param>
    /// <param name="month">The target month (1-12).</param>
    /// <param name="sessionName">The serving session to analyze (e.g., "Lunch", "Breakfast").</param>
    /// <param name="buildingName">Optional building name for the calendar subtitle.</param>
    ProcessedMonth Analyze(FamilyMenuResponse menuResponse, IReadOnlySet<string> selectedAllergenIds, IReadOnlySet<string> notPreferredNames, IReadOnlySet<string> favoriteNames, int year, int month, string sessionName, string? buildingName = null);
}
