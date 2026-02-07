import Foundation
import os

/// Scrapes day label entries (Red Day / White Day) from the ISD 194 Finalsite CMS calendar page.
struct DayLabelFetchService {
    private static let calendarUrl = "https://cms.isd194.org/news-and-events/calendar"
    private static let logger = Logger(subsystem: "com.schoollunchmenu", category: "DayLabelFetchService")

    /// Result of a day label fetch operation.
    struct FetchResult {
        /// Date-label pairs found on the calendar page, sorted by date.
        let entries: [(date: DateComponents, label: String)]
        /// Distinct label names found, in order of first appearance.
        let distinctLabels: [String]
    }

    /// Fetches day labels from the ISD 194 CMS calendar page.
    func fetch() async throws -> FetchResult {
        Self.logger.info("Fetching day labels from \(Self.calendarUrl)")

        guard let url = URL(string: Self.calendarUrl) else {
            throw URLError(.badURL)
        }

        let (data, _) = try await URLSession.shared.data(from: url)
        guard let html = String(data: data, encoding: .utf8) else {
            throw URLError(.cannotDecodeContentData)
        }

        let entries = parseDayLabels(html: html)

        // Collect distinct labels in order of first appearance
        var seen = Set<String>()
        var distinctLabels: [String] = []
        for entry in entries {
            let lower = entry.label.lowercased()
            if !seen.contains(lower) {
                seen.insert(lower)
                distinctLabels.append(entry.label)
            }
        }

        Self.logger.info("Found \(entries.count) day label entries with \(distinctLabels.count) distinct labels")

        return FetchResult(entries: entries, distinctLabels: distinctLabels)
    }

    /// Parses day label entries from Finalsite CMS calendar HTML.
    ///
    /// Each calendar day is a `fsCalendarDate` div with `data-day`, `data-year`, `data-month` attributes.
    /// Events within are `fsCalendarEventTitle` anchors with a `title` attribute.
    /// Months are 0-indexed in the HTML (January = 0).
    private func parseDayLabels(html: String) -> [(date: DateComponents, label: String)] {
        var results: [(date: DateComponents, label: String)] = []

        // Match fsCalendarDate divs with data attributes
        let datePattern = #"fsCalendarDate[^>]*?data-day="(\d+)"[^>]*?data-year="(\d+)"[^>]*?data-month="(\d+)""#
        guard let dateRegex = try? NSRegularExpression(pattern: datePattern, options: .dotMatchesLineSeparators) else {
            return results
        }

        let nsHtml = html as NSString
        let dateMatches = dateRegex.matches(in: html, range: NSRange(location: 0, length: nsHtml.length))

        // Match event titles
        let titlePattern = #"fsCalendarEventTitle[^>]*?title="([^"]+)""#
        guard let titleRegex = try? NSRegularExpression(pattern: titlePattern, options: .dotMatchesLineSeparators) else {
            return results
        }

        // Day label pattern: "Red Day", "White Day", etc.
        let labelPattern = #"^(?:Red|White|Blue|Gold|Green|Silver|Black|Orange|Purple|Day\s*[A-Z]|[A-Z])\s*Day$"#
        guard let labelRegex = try? NSRegularExpression(pattern: labelPattern, options: .caseInsensitive) else {
            return results
        }

        for (i, dateMatch) in dateMatches.enumerated() {
            guard dateMatch.numberOfRanges >= 4,
                  let dayRange = Range(dateMatch.range(at: 1), in: html),
                  let yearRange = Range(dateMatch.range(at: 2), in: html),
                  let month0Range = Range(dateMatch.range(at: 3), in: html),
                  let day = Int(html[dayRange]),
                  let year = Int(html[yearRange]),
                  let month0 = Int(html[month0Range]) else {
                continue
            }

            // Finalsite months are 0-indexed
            let month = month0 + 1

            guard month >= 1, month <= 12, day >= 1 else { continue }

            // Find the block of HTML for this date (until the next date or end of string)
            let blockStart = dateMatch.range.location
            let blockEnd: Int
            if i + 1 < dateMatches.count {
                blockEnd = dateMatches[i + 1].range.location
            } else {
                blockEnd = nsHtml.length
            }

            let blockRange = NSRange(location: blockStart, length: blockEnd - blockStart)
            let block = nsHtml.substring(with: blockRange)

            // Find event titles within this block
            let titleMatches = titleRegex.matches(in: block, range: NSRange(location: 0, length: (block as NSString).length))
            for titleMatch in titleMatches {
                guard titleMatch.numberOfRanges >= 2,
                      let titleRange = Range(titleMatch.range(at: 1), in: block) else {
                    continue
                }

                let title = String(block[titleRange]).trimmingCharacters(in: .whitespaces)

                // Check if this matches a day label pattern
                let titleNSRange = NSRange(location: 0, length: (title as NSString).length)
                if labelRegex.firstMatch(in: title, range: titleNSRange) != nil {
                    let components = DateComponents(year: year, month: month, day: day)
                    results.append((date: components, label: title))
                }
            }
        }

        // Sort by date
        results.sort { a, b in
            let aDate = (a.date.year ?? 0) * 10000 + (a.date.month ?? 0) * 100 + (a.date.day ?? 0)
            let bDate = (b.date.year ?? 0) * 10000 + (b.date.month ?? 0) * 100 + (b.date.day ?? 0)
            return aDate < bDate
        }

        return results
    }
}
