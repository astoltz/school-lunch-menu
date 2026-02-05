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

            // Description
            Text("Generate printable school lunch calendars\nwith allergen filtering and favorites.")
                .multilineTextAlignment(.center)
                .foregroundColor(.secondary)
                .font(.caption)

            Spacer()

            // Copyright
            Text("© 2024-2025")
                .font(.caption2)
                .foregroundColor(.secondary)
        }
        .padding(30)
        .frame(width: 350, height: 450)
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
