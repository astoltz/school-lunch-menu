import Foundation

// MARK: - MenuCache

/// Disk-persisted cache of the last fetched menu data and allergens,
/// allowing the app to preload data on startup without an API call.
struct MenuCache: Codable {
    /// When this cache was written (UTC).
    var savedAtUtc: Date

    /// The cached menu response.
    var menuResponse: FamilyMenuResponse?

    /// The cached allergen list.
    var allergies: [AllergyItem]?

    /// The cached identifier response (district + buildings).
    var identifierResponse: FamilyMenuIdentifierResponse?

    init(
        savedAtUtc: Date = Date(),
        menuResponse: FamilyMenuResponse? = nil,
        allergies: [AllergyItem]? = nil,
        identifierResponse: FamilyMenuIdentifierResponse? = nil
    ) {
        self.savedAtUtc = savedAtUtc
        self.menuResponse = menuResponse
        self.allergies = allergies
        self.identifierResponse = identifierResponse
    }
}
