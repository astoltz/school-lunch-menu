namespace SchoolLunchMenu.Services;

using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Scrapes day label entries (Red Day / White Day) from the ISD194 Finalsite CMS calendar page.
/// </summary>
public partial class DayLabelFetchService : IDayLabelFetchService
{
    private const string CalendarUrl = "https://cms.isd194.org/news-and-events/calendar";

    private readonly HttpClient _httpClient;
    private readonly ILogger<DayLabelFetchService> _logger;

    public DayLabelFetchService(HttpClient httpClient, ILogger<DayLabelFetchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DayLabelFetchResult> FetchAsync()
    {
        _logger.LogInformation("Fetching day labels from {Url}", CalendarUrl);

        var html = await _httpClient.GetStringAsync(CalendarUrl);

        var entries = ParseDayLabels(html);

        var distinctLabels = entries
            .Select(e => e.Label)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation("Found {Count} day label entries with {Distinct} distinct labels",
            entries.Count, distinctLabels.Count);

        return new DayLabelFetchResult
        {
            Entries = entries,
            DistinctLabels = distinctLabels
        };
    }

    /// <summary>
    /// Parses day label entries from the Finalsite CMS calendar HTML.
    /// Each calendar day is a <c>fsCalendarDate</c> div with data-day, data-year, data-month attributes.
    /// Events within are <c>fsCalendarEventTitle</c> anchors with a title attribute.
    /// Months are 0-indexed in the HTML (January = 0).
    /// </summary>
    private List<(DateOnly Date, string Label)> ParseDayLabels(string html)
    {
        var results = new List<(DateOnly Date, string Label)>();

        // Split HTML into date blocks by finding each fsCalendarDate section.
        // We track the current date context and look for event titles within each block.
        var dateMatches = DateBlockRegex().Matches(html);

        foreach (Match dateMatch in dateMatches)
        {
            if (!int.TryParse(dateMatch.Groups["day"].Value, out var day) ||
                !int.TryParse(dateMatch.Groups["year"].Value, out var year) ||
                !int.TryParse(dateMatch.Groups["month"].Value, out var month0))
                continue;

            // Finalsite months are 0-indexed
            var month = month0 + 1;

            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                continue;

            var date = new DateOnly(year, month, day);

            // Find the end of this day block (next fsCalendarDate or end of string)
            var blockStart = dateMatch.Index;
            var nextDateMatch = dateMatches.Cast<Match>()
                .FirstOrDefault(m => m.Index > blockStart);
            var blockEnd = nextDateMatch?.Index ?? html.Length;
            var block = html[blockStart..blockEnd];

            // Find event titles within this block
            var titleMatches = EventTitleRegex().Matches(block);
            foreach (Match titleMatch in titleMatches)
            {
                var title = titleMatch.Groups["title"].Value.Trim();

                // Match "Red Day", "White Day", or similar rotation labels
                if (DayLabelPattern().IsMatch(title))
                {
                    results.Add((date, title));
                }
            }
        }

        results.Sort((a, b) => a.Date.CompareTo(b.Date));
        return results;
    }

    // Matches fsCalendarDate div with data attributes (handles any attribute order)
    [GeneratedRegex(
        @"fsCalendarDate[^>]*?data-day=""(?<day>\d+)""[^>]*?data-year=""(?<year>\d+)""[^>]*?data-month=""(?<month>\d+)""",
        RegexOptions.Singleline)]
    private static partial Regex DateBlockRegex();

    // Matches fsCalendarEventTitle anchor with title attribute
    [GeneratedRegex(
        @"fsCalendarEventTitle[^>]*?title=""(?<title>[^""]+)""",
        RegexOptions.Singleline)]
    private static partial Regex EventTitleRegex();

    // Matches day label patterns like "Red Day", "White Day"
    [GeneratedRegex(@"^(?:Red|White|Blue|Gold|Green|Silver|Black|Orange|Purple|Day\s*[A-Z]|[A-Z])\s*Day$",
        RegexOptions.IgnoreCase)]
    private static partial Regex DayLabelPattern();
}
