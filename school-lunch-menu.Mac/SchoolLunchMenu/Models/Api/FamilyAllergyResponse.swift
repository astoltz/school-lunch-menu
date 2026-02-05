import Foundation

/// An allergen item returned by the LINQ Connect family allergy endpoint.
/// The response is a JSON array of these items.
struct AllergyItem: Codable {
    /// The unique identifier for the allergen.
    let allergyId: String

    /// The sort order used for display purposes.
    let sortOrder: Int

    /// The display name of the allergen.
    let name: String

    enum CodingKeys: String, CodingKey {
        case allergyId = "AllergyId"
        case sortOrder = "SortOrder"
        case name = "Name"
    }

    init(allergyId: String, sortOrder: Int, name: String) {
        self.allergyId = allergyId
        self.sortOrder = sortOrder
        self.name = name
    }
}
