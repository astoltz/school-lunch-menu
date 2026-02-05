import Foundation

// MARK: - ProcessedLine

/// A single menu plan line analyzed for allergen safety.
struct ProcessedLine {
    /// The display name of the menu plan (e.g., "Lunch - MS", "Big Cat Cafe - MS").
    let planName: String

    /// Whether this line has at least one allergen-safe, preferred entree.
    let isSafe: Bool

    /// Entrees available on this line with allergen and preference flags.
    let entrees: [RecipeItem]
}

// MARK: - ProcessedDay

/// Analyzed school day with allergen classification for each menu plan line.
struct ProcessedDay {
    /// The date of this school day.
    let date: Date

    /// All menu plan lines for this day.
    let lines: [ProcessedLine]

    /// Note from the academic calendar (e.g., "No School", "President's Day").
    let academicNote: String?

    /// True if this is a no-school day.
    var isNoSchool: Bool {
        guard let note = academicNote else { return false }
        return note.localizedCaseInsensitiveContains("No School")
    }

    /// True if this is a day with a special academic note that is NOT a no-school day.
    var hasSpecialNote: Bool {
        academicNote != nil && !isNoSchool
    }

    /// True if any line has at least one safe entree.
    var anyLineSafe: Bool {
        lines.contains { $0.isSafe }
    }

    /// True if any line has entrees.
    var hasMenu: Bool {
        lines.contains { !$0.entrees.isEmpty }
    }

    init(date: Date, lines: [ProcessedLine], academicNote: String? = nil) {
        self.date = date
        self.lines = lines
        self.academicNote = academicNote
    }
}
