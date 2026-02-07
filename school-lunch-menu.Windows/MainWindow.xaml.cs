using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchoolLunchMenu.ViewModels;

namespace SchoolLunchMenu;

/// <summary>
/// Main application window hosting the menu calendar UI.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private string? _initialHarFilePath;

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Navigate WebBrowser when HTML is generated
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Loaded += OnWindowLoaded;
    }

    /// <summary>
    /// Sets the HAR file path to load on startup (from command-line args).
    /// </summary>
    public void InitializeWithHarFile(string? harFilePath)
    {
        _initialHarFilePath = harFilePath;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync(_initialHarFilePath);

        // Auto-focus month selector
        MonthCombo.Focus();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.GeneratedHtml) && _viewModel.GeneratedHtml is not null)
        {
            PreviewBrowser.NavigateToString(_viewModel.GeneratedHtml);
            PreviewBrowser.Visibility = Visibility.Visible;
        }
    }

    private void OnClose(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files.Length == 1 && Path.GetExtension(files[0]).Equals(".har", StringComparison.OrdinalIgnoreCase))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }
        }

        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OnPlanLabelTextBoxVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && sender is TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void OnPlanLabelTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox { DataContext: ViewModels.PlanLabelEntry entry })
        {
            _viewModel.EditPlanLabelCommand.Execute(entry);
            e.Handled = true;
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        if (files.Length != 1)
            return;

        var filePath = files[0];
        if (!Path.GetExtension(filePath).Equals(".har", StringComparison.OrdinalIgnoreCase))
            return;

        await _viewModel.LoadHarFileAsync(filePath);
    }
}
