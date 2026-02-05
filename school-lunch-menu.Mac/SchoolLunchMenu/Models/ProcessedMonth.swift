import Foundation

// MARK: - ProcessedMonth

/// Collection of processed days for a calendar month.
struct ProcessedMonth {
    /// The year of this month.
    let year: Int

    /// The month number (1-12).
    let month: Int

    /// All processed weekdays in this month.
    let days: [ProcessedDay]

    /// The building name for the calendar subtitle.
    let buildingName: String?

    /// The serving session name (e.g., "Lunch", "Breakfast") for the calendar title.
    let sessionName: String?

    /// Display name of the month (e.g., "February 2026").
    var displayName: String {
        var components = DateComponents()
        components.year = year
        components.month = month
        components.day = 1

        guard let date = Calendar.current.date(from: components) else {
            return "\(month)/\(year)"
        }

        let formatter = DateFormatter()
        formatter.dateFormat = "MMMM yyyy"
        return formatter.string(from: date)
    }

    init(year: Int, month: Int, days: [ProcessedDay], buildingName: String? = nil, sessionName: String? = nil) {
        self.year = year
        self.month = month
        self.days = days
        self.buildingName = buildingName
        self.sessionName = sessionName
    }
}
