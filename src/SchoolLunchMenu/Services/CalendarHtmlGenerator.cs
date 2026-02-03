namespace SchoolLunchMenu.Services;

using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using QRCoder;
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
    public string Generate(ProcessedMonth month, IReadOnlyList<string> allergenNames, IReadOnlySet<DayOfWeek> forcedHomeDays, CalendarTheme theme, CalendarRenderOptions? options = null)
    {
        _logger.LogInformation("Generating HTML calendar for {Month} with theme {Theme}", month.DisplayName, theme.Name);

        options ??= new CalendarRenderOptions();
        options.HomeBadgeBg = theme.HomeBadgeBg;
        var sessionLabel = month.SessionName ?? "Lunch";
        var linePalette = BuildLinePalette(month);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine($"<title>{Encode(month.DisplayName)} {Encode(sessionLabel)} Calendar</title>");
        sb.AppendLine("<style>");
        AppendCss(sb, theme, options);

        // Dynamic badge CSS per plan (badges + grid buttons)
        foreach (var (planName, (cssClass, color)) in linePalette)
        {
            sb.AppendLine($".{cssClass} {{ background: {color}; }}");
            sb.AppendLine($".grid-btn.{cssClass} {{ background: {color}; }}");
        }

        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body style=\"width:10.5in;\">");

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
        sb.AppendLine($"<span class=\"legend-item\"><span class=\"badge home\">\U0001F3E0 {Encode(sessionLabel)} from Home</span> No safe options / forced home day</span>");
        sb.AppendLine("<span class=\"legend-item\"><span class=\"swatch no-school-swatch\"></span> \U0001F3E0 No School</span>");

        // Plan line legend entries (in user-defined display order)
        var orderedPlanNames = GetOrderedPlanNames(linePalette, options.PlanDisplayOrder);
        foreach (var planName in orderedPlanNames)
        {
            var (cssClass, _) = linePalette[planName];
            var legendLabel = options.PlanLabelOverrides.TryGetValue(planName, out var lo) ? lo : planName;
            sb.AppendLine($"<span class=\"legend-item\"><span class=\"badge {cssClass}\">{Encode(legendLabel)}</span></span>");
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

        // Build day label lookup
        var dayLabelLookup = BuildDayLabelLookup(month, options);

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
                    AppendDayCell(sb, day, sessionLabel, forcedHomeDays, linePalette, options, dayLabelLookup);
                }
                else
                {
                    // Weekday with no data = no school
                    var noDataClasses = "no-school";
                    if (options.CrossOutPastDays && options.Today.HasValue && cellDate < options.Today.Value)
                        noDataClasses += " past-day";
                    sb.AppendLine($"<td class=\"{noDataClasses}\"><div class=\"day-number\">{cellDate.Day}</div></td>");
                }
            }
            sb.AppendLine("</tr>");
            current = current.AddDays(7);
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");

        if (options.ShowShareFooter)
            AppendShareFooter(sb, options);

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
    private static void AppendCss(StringBuilder sb, CalendarTheme theme, CalendarRenderOptions options)
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
                background: {{theme.HomeBadgeBg}};
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
                content: '\2715';
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
        """);

        // Corner-specific day label positioning
        var (triangleBorder, trianglePos, textPos, textRotation, borderColorProp) = options.DayLabelCorner switch
        {
            "TopLeft" => ("0 0 32px 32px", "top:0;left:0;", "top:2px;left:1px;", "rotate(-45deg)", "border-left-color"),
            "BottomRight" => ("32px 0 0 32px", "bottom:0;right:0;", "bottom:2px;right:1px;", "rotate(-45deg)", "border-left-color"),
            "BottomLeft" => ("32px 32px 0 0", "bottom:0;left:0;", "bottom:2px;left:1px;", "rotate(45deg)", "border-right-color"),
            _ => ("0 32px 32px 0", "top:0;right:0;", "top:2px;right:1px;", "rotate(45deg)", "border-right-color") // TopRight default
        };
        sb.AppendLine($".day-label {{ border-width:{triangleBorder};{trianglePos} }}");
        sb.AppendLine($".day-label-text {{ {textPos}transform:{textRotation}; }}");

        if (theme.CellPattern is not null)
        {
            sb.AppendLine($"td {{ background-image: {theme.CellPattern}; background-size: 20px 20px; }}");
            sb.AppendLine("td.empty, td.no-school { background-image: none; }");
        }
    }

    /// <summary>
    /// Returns plan names ordered by user-defined display order, then alphabetically for remaining.
    /// </summary>
    private static List<string> GetOrderedPlanNames(
        Dictionary<string, (string CssClass, string Color)> linePalette,
        IReadOnlyList<string> displayOrder)
    {
        var ordered = new List<string>();
        var remaining = new HashSet<string>(linePalette.Keys);

        foreach (var name in displayOrder)
        {
            if (remaining.Remove(name))
                ordered.Add(name);
        }

        ordered.AddRange(remaining.OrderBy(n => n));
        return ordered;
    }

    /// <summary>
    /// Builds a lookup from date to day label for the rotating day label cycle.
    /// School days (has menu, not no-school) cycle through the labels in order.
    /// </summary>
    private static Dictionary<DateOnly, DayLabel> BuildDayLabelLookup(ProcessedMonth month, CalendarRenderOptions options)
    {
        var lookup = new Dictionary<DateOnly, DayLabel>();
        if (options.DayLabelCycle.Count == 0)
            return lookup;

        // Collect school days in date order (days that have a menu and are not no-school)
        var schoolDays = month.Days
            .Where(d => d.HasMenu && !d.IsNoSchool)
            .OrderBy(d => d.Date)
            .Select(d => d.Date)
            .ToList();

        if (schoolDays.Count == 0)
            return lookup;

        // Determine anchor date
        var anchorDate = options.DayLabelStartDate ?? schoolDays[0];
        var anchorIndex = schoolDays.IndexOf(anchorDate);

        // If anchor date is not in the school days list, find the nearest school day
        // and compute the offset from the anchor
        int anchorOffset;
        if (anchorIndex >= 0)
        {
            anchorOffset = anchorIndex;
        }
        else
        {
            // Anchor is outside this month's school days; compute how many school days
            // the anchor is before the first school day (simplified: just use first day as index 0)
            anchorOffset = 0;
        }

        var cycleLen = options.DayLabelCycle.Count;
        for (var i = 0; i < schoolDays.Count; i++)
        {
            var cycleIndex = ((i - anchorOffset) % cycleLen + cycleLen) % cycleLen;
            lookup[schoolDays[i]] = options.DayLabelCycle[cycleIndex];
        }

        return lookup;
    }

    /// <summary>
    /// Appends a single day cell to the HTML output, rendering per-plan sections with badges.
    /// Supports List, IconsLeft, and IconsRight layout modes.
    /// </summary>
    private static void AppendDayCell(StringBuilder sb, ProcessedDay day, string sessionLabel,
        IReadOnlySet<DayOfWeek> forcedHomeDays,
        Dictionary<string, (string CssClass, string Color)> linePalette,
        CalendarRenderOptions options,
        Dictionary<DateOnly, DayLabel> dayLabelLookup)
    {
        var isPast = options.CrossOutPastDays && options.Today.HasValue && day.Date < options.Today.Value;
        var hasLabel = dayLabelLookup.TryGetValue(day.Date, out var dayLabel);

        if (day.IsNoSchool)
        {
            var (emoji, message) = GetNoSchoolDisplay(day.AcademicNote!, options.HolidayOverrides);
            var noSchoolClasses = new List<string> { "no-school" };
            if (isPast) noSchoolClasses.Add("past-day");
            sb.AppendLine($"<td class=\"{string.Join(" ", noSchoolClasses)}\">");
            sb.AppendLine($"<div class=\"day-number\">{day.Date.Day}</div>");
            sb.AppendLine($"<div class=\"no-school-emoji\">{emoji}</div>");
            sb.AppendLine($"<div class=\"no-school-note\">{Encode(CleanNoSchoolNote(message))}</div>");
            sb.AppendLine("</td>");
            return;
        }

        if (!day.HasMenu)
        {
            var noMenuClasses = new List<string> { "no-school" };
            if (isPast) noMenuClasses.Add("past-day");
            sb.AppendLine($"<td class=\"{string.Join(" ", noMenuClasses)}\">");
            sb.AppendLine($"<div class=\"day-number\">{day.Date.Day}</div>");
            if (day.AcademicNote is not null)
            {
                var (emoji, message) = GetNoSchoolDisplay(day.AcademicNote, options.HolidayOverrides);
                sb.AppendLine($"<div class=\"no-school-emoji\">{emoji}</div>");
                sb.AppendLine($"<div class=\"no-school-note\">{Encode(CleanNoSchoolNote(message))}</div>");
            }
            sb.AppendLine("</td>");
            return;
        }

        var dayHasFavorite = day.Lines
            .SelectMany(l => l.Entrees)
            .Any(e => e.IsFavorite && !e.ContainsAllergen);

        var isForcedHomeDay = forcedHomeDays.Contains(day.Date.DayOfWeek);
        var isHomeDay = !day.AnyLineSafe || isForcedHomeDay;

        var classes = new List<string>();
        if (dayHasFavorite) classes.Add("favorite-day");
        if (isPast) classes.Add("past-day");
        var needsRelative = hasLabel || isPast;
        var classAttr = classes.Count > 0 ? $" class=\"{string.Join(" ", classes)}\"" : "";
        var styleAttr = needsRelative ? " style=\"position:relative;\"" : "";
        sb.AppendLine($"<td{classAttr}{styleAttr}>");

        // Day label corner triangle
        if (hasLabel)
        {
            var borderProp = options.DayLabelCorner switch
            {
                "TopLeft" or "BottomRight" => "border-left-color",
                _ => "border-right-color"
            };
            sb.AppendLine($"<div class=\"day-label\" style=\"{borderProp}: {dayLabel!.Color};\"></div>");
            sb.AppendLine($"<div class=\"day-label-text\">{Encode(dayLabel.Label)}</div>");
        }

        sb.AppendLine($"<div class=\"day-number\">{day.Date.Day}</div>");

        if (day.HasSpecialNote)
            sb.AppendLine($"<div class=\"special-note\">{Encode(day.AcademicNote!)}</div>");

        var isGrid = options.LayoutMode is "IconsLeft" or "IconsRight";

        if (isGrid)
        {
            AppendGridLayout(sb, day, sessionLabel, isHomeDay, isForcedHomeDay, linePalette, options);
        }
        else
        {
            AppendListLayout(sb, day, sessionLabel, isHomeDay, isForcedHomeDay, linePalette, options);
        }

        sb.AppendLine("</td>");
    }

    /// <summary>
    /// Renders List mode: stacked plan sections with badge + items, no grid.
    /// </summary>
    private static void AppendListLayout(StringBuilder sb, ProcessedDay day, string sessionLabel,
        bool isHomeDay, bool isForcedHomeDay,
        Dictionary<string, (string CssClass, string Color)> linePalette,
        CalendarRenderOptions options)
    {
        var orderedPlanNames = GetOrderedPlanNames(linePalette, options.PlanDisplayOrder);

        foreach (var planName in orderedPlanNames)
        {
            var line = day.Lines.FirstOrDefault(l => l.PlanName == planName);
            if (line is null || line.Entrees.Count == 0)
                continue;

            var hasVisibleItems = line.Entrees.Any(e => !e.ContainsAllergen);
            if (!hasVisibleItems)
                continue;

            var sectionClass = isForcedHomeDay && hasVisibleItems ? "plan-section forced-home" : "plan-section";
            sb.AppendLine($"<div class=\"{sectionClass}\">");

            if (linePalette.TryGetValue(line.PlanName, out var badge))
            {
                var badgeLabel = options.PlanLabelOverrides.TryGetValue(line.PlanName, out var bl) ? bl : line.PlanName;
                sb.AppendLine($"<div><span class=\"badge {badge.CssClass}\">{Encode(badgeLabel)}</span></div>");
            }

            AppendEntreeItems(sb, line);
            sb.AppendLine("</div>");
        }

        if (isHomeDay)
            sb.AppendLine($"<div><span class=\"badge home\">\U0001F3E0 {Encode(sessionLabel)} from Home</span></div>");
    }

    /// <summary>
    /// Renders IconsLeft/IconsRight mode: CSS grid with button + items per plan row.
    /// </summary>
    private static void AppendGridLayout(StringBuilder sb, ProcessedDay day, string sessionLabel,
        bool isHomeDay, bool isForcedHomeDay,
        Dictionary<string, (string CssClass, string Color)> linePalette,
        CalendarRenderOptions options)
    {
        var buttonsLeft = options.LayoutMode == "IconsLeft";
        var gridClass = buttonsLeft ? "buttons-left" : "buttons-right";
        sb.AppendLine($"<div class=\"day-grid {gridClass}\">");

        var orderedPlanNames = GetOrderedPlanNames(linePalette, options.PlanDisplayOrder);

        foreach (var planName in orderedPlanNames)
        {
            var line = day.Lines.FirstOrDefault(l => l.PlanName == planName);
            var lineAllergenSafe = line is not null && line.Entrees.Any(e => !e.ContainsAllergen);

            if (!lineAllergenSafe && !options.ShowUnsafeLines)
                continue;

            var (cssClass, color) = linePalette[planName];
            var label = options.PlanLabelOverrides.TryGetValue(planName, out var overrideLabel) ? overrideLabel : planName;
            var defaultIcon = lineAllergenSafe ? "✅" : "⛔";
            var icon = options.PlanIconOverrides.TryGetValue(planName, out var customIcon) && !string.IsNullOrWhiteSpace(customIcon)
                ? customIcon
                : defaultIcon;
            var stateClass = !lineAllergenSafe ? "btn-off" : isForcedHomeDay ? "btn-forced-home" : "";
            var rowTint = $"{color}1a"; // ~10% opacity tint of the plan color

            sb.AppendLine("<div class=\"grid-row\">");

            // Button cell
            var buttonHtml = $"<div class=\"grid-btn {cssClass} {stateClass}\"><span class=\"grid-icon\">{icon}</span><span class=\"grid-label\">{Encode(label)}</span></div>";

            // Items cell — tinted background connects it visually to the button
            var itemsHtml = new StringBuilder();
            itemsHtml.Append($"<div class=\"grid-items\" style=\"background:{rowTint}\">");
            if (lineAllergenSafe)
            {
                AppendEntreeItems(itemsHtml, line!);
            }
            else
            {
                itemsHtml.Append($"<div class=\"not-preferred-item\">{Encode(options.UnsafeLineMessage)}</div>");
            }
            itemsHtml.Append("</div>");

            if (buttonsLeft)
            {
                sb.AppendLine(buttonHtml);
                sb.AppendLine(itemsHtml.ToString());
            }
            else
            {
                sb.AppendLine(itemsHtml.ToString());
                sb.AppendLine(buttonHtml);
            }

            sb.AppendLine("</div>");
        }

        // Home row — styled like a regular meal item
        {
            var buttonHtml = $"<div class=\"grid-btn btn-home\"><span class=\"grid-icon\">\U0001F3E0</span><span class=\"grid-label\">Home</span></div>";
            var homeTint = $"{options.HomeBadgeBg}1a";
            var homeItemText = $"Home {Encode(sessionLabel)}";
            var itemsHtml = $"<div class=\"grid-items\" style=\"background:{homeTint}\"><div class=\"safe-item\">{homeItemText}</div></div>";

            sb.AppendLine("<div class=\"grid-row\">");
            if (buttonsLeft)
            {
                sb.AppendLine(buttonHtml);
                sb.AppendLine(itemsHtml);
            }
            else
            {
                sb.AppendLine(itemsHtml);
                sb.AppendLine(buttonHtml);
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>"); // close .day-grid
    }

    /// <summary>
    /// Appends entree items for a single plan line.
    /// </summary>
    private static void AppendEntreeItems(StringBuilder sb, ProcessedLine line)
    {
        foreach (var item in line.Entrees)
        {
            if (item.ContainsAllergen)
                continue;

            if (item.IsNotPreferred)
                sb.AppendLine($"<div class=\"not-preferred-item\">{Encode(item.Name)}</div>");
            else if (item.IsFavorite)
                sb.AppendLine($"<div class=\"favorite-item\"><span class=\"favorite-star\">&#9733;</span> {Encode(item.Name)}</div>");
            else
                sb.AppendLine($"<div class=\"safe-item\">{Encode(item.Name)}</div>");
        }
    }

    /// <summary>
    /// Returns a holiday-specific emoji and display message for a no-school day.
    /// Checks user-configured overrides first, then falls back to hardcoded keyword matching.
    /// </summary>
    internal static (string Emoji, string Message) GetNoSchoolDisplay(string note, IReadOnlyDictionary<string, HolidayOverride> overrides)
    {
        var lower = note.ToLowerInvariant();

        // Check user-configured overrides first
        foreach (var (keyword, holidayOverride) in overrides)
        {
            if (lower.Contains(keyword.ToLowerInvariant()))
            {
                var message = holidayOverride.CustomMessage ?? note;
                return (holidayOverride.Emoji, message);
            }
        }

        // Fall back to hardcoded detection
        return (GetNoSchoolEmoji(note), note);
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
    /// Strips redundant "No School" suffixes from academic note text,
    /// since the cell is already styled as a no-school day.
    /// </summary>
    private static string CleanNoSchoolNote(string note)
    {
        // Strip redundant "- No School" / "- no school" suffix
        var idx = note.IndexOf(" - No School", StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
            return note[..idx].Trim();
        // Also handle without the dash
        idx = note.IndexOf(" No School", StringComparison.OrdinalIgnoreCase);
        if (idx > 0 && note[..idx].TrimEnd().EndsWith('-'))
            return note[..idx].TrimEnd().TrimEnd('-').Trim();
        return note;
    }

    /// <summary>
    /// Appends a shareable footer with QR codes linking to the project GitHub page
    /// and optionally the LINQ Connect source menu.
    /// </summary>
    private static void AppendShareFooter(StringBuilder sb, CalendarRenderOptions options)
    {
        sb.AppendLine("<div class=\"share-footer\">");

        // Source menu QR code (if a source URL is available)
        if (!string.IsNullOrEmpty(options.SourceUrl))
        {
            var sourceBase64 = GenerateQrBase64(options.SourceUrl);
            sb.AppendLine("<div class=\"share-group\">");
            sb.AppendLine($"<img src=\"data:image/png;base64,{sourceBase64}\" alt=\"Menu source QR code\" />");
            sb.AppendLine("<span class=\"share-text\">Scan to view the full school menu online</span>");
            sb.AppendLine("</div>");
        }

        // Project GitHub QR code
        var githubBase64 = GenerateQrBase64("https://github.com/astoltz/school-lunch-menu");
        sb.AppendLine("<div class=\"share-group\">");
        sb.AppendLine($"<img src=\"data:image/png;base64,{githubBase64}\" alt=\"Project QR code\" />");
        sb.AppendLine("<span class=\"share-text\">Want your own allergen-friendly lunch calendar? Scan to learn more!</span>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
    }

    /// <summary>
    /// Generates a QR code as a base64-encoded PNG data URI string.
    /// </summary>
    private static string GenerateQrBase64(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = pngQrCode.GetGraphic(4);
        return Convert.ToBase64String(pngBytes);
    }

    /// <summary>
    /// HTML-encodes a string for safe embedding.
    /// </summary>
    private static string Encode(string text) => HttpUtility.HtmlEncode(text);
}
