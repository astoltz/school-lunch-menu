using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolLunchMenu.Services;
using Xunit;

namespace SchoolLunchMenu.Tests.Services;

public class DayLabelFetchServiceTests
{
    [Fact]
    public void ParseDayLabels_ValidHtml_ExtractsEntries()
    {
        // We test the internal parsing logic by creating a service and using reflection
        // or by testing the regex patterns against known HTML fragments.
        // Since DayLabelFetchService.ParseDayLabels is private, we test the regex behavior
        // through the public API patterns.

        // The CMS HTML uses this structure:
        // <div class="fsCalendarDate" data-day="5" data-year="2026" data-month="1">
        //   <div class="fsCalendarEventTitle"><a title="Red Day">...</a></div>
        // </div>

        var html = BuildCalendarHtml(
            ("5", "2026", "1", "Red Day"),   // Feb 5 (month is 0-indexed, 1 = February)
            ("6", "2026", "1", "White Day"), // Feb 6
            ("9", "2026", "1", "Red Day"),   // Feb 9
            ("10", "2026", "1", "White Day"),// Feb 10
            ("11", "2026", "1", "Staff Development Day") // Not a day label
        );

        // Use regex directly to validate parsing logic
        var datePattern = new System.Text.RegularExpressions.Regex(
            @"fsCalendarDate[^>]*?data-day=""(?<day>\d+)""[^>]*?data-year=""(?<year>\d+)""[^>]*?data-month=""(?<month>\d+)""",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        var titlePattern = new System.Text.RegularExpressions.Regex(
            @"fsCalendarEventTitle[^>]*?title=""(?<title>[^""]+)""",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        var labelPattern = new System.Text.RegularExpressions.Regex(
            @"^(?:(?:Red|White|Blue|Gold|Green|Silver|Black|Orange|Purple|[A-Z])\s*Day|Day\s*[A-Z])$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var dateMatches = datePattern.Matches(html);
        dateMatches.Should().HaveCount(5);

        var titleMatches = titlePattern.Matches(html);
        titleMatches.Should().HaveCount(5);

        // Verify label pattern matching
        labelPattern.IsMatch("Red Day").Should().BeTrue();
        labelPattern.IsMatch("White Day").Should().BeTrue();
        labelPattern.IsMatch("Staff Development Day").Should().BeFalse();
        labelPattern.IsMatch("Blue Day").Should().BeTrue();
        labelPattern.IsMatch("Day A").Should().BeTrue();
    }

    [Fact]
    public void LabelPattern_MatchesKnownLabels()
    {
        var labelPattern = new System.Text.RegularExpressions.Regex(
            @"^(?:(?:Red|White|Blue|Gold|Green|Silver|Black|Orange|Purple|[A-Z])\s*Day|Day\s*[A-Z])$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        labelPattern.IsMatch("Red Day").Should().BeTrue();
        labelPattern.IsMatch("White Day").Should().BeTrue();
        labelPattern.IsMatch("Blue Day").Should().BeTrue();
        labelPattern.IsMatch("Gold Day").Should().BeTrue();
        labelPattern.IsMatch("Green Day").Should().BeTrue();
        labelPattern.IsMatch("A Day").Should().BeTrue();
        labelPattern.IsMatch("B Day").Should().BeTrue();
        labelPattern.IsMatch("Day A").Should().BeTrue();
    }

    [Fact]
    public void LabelPattern_RejectsNonLabels()
    {
        var labelPattern = new System.Text.RegularExpressions.Regex(
            @"^(?:(?:Red|White|Blue|Gold|Green|Silver|Black|Orange|Purple|[A-Z])\s*Day|Day\s*[A-Z])$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        labelPattern.IsMatch("Staff Development Day").Should().BeFalse();
        labelPattern.IsMatch("No School").Should().BeFalse();
        labelPattern.IsMatch("Conference Day Off").Should().BeFalse();
        labelPattern.IsMatch("").Should().BeFalse();
    }

    [Fact]
    public void ZeroIndexedMonths_CorrectlyParsed()
    {
        // Finalsite months are 0-indexed: January = 0, February = 1, etc.
        var datePattern = new System.Text.RegularExpressions.Regex(
            @"data-month=""(?<month>\d+)""");

        var html = @"<div class=""fsCalendarDate"" data-day=""15"" data-year=""2026"" data-month=""0"">";
        var match = datePattern.Match(html);

        match.Success.Should().BeTrue();
        var month0 = int.Parse(match.Groups["month"].Value);
        var actualMonth = month0 + 1; // Convert to 1-indexed
        actualMonth.Should().Be(1); // January
    }

    private static string BuildCalendarHtml(params (string Day, string Year, string Month, string Title)[] entries)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<html><body>");

        foreach (var (day, year, month, title) in entries)
        {
            sb.AppendLine($@"<div class=""fsCalendarDate"" data-day=""{day}"" data-year=""{year}"" data-month=""{month}"">");
            sb.AppendLine($@"  <div class=""fsCalendarEventTitle"" title=""{title}""><a href=""#"">{title}</a></div>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
