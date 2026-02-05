import Foundation

/// A meal item returned by the LINQ Connect family menu meals endpoint.
/// The response is a JSON array of these items.
struct MealItem: Codable {
    /// The unique identifier for the meal.
    let mealId: String

    /// The display name of the meal (e.g., "Monday 1").
    let name: String

    /// The sort order used for display purposes.
    let sortOrder: Int

    enum CodingKeys: String, CodingKey {
        case mealId = "MealId"
        case name = "Name"
        case sortOrder = "SortOrder"
    }
}
