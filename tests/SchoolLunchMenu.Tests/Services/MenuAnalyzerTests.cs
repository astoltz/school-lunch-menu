using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolLunchMenu.Models;
using SchoolLunchMenu.Models.Api;
using SchoolLunchMenu.Services;
using Xunit;

namespace SchoolLunchMenu.Tests.Services;

public class MenuAnalyzerTests
{
    private readonly MenuAnalyzer _analyzer = new(NullLogger<MenuAnalyzer>.Instance);

    private static FamilyMenuResponse BuildMenuResponse(
        string session = "Lunch",
        string planName = "Lunch - MS",
        List<(string Date, List<(string Name, List<string> Allergens, bool IsEntree)> Recipes)>? days = null,
        List<(string Date, string Note)>? academicNotes = null)
    {
        var menuDays = new List<MenuDay>();
        if (days is not null)
        {
            foreach (var (date, recipes) in days)
            {
                menuDays.Add(new MenuDay
                {
                    Date = date,
                    MenuMeals =
                    [
                        new MenuMeal
                        {
                            RecipeCategories =
                            [
                                new RecipeCategory
                                {
                                    IsEntree = true,
                                    Recipes = recipes.Where(r => r.IsEntree).Select(r => new Recipe
                                    {
                                        RecipeName = r.Name,
                                        Allergens = r.Allergens
                                    }).ToList()
                                },
                                new RecipeCategory
                                {
                                    IsEntree = false,
                                    Recipes = recipes.Where(r => !r.IsEntree).Select(r => new Recipe
                                    {
                                        RecipeName = r.Name,
                                        Allergens = r.Allergens
                                    }).ToList()
                                }
                            ]
                        }
                    ]
                });
            }
        }

        var academicCalendars = new List<AcademicCalendar>();
        if (academicNotes is not null)
        {
            academicCalendars.Add(new AcademicCalendar
            {
                Days = academicNotes.Select(n => new AcademicCalendarDay
                {
                    Date = n.Date,
                    Note = n.Note
                }).ToList()
            });
        }

        return new FamilyMenuResponse
        {
            FamilyMenuSessions =
            [
                new MenuSession
                {
                    ServingSession = session,
                    MenuPlans =
                    [
                        new MenuPlan
                        {
                            MenuPlanName = planName,
                            Days = menuDays
                        }
                    ]
                }
            ],
            AcademicCalendars = academicCalendars
        };
    }

    [Fact]
    public void Analyze_SafeEntree_MarksSafe()
    {
        var response = BuildMenuResponse(days:
        [
            ("2/2/2026", [("Pizza", [], true)])
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        result.Days.Should().ContainSingle(d => d.Date == new DateOnly(2026, 2, 2));
        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 2));
        day.AnyLineSafe.Should().BeTrue();
        day.Lines.First().IsSafe.Should().BeTrue();
        day.Lines.First().Entrees.Should().ContainSingle(e => e.Name == "Pizza" && !e.ContainsAllergen);
    }

    [Fact]
    public void Analyze_UnsafeEntree_FlagsAllergen()
    {
        var milkId = "milk-uuid";
        var response = BuildMenuResponse(days:
        [
            ("2/2/2026", [("Cheese Pizza", [milkId], true)])
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string> { milkId }, new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 2));
        day.Lines.First().IsSafe.Should().BeFalse();
        day.Lines.First().Entrees.Should().ContainSingle(e => e.ContainsAllergen);
    }

    [Fact]
    public void Analyze_NotPreferredEntree_DoesNotCountAsSafe()
    {
        var response = BuildMenuResponse(days:
        [
            ("2/2/2026", [("Corn Dog", [], true)])
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string> { "Corn Dog" }, new HashSet<string>(), 2026, 2, "Lunch");

        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 2));
        day.Lines.First().IsSafe.Should().BeFalse();
        day.Lines.First().Entrees.Should().ContainSingle(e => e.IsNotPreferred);
    }

    [Fact]
    public void Analyze_FavoriteEntree_IsFlagged()
    {
        var response = BuildMenuResponse(days:
        [
            ("2/2/2026", [("Pizza", [], true)])
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string>(), new HashSet<string> { "Pizza" }, 2026, 2, "Lunch");

        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 2));
        day.Lines.First().Entrees.Should().ContainSingle(e => e.IsFavorite);
    }

    [Fact]
    public void Analyze_NoSchoolDay_RecognizedByNote()
    {
        var response = BuildMenuResponse(
            days: [("2/16/2026", [])],
            academicNotes: [("2/16/2026", "Presidents Day - No School")]
        );

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 16));
        day.IsNoSchool.Should().BeTrue();
    }

    [Fact]
    public void Analyze_MultiPlan_EachPlanAnalyzedSeparately()
    {
        var milkId = "milk-uuid";
        var response = new FamilyMenuResponse
        {
            FamilyMenuSessions =
            [
                new MenuSession
                {
                    ServingSession = "Lunch",
                    MenuPlans =
                    [
                        new MenuPlan
                        {
                            MenuPlanName = "Regular",
                            Days =
                            [
                                new MenuDay
                                {
                                    Date = "2/2/2026",
                                    MenuMeals =
                                    [
                                        new MenuMeal
                                        {
                                            RecipeCategories =
                                            [
                                                new RecipeCategory
                                                {
                                                    IsEntree = true,
                                                    Recipes = [new Recipe { RecipeName = "Cheese Pizza", Allergens = [milkId] }]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        new MenuPlan
                        {
                            MenuPlanName = "Big Cat",
                            Days =
                            [
                                new MenuDay
                                {
                                    Date = "2/2/2026",
                                    MenuMeals =
                                    [
                                        new MenuMeal
                                        {
                                            RecipeCategories =
                                            [
                                                new RecipeCategory
                                                {
                                                    IsEntree = true,
                                                    Recipes = [new Recipe { RecipeName = "PB&J", Allergens = [] }]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = _analyzer.Analyze(response, new HashSet<string> { milkId }, new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 2));
        day.Lines.Should().HaveCount(2);
        day.Lines.First(l => l.PlanName == "Regular").IsSafe.Should().BeFalse();
        day.Lines.First(l => l.PlanName == "Big Cat").IsSafe.Should().BeTrue();
        day.AnyLineSafe.Should().BeTrue();
    }

    [Fact]
    public void Analyze_NonEntreeCategory_IsIgnored()
    {
        var milkId = "milk-uuid";
        var response = BuildMenuResponse(days:
        [
            ("2/2/2026", [
                ("Pizza", [], true),
                ("Milk Carton", [milkId], false) // side, not entree
            ])
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string> { milkId }, new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        var day = result.Days.First(d => d.Date == new DateOnly(2026, 2, 2));
        day.Lines.First().IsSafe.Should().BeTrue();
        day.Lines.First().Entrees.Should().ContainSingle(e => e.Name == "Pizza");
    }

    [Fact]
    public void Analyze_WeekendsExcluded()
    {
        var response = BuildMenuResponse(days:
        [
            ("2/1/2026", [("Pizza", [], true)]) // Feb 1 2026 is a Sunday
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        result.Days.Should().NotContain(d => d.Date == new DateOnly(2026, 2, 1));
    }

    [Fact]
    public void Analyze_WrongSession_ReturnsEmptyMonth()
    {
        var response = BuildMenuResponse(session: "Breakfast", days:
        [
            ("2/2/2026", [("Pancakes", [], true)])
        ]);

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch");

        result.Days.Should().AllSatisfy(d => d.HasMenu.Should().BeFalse());
    }

    [Fact]
    public void Analyze_SetsMonthMetadata()
    {
        var response = BuildMenuResponse(days: [("2/2/2026", [("Pizza", [], true)])]);

        var result = _analyzer.Analyze(response, new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), 2026, 2, "Lunch", "Test School");

        result.Year.Should().Be(2026);
        result.Month.Should().Be(2);
        result.BuildingName.Should().Be("Test School");
        result.SessionName.Should().Be("Lunch");
        result.DisplayName.Should().Be("February 2026");
    }
}
