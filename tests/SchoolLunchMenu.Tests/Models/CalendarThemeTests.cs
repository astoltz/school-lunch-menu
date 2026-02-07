using FluentAssertions;
using SchoolLunchMenu.Models;
using Xunit;

namespace SchoolLunchMenu.Tests.Models;

public class CalendarThemeTests
{
    [Fact]
    public void All_Has21Themes()
    {
        CalendarThemes.All.Should().HaveCount(21);
    }

    [Fact]
    public void All_HasSeasonalCategory()
    {
        CalendarThemes.All.Where(t => t.Category == "Seasonal").Should().HaveCount(12);
    }

    [Fact]
    public void All_HasFunCategory()
    {
        CalendarThemes.All.Where(t => t.Category == "Fun").Should().HaveCount(7);
    }

    [Fact]
    public void All_HasBasicCategory()
    {
        CalendarThemes.All.Where(t => t.Category == "Basic").Should().HaveCount(1);
    }

    [Fact]
    public void All_HasDefaultTheme()
    {
        CalendarThemes.All.Should().ContainSingle(t => t.Name == "Default");
    }

    [Fact]
    public void All_CategoriesAreValid()
    {
        var validCategories = new[] { "Seasonal", "Fun", "Basic" };
        CalendarThemes.All.Should().AllSatisfy(t =>
            validCategories.Should().Contain(t.Category));
    }

    [Fact]
    public void All_NamesAreUnique()
    {
        var names = CalendarThemes.All.Select(t => t.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AutoSuggestion_CoversAllMonths()
    {
        // Each month 1-12 should have at least one theme suggesting it
        for (var month = 1; month <= 12; month++)
        {
            var m = month;
            CalendarThemes.All.Should().Contain(
                t => t.SuggestedMonth == m || t.SuggestedMonth2 == m,
                because: $"month {month} should have a suggested theme");
        }
    }

    [Fact]
    public void DefaultTheme_HasExpectedColors()
    {
        var theme = CalendarThemes.All.First(t => t.Name == "Default");

        theme.Emoji.Should().Be("\U0001F4C5"); // 📅
        theme.Category.Should().Be("Basic");
        theme.SafeColor.Should().Be("#155724");
        theme.HomeBadgeBg.Should().Be("#dc3545");
        theme.HeaderBg.Should().Be("#343a40");
        theme.BodyBg.Should().Be("#ffffff");
    }

    [Fact]
    public void AllThemes_HaveRequiredProperties()
    {
        CalendarThemes.All.Should().AllSatisfy(t =>
        {
            t.Name.Should().NotBeNullOrWhiteSpace();
            t.Emoji.Should().NotBeNullOrWhiteSpace();
            t.Category.Should().NotBeNullOrWhiteSpace();
            t.HeaderBg.Should().StartWith("#");
            t.HeaderFg.Should().StartWith("#");
            t.TitleColor.Should().StartWith("#");
            t.SafeColor.Should().StartWith("#");
            t.FavoriteStar.Should().StartWith("#");
            t.FavoriteBorder.Should().StartWith("#");
            t.FavoriteBg.Should().StartWith("#");
            t.HomeBadgeBg.Should().StartWith("#");
            t.AccentBorder.Should().StartWith("#");
            t.BodyBg.Should().StartWith("#");
        });
    }
}
