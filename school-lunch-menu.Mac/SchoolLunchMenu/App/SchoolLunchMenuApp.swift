import SwiftUI

/// Main entry point for the School Lunch Menu macOS app.
@main
struct SchoolLunchMenuApp: App {
    var body: some Scene {
        WindowGroup {
            MainView()
        }
        .windowStyle(.automatic)
        .defaultSize(width: 1100, height: 800)
        .commands {
            // App menu - About
            CommandGroup(replacing: .appInfo) {
                AboutButton()
            }

            // File menu commands
            CommandGroup(replacing: .newItem) {
                OpenHarFileButton()
            }

            // View menu commands
            CommandGroup(after: .toolbar) {
                Button("Zoom In") {
                    NotificationCenter.default.post(name: .zoomIn, object: nil)
                }
                .keyboardShortcut("+", modifiers: .command)

                Button("Zoom Out") {
                    NotificationCenter.default.post(name: .zoomOut, object: nil)
                }
                .keyboardShortcut("-", modifiers: .command)

                Button("Reset Zoom") {
                    NotificationCenter.default.post(name: .resetZoom, object: nil)
                }
                .keyboardShortcut("0", modifiers: .command)
            }
        }

        // About window
        Window("About School Lunch Menu", id: "about") {
            AboutView()
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
    }
}

/// Button that opens the About window using SwiftUI's openWindow environment.
struct AboutButton: View {
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        Button("About School Lunch Menu") {
            openWindow(id: "about")
        }
    }
}

/// Button that opens a HAR file using NSOpenPanel.
struct OpenHarFileButton: View {
    var body: some View {
        Button("Open HAR File...") {
            let panel = NSOpenPanel()
            panel.allowedContentTypes = [.init(filenameExtension: "har")!]
            panel.allowsMultipleSelection = false
            panel.canChooseDirectories = false
            panel.message = "Select a HAR file containing LINQ Connect API responses"
            panel.prompt = "Open"

            if panel.runModal() == .OK, let url = panel.url {
                NotificationCenter.default.post(name: .openHarFile, object: url)
            }
        }
        .keyboardShortcut("o", modifiers: .command)
    }
}

// MARK: - Notification Names for menu commands

extension Notification.Name {
    static let openHarFile = Notification.Name("openHarFile")
    static let zoomIn = Notification.Name("zoomIn")
    static let zoomOut = Notification.Name("zoomOut")
    static let resetZoom = Notification.Name("resetZoom")
}
