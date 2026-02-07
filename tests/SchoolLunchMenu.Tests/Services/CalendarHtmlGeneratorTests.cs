using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolLunchMenu.Models;
using SchoolLunchMenu.Services;
using Xunit;

namespace SchoolLunchMenu.Tests.Services;

public class CalendarHtmlGeneratorTests
{
    private readonly CalendarHtmlGenerator _generator = new(NullLogger<CalendarHtmlGenerator>.Instance);

    private static CalendarTheme DefaultTheme => CalendarThemes.All.First(t => t.Name == "Default");

    private static ProcessedMonth BuildMonth(int year = 2026, int month = 2, List<ProcessedDay>? days = null)
    {
        days ??= [];
        return new ProcessedMonth
        {
            Year = year,
            Month = month,
            Days = days,
            BuildingName = "Test School",
            SessionName = "Lunch"
        };
    }

    private static ProcessedDay BuildDay(DateOnly date, bool safe = true, bool favorite = false, string? note = null, string planName = "Lunch - MS")
    {
        var entrees = new List<RecipeItem>();
        if (safe)
            entrees.Add(new RecipeItem("Pizza", false, false, favorite));
        else
            entrees.Add(new RecipeItem("Cheese Pizza", true, false, false));

        return new ProcessedDay
        {
            Date = date,
            Lines =
            [
                new ProcessedLine
                {
                    PlanName = planName,
                    IsSafe = safe,
                    Entrees = entrees
                }
            ],
            AcademicNote = note
        };
    }

    [Fact]
    public void Generate_ContainsMonthTitle()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, ["Milk"], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("February 2026");
    }

    [Fact]
    public void Generate_ContainsBuildingName()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, ["Milk"], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("Test School");
    }

    [Fact]
    public void Generate_SafeItemsRendered()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("safe-item");
        html.Should().Contain("Pizza");
    }

    [Fact]
    public void Generate_FavoriteStarRendered()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2), favorite: true)]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("favorite-star");
        html.Should().Contain("favorite-day");
    }

    [Fact]
    public void Generate_HomeBadgeOnUnsafeDay()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2), safe: false)]);

        var html = _generator.Generate(month, ["Milk"], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("from Home");
    }

    [Fact]
    public void Generate_ThemeColorsApplied()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain(DefaultTheme.HeaderBg);
        html.Should().Contain(DefaultTheme.SafeColor);
    }

    [Fact]
    public void Generate_GridMode_ContainsGridClasses()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);
        var options = new CalendarRenderOptions { LayoutMode = "IconsLeft" };

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme, options);

        html.Should().Contain("day-grid");
        html.Should().Contain("buttons-left");
        html.Should().Contain("grid-btn");
    }

    [Fact]
    public void Generate_PastDays_CrossedOut()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);
        var options = new CalendarRenderOptions
        {
            CrossOutPastDays = true,
            Today = new DateOnly(2026, 2, 10)
        };

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme, options);

        html.Should().Contain("past-day");
    }

    [Fact]
    public void Generate_DayLabels_RenderedAsTriangles()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);
        var options = new CalendarRenderOptions
        {
            DayLabelCycle = [new DayLabel { Label = "Red", Color = "#dc3545" }],
            DayLabelCorner = "TopRight"
        };

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme, options);

        html.Should().Contain("day-label");
        html.Should().Contain("day-label-text");
        html.Should().Contain("Red");
    }

    [Fact]
    public void Generate_ShareFooter_IncludesQrCode()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);
        var options = new CalendarRenderOptions
        {
            ShowShareFooter = true,
            SourceUrl = "https://linqconnect.com/public/menu/TEST123"
        };

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme, options);

        html.Should().Contain("share-footer");
        html.Should().Contain("data:image/png;base64,");
        html.Should().Contain("Want your own allergen-friendly lunch calendar?");
    }

    [Fact]
    public void Generate_NoSchoolDay_ShowsEmoji()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 16),
            Lines = [],
            AcademicNote = "Presidents Day - No School"
        };
        var month = BuildMonth(days: [day]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("no-school");
        html.Should().Contain("no-school-emoji");
    }

    [Fact]
    public void Generate_WeekdayHeaders_Present()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("<th>Monday</th>");
        html.Should().Contain("<th>Friday</th>");
    }

    [Fact]
    public void Generate_SelfContainedHtml()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<style>");
        html.Should().Contain("</html>");
        // No external links or scripts
        html.Should().NotContain("<link");
        html.Should().NotContain("<script");
    }

    [Fact]
    public void Generate_PrintCss_Included()
    {
        var month = BuildMonth(days: [BuildDay(new DateOnly(2026, 2, 2))]);

        var html = _generator.Generate(month, [], new HashSet<DayOfWeek>(), DefaultTheme);

        html.Should().Contain("@page");
        html.Should().Contain("landscape");
        html.Should().Contain("print-color-adjust: exact");
    }
}
