import Foundation

// MARK: - CalendarTheme

/// Defines the visual theme for a generated HTML calendar.
struct CalendarTheme {
    /// Theme display name (e.g., "Valentines", "Dinosaurs").
    let name: String

    /// Emoji used to decorate the calendar title.
    let emoji: String

    /// Category for grouping in the theme dropdown ("Seasonal", "Fun", "Basic").
    let category: String

    /// Table header background color.
    let headerBg: String

    /// Table header text color.
    let headerFg: String

    /// Title (h1) color.
    let titleColor: String

    /// Safe item text color.
    let safeColor: String

    /// Favorite star color.
    let favoriteStar: String

    /// Favorite day border color.
    let favoriteBorder: String

    /// Favorite day background color.
    let favoriteBg: String

    /// "From Home" badge background color.
    let homeBadgeBg: String

    /// Cell border accent color.
    let accentBorder: String

    /// Body/page background color.
    let bodyBg: String

    /// Optional CSS background pattern for cells.
    let cellPattern: String?

    /// Auto-suggest this theme for the given month (1-12), or nil for no auto-suggestion.
    let suggestedMonth: Int?

    /// Secondary suggested month, for themes spanning two months.
    let suggestedMonth2: Int?

    init(
        name: String,
        emoji: String,
        category: String,
        headerBg: String,
        headerFg: String,
        titleColor: String,
        safeColor: String,
        favoriteStar: String,
        favoriteBorder: String,
        favoriteBg: String,
        homeBadgeBg: String,
        accentBorder: String,
        bodyBg: String,
        cellPattern: String? = nil,
        suggestedMonth: Int? = nil,
        suggestedMonth2: Int? = nil
    ) {
        self.name = name
        self.emoji = emoji
        self.category = category
        self.headerBg = headerBg
        self.headerFg = headerFg
        self.titleColor = titleColor
        self.safeColor = safeColor
        self.favoriteStar = favoriteStar
        self.favoriteBorder = favoriteBorder
        self.favoriteBg = favoriteBg
        self.homeBadgeBg = homeBadgeBg
        self.accentBorder = accentBorder
        self.bodyBg = bodyBg
        self.cellPattern = cellPattern
        self.suggestedMonth = suggestedMonth
        self.suggestedMonth2 = suggestedMonth2
    }
}

// MARK: - CalendarThemes

/// Built-in calendar themes grouped by category.
/// Ordered so that auto-suggest picks the most specific holiday theme first.
enum CalendarThemes {
    static let all: [CalendarTheme] = [
        // MARK: - Seasonal
        CalendarTheme(
            name: "New Year",
            emoji: "\u{1F386}", // 🎆
            category: "Seasonal",
            headerBg: "#212121",
            headerFg: "#ffd700",
            titleColor: "#212121",
            safeColor: "#1b5e20",
            favoriteStar: "#ffd700",
            favoriteBorder: "#ffd700",
            favoriteBg: "#fffde7",
            homeBadgeBg: "#c62828",
            accentBorder: "#bdbdbd",
            bodyBg: "#fafafa",
            cellPattern: "radial-gradient(circle, #fffde7 1px, transparent 1px)",
            suggestedMonth: 1
        ),
        CalendarTheme(
            name: "Valentines",
            emoji: "\u{1F495}", // 💕
            category: "Seasonal",
            headerBg: "#c2185b",
            headerFg: "#ffffff",
            titleColor: "#ad1457",
            safeColor: "#880e4f",
            favoriteStar: "#e91e63",
            favoriteBorder: "#f06292",
            favoriteBg: "#fce4ec",
            homeBadgeBg: "#d32f2f",
            accentBorder: "#f8bbd0",
            bodyBg: "#fff0f3",
            cellPattern: "radial-gradient(circle, #fce4ec 1px, transparent 1px)",
            suggestedMonth: 2
        ),
        CalendarTheme(
            name: "St. Patrick's",
            emoji: "\u{2618}\u{FE0F}", // ☘️
            category: "Seasonal",
            headerBg: "#2e7d32",
            headerFg: "#ffffff",
            titleColor: "#1b5e20",
            safeColor: "#1b5e20",
            favoriteStar: "#ffd700",
            favoriteBorder: "#66bb6a",
            favoriteBg: "#e8f5e9",
            homeBadgeBg: "#c62828",
            accentBorder: "#a5d6a7",
            bodyBg: "#f1f8e9",
            cellPattern: "radial-gradient(circle, #e8f5e9 1px, transparent 1px)",
            suggestedMonth: 3
        ),
        CalendarTheme(
            name: "Easter",
            emoji: "\u{1F423}", // 🐣
            category: "Seasonal",
            headerBg: "#ec407a",
            headerFg: "#ffffff",
            titleColor: "#ad1457",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#f48fb1",
            favoriteBg: "#fce4ec",
            homeBadgeBg: "#7b1fa2",
            accentBorder: "#f8bbd0",
            bodyBg: "#fff8e1",
            cellPattern: "radial-gradient(circle, #e1f5fe 1px, transparent 1px)",
            suggestedMonth: 4
        ),
        CalendarTheme(
            name: "Spring",
            emoji: "\u{1F338}", // 🌸
            category: "Seasonal",
            headerBg: "#2e7d32",
            headerFg: "#ffffff",
            titleColor: "#1b5e20",
            safeColor: "#1b5e20",
            favoriteStar: "#ff6d00",
            favoriteBorder: "#81c784",
            favoriteBg: "#e8f5e9",
            homeBadgeBg: "#c62828",
            accentBorder: "#c8e6c9",
            bodyBg: "#f1f8e9",
            suggestedMonth2: 5
        ),
        CalendarTheme(
            name: "Summer",
            emoji: "\u{2600}\u{FE0F}", // ☀️
            category: "Seasonal",
            headerBg: "#0277bd",
            headerFg: "#ffffff",
            titleColor: "#01579b",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#4fc3f7",
            favoriteBg: "#e1f5fe",
            homeBadgeBg: "#d84315",
            accentBorder: "#b3e5fc",
            bodyBg: "#fffde7",
            cellPattern: "radial-gradient(circle, #fff9c4 1px, transparent 1px)",
            suggestedMonth: 6
        ),
        CalendarTheme(
            name: "Fourth of July",
            emoji: "\u{1F386}", // 🎆
            category: "Seasonal",
            headerBg: "#1565c0",
            headerFg: "#ffffff",
            titleColor: "#b71c1c",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#ef5350",
            favoriteBg: "#ffebee",
            homeBadgeBg: "#b71c1c",
            accentBorder: "#bbdefb",
            bodyBg: "#f5f5f5",
            cellPattern: "radial-gradient(circle, #e3f2fd 1px, transparent 1px)",
            suggestedMonth: 7
        ),
        CalendarTheme(
            name: "Back to School",
            emoji: "\u{1F392}", // 🎒
            category: "Seasonal",
            headerBg: "#1a237e",
            headerFg: "#ffeb3b",
            titleColor: "#1a237e",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#5c6bc0",
            favoriteBg: "#e8eaf6",
            homeBadgeBg: "#c62828",
            accentBorder: "#9fa8da",
            bodyBg: "#fffde7",
            suggestedMonth: 8
        ),
        CalendarTheme(
            name: "Fall",
            emoji: "\u{1F342}", // 🍂
            category: "Seasonal",
            headerBg: "#4e342e",
            headerFg: "#ffcc80",
            titleColor: "#3e2723",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#a1887f",
            favoriteBg: "#efebe9",
            homeBadgeBg: "#bf360c",
            accentBorder: "#bcaaa4",
            bodyBg: "#fff3e0",
            cellPattern: "radial-gradient(circle, #efebe9 1px, transparent 1px)",
            suggestedMonth: 9
        ),
        CalendarTheme(
            name: "Spooky",
            emoji: "\u{1F383}", // 🎃
            category: "Seasonal",
            headerBg: "#212121",
            headerFg: "#ff6d00",
            titleColor: "#e65100",
            safeColor: "#1b5e20",
            favoriteStar: "#ff6d00",
            favoriteBorder: "#ff9800",
            favoriteBg: "#fff3e0",
            homeBadgeBg: "#6a1b9a",
            accentBorder: "#424242",
            bodyBg: "#1a1a2e",
            cellPattern: "radial-gradient(circle, #2d2d44 1px, transparent 1px)",
            suggestedMonth: 10
        ),
        CalendarTheme(
            name: "Thanksgiving",
            emoji: "\u{1F983}", // 🦃
            category: "Seasonal",
            headerBg: "#4e342e",
            headerFg: "#ffe0b2",
            titleColor: "#3e2723",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#a1887f",
            favoriteBg: "#efebe9",
            homeBadgeBg: "#bf360c",
            accentBorder: "#d7ccc8",
            bodyBg: "#fff8e1",
            cellPattern: "radial-gradient(circle, #fff3e0 1px, transparent 1px)",
            suggestedMonth: 11
        ),
        CalendarTheme(
            name: "Winter",
            emoji: "\u{2744}\u{FE0F}", // ❄️
            category: "Seasonal",
            headerBg: "#1565c0",
            headerFg: "#e3f2fd",
            titleColor: "#0d47a1",
            safeColor: "#1a237e",
            favoriteStar: "#ffd600",
            favoriteBorder: "#90caf9",
            favoriteBg: "#e3f2fd",
            homeBadgeBg: "#c62828",
            accentBorder: "#bbdefb",
            bodyBg: "#f5f9ff",
            cellPattern: "radial-gradient(circle, #e3f2fd 2px, transparent 2px)",
            suggestedMonth: 12
        ),

        // MARK: - Fun
        CalendarTheme(
            name: "Cardinal",
            emoji: "\u{1F426}", // 🐦
            category: "Fun",
            headerBg: "#b71c1c",
            headerFg: "#ffffff",
            titleColor: "#b71c1c",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#ef5350",
            favoriteBg: "#ffebee",
            homeBadgeBg: "#c62828",
            accentBorder: "#ef9a9a",
            bodyBg: "#fff8f0",
            cellPattern: "radial-gradient(circle, #ffebee 1px, transparent 1px)"
        ),
        CalendarTheme(
            name: "Blue Jay",
            emoji: "\u{1F426}\u{200D}\u{2B1B}", // 🐦‍⬛
            category: "Fun",
            headerBg: "#1565c0",
            headerFg: "#ffffff",
            titleColor: "#0d47a1",
            safeColor: "#1b5e20",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#42a5f5",
            favoriteBg: "#e3f2fd",
            homeBadgeBg: "#c62828",
            accentBorder: "#90caf9",
            bodyBg: "#f0f9ff",
            cellPattern: "radial-gradient(circle, #e3f2fd 1px, transparent 1px)"
        ),
        CalendarTheme(
            name: "Cats",
            emoji: "\u{1F431}", // 🐱
            category: "Fun",
            headerBg: "#e65100",
            headerFg: "#ffffff",
            titleColor: "#bf360c",
            safeColor: "#33691e",
            favoriteStar: "#ff8f00",
            favoriteBorder: "#ffb74d",
            favoriteBg: "#fff3e0",
            homeBadgeBg: "#c62828",
            accentBorder: "#ffcc80",
            bodyBg: "#fff8f0"
        ),
        CalendarTheme(
            name: "Dinosaurs",
            emoji: "\u{1F995}", // 🦕
            category: "Fun",
            headerBg: "#33691e",
            headerFg: "#ffffff",
            titleColor: "#33691e",
            safeColor: "#1b5e20",
            favoriteStar: "#ff6d00",
            favoriteBorder: "#8bc34a",
            favoriteBg: "#f1f8e9",
            homeBadgeBg: "#bf360c",
            accentBorder: "#a5d6a7",
            bodyBg: "#f9fbe7"
        ),
        CalendarTheme(
            name: "Princess",
            emoji: "\u{1F451}", // 👑
            category: "Fun",
            headerBg: "#7b1fa2",
            headerFg: "#ffffff",
            titleColor: "#6a1b9a",
            safeColor: "#4a148c",
            favoriteStar: "#ff6ff2",
            favoriteBorder: "#ce93d8",
            favoriteBg: "#f3e5f5",
            homeBadgeBg: "#ab47bc",
            accentBorder: "#e1bee7",
            bodyBg: "#fdf2ff",
            cellPattern: "radial-gradient(circle, #f3e5f5 1px, transparent 1px)"
        ),
        CalendarTheme(
            name: "Robots",
            emoji: "\u{1F916}", // 🤖
            category: "Fun",
            headerBg: "#455a64",
            headerFg: "#00e5ff",
            titleColor: "#37474f",
            safeColor: "#004d40",
            favoriteStar: "#ff6d00",
            favoriteBorder: "#00bcd4",
            favoriteBg: "#e0f7fa",
            homeBadgeBg: "#bf360c",
            accentBorder: "#90a4ae",
            bodyBg: "#eceff1",
            cellPattern: "linear-gradient(90deg, #eceff1 1px, transparent 1px), linear-gradient(#eceff1 1px, transparent 1px)"
        ),
        CalendarTheme(
            name: "Unicorn",
            emoji: "\u{1F984}", // 🦄
            category: "Fun",
            headerBg: "#7b1fa2",
            headerFg: "#ffffff",
            titleColor: "#6a1b9a",
            safeColor: "#1b5e20",
            favoriteStar: "#ff6ff2",
            favoriteBorder: "#ce93d8",
            favoriteBg: "#f3e5f5",
            homeBadgeBg: "#e91e63",
            accentBorder: "#e1bee7",
            bodyBg: "#fce4ec",
            cellPattern: "linear-gradient(135deg, #f3e5f5 0%, #e1f5fe 25%, #fff9c4 50%, #fce4ec 75%, #e8f5e9 100%)"
        ),

        // MARK: - Basic
        CalendarTheme(
            name: "Default",
            emoji: "\u{1F4C5}", // 📅
            category: "Basic",
            headerBg: "#343a40",
            headerFg: "#ffffff",
            titleColor: "#212529",
            safeColor: "#155724",
            favoriteStar: "#ff8c00",
            favoriteBorder: "#ff8c00",
            favoriteBg: "#fff8f0",
            homeBadgeBg: "#dc3545",
            accentBorder: "#dee2e6",
            bodyBg: "#ffffff"
        )
    ]
}
