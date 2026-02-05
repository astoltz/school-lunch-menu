import Foundation
import os

/// Analyzes LINQ Connect menu data to classify each day by allergen safety per menu plan line.
class MenuAnalyzer {
    private let logger = Logger(subsystem: "com.schoollunchmenu", category: "MenuAnalyzer")

    /// Date formatter for parsing/formatting date strings in M/d/yyyy format (e.g., "2/14/2026").
    private let dateFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateFormat = "M/d/yyyy"
        formatter.locale = Locale(identifier: "en_US_POSIX")
        return formatter
    }()

    /// Analyzes LINQ Connect menu data to classify each day by allergen safety per menu plan line.
    /// - Parameters:
    ///   - menuResponse: The full menu response from LINQ Connect API.
    ///   - selectedAllergenIds: Set of allergen IDs to check against recipes.
    ///   - notPreferredNames: Set of recipe names the user has marked as not preferred.
    ///   - favoriteNames: Set of recipe names the user has marked as favorites.
    ///   - year: The year to analyze.
    ///   - month: The month to analyze (1-12).
    ///   - sessionName: The serving session name to look for (e.g., "Lunch").
    ///   - buildingName: Optional building name for display purposes.
    /// - Returns: A ProcessedMonth containing all processed weekdays.
    func analyze(
        menuResponse: FamilyMenuResponse,
        selectedAllergenIds: Set<String>,
        notPreferredNames: Set<String>,
        favoriteNames: Set<String>,
        year: Int,
        month: Int,
        sessionName: String,
        buildingName: String? = nil
    ) -> ProcessedMonth {
        logger.info("Analyzing menu for \(month)/\(year) session=\(sessionName) with \(selectedAllergenIds.count) selected allergens")

        // Build academic calendar lookup: date string -> note
        let academicNotes = buildAcademicCalendarLookup(menuResponse)

        // Find the requested session (case-insensitive)
        guard let session = menuResponse.familyMenuSessions.first(where: {
            $0.servingSession.caseInsensitiveCompare(sessionName) == .orderedSame
        }) else {
            logger.warning("No \(sessionName) session found in menu data")
            return buildEmptyMonth(year: year, month: month, academicNotes: academicNotes, buildingName: buildingName)
        }

        // Index all plans by date
        var planDayIndexes: [(planName: String, dayIndex: [String: MenuDay])] = []
        for plan in session.menuPlans {
            let dayIndex = indexDaysByDate(plan)
            planDayIndexes.append((plan.menuPlanName, dayIndex))
            logger.info("Found plan: \(plan.menuPlanName) with \(dayIndex.count) days")
        }

        // Process each weekday in the month
        var processedDays: [ProcessedDay] = []
        let calendar = Calendar.current

        // Get first and last day of the month
        var components = DateComponents()
        components.year = year
        components.month = month
        components.day = 1

        guard let firstDay = calendar.date(from: components) else {
            logger.error("Failed to create first day of month for \(month)/\(year)")
            return ProcessedMonth(year: year, month: month, days: [], buildingName: buildingName, sessionName: sessionName)
        }

        guard let range = calendar.range(of: .day, in: .month, for: firstDay) else {
            logger.error("Failed to get day range for month \(month)/\(year)")
            return ProcessedMonth(year: year, month: month, days: [], buildingName: buildingName, sessionName: sessionName)
        }

        for day in range {
            components.day = day
            guard let date = calendar.date(from: components) else { continue }

            let weekday = calendar.component(.weekday, from: date)
            // Skip Saturday (7) and Sunday (1)
            if weekday == 1 || weekday == 7 {
                continue
            }

            let dateKey = dateFormatter.string(from: date)
            let academicNote = academicNotes[dateKey]

            var lines: [ProcessedLine] = []
            for (planName, dayIndex) in planDayIndexes {
                let entrees = extractEntrees(
                    dayIndex: dayIndex,
                    dateKey: dateKey,
                    allergenIds: selectedAllergenIds,
                    notPreferredNames: notPreferredNames,
                    favoriteNames: favoriteNames
                )
                let isSafe = entrees.contains { !$0.containsAllergen && !$0.isNotPreferred }

                lines.append(ProcessedLine(
                    planName: planName,
                    isSafe: isSafe,
                    entrees: entrees
                ))
            }

            let processedDay = ProcessedDay(
                date: date,
                lines: lines,
                academicNote: academicNote
            )

            let safeCount = lines.filter { $0.isSafe }.count
            logger.debug("Day \(dateKey): \(safeCount)/\(lines.count) lines safe, Note=\(academicNote ?? "nil")")

            processedDays.append(processedDay)
        }

        return ProcessedMonth(
            year: year,
            month: month,
            days: processedDays,
            buildingName: buildingName,
            sessionName: sessionName
        )
    }

    // MARK: - Private Methods

    /// Builds a lookup from date string to academic calendar note.
    private func buildAcademicCalendarLookup(_ response: FamilyMenuResponse) -> [String: String] {
        var lookup: [String: String] = [:]

        for calendar in response.academicCalendars {
            for day in calendar.days {
                if !day.date.isEmpty && !day.note.isEmpty {
                    lookup[day.date] = day.note
                }
            }
        }

        return lookup
    }

    /// Indexes menu plan days by their date string for O(1) lookup.
    private func indexDaysByDate(_ plan: MenuPlan) -> [String: MenuDay] {
        var index: [String: MenuDay] = [:]
        for day in plan.days {
            index[day.date] = day
        }
        return index
    }

    /// Extracts entree recipes from a menu day and flags allergen-containing, not-preferred, and favorite items.
    private func extractEntrees(
        dayIndex: [String: MenuDay],
        dateKey: String,
        allergenIds: Set<String>,
        notPreferredNames: Set<String>,
        favoriteNames: Set<String>
    ) -> [RecipeItem] {
        guard let menuDay = dayIndex[dateKey] else {
            return []
        }

        var entrees: [RecipeItem] = []

        for meal in menuDay.menuMeals {
            for category in meal.recipeCategories {
                guard category.isEntree else { continue }

                // Track whether the preceding parent entree contains an allergen,
                // so "with ..." companion items inherit the parent's allergen status.
                var parentContainsAllergen = false

                for recipe in category.recipes {
                    let isCompanion = recipe.recipeName.lowercased().hasPrefix("with ")
                    var containsAllergen = recipe.allergens.contains { allergenIds.contains($0) }

                    if isCompanion {
                        // Companion inherits allergen flag from parent
                        containsAllergen = containsAllergen || parentContainsAllergen
                    } else {
                        parentContainsAllergen = containsAllergen
                    }

                    logger.debug("Recipe '\(recipe.recipeName)' allergens=\(recipe.allergens.joined(separator: ",")), containsSelected=\(containsAllergen)")

                    let isNotPreferred = !containsAllergen && notPreferredNames.contains(recipe.recipeName)
                    let isFavorite = !containsAllergen && !isNotPreferred && favoriteNames.contains(recipe.recipeName)

                    entrees.append(RecipeItem(
                        name: recipe.recipeName,
                        containsAllergen: containsAllergen,
                        isNotPreferred: isNotPreferred,
                        isFavorite: isFavorite
                    ))
                }
            }
        }

        return entrees
    }

    /// Builds a month with no menu data, only academic calendar notes.
    private func buildEmptyMonth(
        year: Int,
        month: Int,
        academicNotes: [String: String],
        buildingName: String?
    ) -> ProcessedMonth {
        var days: [ProcessedDay] = []
        let calendar = Calendar.current

        var components = DateComponents()
        components.year = year
        components.month = month
        components.day = 1

        guard let firstDay = calendar.date(from: components),
              let range = calendar.range(of: .day, in: .month, for: firstDay) else {
            return ProcessedMonth(year: year, month: month, days: [], buildingName: buildingName, sessionName: nil)
        }

        for day in range {
            components.day = day
            guard let date = calendar.date(from: components) else { continue }

            let weekday = calendar.component(.weekday, from: date)
            // Skip Saturday (7) and Sunday (1)
            if weekday == 1 || weekday == 7 {
                continue
            }

            let dateKey = dateFormatter.string(from: date)
            let note = academicNotes[dateKey]

            days.append(ProcessedDay(
                date: date,
                lines: [],
                academicNote: note
            ))
        }

        return ProcessedMonth(year: year, month: month, days: days, buildingName: buildingName, sessionName: nil)
    }
}
