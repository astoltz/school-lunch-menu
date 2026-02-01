namespace SchoolLunchMenu.Models;

/// <summary>
/// Represents a recipe with its allergen safety and preference status.
/// </summary>
/// <param name="Name">The display name of the recipe.</param>
/// <param name="ContainsAllergen">True if the recipe contains any of the user's selected allergens.</param>
/// <param name="IsNotPreferred">True if the recipe is allergen-free but marked as not preferred by the user.</param>
/// <param name="IsFavorite">True if the recipe is marked as a favorite by the user.</param>
public record RecipeItem(string Name, bool ContainsAllergen, bool IsNotPreferred, bool IsFavorite);
