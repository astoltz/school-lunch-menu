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
    string Generate(ProcessedMonth month, IReadOnlyList<string> allergenNames, IReadOnlySet<DayOfWeek> forcedHomeDays, CalendarTheme theme);
}
