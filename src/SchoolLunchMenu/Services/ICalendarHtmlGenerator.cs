namespace SchoolLunchMenu.Services;

using SchoolLunchMenu.Models;

/// <summary>
/// Generates a printable HTML calendar from processed menu data.
/// </summary>
public interface ICalendarHtmlGenerator
{
    /// <summary>
    /// Generates a self-contained HTML document with an ADHD-friendly calendar layout.
    /// </summary>
    /// <param name="month">The processed month data.</param>
    /// <param name="allergenNames">Display names of the selected allergens for the header.</param>
    /// <param name="forcedHomeDays">Weekdays that force a "From Home" badge regardless of safe options.</param>
    /// <param name="theme">The visual theme to apply to the calendar.</param>
    /// <param name="options">Optional rendering options for meal buttons and holiday overrides.</param>
    string Generate(ProcessedMonth month, IReadOnlyList<string> allergenNames, IReadOnlySet<DayOfWeek> forcedHomeDays, CalendarTheme theme, CalendarRenderOptions? options = null);
}

/// <summary>
/// Additional rendering options for the calendar HTML generator.
/// </summary>
public class CalendarRenderOptions
{
    /// <summary>Calendar layout mode: "List", "IconsLeft", or "IconsRight".</summary>
    public string LayoutMode { get; init; } = "List";

    /// <summary>Custom short labels for plan line names (key = original plan name, value = short label).</summary>
    public IReadOnlyDictionary<string, string> PlanLabelOverrides { get; init; } = new Dictionary<string, string>();

    /// <summary>User-editable emoji icons per plan line name.</summary>
    public IReadOnlyDictionary<string, string> PlanIconOverrides { get; init; } = new Dictionary<string, string>();

    /// <summary>User-defined display order for plan lines.</summary>
    public IReadOnlyList<string> PlanDisplayOrder { get; init; } = [];

    /// <summary>Whether to show plan lines that have no allergen-safe items (grayed-out with helper text).</summary>
    public bool ShowUnsafeLines { get; init; }

    /// <summary>Message shown next to grayed-out buttons for unsafe plan lines.</summary>
    public string UnsafeLineMessage { get; init; } = "No safe options";

    /// <summary>Theme home badge background color (set internally by the generator from the theme).</summary>
    public string HomeBadgeBg { get; set; } = "#dc3545";

    /// <summary>Custom holiday emoji/message overrides (key = keyword, value = override).</summary>
    public IReadOnlyDictionary<string, HolidayOverride> HolidayOverrides { get; init; } = new Dictionary<string, HolidayOverride>();

    /// <summary>Whether to cross out past days with a faded overlay and diagonal X.</summary>
    public bool CrossOutPastDays { get; init; }

    /// <summary>Today's date for determining past days (set by the view model at generation time).</summary>
    public DateOnly? Today { get; init; }

    /// <summary>Rotating day label cycle for corner triangle display.</summary>
    public IReadOnlyList<DayLabel> DayLabelCycle { get; init; } = [];

    /// <summary>Anchor date for the day label cycle start.</summary>
    public DateOnly? DayLabelStartDate { get; init; }

    /// <summary>Which corner the day label triangle appears in: "TopRight", "TopLeft", "BottomRight", "BottomLeft".</summary>
    public string DayLabelCorner { get; init; } = "TopRight";

    /// <summary>Whether to append a shareable footer with QR code to the calendar.</summary>
    public bool ShowShareFooter { get; init; }

    /// <summary>The LINQ Connect public menu URL for this school, shown as a QR code in the share footer.</summary>
    public string? SourceUrl { get; init; }
}
