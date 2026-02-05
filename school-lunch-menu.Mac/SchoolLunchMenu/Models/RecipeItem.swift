import Foundation

// MARK: - RecipeItem

/// Represents a recipe with its allergen safety and preference status.
struct RecipeItem {
    /// The display name of the recipe.
    let name: String

    /// True if the recipe contains any of the user's selected allergens.
    let containsAllergen: Bool

    /// True if the recipe is allergen-free but marked as not preferred by the user.
    let isNotPreferred: Bool

    /// True if the recipe is marked as a favorite by the user.
    let isFavorite: Bool

    init(name: String, containsAllergen: Bool, isNotPreferred: Bool, isFavorite: Bool) {
        self.name = name
        self.containsAllergen = containsAllergen
        self.isNotPreferred = isNotPreferred
        self.isFavorite = isFavorite
    }
}
