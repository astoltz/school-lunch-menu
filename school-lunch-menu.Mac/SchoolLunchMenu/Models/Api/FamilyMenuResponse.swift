import Foundation

/// Top-level response from the LINQ Connect family menu endpoint,
/// containing serving sessions with their full menu trees and academic calendars.
struct FamilyMenuResponse: Codable {
    /// The list of serving sessions (e.g., Breakfast, Lunch) with their menu plans.
    let familyMenuSessions: [MenuSession]

    /// The academic calendars indicating school closures and special dates.
    let academicCalendars: [AcademicCalendar]

    enum CodingKeys: String, CodingKey {
        case familyMenuSessions = "FamilyMenuSessions"
        case academicCalendars = "AcademicCalendars"
    }
}

/// A serving session such as Breakfast or Lunch, containing one or more menu plans.
struct MenuSession: Codable {
    /// The base64-encoded key for the serving session.
    let servingSessionKey: String

    /// The unique identifier for the serving session.
    let servingSessionId: String

    /// The display name of the serving session (e.g., "Lunch").
    let servingSession: String

    /// The menu plans available within this serving session.
    let menuPlans: [MenuPlan]

    enum CodingKeys: String, CodingKey {
        case servingSessionKey = "ServingSessionKey"
        case servingSessionId = "ServingSessionId"
        case servingSession = "ServingSession"
        case menuPlans = "MenuPlans"
    }
}

/// A menu plan containing days of scheduled meals.
struct MenuPlan: Codable {
    /// The display name of the menu plan.
    let menuPlanName: String

    /// The unique identifier for the menu plan.
    let menuPlanId: String

    /// The list of days with their scheduled meals.
    let days: [MenuDay]

    enum CodingKeys: String, CodingKey {
        case menuPlanName = "MenuPlanName"
        case menuPlanId = "MenuPlanId"
        case days = "Days"
    }
}

/// A single day within a menu plan, containing the meals served that day.
struct MenuDay: Codable {
    /// The date string for this menu day (e.g., "2/2/2026").
    let date: String

    /// The meals available on this day.
    let menuMeals: [MenuMeal]

    /// The academic calendar identifier associated with this day, if any.
    let academicCalenderId: String?

    enum CodingKeys: String, CodingKey {
        case date = "Date"
        case menuMeals = "MenuMeals"
        case academicCalenderId = "AcademicCalenderId"
    }
}

/// A single meal offering within a menu day.
struct MenuMeal: Codable {
    /// The name of the menu plan this meal belongs to.
    let menuPlanName: String

    /// The display name of this meal (e.g., "Monday 1 - MS").
    let menuMealName: String

    /// The unique identifier for this meal.
    let menuMealId: String

    /// The recipe categories (e.g., Main Entrees, Sides) within this meal.
    let recipeCategories: [RecipeCategory]

    enum CodingKeys: String, CodingKey {
        case menuPlanName = "MenuPlanName"
        case menuMealName = "MenuMealName"
        case menuMealId = "MenuMealId"
        case recipeCategories = "RecipeCategories"
    }
}

/// A category of recipes within a meal (e.g., "Main Entrees", "Sides").
struct RecipeCategory: Codable {
    /// The display name of the category.
    let categoryName: String

    /// The hex color code used for display (e.g., "#000000").
    let color: String

    /// Whether this category represents an entree selection.
    let isEntree: Bool?

    /// The recipes available in this category.
    let recipes: [Recipe]

    enum CodingKeys: String, CodingKey {
        case categoryName = "CategoryName"
        case color = "Color"
        case isEntree = "IsEntree"
        case recipes = "Recipes"
    }
}

/// A single recipe (food item) with nutritional and allergen information.
struct Recipe: Codable {
    /// The unique item identifier for this recipe.
    let itemId: String

    /// The recipe identifier code (e.g., "R2410").
    let recipeIdentifier: String

    /// The display name of the recipe.
    let recipeName: String

    /// The serving size description (e.g., "Sandwich").
    let servingSize: String

    /// The weight in grams per serving.
    let gramPerServing: Double

    /// The nutritional information for this recipe.
    let nutrients: [Nutrient]

    /// The list of allergen identifiers associated with this recipe.
    let allergens: [String]

    /// The list of religious restriction identifiers for this recipe.
    let religiousRestrictions: [String]

    /// The list of dietary restriction identifiers for this recipe.
    let dietaryRestrictions: [String]

    /// Whether this recipe has nutrient data available.
    let hasNutrients: Bool

    enum CodingKeys: String, CodingKey {
        case itemId = "ItemId"
        case recipeIdentifier = "RecipeIdentifier"
        case recipeName = "RecipeName"
        case servingSize = "ServingSize"
        case gramPerServing = "GramPerServing"
        case nutrients = "Nutrients"
        case allergens = "Allergens"
        case religiousRestrictions = "ReligiousRestrictions"
        case dietaryRestrictions = "DietaryRestrictions"
        case hasNutrients = "HasNutrients"
    }
}

/// A single nutrient value for a recipe (e.g., Calories, Fat, Protein).
struct Nutrient: Codable {
    /// The display name of the nutrient.
    let name: String

    /// The numeric value of the nutrient per serving.
    let value: Double

    /// Whether any ingredient nutrients are missing from the calculation.
    let hasMissingNutrients: Bool

    /// The unit of measurement (e.g., "kcals", "g", "mg").
    let unit: String

    /// The abbreviated display label (e.g., "Cal").
    let abbreviation: String

    enum CodingKeys: String, CodingKey {
        case name = "Name"
        case value = "Value"
        case hasMissingNutrients = "HasMissingNutrients"
        case unit = "Unit"
        case abbreviation = "Abbreviation"
    }
}

/// An academic calendar containing dates with notes (e.g., school closures).
struct AcademicCalendar: Codable {
    /// The unique identifier for this academic calendar.
    let academicCalendarId: String

    /// The list of notable days on this academic calendar.
    let days: [AcademicCalendarDay]

    enum CodingKeys: String, CodingKey {
        case academicCalendarId = "AcademicCalendarId"
        case days = "Days"
    }
}

/// A single day on an academic calendar with an associated note.
struct AcademicCalendarDay: Codable {
    /// The date string for this calendar day (e.g., "2/13/2026").
    let date: String

    /// The note describing this calendar day (e.g., "No School").
    let note: String

    enum CodingKeys: String, CodingKey {
        case date = "Date"
        case note = "Note"
    }
}
