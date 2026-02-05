import SwiftUI
import WebKit

// MARK: - PreviewView

/// WKWebView wrapper for HTML preview.
struct PreviewView: NSViewRepresentable {
    let html: String?

    func makeNSView(context: Context) -> WKWebView {
        let webView = WKWebView()
        webView.setValue(false, forKey: "drawsBackground")
        return webView
    }

    func updateNSView(_ webView: WKWebView, context: Context) {
        if let html = html {
            webView.loadHTMLString(html, baseURL: nil)
        } else {
            // Show placeholder
            let placeholder = """
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        height: 100vh;
                        margin: 0;
                        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", system-ui, sans-serif;
                        color: #ADB5BD;
                        background-color: #F8F9FA;
                    }
                    .placeholder {
                        text-align: center;
                        padding: 20px;
                    }
                    .icon {
                        font-size: 48px;
                        margin-bottom: 16px;
                    }
                    p {
                        margin: 0;
                        font-size: 14px;
                    }
                </style>
            </head>
            <body>
                <div class="placeholder">
                    <div class="icon">📅</div>
                    <p>Calendar preview will appear here after generation.</p>
                </div>
            </body>
            </html>
            """
            webView.loadHTMLString(placeholder, baseURL: nil)
        }
    }
}

// MARK: - Preview

#Preview {
    PreviewView(html: nil)
        .frame(width: 600, height: 400)
}
