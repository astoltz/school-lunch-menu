namespace SchoolLunchMenu.Models;

/// <summary>
/// Defines the visual theme for a generated HTML calendar.
/// </summary>
public record CalendarTheme
{
    /// <summary>Theme display name (e.g., "Valentines", "Dinosaurs").</summary>
    public required string Name { get; init; }

    /// <summary>Emoji used to decorate the calendar title.</summary>
    public required string Emoji { get; init; }

    /// <summary>Category for grouping in the theme dropdown ("Seasonal", "Fun", "Basic").</summary>
    public required string Category { get; init; }

    /// <summary>Table header background color.</summary>
    public required string HeaderBg { get; init; }

    /// <summary>Table header text color.</summary>
    public required string HeaderFg { get; init; }

    /// <summary>Title (h1) color.</summary>
    public required string TitleColor { get; init; }

    /// <summary>Safe item text color.</summary>
    public required string SafeColor { get; init; }

    /// <summary>Favorite star color.</summary>
    public required string FavoriteStar { get; init; }

    /// <summary>Favorite day border color.</summary>
    public required string FavoriteBorder { get; init; }

    /// <summary>Favorite day background color.</summary>
    public required string FavoriteBg { get; init; }

    /// <summary>"From Home" badge background color.</summary>
    public required string HomeBadgeBg { get; init; }

    /// <summary>Cell border accent color.</summary>
    public required string AccentBorder { get; init; }

    /// <summary>Body/page background color.</summary>
    public required string BodyBg { get; init; }

    /// <summary>Optional CSS background pattern for cells.</summary>
    public string? CellPattern { get; init; }

    /// <summary>Auto-suggest this theme for the given month (1-12), or null for no auto-suggestion.</summary>
    public int? SuggestedMonth { get; init; }

    /// <summary>Secondary suggested month, for themes spanning two months.</summary>
    public int? SuggestedMonth2 { get; init; }
}

/// <summary>
/// Built-in calendar themes grouped by category.
/// Ordered so that auto-suggest picks the most specific holiday theme first.
/// </summary>
public static class CalendarThemes
{
    public static readonly IReadOnlyList<CalendarTheme> All =
    [
        // ── Seasonal ──
        new CalendarTheme
        {
            Name = "New Year",
            Emoji = "🎆",
            Category = "Seasonal",
            HeaderBg = "#212121",
            HeaderFg = "#ffd700",
            TitleColor = "#212121",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ffd700",
            FavoriteBorder = "#ffd700",
            FavoriteBg = "#fffde7",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#bdbdbd",
            BodyBg = "#fafafa",
            CellPattern = "radial-gradient(circle, #fffde7 1px, transparent 1px)",
            SuggestedMonth = 1,
        },
        new CalendarTheme
        {
            Name = "Valentines",
            Emoji = "💕",
            Category = "Seasonal",
            HeaderBg = "#c2185b",
            HeaderFg = "#ffffff",
            TitleColor = "#ad1457",
            SafeColor = "#880e4f",
            FavoriteStar = "#e91e63",
            FavoriteBorder = "#f06292",
            FavoriteBg = "#fce4ec",
            HomeBadgeBg = "#d32f2f",
            AccentBorder = "#f8bbd0",
            BodyBg = "#fff0f3",
            CellPattern = "radial-gradient(circle, #fce4ec 1px, transparent 1px)",
            SuggestedMonth = 2,
        },
        new CalendarTheme
        {
            Name = "St. Patrick's",
            Emoji = "☘️",
            Category = "Seasonal",
            HeaderBg = "#2e7d32",
            HeaderFg = "#ffffff",
            TitleColor = "#1b5e20",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ffd700",
            FavoriteBorder = "#66bb6a",
            FavoriteBg = "#e8f5e9",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#a5d6a7",
            BodyBg = "#f1f8e9",
            CellPattern = "radial-gradient(circle, #e8f5e9 1px, transparent 1px)",
            SuggestedMonth = 3,
        },
        new CalendarTheme
        {
            Name = "Easter",
            Emoji = "🐣",
            Category = "Seasonal",
            HeaderBg = "#ec407a",
            HeaderFg = "#ffffff",
            TitleColor = "#ad1457",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#f48fb1",
            FavoriteBg = "#fce4ec",
            HomeBadgeBg = "#7b1fa2",
            AccentBorder = "#f8bbd0",
            BodyBg = "#fff8e1",
            CellPattern = "radial-gradient(circle, #e1f5fe 1px, transparent 1px)",
            SuggestedMonth = 4,
        },
        new CalendarTheme
        {
            Name = "Spring",
            Emoji = "🌸",
            Category = "Seasonal",
            HeaderBg = "#2e7d32",
            HeaderFg = "#ffffff",
            TitleColor = "#1b5e20",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff6d00",
            FavoriteBorder = "#81c784",
            FavoriteBg = "#e8f5e9",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#c8e6c9",
            BodyBg = "#f1f8e9",
            SuggestedMonth2 = 5,
        },
        new CalendarTheme
        {
            Name = "Summer",
            Emoji = "☀️",
            Category = "Seasonal",
            HeaderBg = "#0277bd",
            HeaderFg = "#ffffff",
            TitleColor = "#01579b",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#4fc3f7",
            FavoriteBg = "#e1f5fe",
            HomeBadgeBg = "#d84315",
            AccentBorder = "#b3e5fc",
            BodyBg = "#fffde7",
            CellPattern = "radial-gradient(circle, #fff9c4 1px, transparent 1px)",
            SuggestedMonth = 6,
        },
        new CalendarTheme
        {
            Name = "Fourth of July",
            Emoji = "🎆",
            Category = "Seasonal",
            HeaderBg = "#1565c0",
            HeaderFg = "#ffffff",
            TitleColor = "#b71c1c",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#ef5350",
            FavoriteBg = "#ffebee",
            HomeBadgeBg = "#b71c1c",
            AccentBorder = "#bbdefb",
            BodyBg = "#f5f5f5",
            CellPattern = "radial-gradient(circle, #e3f2fd 1px, transparent 1px)",
            SuggestedMonth = 7,
        },
        new CalendarTheme
        {
            Name = "Back to School",
            Emoji = "🎒",
            Category = "Seasonal",
            HeaderBg = "#1a237e",
            HeaderFg = "#ffeb3b",
            TitleColor = "#1a237e",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#5c6bc0",
            FavoriteBg = "#e8eaf6",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#9fa8da",
            BodyBg = "#fffde7",
            SuggestedMonth = 8,
        },
        new CalendarTheme
        {
            Name = "Fall",
            Emoji = "🍂",
            Category = "Seasonal",
            HeaderBg = "#4e342e",
            HeaderFg = "#ffcc80",
            TitleColor = "#3e2723",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#a1887f",
            FavoriteBg = "#efebe9",
            HomeBadgeBg = "#bf360c",
            AccentBorder = "#bcaaa4",
            BodyBg = "#fff3e0",
            CellPattern = "radial-gradient(circle, #efebe9 1px, transparent 1px)",
            SuggestedMonth = 9,
        },
        new CalendarTheme
        {
            Name = "Spooky",
            Emoji = "🎃",
            Category = "Seasonal",
            HeaderBg = "#212121",
            HeaderFg = "#ff6d00",
            TitleColor = "#e65100",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff6d00",
            FavoriteBorder = "#ff9800",
            FavoriteBg = "#fff3e0",
            HomeBadgeBg = "#6a1b9a",
            AccentBorder = "#424242",
            BodyBg = "#1a1a2e",
            CellPattern = "radial-gradient(circle, #2d2d44 1px, transparent 1px)",
            SuggestedMonth = 10,
        },
        new CalendarTheme
        {
            Name = "Thanksgiving",
            Emoji = "🦃",
            Category = "Seasonal",
            HeaderBg = "#4e342e",
            HeaderFg = "#ffe0b2",
            TitleColor = "#3e2723",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#a1887f",
            FavoriteBg = "#efebe9",
            HomeBadgeBg = "#bf360c",
            AccentBorder = "#d7ccc8",
            BodyBg = "#fff8e1",
            CellPattern = "radial-gradient(circle, #fff3e0 1px, transparent 1px)",
            SuggestedMonth = 11,
        },
        new CalendarTheme
        {
            Name = "Winter",
            Emoji = "❄️",
            Category = "Seasonal",
            HeaderBg = "#1565c0",
            HeaderFg = "#e3f2fd",
            TitleColor = "#0d47a1",
            SafeColor = "#1a237e",
            FavoriteStar = "#ffd600",
            FavoriteBorder = "#90caf9",
            FavoriteBg = "#e3f2fd",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#bbdefb",
            BodyBg = "#f5f9ff",
            CellPattern = "radial-gradient(circle, #e3f2fd 2px, transparent 2px)",
            SuggestedMonth = 12,
        },

        // ── Fun ──
        new CalendarTheme
        {
            Name = "Cardinal",
            Emoji = "🐦",
            Category = "Fun",
            HeaderBg = "#b71c1c",
            HeaderFg = "#ffffff",
            TitleColor = "#b71c1c",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#ef5350",
            FavoriteBg = "#ffebee",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#ef9a9a",
            BodyBg = "#fff8f0",
            CellPattern = "radial-gradient(circle, #ffebee 1px, transparent 1px)",
        },
        new CalendarTheme
        {
            Name = "Blue Jay",
            Emoji = "🐦\u200d⬛",
            Category = "Fun",
            HeaderBg = "#1565c0",
            HeaderFg = "#ffffff",
            TitleColor = "#0d47a1",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#42a5f5",
            FavoriteBg = "#e3f2fd",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#90caf9",
            BodyBg = "#f0f9ff",
            CellPattern = "radial-gradient(circle, #e3f2fd 1px, transparent 1px)",
        },
        new CalendarTheme
        {
            Name = "Cats",
            Emoji = "🐱",
            Category = "Fun",
            HeaderBg = "#e65100",
            HeaderFg = "#ffffff",
            TitleColor = "#bf360c",
            SafeColor = "#33691e",
            FavoriteStar = "#ff8f00",
            FavoriteBorder = "#ffb74d",
            FavoriteBg = "#fff3e0",
            HomeBadgeBg = "#c62828",
            AccentBorder = "#ffcc80",
            BodyBg = "#fff8f0",
        },
        new CalendarTheme
        {
            Name = "Dinosaurs",
            Emoji = "🦕",
            Category = "Fun",
            HeaderBg = "#33691e",
            HeaderFg = "#ffffff",
            TitleColor = "#33691e",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff6d00",
            FavoriteBorder = "#8bc34a",
            FavoriteBg = "#f1f8e9",
            HomeBadgeBg = "#bf360c",
            AccentBorder = "#a5d6a7",
            BodyBg = "#f9fbe7",
        },
        new CalendarTheme
        {
            Name = "Princess",
            Emoji = "👑",
            Category = "Fun",
            HeaderBg = "#7b1fa2",
            HeaderFg = "#ffffff",
            TitleColor = "#6a1b9a",
            SafeColor = "#4a148c",
            FavoriteStar = "#ff6ff2",
            FavoriteBorder = "#ce93d8",
            FavoriteBg = "#f3e5f5",
            HomeBadgeBg = "#ab47bc",
            AccentBorder = "#e1bee7",
            BodyBg = "#fdf2ff",
            CellPattern = "radial-gradient(circle, #f3e5f5 1px, transparent 1px)",
        },
        new CalendarTheme
        {
            Name = "Robots",
            Emoji = "🤖",
            Category = "Fun",
            HeaderBg = "#455a64",
            HeaderFg = "#00e5ff",
            TitleColor = "#37474f",
            SafeColor = "#004d40",
            FavoriteStar = "#ff6d00",
            FavoriteBorder = "#00bcd4",
            FavoriteBg = "#e0f7fa",
            HomeBadgeBg = "#bf360c",
            AccentBorder = "#90a4ae",
            BodyBg = "#eceff1",
            CellPattern = "linear-gradient(90deg, #eceff1 1px, transparent 1px), linear-gradient(#eceff1 1px, transparent 1px)",
        },
        new CalendarTheme
        {
            Name = "Unicorn",
            Emoji = "🦄",
            Category = "Fun",
            HeaderBg = "#7b1fa2",
            HeaderFg = "#ffffff",
            TitleColor = "#6a1b9a",
            SafeColor = "#1b5e20",
            FavoriteStar = "#ff6ff2",
            FavoriteBorder = "#ce93d8",
            FavoriteBg = "#f3e5f5",
            HomeBadgeBg = "#e91e63",
            AccentBorder = "#e1bee7",
            BodyBg = "#fce4ec",
            CellPattern = "linear-gradient(135deg, #f3e5f5 0%, #e1f5fe 25%, #fff9c4 50%, #fce4ec 75%, #e8f5e9 100%)",
        },

        // ── Basic ──
        new CalendarTheme
        {
            Name = "Default",
            Emoji = "📅",
            Category = "Basic",
            HeaderBg = "#343a40",
            HeaderFg = "#ffffff",
            TitleColor = "#212529",
            SafeColor = "#155724",
            FavoriteStar = "#ff8c00",
            FavoriteBorder = "#ff8c00",
            FavoriteBg = "#fff8f0",
            HomeBadgeBg = "#dc3545",
            AccentBorder = "#dee2e6",
            BodyBg = "#ffffff",
        },
    ];
}
