import Foundation
import CoreImage
import os

#if os(macOS)
import AppKit
#else
import UIKit
#endif

// MARK: - Supporting Types

// Note: HolidayOverride and DayLabel are defined in AppSettings.swift with Codable conformance.

/// Additional rendering options for the calendar HTML generator.
struct CalendarRenderOptions {
    /// Calendar layout mode: "List", "IconsLeft", or "IconsRight".
    var layoutMode: String = "List"

    /// Custom short labels for plan line names (key = original plan name, value = short label).
    var planLabelOverrides: [String: String] = [:]

    /// User-editable emoji icons per plan line name.
    var planIconOverrides: [String: String] = [:]

    /// User-defined display order for plan lines.
    var planDisplayOrder: [String] = []

    /// Whether to show plan lines that have no allergen-safe items (grayed-out with helper text).
    var showUnsafeLines: Bool = false

    /// Message shown next to grayed-out buttons for unsafe plan lines.
    var unsafeLineMessage: String = "No safe options"

    /// Theme home badge background color (set internally by the generator from the theme).
    var homeBadgeBg: String = "#dc3545"

    /// Custom holiday emoji/message overrides (key = keyword, value = override).
    var holidayOverrides: [String: HolidayOverride] = [:]

    /// Whether to cross out past days with a faded overlay and diagonal X.
    var crossOutPastDays: Bool = false

    /// Today's date for determining past days (set by the view model at generation time).
    var today: Date? = nil

    /// Rotating day label cycle for corner triangle display.
    var dayLabelCycle: [DayLabel] = []

    /// Anchor date for the day label cycle start.
    var dayLabelStartDate: Date? = nil

    /// Which corner the day label triangle appears in: "TopRight", "TopLeft", "BottomRight", "BottomLeft".
    var dayLabelCorner: String = "TopRight"

    /// Whether to append a shareable footer with QR code to the calendar.
    var showShareFooter: Bool = false

    /// The LINQ Connect public menu URL for this school, shown as a QR code in the share footer.
    var sourceUrl: String? = nil

    init() {}
}

// MARK: - CalendarHtmlGenerator

/// Generates a self-contained, printable HTML calendar with ADHD-friendly color coding.
class CalendarHtmlGenerator {
    private let logger = Logger(subsystem: "com.schoollunchmenu", category: "CalendarHtmlGenerator")

    private static let linePalette = [
        "#0d6efd", "#6f42c1", "#d63384", "#fd7e14", "#20c997",
        "#0dcaf0", "#6610f2", "#e83e8c", "#198754", "#dc3545"
    ]

    init() {}

    /// Generates a self-contained HTML document with an ADHD-friendly calendar layout.
    /// - Parameters:
    ///   - month: The processed month data.
    ///   - allergenNames: Display names of the selected allergens for the header.
    ///   - forcedHomeDays: Weekdays (1=Sunday, 2=Monday, etc.) that force a "From Home" badge regardless of safe options.
    ///   - theme: The visual theme to apply to the calendar.
    ///   - options: Optional rendering options for meal buttons and holiday overrides.
    /// - Returns: A complete HTML document string.
    func generate(
        month: ProcessedMonth,
        allergenNames: [String],
        forcedHomeDays: Set<Int>,
        theme: CalendarTheme,
        options: CalendarRenderOptions = CalendarRenderOptions()
    ) -> String {
        logger.info("Generating HTML calendar for \(month.displayName) with theme \(theme.name)")

        var options = options
        options.homeBadgeBg = theme.homeBadgeBg
        let sessionLabel = month.sessionName ?? "Lunch"
        let linePalette = buildLinePalette(month: month)

        var html = ""
        html += "<!DOCTYPE html>\n"
        html += "<html lang=\"en\">\n"
        html += "<head>\n"
        html += "<meta charset=\"UTF-8\">\n"
        html += "<title>\(htmlEncode(month.displayName)) \(htmlEncode(sessionLabel)) Calendar</title>\n"
        html += "<style>\n"
        html += buildCss(theme: theme, options: options)

        // Dynamic badge CSS per plan (badges + grid buttons)
        for (planName, (cssClass, color)) in linePalette {
            html += ".\(cssClass) { background: \(color); }\n"
            html += ".grid-btn.\(cssClass) { background: \(color); }\n"
        }

        html += "</style>\n"
        html += "</head>\n"
        html += "<body style=\"width:10.5in;\">\n"

        // Header
        let filterLabel: String
        switch allergenNames.count {
        case 0:
            filterLabel = "No Allergen Filter"
        case 1 where allergenNames[0].lowercased() == "milk":
            filterLabel = "Dairy-Free"
        default:
            filterLabel = allergenNames.joined(separator: ", ") + " Free"
        }
        html += "<h1>\(theme.emoji) \(htmlEncode(month.displayName)) &mdash; \(htmlEncode(filterLabel)) \(htmlEncode(sessionLabel)) Calendar \(theme.emoji)</h1>\n"

        if let buildingName = month.buildingName {
            html += "<h2>\(htmlEncode(buildingName))</h2>\n"
        }

        // Legend
        html += "<div class=\"legend\">\n"
        html += "<span class=\"legend-item\"><span class=\"swatch safe-swatch\"></span> Safe options available</span>\n"
        html += "<span class=\"legend-item\"><span class=\"favorite-star\">&#9733;</span> Favorite item</span>\n"
        html += "<span class=\"legend-item\"><span class=\"badge home\">\u{1F3E0} \(htmlEncode(sessionLabel)) from Home</span> No safe options / forced home day</span>\n"
        html += "<span class=\"legend-item\"><span class=\"swatch no-school-swatch\"></span> \u{1F3E0} No School</span>\n"

        // Plan line legend entries (in user-defined display order)
        let orderedPlanNames = getOrderedPlanNames(linePalette: linePalette, displayOrder: options.planDisplayOrder)
        for planName in orderedPlanNames {
            if let (cssClass, _) = linePalette[planName] {
                let legendLabel = options.planLabelOverrides[planName] ?? planName
                html += "<span class=\"legend-item\"><span class=\"badge \(cssClass)\">\(htmlEncode(legendLabel))</span></span>\n"
            }
        }

        html += "</div>\n"

        // Calendar table
        html += "<table>\n"
        html += "<thead><tr>\n"
        html += "<th>Monday</th><th>Tuesday</th><th>Wednesday</th><th>Thursday</th><th>Friday</th>\n"
        html += "</tr></thead>\n"
        html += "<tbody>\n"

        // Build day lookup
        var dayLookup: [DateComponents: ProcessedDay] = [:]
        let calendar = Calendar.current
        for day in month.days {
            let components = calendar.dateComponents([.year, .month, .day], from: day.date)
            dayLookup[components] = day
        }

        // Build day label lookup
        let dayLabelLookup = buildDayLabelLookup(month: month, options: options)

        // Walk through calendar weeks
        var firstOfMonthComponents = DateComponents()
        firstOfMonthComponents.year = month.year
        firstOfMonthComponents.month = month.month
        firstOfMonthComponents.day = 1
        guard let firstOfMonth = calendar.date(from: firstOfMonthComponents) else {
            logger.error("Failed to create first of month date")
            return html
        }

        guard let lastOfMonth = calendar.date(byAdding: DateComponents(month: 1, day: -1), to: firstOfMonth) else {
            logger.error("Failed to create last of month date")
            return html
        }

        // Find the Monday of the first week
        var current = firstOfMonth
        while calendar.component(.weekday, from: current) != 2 { // 2 = Monday
            current = calendar.date(byAdding: .day, value: -1, to: current)!
        }

        while current <= lastOfMonth {
            html += "<tr>\n"
            for dow in 0..<5 { // Mon-Fri
                guard let cellDate = calendar.date(byAdding: .day, value: dow, to: current) else { continue }
                let cellMonth = calendar.component(.month, from: cellDate)

                if cellMonth != month.month {
                    html += "<td class=\"empty\"></td>\n"
                    continue
                }

                let cellComponents = calendar.dateComponents([.year, .month, .day], from: cellDate)
                if let day = dayLookup[cellComponents] {
                    html += appendDayCell(
                        day: day,
                        sessionLabel: sessionLabel,
                        forcedHomeDays: forcedHomeDays,
                        linePalette: linePalette,
                        options: options,
                        dayLabelLookup: dayLabelLookup
                    )
                } else {
                    // Weekday with no data = no school
                    var noDataClasses = "no-school"
                    if options.crossOutPastDays, let today = options.today, cellDate < today {
                        noDataClasses += " past-day"
                    }
                    let dayNumber = calendar.component(.day, from: cellDate)
                    html += "<td class=\"\(noDataClasses)\"><div class=\"day-number\">\(dayNumber)</div></td>\n"
                }
            }
            html += "</tr>\n"
            current = calendar.date(byAdding: .day, value: 7, to: current)!
        }

        html += "</tbody>\n"
        html += "</table>\n"

        if options.showShareFooter {
            html += appendShareFooter(options: options)
        }

        html += "</body>\n"
        html += "</html>\n"

        logger.info("Generated HTML calendar: \(html.count) characters")
        return html
    }

    // MARK: - Private Helper Methods

    /// Builds a mapping from plan name to (CSS class, color) for per-plan badge rendering.
    private func buildLinePalette(month: ProcessedMonth) -> [String: (String, String)] {
        let planNames = month.days
            .flatMap { $0.lines }
            .map { $0.planName }
            .reduce(into: Set<String>()) { $0.insert($1) }
            .sorted()

        var palette: [String: (String, String)] = [:]
        for (index, planName) in planNames.enumerated() {
            let cssClass = "line-\(index)"
            let color = Self.linePalette[index % Self.linePalette.count]
            palette[planName] = (cssClass, color)
        }
        return palette
    }

    /// Appends CSS styles for the calendar layout and print support, using the given theme.
    private func buildCss(theme: CalendarTheme, options: CalendarRenderOptions) -> String {
        var css = """
            @page {
                size: landscape;
                margin: 0.25in;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body {
                font-family: 'Segoe UI', Arial, sans-serif;
                font-size: 11px;
                padding: 0.25in;
                print-color-adjust: exact;
                -webkit-print-color-adjust: exact;
                background: \(theme.bodyBg);
            }
            h1 {
                font-size: 18px;
                margin-bottom: 2px;
                text-align: center;
                color: \(theme.titleColor);
            }
            h2 {
                font-size: 13px;
                font-weight: normal;
                color: #666;
                margin-bottom: 6px;
                text-align: center;
            }
            .legend {
                display: flex;
                gap: 12px;
                justify-content: center;
                margin-bottom: 6px;
                flex-wrap: wrap;
                font-size: 10px;
            }
            .legend-item {
                display: flex;
                align-items: center;
                gap: 4px;
            }
            .swatch {
                display: inline-block;
                width: 14px;
                height: 14px;
                border-radius: 2px;
            }
            .safe-swatch { background: \(theme.safeColor); }
            .no-school-swatch { background: #e9ecef; }
            table {
                width: 100%;
                border-collapse: collapse;
                table-layout: fixed;
            }
            th {
                background: \(theme.headerBg);
                color: \(theme.headerFg);
                padding: 4px;
                text-align: center;
                font-size: 12px;
            }
            td {
                border: 1px solid \(theme.accentBorder);
                padding: 3px 4px;
                vertical-align: top;
                overflow: hidden;
            }
            td.empty {
                background: #f8f9fa;
            }
            td.no-school {
                background: #e9ecef;
                color: #6c757d;
                vertical-align: middle;
                text-align: center;
            }
            .day-number {
                font-size: 14px;
                font-weight: bold;
                margin-bottom: 2px;
            }
            .badge {
                display: inline-block;
                padding: 1px 5px;
                border-radius: 3px;
                color: white;
                font-size: 9px;
                font-weight: bold;
                margin-bottom: 1px;
            }
            .badge.home { background: \(theme.homeBadgeBg); }
            .plan-section {
                margin-bottom: 2px;
            }
            .safe-item {
                color: \(theme.safeColor);
                font-weight: bold;
                font-size: 10px;
            }
            .favorite-item {
                color: \(theme.safeColor);
                font-weight: bold;
                font-size: 10px;
            }
            .favorite-star {
                color: \(theme.favoriteStar);
                font-size: 11px;
            }
            .favorite-day {
                border: 2px solid \(theme.favoriteBorder) !important;
                background: \(theme.favoriteBg);
            }
            .not-preferred-item {
                color: #6c757d;
                font-style: italic;
                font-size: 10px;
            }
            .no-school-note {
                font-style: italic;
                font-size: 11px;
                margin-top: 4px;
                text-align: center;
            }
            .no-school-emoji {
                font-size: 20px;
                text-align: center;
            }
            .special-note {
                font-size: 9px;
                color: #856404;
                font-style: italic;
            }
            .day-grid {
                display: grid;
                gap: 2px 3px;
            }
            .day-grid.buttons-left {
                grid-template-columns: 38px 1fr;
            }
            .day-grid.buttons-right {
                grid-template-columns: 1fr 38px;
            }
            .grid-row {
                display: contents;
            }
            .grid-btn {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                padding: 1px 2px;
                border-radius: 4px;
                color: white;
                font-weight: 700;
                text-align: center;
                line-height: 1;
                min-height: 22px;
                box-shadow: 0 1px 0 rgba(0,0,0,0.2), inset 0 1px 0 rgba(255,255,255,0.2);
                cursor: default;
                overflow: hidden;
            }
            .grid-btn .grid-icon {
                font-size: 12px;
                line-height: 1;
            }
            .grid-btn .grid-label {
                font-size: 6px;
                line-height: 1;
                white-space: nowrap;
                text-overflow: ellipsis;
                overflow: hidden;
                max-width: 100%;
            }
            .grid-btn.btn-off {
                background: #ccc !important;
                color: #f0f0f0;
                box-shadow: none;
                opacity: 0.5;
            }
            .grid-btn.btn-forced-home {
                opacity: 0.5;
                filter: grayscale(0.6);
                box-shadow: none;
            }
            .plan-section.forced-home .badge {
                opacity: 0.5;
                filter: grayscale(0.6);
            }
            .grid-btn.btn-home {
                background: \(theme.homeBadgeBg);
            }
            .grid-items {
                min-width: 0;
                padding: 1px 3px;
                border-radius: 3px;
            }
            td.past-day {
                position: relative;
                opacity: 0.45;
            }
            td.past-day::after {
                content: '\\2715';
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                font-size: 48px;
                font-weight: bold;
                color: rgba(0, 0, 0, 0.12);
                pointer-events: none;
            }
            .day-label {
                position: absolute;
                width: 0;
                height: 0;
                border-style: solid;
                border-color: transparent;
            }
            .day-label-text {
                position: absolute;
                font-size: 7px;
                font-weight: bold;
                color: white;
                transform-origin: center;
                pointer-events: none;
                white-space: nowrap;
            }
            .share-footer {
                margin-top: 12px;
                padding: 8px 16px;
                text-align: center;
                font-size: 11px;
                color: #6c757d;
                border-top: 1px solid #dee2e6;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 24px;
            }
            .share-footer .share-group {
                display: flex;
                align-items: center;
                gap: 8px;
            }
            .share-footer img { width: 64px; height: 64px; }
            .share-footer .share-text { font-size: 12px; color: #495057; }
            @media print {
                body { padding: 0; margin: 0; zoom: 1 !important; }
                table { page-break-inside: avoid; }
            }

        """

        // Corner-specific day label positioning
        let triangleBorder: String
        let trianglePos: String
        let textPos: String
        let textRotation: String
        let borderColorProp: String

        switch options.dayLabelCorner {
        case "TopLeft":
            triangleBorder = "0 0 32px 32px"
            trianglePos = "top:0;left:0;"
            textPos = "top:2px;left:1px;"
            textRotation = "rotate(-45deg)"
            borderColorProp = "border-left-color"
        case "BottomRight":
            triangleBorder = "32px 0 0 32px"
            trianglePos = "bottom:0;right:0;"
            textPos = "bottom:2px;right:1px;"
            textRotation = "rotate(-45deg)"
            borderColorProp = "border-left-color"
        case "BottomLeft":
            triangleBorder = "32px 32px 0 0"
            trianglePos = "bottom:0;left:0;"
            textPos = "bottom:2px;left:1px;"
            textRotation = "rotate(45deg)"
            borderColorProp = "border-right-color"
        default: // TopRight
            triangleBorder = "0 32px 32px 0"
            trianglePos = "top:0;right:0;"
            textPos = "top:2px;right:1px;"
            textRotation = "rotate(45deg)"
            borderColorProp = "border-right-color"
        }

        css += ".day-label { border-width:\(triangleBorder);\(trianglePos) }\n"
        css += ".day-label-text { \(textPos)transform:\(textRotation); }\n"

        if let cellPattern = theme.cellPattern {
            css += "td { background-image: \(cellPattern); background-size: 20px 20px; }\n"
            css += "td.empty, td.no-school { background-image: none; }\n"
        }

        return css
    }

    /// Returns plan names ordered by user-defined display order, then alphabetically for remaining.
    private func getOrderedPlanNames(linePalette: [String: (String, String)], displayOrder: [String]) -> [String] {
        var ordered: [String] = []
        var remaining = Set(linePalette.keys)

        for name in displayOrder {
            if remaining.remove(name) != nil {
                ordered.append(name)
            }
        }

        ordered.append(contentsOf: remaining.sorted())
        return ordered
    }

    /// Builds a lookup from date to day label for the rotating day label cycle.
    /// School days (has menu, not no-school) cycle through the labels in order.
    private func buildDayLabelLookup(month: ProcessedMonth, options: CalendarRenderOptions) -> [DateComponents: DayLabel] {
        var lookup: [DateComponents: DayLabel] = [:]
        if options.dayLabelCycle.isEmpty {
            return lookup
        }

        let calendar = Calendar.current

        // Collect school days in date order (days that have a menu and are not no-school)
        let schoolDays = month.days
            .filter { $0.hasMenu && !$0.isNoSchool }
            .sorted { $0.date < $1.date }
            .map { calendar.dateComponents([.year, .month, .day], from: $0.date) }

        if schoolDays.isEmpty {
            return lookup
        }

        // Determine anchor date
        let anchorComponents: DateComponents
        if let anchorDate = options.dayLabelStartDate {
            anchorComponents = calendar.dateComponents([.year, .month, .day], from: anchorDate)
        } else {
            anchorComponents = schoolDays[0]
        }

        // Find anchor index in school days
        let anchorIndex = schoolDays.firstIndex(of: anchorComponents)

        // Calculate anchor offset
        let anchorOffset: Int
        if let index = anchorIndex {
            anchorOffset = index
        } else {
            // Anchor is outside this month's school days; use first day as index 0
            anchorOffset = 0
        }

        let cycleLen = options.dayLabelCycle.count
        for (i, schoolDay) in schoolDays.enumerated() {
            let cycleIndex = ((i - anchorOffset) % cycleLen + cycleLen) % cycleLen
            lookup[schoolDay] = options.dayLabelCycle[cycleIndex]
        }

        return lookup
    }

    /// Appends a single day cell to the HTML output, rendering per-plan sections with badges.
    /// Supports List, IconsLeft, and IconsRight layout modes.
    private func appendDayCell(
        day: ProcessedDay,
        sessionLabel: String,
        forcedHomeDays: Set<Int>,
        linePalette: [String: (String, String)],
        options: CalendarRenderOptions,
        dayLabelLookup: [DateComponents: DayLabel]
    ) -> String {
        let calendar = Calendar.current
        let dayComponents = calendar.dateComponents([.year, .month, .day], from: day.date)
        let dayNumber = calendar.component(.day, from: day.date)

        let isPast = options.crossOutPastDays && options.today != nil && day.date < options.today!
        let dayLabel = dayLabelLookup[dayComponents]
        let hasLabel = dayLabel != nil

        if day.isNoSchool {
            let (emoji, message) = getNoSchoolDisplay(note: day.academicNote!, overrides: options.holidayOverrides)
            var noSchoolClasses = ["no-school"]
            if isPast { noSchoolClasses.append("past-day") }
            var html = "<td class=\"\(noSchoolClasses.joined(separator: " "))\">\n"
            html += "<div class=\"day-number\">\(dayNumber)</div>\n"
            html += "<div class=\"no-school-emoji\">\(emoji)</div>\n"
            html += "<div class=\"no-school-note\">\(htmlEncode(cleanNoSchoolNote(message)))</div>\n"
            html += "</td>\n"
            return html
        }

        if !day.hasMenu {
            var noMenuClasses = ["no-school"]
            if isPast { noMenuClasses.append("past-day") }
            var html = "<td class=\"\(noMenuClasses.joined(separator: " "))\">\n"
            html += "<div class=\"day-number\">\(dayNumber)</div>\n"
            if let note = day.academicNote {
                let (emoji, message) = getNoSchoolDisplay(note: note, overrides: options.holidayOverrides)
                html += "<div class=\"no-school-emoji\">\(emoji)</div>\n"
                html += "<div class=\"no-school-note\">\(htmlEncode(cleanNoSchoolNote(message)))</div>\n"
            }
            html += "</td>\n"
            return html
        }

        let dayHasFavorite = day.lines
            .flatMap { $0.entrees }
            .contains { $0.isFavorite && !$0.containsAllergen }

        let dayOfWeek = calendar.component(.weekday, from: day.date)
        let isForcedHomeDay = forcedHomeDays.contains(dayOfWeek)
        let isHomeDay = !day.anyLineSafe || isForcedHomeDay

        var classes: [String] = []
        if dayHasFavorite { classes.append("favorite-day") }
        if isPast { classes.append("past-day") }
        let needsRelative = hasLabel || isPast
        let classAttr = classes.isEmpty ? "" : " class=\"\(classes.joined(separator: " "))\""
        let styleAttr = needsRelative ? " style=\"position:relative;\"" : ""

        var html = "<td\(classAttr)\(styleAttr)>\n"

        // Day label corner triangle
        if let label = dayLabel {
            let borderProp: String
            switch options.dayLabelCorner {
            case "TopLeft", "BottomRight":
                borderProp = "border-left-color"
            default:
                borderProp = "border-right-color"
            }
            html += "<div class=\"day-label\" style=\"\(borderProp): \(label.color);\"></div>\n"
            html += "<div class=\"day-label-text\">\(htmlEncode(label.label))</div>\n"
        }

        html += "<div class=\"day-number\">\(dayNumber)</div>\n"

        if day.hasSpecialNote {
            html += "<div class=\"special-note\">\(htmlEncode(day.academicNote!))</div>\n"
        }

        let isGrid = options.layoutMode == "IconsLeft" || options.layoutMode == "IconsRight"

        if isGrid {
            html += appendGridLayout(
                day: day,
                sessionLabel: sessionLabel,
                isHomeDay: isHomeDay,
                isForcedHomeDay: isForcedHomeDay,
                linePalette: linePalette,
                options: options
            )
        } else {
            html += appendListLayout(
                day: day,
                sessionLabel: sessionLabel,
                isHomeDay: isHomeDay,
                isForcedHomeDay: isForcedHomeDay,
                linePalette: linePalette,
                options: options
            )
        }

        html += "</td>\n"
        return html
    }

    /// Renders List mode: stacked plan sections with badge + items, no grid.
    private func appendListLayout(
        day: ProcessedDay,
        sessionLabel: String,
        isHomeDay: Bool,
        isForcedHomeDay: Bool,
        linePalette: [String: (String, String)],
        options: CalendarRenderOptions
    ) -> String {
        var html = ""
        let orderedPlanNames = getOrderedPlanNames(linePalette: linePalette, displayOrder: options.planDisplayOrder)

        for planName in orderedPlanNames {
            guard let line = day.lines.first(where: { $0.planName == planName }),
                  !line.entrees.isEmpty else {
                continue
            }

            let hasVisibleItems = line.entrees.contains { !$0.containsAllergen }
            if !hasVisibleItems {
                continue
            }

            let sectionClass = isForcedHomeDay && hasVisibleItems ? "plan-section forced-home" : "plan-section"
            html += "<div class=\"\(sectionClass)\">\n"

            if let (cssClass, _) = linePalette[line.planName] {
                let badgeLabel = options.planLabelOverrides[line.planName] ?? line.planName
                html += "<div><span class=\"badge \(cssClass)\">\(htmlEncode(badgeLabel))</span></div>\n"
            }

            html += appendEntreeItems(line: line)
            html += "</div>\n"
        }

        if isHomeDay {
            html += "<div><span class=\"badge home\">\u{1F3E0} \(htmlEncode(sessionLabel)) from Home</span></div>\n"
        }

        return html
    }

    /// Renders IconsLeft/IconsRight mode: CSS grid with button + items per plan row.
    private func appendGridLayout(
        day: ProcessedDay,
        sessionLabel: String,
        isHomeDay: Bool,
        isForcedHomeDay: Bool,
        linePalette: [String: (String, String)],
        options: CalendarRenderOptions
    ) -> String {
        let buttonsLeft = options.layoutMode == "IconsLeft"
        let gridClass = buttonsLeft ? "buttons-left" : "buttons-right"
        var html = "<div class=\"day-grid \(gridClass)\">\n"

        let orderedPlanNames = getOrderedPlanNames(linePalette: linePalette, displayOrder: options.planDisplayOrder)

        for planName in orderedPlanNames {
            let line = day.lines.first { $0.planName == planName }
            let lineAllergenSafe = line != nil && line!.entrees.contains { !$0.containsAllergen }

            if !lineAllergenSafe && !options.showUnsafeLines {
                continue
            }

            let (cssClass, color) = linePalette[planName]!
            let label = options.planLabelOverrides[planName] ?? planName
            let defaultIcon = lineAllergenSafe ? "\u{2705}" : "\u{26D4}" // ✅ : ⛔
            let icon: String
            if let customIcon = options.planIconOverrides[planName], !customIcon.isEmpty {
                icon = customIcon
            } else {
                icon = defaultIcon
            }
            let stateClass: String
            if !lineAllergenSafe {
                stateClass = "btn-off"
            } else if isForcedHomeDay {
                stateClass = "btn-forced-home"
            } else {
                stateClass = ""
            }
            let rowTint = "\(color)1a" // ~10% opacity tint of the plan color

            html += "<div class=\"grid-row\">\n"

            // Button cell
            let buttonHtml = "<div class=\"grid-btn \(cssClass) \(stateClass)\"><span class=\"grid-icon\">\(icon)</span><span class=\"grid-label\">\(htmlEncode(label))</span></div>\n"

            // Items cell - tinted background connects it visually to the button
            var itemsHtml = "<div class=\"grid-items\" style=\"background:\(rowTint)\">"
            if lineAllergenSafe {
                itemsHtml += appendEntreeItems(line: line!)
            } else {
                itemsHtml += "<div class=\"not-preferred-item\">\(htmlEncode(options.unsafeLineMessage))</div>"
            }
            itemsHtml += "</div>\n"

            if buttonsLeft {
                html += buttonHtml
                html += itemsHtml
            } else {
                html += itemsHtml
                html += buttonHtml
            }

            html += "</div>\n"
        }

        // Home row - styled like a regular meal item
        let homeButtonHtml = "<div class=\"grid-btn btn-home\"><span class=\"grid-icon\">\u{1F3E0}</span><span class=\"grid-label\">Home</span></div>\n"
        let homeTint = "\(options.homeBadgeBg)1a"
        let homeItemText = "Home \(htmlEncode(sessionLabel))"
        let homeItemsHtml = "<div class=\"grid-items\" style=\"background:\(homeTint)\"><div class=\"safe-item\">\(homeItemText)</div></div>\n"

        html += "<div class=\"grid-row\">\n"
        if buttonsLeft {
            html += homeButtonHtml
            html += homeItemsHtml
        } else {
            html += homeItemsHtml
            html += homeButtonHtml
        }
        html += "</div>\n"

        html += "</div>\n" // close .day-grid
        return html
    }

    /// Appends entree items for a single plan line.
    private func appendEntreeItems(line: ProcessedLine) -> String {
        var html = ""
        for item in line.entrees {
            if item.containsAllergen {
                continue
            }

            if item.isNotPreferred {
                html += "<div class=\"not-preferred-item\">\(htmlEncode(item.name))</div>\n"
            } else if item.isFavorite {
                html += "<div class=\"favorite-item\"><span class=\"favorite-star\">&#9733;</span> \(htmlEncode(item.name))</div>\n"
            } else {
                html += "<div class=\"safe-item\">\(htmlEncode(item.name))</div>\n"
            }
        }
        return html
    }

    /// Returns a holiday-specific emoji and display message for a no-school day.
    /// Checks user-configured overrides first, then falls back to hardcoded keyword matching.
    func getNoSchoolDisplay(note: String, overrides: [String: HolidayOverride]) -> (String, String) {
        let lower = note.lowercased()

        // Check user-configured overrides first
        for (keyword, holidayOverride) in overrides {
            if lower.contains(keyword.lowercased()) {
                let message = holidayOverride.customMessage ?? note
                return (holidayOverride.emoji, message)
            }
        }

        // Fall back to hardcoded detection
        return (getNoSchoolEmoji(note: note), note)
    }

    /// Returns a holiday-specific emoji based on keyword matching on the academic note text.
    func getNoSchoolEmoji(note: String) -> String {
        let lower = note.lowercased()

        if lower.contains("winter break") || lower.contains("christmas") {
            return "\u{2744}\u{FE0F}" // ❄️
        }
        if lower.contains("thanksgiving") {
            return "\u{1F983}" // 🦃
        }
        if lower.contains("president") {
            return "\u{1F1FA}\u{1F1F8}" // 🇺🇸
        }
        if lower.contains("mlk") || lower.contains("martin luther king") {
            return "\u{270A}" // ✊
        }
        if lower.contains("memorial") {
            return "\u{1F1FA}\u{1F1F8}" // 🇺🇸
        }
        if lower.contains("labor") {
            return "\u{1F1FA}\u{1F1F8}" // 🇺🇸
        }
        if lower.contains("spring break") {
            return "\u{1F338}" // 🌸
        }
        if lower.contains("teacher") {
            return "\u{1F4DA}" // 📚
        }

        return "\u{1F3E0}" // 🏠
    }

    /// Strips redundant "No School" suffixes from academic note text,
    /// since the cell is already styled as a no-school day.
    private func cleanNoSchoolNote(_ note: String) -> String {
        // Strip redundant "- No School" / "- no school" suffix
        if let range = note.range(of: " - No School", options: .caseInsensitive) {
            let prefix = note[..<range.lowerBound]
            return String(prefix).trimmingCharacters(in: .whitespaces)
        }
        // Also handle without the dash
        if let range = note.range(of: " No School", options: .caseInsensitive) {
            let prefix = String(note[..<range.lowerBound]).trimmingCharacters(in: .whitespaces)
            if prefix.hasSuffix("-") {
                return String(prefix.dropLast()).trimmingCharacters(in: .whitespaces)
            }
        }
        return note
    }

    /// Appends a shareable footer with QR codes linking to the project GitHub page
    /// and optionally the LINQ Connect source menu.
    private func appendShareFooter(options: CalendarRenderOptions) -> String {
        var html = "<div class=\"share-footer\">\n"

        // Source menu QR code (if a source URL is available)
        if let sourceUrl = options.sourceUrl, !sourceUrl.isEmpty {
            let sourceBase64 = generateQrBase64(url: sourceUrl)
            html += "<div class=\"share-group\">\n"
            html += "<img src=\"data:image/png;base64,\(sourceBase64)\" alt=\"Menu source QR code\" />\n"
            html += "<span class=\"share-text\">Scan to view the full school menu online</span>\n"
            html += "</div>\n"
        }

        // Project GitHub QR code
        let githubBase64 = generateQrBase64(url: "https://github.com/astoltz/school-lunch-menu")
        html += "<div class=\"share-group\">\n"
        html += "<img src=\"data:image/png;base64,\(githubBase64)\" alt=\"Project QR code\" />\n"
        html += "<span class=\"share-text\">Want your own allergen-friendly lunch calendar? Scan to learn more!</span>\n"
        html += "</div>\n"

        html += "</div>\n"
        return html
    }

    /// Generates a QR code as a base64-encoded PNG data URI string.
    private func generateQrBase64(url: String) -> String {
        guard let data = url.data(using: .utf8) else {
            logger.warning("Failed to encode URL for QR code: \(url)")
            return ""
        }

        guard let filter = CIFilter(name: "CIQRCodeGenerator") else {
            logger.warning("CIQRCodeGenerator filter not available")
            return ""
        }

        filter.setValue(data, forKey: "inputMessage")
        filter.setValue("M", forKey: "inputCorrectionLevel") // Medium error correction

        guard let outputImage = filter.outputImage else {
            logger.warning("Failed to generate QR code image")
            return ""
        }

        // Scale up the QR code for better quality
        let scale = CGAffineTransform(scaleX: 4, y: 4)
        let scaledImage = outputImage.transformed(by: scale)

        let context = CIContext()
        guard let cgImage = context.createCGImage(scaledImage, from: scaledImage.extent) else {
            logger.warning("Failed to create CGImage from QR code")
            return ""
        }

        // Convert to PNG data
        #if os(macOS)
        let bitmapRep = NSBitmapImageRep(cgImage: cgImage)
        guard let pngData = bitmapRep.representation(using: .png, properties: [:]) else {
            logger.warning("Failed to create PNG representation")
            return ""
        }
        #else
        // For iOS, use UIImage
        let uiImage = UIImage(cgImage: cgImage)
        guard let pngData = uiImage.pngData() else {
            logger.warning("Failed to create PNG data")
            return ""
        }
        #endif

        return pngData.base64EncodedString()
    }

    /// HTML-encodes a string for safe embedding.
    private func htmlEncode(_ text: String) -> String {
        var encoded = text
        encoded = encoded.replacingOccurrences(of: "&", with: "&amp;")
        encoded = encoded.replacingOccurrences(of: "<", with: "&lt;")
        encoded = encoded.replacingOccurrences(of: ">", with: "&gt;")
        encoded = encoded.replacingOccurrences(of: "\"", with: "&quot;")
        encoded = encoded.replacingOccurrences(of: "'", with: "&#39;")
        return encoded
    }
}
