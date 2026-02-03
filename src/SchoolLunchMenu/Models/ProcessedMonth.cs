namespace SchoolLunchMenu.Models;

/// <summary>
/// Collection of processed days for a calendar month.
/// </summary>
public record ProcessedMonth
{
    /// <summary>The year of this month.</summary>
    public required int Year { get; init; }

    /// <summary>The month number (1-12).</summary>
    public required int Month { get; init; }

    /// <summary>All processed weekdays in this month.</summary>
    public required IReadOnlyList<ProcessedDay> Days { get; init; }

    /// <summary>The building name for the calendar subtitle.</summary>
    public string? BuildingName { get; init; }

    /// <summary>The serving session name (e.g., "Lunch", "Breakfast") for the calendar title.</summary>
    public string? SessionName { get; init; }

    /// <summary>Display name of the month (e.g., "February 2026").</summary>
    public string DisplayName => new DateOnly(Year, Month, 1).ToString("MMMM yyyy");
}
