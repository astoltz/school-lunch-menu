using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace SchoolLunchMenu;

/// <summary>
/// About dialog showing app version, git info, and credits.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        PopulateVersionInfo();
    }

    private void PopulateVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        // InformationalVersion format from CI: "1.0.0+abc1234 (branch)"
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? version;

        VersionText.Text = $"Version {version}";

        // Parse git commit and branch from informational version
        var plusIndex = infoVersion.IndexOf('+');
        if (plusIndex >= 0)
        {
            var suffix = infoVersion[(plusIndex + 1)..];
            var parenOpen = suffix.IndexOf('(');
            var parenClose = suffix.IndexOf(')');

            var commit = parenOpen > 0 ? suffix[..parenOpen].Trim() : suffix;
            var branch = parenOpen >= 0 && parenClose > parenOpen
                ? suffix[(parenOpen + 1)..parenClose]
                : "unknown";

            GitInfoText.Text = $"Commit: {commit}  Branch: {branch}";
        }
        else
        {
            GitInfoText.Text = $"Build: {infoVersion}";
        }
    }

    private void OnHyperlinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
