namespace SchoolLunchMenu.Services;

/// <summary>
/// Fetches day label assignments (e.g., Red Day / White Day) from an external calendar source.
/// </summary>
public interface IDayLabelFetchService
{
    /// <summary>
    /// Scrapes the school calendar page and returns day label entries for the current month.
    /// </summary>
    Task<DayLabelFetchResult> FetchAsync();
}

/// <summary>
/// Result of a day label fetch operation.
/// </summary>
public class DayLabelFetchResult
{
    /// <summary>Date-label pairs found on the calendar page, sorted by date.</summary>
    public List<(DateOnly Date, string Label)> Entries { get; init; } = [];

    /// <summary>Distinct label names found, in order of first appearance.</summary>
    public List<string> DistinctLabels { get; init; } = [];
}
