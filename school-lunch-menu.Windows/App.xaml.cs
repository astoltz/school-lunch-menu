using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Models;
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

        // Load User-Agent from settings before DI setup (read file directly to avoid DI chicken-and-egg)
        var userAgent = LoadUserAgentFromSettings()
            ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:137.0) Gecko/20100101 Firefox/137.0";

        var services = new ServiceCollection();
        ConfigureServices(services, userAgent);
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
    /// Reads the UserAgent field from settings.json without requiring DI services.
    /// Returns null if the file doesn't exist or doesn't contain a UserAgent.
    /// </summary>
    private static string? LoadUserAgentFromSettings()
    {
        try
        {
            var settingsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "settings.json");
            if (!System.IO.File.Exists(settingsPath))
                return null;

            var json = System.IO.File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return string.IsNullOrEmpty(settings?.UserAgent) ? null : settings.UserAgent;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Registers all services, view models, and views with the DI container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services, string userAgent)
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

        // HTTP — configure User-Agent on all typed HttpClients
        services.AddHttpClient<ILinqConnectApiService, LinqConnectApiService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        });
        services.AddHttpClient<IDayLabelFetchService, DayLabelFetchService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        });

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
