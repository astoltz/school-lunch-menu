import SwiftUI

/// About window showing app information and version details.
struct AboutView: View {
    var body: some View {
        VStack(spacing: 20) {
            // App icon
            Image(nsImage: NSApp.applicationIconImage)
                .resizable()
                .frame(width: 128, height: 128)

            // App name
            Text("School Lunch Menu")
                .font(.title)
                .fontWeight(.bold)

            // Version
            Text(BuildInfo.displayVersion)
                .font(.title3)
                .foregroundColor(.secondary)

            Divider()
                .frame(width: 200)

            // Build details
            VStack(alignment: .leading, spacing: 4) {
                buildInfoRow("Version", BuildInfo.version)
                buildInfoRow("Build", BuildInfo.buildNumber)
                buildInfoRow("Commit", BuildInfo.gitCommit)
                buildInfoRow("Branch", BuildInfo.gitBranch)
                buildInfoRow("Built", formattedBuildDate)
            }
            .font(.system(.body, design: .monospaced))

            Divider()
                .frame(width: 200)

            // Credits
            VStack(spacing: 6) {
                Text("Credits")
                    .font(.headline)

                Link("Menu data from LINQ Connect",
                     destination: URL(string: "https://linqconnect.com")!)
                    .font(.caption)

                Link("Day labels from ISD 194 CMS Calendar",
                     destination: URL(string: "https://cms.isd194.org/news-and-events/calendar")!)
                    .font(.caption)

                Link("GitHub Project Page",
                     destination: URL(string: "https://github.com/astoltz/school-lunch-menu")!)
                    .font(.caption)

                Text("Licensed under the MIT License")
                    .font(.caption2)
                    .foregroundColor(.secondary)
                    .padding(.top, 2)
            }

            Divider()
                .frame(width: 200)

            // Description
            Text("Generate printable school lunch calendars\nwith allergen filtering and favorites.")
                .multilineTextAlignment(.center)
                .foregroundColor(.secondary)
                .font(.caption)

            // Copyright
            Text("\u{00A9} 2024-2026 astoltz")
                .font(.caption2)
                .foregroundColor(.secondary)
                .padding(.top, 10)
        }
        .padding(30)
        .frame(width: 350)
    }

    private func buildInfoRow(_ label: String, _ value: String) -> some View {
        HStack {
            Text("\(label):")
                .foregroundColor(.secondary)
                .frame(width: 60, alignment: .trailing)
            Text(value)
                .textSelection(.enabled)
            Spacer()
        }
    }

    private var formattedBuildDate: String {
        let formatter = ISO8601DateFormatter()
        if let date = formatter.date(from: BuildInfo.buildDate) {
            let displayFormatter = DateFormatter()
            displayFormatter.dateStyle = .medium
            displayFormatter.timeStyle = .short
            return displayFormatter.string(from: date)
        }
        return BuildInfo.buildDate
    }
}

#Preview {
    AboutView()
}
