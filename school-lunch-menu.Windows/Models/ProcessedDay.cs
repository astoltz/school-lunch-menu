namespace SchoolLunchMenu.Models;

/// <summary>
/// A single menu plan line analyzed for allergen safety.
/// </summary>
public record ProcessedLine
{
    /// <summary>The display name of the menu plan (e.g., "Lunch - MS", "Big Cat Cafe - MS").</summary>
    public required string PlanName { get; init; }

    /// <summary>Whether this line has at least one allergen-safe, preferred entree.</summary>
    public required bool IsSafe { get; init; }

    /// <summary>Entrees available on this line with allergen and preference flags.</summary>
    public required IReadOnlyList<RecipeItem> Entrees { get; init; }
}

/// <summary>
/// Analyzed school day with allergen classification for each menu plan line.
/// </summary>
public record ProcessedDay
{
    /// <summary>The date of this school day.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>All menu plan lines for this day.</summary>
    public required IReadOnlyList<ProcessedLine> Lines { get; init; }

    /// <summary>Note from the academic calendar (e.g., "No School", "President's Day").</summary>
    public string? AcademicNote { get; init; }

    /// <summary>True if this is a no-school day.</summary>
    public bool IsNoSchool => AcademicNote is not null && AcademicNote.Contains("No School", StringComparison.OrdinalIgnoreCase);

    /// <summary>True if this is a day with a special academic note that is NOT a no-school day.</summary>
    public bool HasSpecialNote => AcademicNote is not null && !IsNoSchool;

    /// <summary>True if any line has at least one safe entree.</summary>
    public bool AnyLineSafe => Lines.Any(l => l.IsSafe);

    /// <summary>True if any line has entrees.</summary>
    public bool HasMenu => Lines.Any(l => l.Entrees.Count > 0);
}
