import Foundation

/// Build information injected by CI/CD pipeline.
/// These values are replaced during the build process.
enum BuildInfo {
    /// Application version (e.g., "1.0.0")
    static let version = "1.0.0"

    /// Git commit SHA (short)
    static let gitCommit = "dev"

    /// Git branch name
    static let gitBranch = "local"

    /// Build date (ISO 8601)
    static let buildDate = "2024-01-01T00:00:00Z"

    /// Build number from CI
    static let buildNumber = "0"

    /// Combined version string for display
    static var displayVersion: String {
        if gitCommit == "dev" {
            return "\(version) (Development)"
        }
        return "\(version) (\(gitCommit))"
    }

    /// Full build info for About dialog
    static var fullBuildInfo: String {
        """
        Version: \(version)
        Build: \(buildNumber)
        Commit: \(gitCommit)
        Branch: \(gitBranch)
        Built: \(buildDate)
        """
    }
}
