using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Services;
using SchoolLunchMenu.ViewModels;
using Serilog;

namespace SchoolLunchMenu;

/// <summary>
/// Application entry point with dependency injection configuration.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog for file logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                System.IO.Path.Combine(AppContext.BaseDirectory, "logs", "SchoolLunchMenu-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Pass the first command-line argument as a HAR file path if provided
        var harFilePath = e.Args.Length > 0 ? e.Args[0] : null;

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.InitializeWithHarFile(harFilePath);
        mainWindow.Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Registers all services, view models, and views with the DI container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
#if DEBUG
            builder.AddConsole();
#endif
        });

        // HTTP
        services.AddHttpClient<ILinqConnectApiService, LinqConnectApiService>();

        // Services
        services.AddSingleton<IHarFileService, HarFileService>();
        services.AddSingleton<IMenuAnalyzer, MenuAnalyzer>();
        services.AddSingleton<ICalendarHtmlGenerator, CalendarHtmlGenerator>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }
}
