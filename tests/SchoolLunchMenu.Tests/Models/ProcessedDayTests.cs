using FluentAssertions;
using SchoolLunchMenu.Models;
using Xunit;

namespace SchoolLunchMenu.Tests.Models;

public class ProcessedDayTests
{
    [Fact]
    public void IsNoSchool_WithNoSchoolNote_ReturnsTrue()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 16),
            Lines = [],
            AcademicNote = "Presidents Day - No School"
        };

        day.IsNoSchool.Should().BeTrue();
    }

    [Fact]
    public void IsNoSchool_WithoutNote_ReturnsFalse()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines = [],
            AcademicNote = null
        };

        day.IsNoSchool.Should().BeFalse();
    }

    [Fact]
    public void IsNoSchool_CaseInsensitive()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 16),
            Lines = [],
            AcademicNote = "NO SCHOOL - Teacher Workshop"
        };

        day.IsNoSchool.Should().BeTrue();
    }

    [Fact]
    public void HasSpecialNote_WithNonNoSchoolNote_ReturnsTrue()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines = [new ProcessedLine { PlanName = "Lunch", IsSafe = true, Entrees = [new RecipeItem("Pizza", false, false, false)] }],
            AcademicNote = "Early Dismissal 1:30 PM"
        };

        day.HasSpecialNote.Should().BeTrue();
        day.IsNoSchool.Should().BeFalse();
    }

    [Fact]
    public void AnyLineSafe_WithSafeLine_ReturnsTrue()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines =
            [
                new ProcessedLine { PlanName = "A", IsSafe = false, Entrees = [] },
                new ProcessedLine { PlanName = "B", IsSafe = true, Entrees = [new RecipeItem("Pizza", false, false, false)] }
            ]
        };

        day.AnyLineSafe.Should().BeTrue();
    }

    [Fact]
    public void AnyLineSafe_AllUnsafe_ReturnsFalse()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines =
            [
                new ProcessedLine { PlanName = "A", IsSafe = false, Entrees = [new RecipeItem("Cheese", true, false, false)] }
            ]
        };

        day.AnyLineSafe.Should().BeFalse();
    }

    [Fact]
    public void AnyLineSafe_NoLines_ReturnsFalse()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines = []
        };

        day.AnyLineSafe.Should().BeFalse();
    }

    [Fact]
    public void HasMenu_WithEntrees_ReturnsTrue()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines =
            [
                new ProcessedLine { PlanName = "Lunch", IsSafe = true, Entrees = [new RecipeItem("Pizza", false, false, false)] }
            ]
        };

        day.HasMenu.Should().BeTrue();
    }

    [Fact]
    public void HasMenu_EmptyEntrees_ReturnsFalse()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines =
            [
                new ProcessedLine { PlanName = "Lunch", IsSafe = false, Entrees = [] }
            ]
        };

        day.HasMenu.Should().BeFalse();
    }

    [Fact]
    public void HasMenu_NoLines_ReturnsFalse()
    {
        var day = new ProcessedDay
        {
            Date = new DateOnly(2026, 2, 2),
            Lines = []
        };

        day.HasMenu.Should().BeFalse();
    }
}
