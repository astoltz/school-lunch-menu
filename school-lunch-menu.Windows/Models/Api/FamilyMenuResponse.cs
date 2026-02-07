using System.Text.Json.Serialization;

namespace SchoolLunchMenu.Models.Api;

/// <summary>
/// Top-level response from the LINQ Connect family menu endpoint,
/// containing serving sessions with their full menu trees and academic calendars.
/// </summary>
public record FamilyMenuResponse
{
    /// <summary>
    /// The list of serving sessions (e.g., Breakfast, Lunch) with their menu plans.
    /// </summary>
    [JsonPropertyName("FamilyMenuSessions")]
    public List<MenuSession> FamilyMenuSessions { get; init; } = [];

    /// <summary>
    /// The academic calendars indicating school closures and special dates.
    /// </summary>
    [JsonPropertyName("AcademicCalendars")]
    public List<AcademicCalendar> AcademicCalendars { get; init; } = [];
}

/// <summary>
/// A serving session such as Breakfast or Lunch, containing one or more menu plans.
/// </summary>
public record MenuSession
{
    /// <summary>
    /// The base64-encoded key for the serving session.
    /// </summary>
    [JsonPropertyName("ServingSessionKey")]
    public string ServingSessionKey { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier for the serving session.
    /// </summary>
    [JsonPropertyName("ServingSessionId")]
    public string ServingSessionId { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the serving session (e.g., "Lunch").
    /// </summary>
    [JsonPropertyName("ServingSession")]
    public string ServingSession { get; init; } = string.Empty;

    /// <summary>
    /// The menu plans available within this serving session.
    /// </summary>
    [JsonPropertyName("MenuPlans")]
    public List<MenuPlan> MenuPlans { get; init; } = [];
}

/// <summary>
/// A menu plan containing days of scheduled meals.
/// </summary>
public record MenuPlan
{
    /// <summary>
    /// The display name of the menu plan.
    /// </summary>
    [JsonPropertyName("MenuPlanName")]
    public string MenuPlanName { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier for the menu plan.
    /// </summary>
    [JsonPropertyName("MenuPlanId")]
    public string MenuPlanId { get; init; } = string.Empty;

    /// <summary>
    /// The list of days with their scheduled meals.
    /// </summary>
    [JsonPropertyName("Days")]
    public List<MenuDay> Days { get; init; } = [];
}

/// <summary>
/// A single day within a menu plan, containing the meals served that day.
/// </summary>
public record MenuDay
{
    /// <summary>
    /// The date string for this menu day (e.g., "2/2/2026").
    /// </summary>
    [JsonPropertyName("Date")]
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The meals available on this day.
    /// </summary>
    [JsonPropertyName("MenuMeals")]
    public List<MenuMeal> MenuMeals { get; init; } = [];

    /// <summary>
    /// The academic calendar identifier associated with this day, if any.
    /// </summary>
    [JsonPropertyName("AcademicCalenderId")]
    public string? AcademicCalenderId { get; init; }
}

/// <summary>
/// A single meal offering within a menu day.
/// </summary>
public record MenuMeal
{
    /// <summary>
    /// The name of the menu plan this meal belongs to.
    /// </summary>
    [JsonPropertyName("MenuPlanName")]
    public string MenuPlanName { get; init; } = string.Empty;

    /// <summary>
    /// The display name of this meal (e.g., "Monday 1 - MS").
    /// </summary>
    [JsonPropertyName("MenuMealName")]
    public string MenuMealName { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier for this meal.
    /// </summary>
    [JsonPropertyName("MenuMealId")]
    public string MenuMealId { get; init; } = string.Empty;

    /// <summary>
    /// The recipe categories (e.g., Main Entrees, Sides) within this meal.
    /// </summary>
    [JsonPropertyName("RecipeCategories")]
    public List<RecipeCategory> RecipeCategories { get; init; } = [];
}

/// <summary>
/// A category of recipes within a meal (e.g., "Main Entrees", "Sides").
/// </summary>
public record RecipeCategory
{
    /// <summary>
    /// The display name of the category.
    /// </summary>
    [JsonPropertyName("CategoryName")]
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// The hex color code used for display (e.g., "#000000").
    /// </summary>
    [JsonPropertyName("Color")]
    public string Color { get; init; } = string.Empty;

    /// <summary>
    /// Whether this category represents an entree selection.
    /// </summary>
    [JsonPropertyName("IsEntree")]
    public bool IsEntree { get; init; } = true;

    /// <summary>
    /// The recipes available in this category.
    /// </summary>
    [JsonPropertyName("Recipes")]
    public List<Recipe> Recipes { get; init; } = [];
}

/// <summary>
/// A single recipe (food item) with nutritional and allergen information.
/// </summary>
public record Recipe
{
    /// <summary>
    /// The unique item identifier for this recipe.
    /// </summary>
    [JsonPropertyName("ItemId")]
    public string ItemId { get; init; } = string.Empty;

    /// <summary>
    /// The recipe identifier code (e.g., "R2410").
    /// </summary>
    [JsonPropertyName("RecipeIdentifier")]
    public string RecipeIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// The display name of the recipe.
    /// </summary>
    [JsonPropertyName("RecipeName")]
    public string RecipeName { get; init; } = string.Empty;

    /// <summary>
    /// The serving size description (e.g., "Sandwich").
    /// </summary>
    [JsonPropertyName("ServingSize")]
    public string ServingSize { get; init; } = string.Empty;

    /// <summary>
    /// The weight in grams per serving.
    /// </summary>
    [JsonPropertyName("GramPerServing")]
    public double GramPerServing { get; init; }

    /// <summary>
    /// The nutritional information for this recipe.
    /// </summary>
    [JsonPropertyName("Nutrients")]
    public List<Nutrient> Nutrients { get; init; } = [];

    /// <summary>
    /// The list of allergen identifiers associated with this recipe.
    /// </summary>
    [JsonPropertyName("Allergens")]
    public List<string> Allergens { get; init; } = [];

    /// <summary>
    /// The list of religious restriction identifiers for this recipe.
    /// </summary>
    [JsonPropertyName("ReligiousRestrictions")]
    public List<string> ReligiousRestrictions { get; init; } = [];

    /// <summary>
    /// The list of dietary restriction identifiers for this recipe.
    /// </summary>
    [JsonPropertyName("DietaryRestrictions")]
    public List<string> DietaryRestrictions { get; init; } = [];

    /// <summary>
    /// Whether this recipe has nutrient data available.
    /// </summary>
    [JsonPropertyName("HasNutrients")]
    public bool HasNutrients { get; init; }
}

/// <summary>
/// A single nutrient value for a recipe (e.g., Calories, Fat, Protein).
/// </summary>
public record Nutrient
{
    /// <summary>
    /// The display name of the nutrient.
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The numeric value of the nutrient per serving.
    /// </summary>
    [JsonPropertyName("Value")]
    public double Value { get; init; }

    /// <summary>
    /// Whether any ingredient nutrients are missing from the calculation.
    /// </summary>
    [JsonPropertyName("HasMissingNutrients")]
    public bool HasMissingNutrients { get; init; }

    /// <summary>
    /// The unit of measurement (e.g., "kcals", "g", "mg").
    /// </summary>
    [JsonPropertyName("Unit")]
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// The abbreviated display label (e.g., "Cal").
    /// </summary>
    [JsonPropertyName("Abbreviation")]
    public string Abbreviation { get; init; } = string.Empty;
}

/// <summary>
/// An academic calendar containing dates with notes (e.g., school closures).
/// </summary>
public record AcademicCalendar
{
    /// <summary>
    /// The unique identifier for this academic calendar.
    /// </summary>
    [JsonPropertyName("AcademicCalendarId")]
    public string AcademicCalendarId { get; init; } = string.Empty;

    /// <summary>
    /// The list of notable days on this academic calendar.
    /// </summary>
    [JsonPropertyName("Days")]
    public List<AcademicCalendarDay> Days { get; init; } = [];
}

/// <summary>
/// A single day on an academic calendar with an associated note.
/// </summary>
public record AcademicCalendarDay
{
    /// <summary>
    /// The date string for this calendar day (e.g., "2/13/2026").
    /// </summary>
    [JsonPropertyName("Date")]
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The note describing this calendar day (e.g., "No School").
    /// </summary>
    [JsonPropertyName("Note")]
    public string Note { get; init; } = string.Empty;
}
