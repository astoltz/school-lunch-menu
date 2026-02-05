import Foundation
import os

/// Service for persisting app settings and menu cache to JSON files
final class SettingsService {

    // MARK: - Properties

    private let logger = Logger(subsystem: "com.schoollunchmenu", category: "SettingsService")
    private let fileManager = FileManager.default

    /// Application support directory for SchoolLunchMenu
    private var appSupportDirectory: URL {
        let appSupport = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        return appSupport.appendingPathComponent("SchoolLunchMenu", isDirectory: true)
    }

    /// Path to the settings JSON file
    private var settingsPath: URL {
        appSupportDirectory.appendingPathComponent("settings.json")
    }

    /// Path to the menu cache JSON file
    private var menuCachePath: URL {
        appSupportDirectory.appendingPathComponent("menu-cache.json")
    }

    /// JSON encoder configured for pretty printing and ISO8601 dates
    private let encoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.outputFormatting = .prettyPrinted
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }()

    /// JSON decoder configured for ISO8601 dates
    private let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()

    // MARK: - Initialization

    init() {
        ensureDirectoryExists()
    }

    // MARK: - Directory Management

    /// Ensures the application support directory exists
    private func ensureDirectoryExists() {
        do {
            if !fileManager.fileExists(atPath: appSupportDirectory.path) {
                try fileManager.createDirectory(at: appSupportDirectory, withIntermediateDirectories: true)
                logger.info("Created app support directory at: \(self.appSupportDirectory.path)")
            }
        } catch {
            logger.error("Failed to create app support directory: \(error.localizedDescription)")
        }
    }

    // MARK: - Settings Methods

    /// Loads app settings from disk
    /// - Returns: AppSettings object, or default settings if loading fails
    func load() async -> AppSettings {
        do {
            guard fileManager.fileExists(atPath: settingsPath.path) else {
                logger.info("No settings file found, returning defaults")
                return AppSettings()
            }

            let data = try Data(contentsOf: settingsPath)
            let settings = try decoder.decode(AppSettings.self, from: data)
            logger.info("Successfully loaded settings from: \(self.settingsPath.path)")
            return settings
        } catch {
            logger.error("Failed to load settings: \(error.localizedDescription)")
            return AppSettings()
        }
    }

    /// Saves app settings to disk
    /// - Parameter settings: The AppSettings object to save
    func save(_ settings: AppSettings) async {
        do {
            ensureDirectoryExists()
            let data = try encoder.encode(settings)
            try data.write(to: settingsPath, options: .atomic)
            logger.info("Successfully saved settings to: \(self.settingsPath.path)")
        } catch {
            logger.error("Failed to save settings: \(error.localizedDescription)")
        }
    }

    // MARK: - Menu Cache Methods

    /// Saves menu cache to disk
    /// - Parameter cache: The MenuCache object to save
    func saveMenuCache(_ cache: MenuCache) async {
        do {
            ensureDirectoryExists()
            let data = try encoder.encode(cache)
            try data.write(to: menuCachePath, options: .atomic)
            logger.info("Successfully saved menu cache to: \(self.menuCachePath.path)")
        } catch {
            logger.error("Failed to save menu cache: \(error.localizedDescription)")
        }
    }

    /// Loads menu cache from disk
    /// - Returns: MenuCache object, or nil if loading fails or file doesn't exist
    func loadMenuCache() async -> MenuCache? {
        do {
            guard fileManager.fileExists(atPath: menuCachePath.path) else {
                logger.info("No menu cache file found")
                return nil
            }

            let data = try Data(contentsOf: menuCachePath)
            let cache = try decoder.decode(MenuCache.self, from: data)
            logger.info("Successfully loaded menu cache from: \(self.menuCachePath.path)")
            return cache
        } catch {
            logger.error("Failed to load menu cache: \(error.localizedDescription)")
            return nil
        }
    }

    // MARK: - Utility Methods

    /// Deletes all stored data (settings and cache)
    func deleteAllData() async {
        do {
            if fileManager.fileExists(atPath: settingsPath.path) {
                try fileManager.removeItem(at: settingsPath)
                logger.info("Deleted settings file")
            }

            if fileManager.fileExists(atPath: menuCachePath.path) {
                try fileManager.removeItem(at: menuCachePath)
                logger.info("Deleted menu cache file")
            }
        } catch {
            logger.error("Failed to delete data: \(error.localizedDescription)")
        }
    }

    /// Returns the paths for debugging purposes
    var debugPaths: (settings: String, cache: String) {
        (settingsPath.path, menuCachePath.path)
    }
}
