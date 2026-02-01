namespace SchoolLunchMenu.Services;

using SchoolLunchMenu.Models;

/// <summary>
/// Provides persistence for application settings and menu cache.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from the settings file, or returns defaults if not found.
    /// </summary>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Saves the current settings to the settings file.
    /// </summary>
    Task SaveAsync(AppSettings settings);

    /// <summary>
    /// Saves the menu response and allergen list to a disk cache file.
    /// </summary>
    Task SaveMenuCacheAsync(MenuCache cache);

    /// <summary>
    /// Loads a previously cached menu response and allergen list, or null if unavailable.
    /// </summary>
    Task<MenuCache?> LoadMenuCacheAsync();
}
