namespace SchoolLunchMenu.Services;

using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Models;

/// <summary>
/// Reads and writes application settings and menu cache to JSON files next to the executable.
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly string _cachePath;
    private readonly ILogger<SettingsService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SettingsService"/>.
    /// </summary>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        var exeDir = AppContext.BaseDirectory;
        _settingsPath = Path.Combine(exeDir, "settings.json");
        _cachePath = Path.Combine(exeDir, "menu-cache.json");
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            _logger.LogInformation("No settings file found at {Path}, using defaults", _settingsPath);
            return new AppSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _settingsPath);
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
            _logger.LogInformation("Settings saved to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsPath);
        }
    }

    /// <inheritdoc />
    public async Task SaveMenuCacheAsync(MenuCache cache)
    {
        try
        {
            var json = JsonSerializer.Serialize(cache, JsonOptions);
            await File.WriteAllTextAsync(_cachePath, json).ConfigureAwait(false);
            _logger.LogInformation("Menu cache saved to {Path}", _cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save menu cache to {Path}", _cachePath);
        }
    }

    /// <inheritdoc />
    public async Task<MenuCache?> LoadMenuCacheAsync()
    {
        if (!File.Exists(_cachePath))
        {
            _logger.LogInformation("No menu cache found at {Path}", _cachePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_cachePath).ConfigureAwait(false);
            var cache = JsonSerializer.Deserialize<MenuCache>(json, JsonOptions);

            if (cache?.MenuResponse is null || cache.Allergies is null)
            {
                _logger.LogWarning("Menu cache at {Path} is incomplete, ignoring", _cachePath);
                return null;
            }

            _logger.LogInformation("Menu cache loaded from {Path} (saved {SavedAt})", _cachePath, cache.SavedAtUtc);
            return cache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load menu cache from {Path}", _cachePath);
            return null;
        }
    }
}
