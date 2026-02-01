namespace SchoolLunchMenu.Services;

using System.Globalization;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Models;
using SchoolLunchMenu.Models.Api;

/// <summary>
/// Analyzes LINQ Connect menu data to classify each day by allergen safety per menu plan line.
/// </summary>
public class MenuAnalyzer : IMenuAnalyzer
{
    private readonly ILogger<MenuAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MenuAnalyzer"/>.
    /// </summary>
    public MenuAnalyzer(ILogger<MenuAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ProcessedMonth Analyze(FamilyMenuResponse menuResponse, IReadOnlySet<string> selectedAllergenIds, IReadOnlySet<string> notPreferredNames, IReadOnlySet<string> favoriteNames, int year, int month, string sessionName, string? buildingName = null)
    {
        _logger.LogInformation("Analyzing menu for {Month}/{Year} session={Session} with {Count} selected allergens", month, year, sessionName, selectedAllergenIds.Count);

        // Build academic calendar lookup: date string -> note
        var academicNotes = BuildAcademicCalendarLookup(menuResponse);

        // Find the requested session
        var session = menuResponse.FamilyMenuSessions
            .FirstOrDefault(s => s.ServingSession.Equals(sessionName, StringComparison.OrdinalIgnoreCase));

        if (session is null)
        {
            _logger.LogWarning("No {Session} session found in menu data", sessionName);
            return BuildEmptyMonth(year, month, academicNotes, buildingName);
        }

        // Index all plans by date
        var planDayIndexes = new List<(string PlanName, Dictionary<string, MenuDay> DayIndex)>();
        foreach (var plan in session.MenuPlans)
        {
            var dayIndex = IndexDaysByDate(plan);
            planDayIndexes.Add((plan.MenuPlanName, dayIndex));
            _logger.LogInformation("Found plan: {PlanName} with {DayCount} days", plan.MenuPlanName, dayIndex.Count);
        }

        // Process each weekday in the month
        var processedDays = new List<ProcessedDay>();
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var dateKey = date.ToString("M/d/yyyy");
            academicNotes.TryGetValue(dateKey, out var academicNote);

            var lines = new List<ProcessedLine>();
            foreach (var (planName, dayIndex) in planDayIndexes)
            {
                var entrees = ExtractEntrees(dayIndex, dateKey, selectedAllergenIds, notPreferredNames, favoriteNames);
                var isSafe = entrees.Any(e => !e.ContainsAllergen && !e.IsNotPreferred);

                lines.Add(new ProcessedLine
                {
                    PlanName = planName,
                    IsSafe = isSafe,
                    Entrees = entrees
                });
            }

            var day = new ProcessedDay
            {
                Date = date,
                Lines = lines,
                AcademicNote = academicNote
            };

            _logger.LogDebug("Day {Date}: {SafeCount}/{TotalCount} lines safe, Note={Note}",
                dateKey, lines.Count(l => l.IsSafe), lines.Count, academicNote);

            processedDays.Add(day);
        }

        return new ProcessedMonth
        {
            Year = year,
            Month = month,
            Days = processedDays,
            BuildingName = buildingName,
            SessionName = sessionName
        };
    }

    /// <summary>
    /// Builds a lookup from date string to academic calendar note.
    /// </summary>
    private Dictionary<string, string> BuildAcademicCalendarLookup(FamilyMenuResponse response)
    {
        var lookup = new Dictionary<string, string>();
        foreach (var calendar in response.AcademicCalendars ?? [])
        {
            foreach (var day in calendar.Days ?? [])
            {
                if (!string.IsNullOrEmpty(day.Date) && !string.IsNullOrEmpty(day.Note))
                {
                    lookup[day.Date] = day.Note;
                }
            }
        }
        return lookup;
    }

    /// <summary>
    /// Indexes menu plan days by their date string for O(1) lookup.
    /// </summary>
    private static Dictionary<string, MenuDay> IndexDaysByDate(MenuPlan? plan)
    {
        if (plan is null) return [];
        return plan.Days.ToDictionary(d => d.Date, d => d);
    }

    /// <summary>
    /// Extracts entree recipes from a menu day and flags allergen-containing, not-preferred, and favorite items.
    /// </summary>
    private List<RecipeItem> ExtractEntrees(Dictionary<string, MenuDay> dayIndex, string dateKey, IReadOnlySet<string> allergenIds, IReadOnlySet<string> notPreferredNames, IReadOnlySet<string> favoriteNames)
    {
        if (!dayIndex.TryGetValue(dateKey, out var menuDay))
            return [];

        var entrees = new List<RecipeItem>();

        foreach (var meal in menuDay.MenuMeals ?? [])
        {
            foreach (var category in meal.RecipeCategories ?? [])
            {
                if (!category.IsEntree)
                    continue;

                // Track whether the preceding parent entree contains an allergen,
                // so "with ..." companion items inherit the parent's allergen status.
                var parentContainsAllergen = false;

                foreach (var recipe in category.Recipes ?? [])
                {
                    var isCompanion = recipe.RecipeName.StartsWith("with ", StringComparison.OrdinalIgnoreCase);
                    var containsAllergen = recipe.Allergens?.Any(a => allergenIds.Contains(a)) ?? false;

                    if (isCompanion)
                    {
                        // Companion inherits allergen flag from parent
                        containsAllergen = containsAllergen || parentContainsAllergen;
                    }
                    else
                    {
                        parentContainsAllergen = containsAllergen;
                    }

                    _logger.LogDebug("Recipe '{Name}' allergens={Allergens}, containsSelected={Contains}",
                        recipe.RecipeName, string.Join(",", recipe.Allergens ?? []), containsAllergen);

                    var isNotPreferred = !containsAllergen && notPreferredNames.Contains(recipe.RecipeName);
                    var isFavorite = !containsAllergen && favoriteNames.Contains(recipe.RecipeName);
                    entrees.Add(new RecipeItem(recipe.RecipeName, containsAllergen, isNotPreferred, isFavorite));
                }
            }
        }

        return entrees;
    }

    /// <summary>
    /// Builds a month with no menu data, only academic calendar notes.
    /// </summary>
    private ProcessedMonth BuildEmptyMonth(int year, int month, Dictionary<string, string> academicNotes, string? buildingName)
    {
        var days = new List<ProcessedDay>();
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        for (var date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var dateKey = date.ToString("M/d/yyyy");
            academicNotes.TryGetValue(dateKey, out var note);

            days.Add(new ProcessedDay
            {
                Date = date,
                Lines = [],
                AcademicNote = note
            });
        }

        return new ProcessedMonth { Year = year, Month = month, Days = days, BuildingName = buildingName, SessionName = null };
    }
}
