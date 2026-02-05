import Foundation

/// Response from the LINQ Connect family menu identifier endpoint,
/// containing district info, buildings, and the menu identifier code.
struct FamilyMenuIdentifierResponse: Codable {
    /// The unique identifier for the school district.
    let districtId: String

    /// The display name of the school district.
    let districtName: String

    /// The list of buildings within the district.
    let buildings: [Building]

    /// A notification message displayed alongside the menu.
    let menuNotification: String?

    /// The short alphanumeric identifier code for the family menu.
    let identifier: String

    enum CodingKeys: String, CodingKey {
        case districtId = "DistrictId"
        case districtName = "DistrictName"
        case buildings = "Buildings"
        case menuNotification = "MenuNotification"
        case identifier = "Identifier"
    }

    init(districtId: String, districtName: String, buildings: [Building], menuNotification: String?, identifier: String) {
        self.districtId = districtId
        self.districtName = districtName
        self.buildings = buildings
        self.menuNotification = menuNotification
        self.identifier = identifier
    }
}

/// A school building within a district.
struct Building: Codable {
    /// The unique identifier for the building.
    let buildingId: String

    /// The display name of the building.
    let name: String

    enum CodingKeys: String, CodingKey {
        case buildingId = "BuildingId"
        case name = "Name"
    }
}
