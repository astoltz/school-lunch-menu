import SwiftUI

// MARK: - MainView

/// The main app view with a horizontal split layout.
struct MainView: View {
    @StateObject private var viewModel = MainViewModel()

    var body: some View {
        HSplitView {
            // Left: Sidebar (300pt wide, scrollable)
            SidebarView(viewModel: viewModel)
                .frame(minWidth: 280, idealWidth: 300, maxWidth: 350)

            // Right: Preview area
            VStack(spacing: 0) {
                // Preview content
                PreviewView(html: viewModel.generatedHtml)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)

                // Status bar at bottom
                StatusBarView(viewModel: viewModel)
            }
        }
        .frame(minWidth: 850, minHeight: 500)
        .task {
            await viewModel.initialize(harFilePath: nil)
        }
        .onDrop(of: [.fileURL], isTargeted: nil) { providers in
            handleDrop(providers: providers)
        }
        .alert("API Fetch Failed", isPresented: $viewModel.showApiFetchFailedAlert) {
            if let sourceUrl = viewModel.sourceUrl, let url = URL(string: sourceUrl) {
                Button("Open in Browser") {
                    NSWorkspace.shared.open(url)
                }
                Button("Cancel", role: .cancel) {}
            } else {
                Button("OK", role: .cancel) {}
            }
        } message: {
            if viewModel.sourceUrl != nil {
                Text("The API request was blocked. You can open the LINQ Connect menu page in your browser and save a HAR file to load instead.")
            } else {
                Text("The API request was blocked. Try loading a HAR file instead.")
            }
        }
    }

    // MARK: - Drop Handling

    private func handleDrop(providers: [NSItemProvider]) -> Bool {
        guard let provider = providers.first else { return false }

        provider.loadItem(forTypeIdentifier: "public.file-url", options: nil) { item, error in
            guard error == nil,
                  let data = item as? Data,
                  let url = URL(dataRepresentation: data, relativeTo: nil),
                  url.pathExtension.lowercased() == "har" else {
                return
            }

            Task { @MainActor in
                await viewModel.loadFromHar(url: url)
            }
        }

        return true
    }
}

// MARK: - StatusBarView

/// The status bar at the bottom of the preview area.
struct StatusBarView: View {
    @ObservedObject var viewModel: MainViewModel

    var body: some View {
        HStack {
            if viewModel.isBusy {
                ProgressView()
                    .scaleEffect(0.7)
                    .frame(width: 16, height: 16)
            }

            Text(viewModel.statusText)
                .font(.caption)
                .lineLimit(1)
                .truncationMode(.tail)

            Spacer()

            // Zoom controls
            HStack(spacing: 4) {
                Button("-") {
                    viewModel.zoomOut()
                }
                .buttonStyle(.borderless)
                .disabled(viewModel.previewZoom <= 50)

                Text("\(viewModel.previewZoom)%")
                    .font(.caption)
                    .frame(width: 40)

                Button("+") {
                    viewModel.zoomIn()
                }
                .buttonStyle(.borderless)
                .disabled(viewModel.previewZoom >= 200)
            }

            // Open in Browser button (if generated)
            if viewModel.generatedHtmlPath != nil {
                Button("Open in Browser") {
                    viewModel.openInBrowser()
                }
                .buttonStyle(.borderedProminent)
            }
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 8)
        .background(Color(nsColor: .controlBackgroundColor))
    }
}

// MARK: - Preview

#Preview {
    MainView()
}
