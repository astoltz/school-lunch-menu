import SwiftUI

// MARK: - SidebarView

/// All settings controls in a scrollable sidebar.
struct SidebarView: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                SchoolLookupSection(viewModel: viewModel)

                Divider()

                MonthYearSection(viewModel: viewModel)

                if viewModel.hasSessions {
                    Divider()
                    SessionSection(viewModel: viewModel)
                }

                Divider()

                ThemeSection(viewModel: viewModel)

                Divider()

                AllergenSection(viewModel: viewModel)

                if viewModel.hasRecipes {
                    Divider()
                    NotPreferredSection(viewModel: viewModel)

                    Divider()
                    FavoritesSection(viewModel: viewModel)
                }

                if viewModel.hasSessions {
                    Divider()
                    FromHomeDaysSection(viewModel: viewModel)
                }

                Divider()

                HolidayIconsSection(viewModel: viewModel)

                Divider()

                DayLayoutSection(viewModel: viewModel)

                Divider()

                DayLabelsSection(viewModel: viewModel)

                Divider()

                ActionButtonsSection(viewModel: viewModel)
            }
            .padding()
        }
        .background(Color(nsColor: .windowBackgroundColor))
    }
}

// MARK: - SchoolLookupSection

struct SchoolLookupSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("School Lookup")
                .font(.headline)

            HStack {
                TextField("Identifier Code", text: $viewModel.identifierCode)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isBusy)

                Button("Look Up") {
                    Task {
                        await viewModel.lookupIdentifier()
                    }
                }
                .disabled(viewModel.isBusy || viewModel.identifierCode.isEmpty)
            }

            if let districtName = viewModel.districtName {
                Text(districtName)
                    .font(.subheadline)
                    .foregroundColor(.secondary)
            }

            if viewModel.hasBuildings {
                buildingPicker
            }

            Text("Enter the code from your school's LINQ Connect page.")
                .font(.caption)
                .foregroundColor(.secondary)
        }
    }

    private var buildingPicker: some View {
        Picker("Building", selection: Binding(
            get: { viewModel.selectedBuilding?.buildingId },
            set: { newId in
                viewModel.selectedBuilding = viewModel.availableBuildings.first { $0.buildingId == newId }
            }
        )) {
            Text("All Buildings").tag(nil as String?)
            ForEach(viewModel.availableBuildings, id: \.buildingId) { building in
                Text(building.name).tag(building.buildingId as String?)
            }
        }
        .disabled(viewModel.isBusy)
    }
}

// MARK: - MonthYearSection

struct MonthYearSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Month & Year")
                .font(.headline)

            HStack {
                Picker("Month", selection: $viewModel.selectedMonth) {
                    ForEach(MainViewModel.monthOptions, id: \.id) { option in
                        Text(option.name).tag(option.id)
                    }
                }
                .labelsHidden()
                .disabled(viewModel.isBusy)

                Picker("Year", selection: $viewModel.selectedYear) {
                    ForEach(viewModel.availableYears, id: \.self) { year in
                        Text(String(year)).tag(year)
                    }
                }
                .labelsHidden()
                .disabled(viewModel.isBusy)
            }
        }
    }
}

// MARK: - SessionSection

struct SessionSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Session")
                .font(.headline)

            Picker("Session", selection: Binding(
                get: { viewModel.selectedSession ?? "" },
                set: { viewModel.selectedSession = $0.isEmpty ? nil : $0 }
            )) {
                ForEach(viewModel.availableSessions, id: \.self) { session in
                    Text(session).tag(session)
                }
            }
            .labelsHidden()
            .disabled(viewModel.isBusy)

            Text("Select the meal session to display.")
                .font(.caption)
                .foregroundColor(.secondary)
        }
    }
}

// MARK: - ThemeSection

struct ThemeSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Theme")
                .font(.headline)

            themePicker
        }
    }

    private var themePicker: some View {
        Picker("Theme", selection: Binding(
            get: { viewModel.selectedTheme.name },
            set: { newName in
                if let item = viewModel.themeListItems.first(where: { $0.theme?.name == newName }) {
                    viewModel.selectedThemeItem = item
                }
            }
        )) {
            ForEach(viewModel.themeListItems.filter { !$0.isHeader }, id: \.id) { item in
                if let theme = item.theme {
                    Text("\(theme.emoji) \(theme.name)").tag(theme.name)
                }
            }
        }
        .labelsHidden()
        .disabled(viewModel.isBusy)
    }
}

// MARK: - AllergenSection

struct AllergenSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            DisclosureGroup(isExpanded: $viewModel.isAllergenExpanded) {
                allergenToggles
            } label: {
                allergenHeader
            }
        }
    }

    private var allergenToggles: some View {
        VStack(alignment: .leading, spacing: 4) {
            ForEach(viewModel.availableAllergens) { allergen in
                Toggle(isOn: Binding(
                    get: { allergen.isSelected },
                    set: { newValue in
                        if let index = viewModel.availableAllergens.firstIndex(where: { $0.id == allergen.id }) {
                            viewModel.availableAllergens[index].isSelected = newValue
                        }
                    }
                )) {
                    Text(allergen.name)
                }
                .toggleStyle(.checkbox)
                .disabled(viewModel.isBusy)
            }
        }
        .padding(.top, 4)
    }

    private var allergenHeader: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text("Allergen Filter")
                .font(.headline)

            if !viewModel.isAllergenExpanded {
                Text(viewModel.allergenSummary)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
    }
}

// MARK: - NotPreferredSection

struct NotPreferredSection: View {
    @ObservedObject var viewModel: MainViewModel
    @State private var isExpanded = false

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            DisclosureGroup(isExpanded: $isExpanded) {
                recipeList
            } label: {
                sectionHeader
            }
        }
    }

    private var recipeList: some View {
        VStack(alignment: .leading, spacing: 8) {
            TextField("Search recipes...", text: $viewModel.recipeSearchText)
                .textFieldStyle(.roundedBorder)

            ScrollView {
                VStack(alignment: .leading, spacing: 4) {
                    ForEach(viewModel.filteredRecipes) { recipe in
                        Toggle(isOn: Binding(
                            get: { recipe.isNotPreferred },
                            set: { newValue in
                                if let index = viewModel.filteredRecipes.firstIndex(where: { $0.id == recipe.id }) {
                                    viewModel.filteredRecipes[index].isNotPreferred = newValue
                                }
                            }
                        )) {
                            Text(recipe.name)
                                .lineLimit(1)
                        }
                        .toggleStyle(.checkbox)
                        .disabled(viewModel.isBusy)
                    }
                }
            }
            .frame(maxHeight: 150)
        }
        .padding(.top, 4)
    }

    private var sectionHeader: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text("Not Preferred Foods")
                .font(.headline)

            if !isExpanded {
                let count = viewModel.filteredRecipes.filter(\.isNotPreferred).count
                if count > 0 {
                    Text("\(count) items marked")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }
        }
    }
}

// MARK: - FavoritesSection

struct FavoritesSection: View {
    @ObservedObject var viewModel: MainViewModel
    @State private var isExpanded = false

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            DisclosureGroup(isExpanded: $isExpanded) {
                favoriteList
            } label: {
                sectionHeader
            }
        }
    }

    private var favoriteList: some View {
        VStack(alignment: .leading, spacing: 8) {
            TextField("Search recipes...", text: $viewModel.favoriteSearchText)
                .textFieldStyle(.roundedBorder)

            ScrollView {
                VStack(alignment: .leading, spacing: 4) {
                    ForEach(viewModel.filteredFavorites) { favorite in
                        Toggle(isOn: Binding(
                            get: { favorite.isFavorite },
                            set: { newValue in
                                if let index = viewModel.filteredFavorites.firstIndex(where: { $0.id == favorite.id }) {
                                    viewModel.filteredFavorites[index].isFavorite = newValue
                                }
                            }
                        )) {
                            Text(favorite.name)
                                .lineLimit(1)
                        }
                        .toggleStyle(.checkbox)
                        .disabled(viewModel.isBusy)
                    }
                }
            }
            .frame(maxHeight: 150)
        }
        .padding(.top, 4)
    }

    private var sectionHeader: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text("Favorites")
                .font(.headline)

            if !isExpanded {
                let count = viewModel.filteredFavorites.filter(\.isFavorite).count
                if count > 0 {
                    Text("\(count) favorites")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }
        }
    }
}

// MARK: - FromHomeDaysSection

struct FromHomeDaysSection: View {
    @ObservedObject var viewModel: MainViewModel

    private let weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri"]

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("From Home Days")
                .font(.headline)

            HStack(spacing: 16) {
                ForEach(Array(weekdays.enumerated()), id: \.offset) { index, dayName in
                    let dayOfWeek = index + 2 // 2=Monday, 3=Tuesday, etc.
                    VStack(spacing: 2) {
                        Text(dayName)
                            .font(.caption)
                            .foregroundColor(.primary)

                        Toggle("", isOn: Binding(
                            get: {
                                viewModel.forcedHomeDays.first { $0.dayOfWeek == dayOfWeek }?.isForced ?? false
                            },
                            set: { newValue in
                                if let index = viewModel.forcedHomeDays.firstIndex(where: { $0.dayOfWeek == dayOfWeek }) {
                                    viewModel.forcedHomeDays[index].isForced = newValue
                                }
                            }
                        ))
                        .toggleStyle(.checkbox)
                        .labelsHidden()
                        .disabled(viewModel.isBusy)
                    }
                }
            }

            Text("Days when your child brings food from home.")
                .font(.caption)
                .foregroundColor(.secondary)
        }
    }
}

// MARK: - HolidayIconsSection

struct HolidayIconsSection: View {
    @ObservedObject var viewModel: MainViewModel
    @State private var isExpanded = false

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            DisclosureGroup(isExpanded: $isExpanded) {
                holidayEntries
            } label: {
                sectionHeader
            }
        }
    }

    private var holidayEntries: some View {
        VStack(alignment: .leading, spacing: 8) {
            ForEach(viewModel.holidayOverrideEntries.indices, id: \.self) { index in
                HStack {
                    TextField("Keyword", text: $viewModel.holidayOverrideEntries[index].keyword)
                        .textFieldStyle(.roundedBorder)
                        .frame(width: 100)

                    TextField("Emoji", text: $viewModel.holidayOverrideEntries[index].emoji)
                        .textFieldStyle(.roundedBorder)
                        .frame(width: 50)

                    Button(action: {
                        viewModel.removeHolidayOverride(viewModel.holidayOverrideEntries[index])
                    }) {
                        Image(systemName: "minus.circle.fill")
                            .foregroundColor(.red)
                    }
                    .buttonStyle(.borderless)
                    .disabled(viewModel.isBusy)
                }
            }

            Button(action: {
                viewModel.addHolidayOverride()
            }) {
                Label("Add Holiday", systemImage: "plus.circle.fill")
            }
            .buttonStyle(.borderless)
            .disabled(viewModel.isBusy)
        }
        .padding(.top, 4)
    }

    private var sectionHeader: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text("Holiday Icons")
                .font(.headline)

            if !isExpanded {
                Text("\(viewModel.holidayOverrideEntries.count) custom icons")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
    }
}

// MARK: - DayLayoutSection

struct DayLayoutSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Day Layout")
                .font(.headline)

            Picker("Layout Mode", selection: $viewModel.selectedLayoutMode) {
                ForEach(MainViewModel.layoutModeOptions, id: \.id) { option in
                    Text(option.displayText).tag(option.value)
                }
            }
            .pickerStyle(.segmented)
            .disabled(viewModel.isBusy)

            Toggle("Show unsafe lines", isOn: $viewModel.showUnsafeLines)
                .disabled(viewModel.isBusy)

            if viewModel.showUnsafeLines {
                TextField("Unsafe message", text: $viewModel.unsafeLineMessage)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isBusy)
            }

            if viewModel.isGridMode && !viewModel.planLabelEntries.isEmpty {
                planLabelsSection
            }

            Divider()
                .padding(.vertical, 4)

            Toggle("Cross out past days", isOn: $viewModel.crossOutPastDays)
                .disabled(viewModel.isBusy)

            Toggle("Show share footer", isOn: $viewModel.showShareFooter)
                .disabled(viewModel.isBusy)
        }
    }

    private var planLabelsSection: some View {
        VStack(alignment: .leading, spacing: 8) {
            Divider()
                .padding(.vertical, 4)

            Text("Plan Labels")
                .font(.subheadline)
                .foregroundColor(.secondary)

            ForEach(viewModel.planLabelEntries) { entry in
                HStack(spacing: 8) {
                    Text(entry.planName)
                        .font(.caption)
                        .lineLimit(1)
                        .frame(maxWidth: .infinity, alignment: .leading)

                    // Icon picker (emoji)
                    Menu {
                        ForEach(MainViewModel.planIconSuggestions, id: \.self) { icon in
                            Button(icon.isEmpty ? "None" : icon) {
                                entry.icon = icon
                            }
                        }
                    } label: {
                        Text(entry.icon.isEmpty ? "🔘" : entry.icon)
                            .frame(width: 30)
                    }
                    .menuStyle(.borderlessButton)
                    .frame(width: 40)
                    .disabled(viewModel.isBusy)

                    // Short label
                    TextField("Label", text: Binding(
                        get: { entry.shortLabel },
                        set: { entry.shortLabel = $0 }
                    ))
                    .textFieldStyle(.roundedBorder)
                    .frame(width: 60)
                    .disabled(viewModel.isBusy)
                }
            }
        }
    }
}

// MARK: - DayLabelsSection

struct DayLabelsSection: View {
    @ObservedObject var viewModel: MainViewModel
    @State private var isExpanded = false

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            DisclosureGroup(isExpanded: $isExpanded) {
                dayLabelControls
            } label: {
                sectionHeader
            }
        }
    }

    private var dayLabelControls: some View {
        VStack(alignment: .leading, spacing: 8) {
            Button(action: {
                Task {
                    await viewModel.fetchDayLabels()
                }
            }) {
                Label("Fetch from CMS", systemImage: "arrow.down.circle")
            }
            .buttonStyle(.bordered)
            .disabled(viewModel.isBusy)

            Picker("Corner", selection: $viewModel.dayLabelCorner) {
                ForEach(MainViewModel.dayLabelCornerOptions, id: \.self) { corner in
                    Text(corner.replacingOccurrences(of: "Top", with: "Top ").replacingOccurrences(of: "Bottom", with: "Bottom ")).tag(corner)
                }
            }
            .disabled(viewModel.isBusy)

            HStack {
                Text("Start Date")
                    .font(.caption)
                TextField("YYYY-MM-DD", text: $viewModel.dayLabelStartDate)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isBusy)
            }

            ForEach(viewModel.dayLabelEntries.indices, id: \.self) { index in
                dayLabelRow(index: index)
            }

            Button(action: {
                viewModel.addDayLabel()
            }) {
                Label("Add Label", systemImage: "plus.circle.fill")
            }
            .buttonStyle(.borderless)
            .disabled(viewModel.isBusy)
        }
        .padding(.top, 4)
    }

    private func dayLabelRow(index: Int) -> some View {
        HStack {
            TextField("Label", text: $viewModel.dayLabelEntries[index].label)
                .textFieldStyle(.roundedBorder)
                .frame(width: 80)
                .disabled(viewModel.isBusy)

            ColorPicker("", selection: Binding(
                get: { Color(hex: viewModel.dayLabelEntries[index].color) ?? .gray },
                set: { viewModel.dayLabelEntries[index].color = $0.hexString }
            ))
            .labelsHidden()
            .disabled(viewModel.isBusy)

            Button(action: {
                viewModel.removeDayLabel(viewModel.dayLabelEntries[index])
            }) {
                Image(systemName: "minus.circle.fill")
                    .foregroundColor(.red)
            }
            .buttonStyle(.borderless)
            .disabled(viewModel.isBusy || viewModel.dayLabelEntries.count <= 1)
        }
    }

    private var sectionHeader: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text("Day Labels")
                .font(.headline)

            if !isExpanded {
                Text(viewModel.dayLabelEntries.map(\.label).joined(separator: ", "))
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
        }
    }
}

// MARK: - ActionButtonsSection

struct ActionButtonsSection: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Actions")
                .font(.headline)

            Button(action: {
                Task {
                    await viewModel.fetchFromApi()
                }
            }) {
                HStack {
                    Image(systemName: "arrow.down.circle")
                    Text("Fetch from API")
                }
                .frame(maxWidth: .infinity)
            }
            .buttonStyle(.bordered)
            .disabled(viewModel.isBusy || viewModel.districtId == nil)

            Button(action: {
                viewModel.showHarFilePicker()
            }) {
                HStack {
                    Image(systemName: "doc.badge.arrow.up")
                    Text("Load from HAR File")
                }
                .frame(maxWidth: .infinity)
            }
            .buttonStyle(.bordered)
            .disabled(viewModel.isBusy)

            Button(action: {
                Task {
                    await viewModel.generateCalendar()
                }
            }) {
                HStack {
                    Image(systemName: "calendar.badge.plus")
                    Text("Generate Calendar")
                }
                .frame(maxWidth: .infinity)
            }
            .buttonStyle(.borderedProminent)
            .tint(.green)
            .disabled(viewModel.isBusy || !viewModel.canGenerate)

            Text("Generate creates an HTML calendar file.")
                .font(.caption)
                .foregroundColor(.secondary)
        }
    }
}

// MARK: - Color Extensions

extension Color {
    init?(hex: String) {
        var hexSanitized = hex.trimmingCharacters(in: .whitespacesAndNewlines)
        hexSanitized = hexSanitized.replacingOccurrences(of: "#", with: "")

        var rgb: UInt64 = 0
        guard Scanner(string: hexSanitized).scanHexInt64(&rgb) else { return nil }

        let r = Double((rgb & 0xFF0000) >> 16) / 255.0
        let g = Double((rgb & 0x00FF00) >> 8) / 255.0
        let b = Double(rgb & 0x0000FF) / 255.0

        self.init(red: r, green: g, blue: b)
    }

    var hexString: String {
        guard let components = NSColor(self).cgColor.components, components.count >= 3 else {
            return "#6c757d"
        }
        let r = Int(components[0] * 255)
        let g = Int(components[1] * 255)
        let b = Int(components[2] * 255)
        return String(format: "#%02X%02X%02X", r, g, b)
    }
}

// MARK: - Preview

#Preview {
    SidebarView(viewModel: MainViewModel())
        .frame(width: 300, height: 800)
}
