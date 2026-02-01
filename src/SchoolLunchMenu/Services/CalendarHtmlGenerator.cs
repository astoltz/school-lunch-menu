namespace SchoolLunchMenu.Services;

using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Models;

/// <summary>
/// Generates a self-contained, printable HTML calendar with ADHD-friendly color coding.
/// </summary>
public class CalendarHtmlGenerator : ICalendarHtmlGenerator
{
    private readonly ILogger<CalendarHtmlGenerator> _logger;

    private static readonly string[] LinePalette =
    [
        "#0d6efd", "#6f42c1", "#d63384", "#fd7e14", "#20c997",
        "#0dcaf0", "#6610f2", "#e83e8c", "#198754", "#dc3545"
    ];

    /// <summary>
    /// Initializes a new instance of <see cref="CalendarHtmlGenerator"/>.
    /// </summary>
    public CalendarHtmlGenerator(ILogger<CalendarHtmlGenerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Generate(ProcessedMonth month, IReadOnlyList<string> allergenNames, IReadOnlySet<DayOfWeek> forcedHomeDays, CalendarTheme theme)
    {
        _logger.LogInformation("Generating HTML calendar for {Month} with theme {Theme}", month.DisplayName, theme.Name);

        var sessionLabel = month.SessionName ?? "Lunch";
        var linePalette = BuildLinePalette(month);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine($"<title>{Encode(month.DisplayName)} {Encode(sessionLabel)} Calendar</title>");
        sb.AppendLine("<style>");
        AppendCss(sb, theme);

        // Dynamic badge CSS per plan
        foreach (var (planName, (cssClass, color)) in linePalette)
        {
            sb.AppendLine($".{cssClass} {{ background: {color}; }}");
        }

        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        var filterLabel = allergenNames.Count switch
        {
            0 => "No Allergen Filter",
            1 when allergenNames[0].Equals("Milk", StringComparison.OrdinalIgnoreCase) => "Dairy-Free",
            _ => string.Join(", ", allergenNames) + " Free"
        };
        sb.AppendLine($"<h1>{theme.Emoji} {Encode(month.DisplayName)} &mdash; {Encode(filterLabel)} {Encode(sessionLabel)} Calendar {theme.Emoji}</h1>");

        if (month.BuildingName is not null)
            sb.AppendLine($"<h2>{Encode(month.BuildingName)}</h2>");

        // Legend
        sb.AppendLine("<div class=\"legend\">");
        sb.AppendLine("<span class=\"legend-item\"><span class=\"swatch safe-swatch\"></span> Safe options available</span>");
        sb.AppendLine("<span class=\"legend-item\"><span class=\"favorite-star\">&#9733;</span> Favorite item</span>");
        sb.AppendLine($"<span class=\"legend-item\"><span class=\"badge home\">\U0001F3E0 {Encode(sessionLabel)} from Home</span> No safe options</span>");
        sb.AppendLine("<span class=\"legend-item\"><span class=\"swatch no-school-swatch\"></span> \U0001F3E0 No School</span>");

        // Plan line legend entries
        foreach (var (planName, (cssClass, _)) in linePalette)
        {
            sb.AppendLine($"<span class=\"legend-item\"><span class=\"badge {cssClass}\">{Encode(planName)}</span></span>");
        }

        sb.AppendLine("</div>");

        // Calendar table
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr>");
        sb.AppendLine("<th>Monday</th><th>Tuesday</th><th>Wednesday</th><th>Thursday</th><th>Friday</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");

        // Build day lookup
        var dayLookup = new Dictionary<DateOnly, ProcessedDay>();
        foreach (var day in month.Days)
            dayLookup[day.Date] = day;

        // Walk through calendar weeks
        var firstOfMonth = new DateOnly(month.Year, month.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        // Find the Monday of the first week
        var current = firstOfMonth;
        while (current.DayOfWeek != DayOfWeek.Monday)
            current = current.AddDays(-1);

        while (current <= lastOfMonth)
        {
            sb.AppendLine("<tr>");
            for (var dow = 0; dow < 5; dow++) // Mon-Fri
            {
                var cellDate = current.AddDays(dow);
                if (cellDate.Month != month.Month)
                {
                    sb.AppendLine("<td class=\"empty\"></td>");
                    continue;
                }

                if (dayLookup.TryGetValue(cellDate, out var day))
                {
                    AppendDayCell(sb, day, sessionLabel, forcedHomeDays, linePalette);
                }
                else
                {
                    // Weekday with no data = no school
                    sb.AppendLine($"<td class=\"no-school\"><div class=\"day-number\">{cellDate.Day}</div></td>");
                }
            }
            sb.AppendLine("</tr>");
            current = current.AddDays(7);
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        var html = sb.ToString();
        _logger.LogInformation("Generated HTML calendar: {Length} characters", html.Length);
        return html;
    }

    /// <summary>
    /// Builds a mapping from plan name to (CSS class, color) for per-plan badge rendering.
    /// </summary>
    private static Dictionary<string, (string CssClass, string Color)> BuildLinePalette(ProcessedMonth month)
    {
        var planNames = month.Days
            .SelectMany(d => d.Lines)
            .Select(l => l.PlanName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var palette = new Dictionary<string, (string CssClass, string Color)>();
        for (var i = 0; i < planNames.Count; i++)
        {
            var cssClass = $"line-{i}";
            var color = LinePalette[i % LinePalette.Length];
            palette[planNames[i]] = (cssClass, color);
        }
        return palette;
    }

    /// <summary>
    /// Appends CSS styles for the calendar layout and print support, using the given theme.
    /// </summary>
    private static void AppendCss(StringBuilder sb, CalendarTheme theme)
    {
        sb.AppendLine($$"""
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
                background: {{theme.BodyBg}};
            }
            h1 {
                font-size: 18px;
                margin-bottom: 2px;
                text-align: center;
                color: {{theme.TitleColor}};
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
            .safe-swatch { background: {{theme.SafeColor}}; }
            .no-school-swatch { background: #e9ecef; }
            table {
                width: 100%;
                border-collapse: collapse;
                table-layout: fixed;
            }
            th {
                background: {{theme.HeaderBg}};
                color: {{theme.HeaderFg}};
                padding: 4px;
                text-align: center;
                font-size: 12px;
            }
            td {
                border: 1px solid {{theme.AccentBorder}};
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
            .badge.home { background: {{theme.HomeBadgeBg}}; }
            .plan-section {
                margin-bottom: 2px;
            }
            .safe-item {
                color: {{theme.SafeColor}};
                font-weight: bold;
                font-size: 10px;
            }
            .favorite-item {
                color: {{theme.SafeColor}};
                font-weight: bold;
                font-size: 10px;
            }
            .favorite-star {
                color: {{theme.FavoriteStar}};
                font-size: 11px;
            }
            .favorite-day {
                border: 2px solid {{theme.FavoriteBorder}} !important;
                background: {{theme.FavoriteBg}};
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
            @media print {
                body { padding: 0; }
                table { page-break-inside: avoid; }
            }
        """);

        if (theme.CellPattern is not null)
        {
            sb.AppendLine($"td {{ background-image: {theme.CellPattern}; background-size: 20px 20px; }}");
            sb.AppendLine("td.empty, td.no-school { background-image: none; }");
        }
    }

    /// <summary>
    /// Appends a single day cell to the HTML output, rendering per-plan sections with badges.
    /// </summary>
    private static void AppendDayCell(StringBuilder sb, ProcessedDay day, string sessionLabel,
        IReadOnlySet<DayOfWeek> forcedHomeDays,
        Dictionary<string, (string CssClass, string Color)> linePalette)
    {
        if (day.IsNoSchool)
        {
            var emoji = GetNoSchoolEmoji(day.AcademicNote!);
            sb.AppendLine("<td class=\"no-school\">");
            sb.AppendLine($"<div class=\"day-number\">{day.Date.Day}</div>");
            sb.AppendLine($"<div class=\"no-school-emoji\">{emoji}</div>");
            sb.AppendLine($"<div class=\"no-school-note\">{Encode(day.AcademicNote!)}</div>");
            sb.AppendLine("</td>");
            return;
        }

        if (!day.HasMenu)
        {
            // No menu data => treat as no school
            sb.AppendLine("<td class=\"no-school\">");
            sb.AppendLine($"<div class=\"day-number\">{day.Date.Day}</div>");
            if (day.AcademicNote is not null)
            {
                var emoji = GetNoSchoolEmoji(day.AcademicNote);
                sb.AppendLine($"<div class=\"no-school-emoji\">{emoji}</div>");
                sb.AppendLine($"<div class=\"no-school-note\">{Encode(day.AcademicNote)}</div>");
            }
            sb.AppendLine("</td>");
            return;
        }

        // Pre-scan for favorites to apply day highlight
        var dayHasFavorite = day.Lines
            .SelectMany(l => l.Entrees)
            .Any(e => e.IsFavorite && !e.ContainsAllergen);

        sb.AppendLine(dayHasFavorite ? "<td class=\"favorite-day\">" : "<td>");
        sb.AppendLine($"<div class=\"day-number\">{day.Date.Day}</div>");

        // Special academic note (not no-school)
        if (day.HasSpecialNote)
            sb.AppendLine($"<div class=\"special-note\">{Encode(day.AcademicNote!)}</div>");

        // Render per-plan sections
        foreach (var line in day.Lines)
        {
            if (line.Entrees.Count == 0)
                continue;

            var hasVisibleItems = line.Entrees.Any(e => !e.ContainsAllergen);
            if (!hasVisibleItems)
                continue;

            sb.AppendLine("<div class=\"plan-section\">");

            // Plan name badge
            if (linePalette.TryGetValue(line.PlanName, out var badge))
                sb.AppendLine($"<div><span class=\"badge {badge.CssClass}\">{Encode(line.PlanName)}</span></div>");

            foreach (var item in line.Entrees)
            {
                if (item.ContainsAllergen)
                    continue;

                if (item.IsNotPreferred)
                {
                    sb.AppendLine($"<div class=\"not-preferred-item\">{Encode(item.Name)}</div>");
                }
                else if (item.IsFavorite)
                {
                    sb.AppendLine($"<div class=\"favorite-item\"><span class=\"favorite-star\">&#9733;</span> {Encode(item.Name)}</div>");
                }
                else
                {
                    sb.AppendLine($"<div class=\"safe-item\">{Encode(item.Name)}</div>");
                }
            }

            sb.AppendLine("</div>");
        }

        // "From Home" badge if no line is safe or day is a forced home day
        if (!day.AnyLineSafe || forcedHomeDays.Contains(day.Date.DayOfWeek))
        {
            sb.AppendLine($"<div><span class=\"badge home\">\U0001F3E0 {Encode(sessionLabel)} from Home</span></div>");
        }

        sb.AppendLine("</td>");
    }

    /// <summary>
    /// Returns a holiday-specific emoji based on keyword matching on the academic note text.
    /// </summary>
    internal static string GetNoSchoolEmoji(string note)
    {
        var lower = note.ToLowerInvariant();

        if (lower.Contains("winter break") || lower.Contains("christmas"))
            return "❄️";
        if (lower.Contains("thanksgiving"))
            return "🦃";
        if (lower.Contains("president"))
            return "🇺🇸";
        if (lower.Contains("mlk") || lower.Contains("martin luther king"))
            return "✊";
        if (lower.Contains("memorial"))
            return "🇺🇸";
        if (lower.Contains("labor"))
            return "🇺🇸";
        if (lower.Contains("spring break"))
            return "🌸";
        if (lower.Contains("teacher"))
            return "📚";

        return "\U0001F3E0";
    }

    /// <summary>
    /// HTML-encodes a string for safe embedding.
    /// </summary>
    private static string Encode(string text) => HttpUtility.HtmlEncode(text);
}
