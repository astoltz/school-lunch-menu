import Foundation
import os

/// Actor-based API service for LinqConnect menu data
actor LinqConnectApiService {

    // MARK: - Properties

    private let baseURL = "https://api.linqconnect.com/api"
    private let urlSession: URLSession
    private let logger = Logger(subsystem: "com.schoollunchmenu", category: "LinqConnectApiService")

    /// Cache storage with URL as key and tuple of (data, expiration date) as value
    private var cache: [String: (data: Data, expiresAt: Date)] = [:]

    /// Cache duration: 1 hour
    private let cacheDuration: TimeInterval = 3600

    /// Date formatter for API requests (M-d-yyyy format)
    private let apiDateFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.dateFormat = "M-d-yyyy"
        formatter.locale = Locale(identifier: "en_US_POSIX")
        return formatter
    }()

    /// JSON decoder configured for API responses
    private let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()

    // MARK: - Initialization

    init(urlSession: URLSession = .shared) {
        self.urlSession = urlSession
    }

    // MARK: - Public API Methods

    /// Fetches menu identifier information for a given identifier
    /// - Parameter identifier: The menu identifier string
    /// - Returns: FamilyMenuIdentifierResponse containing menu identification data
    func getMenuIdentifier(identifier: String) async throws -> FamilyMenuIdentifierResponse {
        let urlString = "\(baseURL)/FamilyMenuIdentifier?identifier=\(identifier)"
        logger.info("Fetching menu identifier for: \(identifier)")
        return try await getCached(urlString: urlString)
    }

    /// Fetches all allergies for a district
    /// - Parameter districtId: The district identifier
    /// - Returns: Array of AllergyItem objects
    func getAllergies(districtId: String) async throws -> [AllergyItem] {
        let urlString = "\(baseURL)/FamilyAllergy?districtId=\(districtId)"
        logger.info("Fetching allergies for district: \(districtId)")
        return try await getCached(urlString: urlString)
    }

    /// Fetches menu data for a specific building and date range
    /// - Parameters:
    ///   - buildingId: The building identifier
    ///   - districtId: The district identifier
    ///   - startDate: Start date for the menu range
    ///   - endDate: End date for the menu range
    /// - Returns: FamilyMenuResponse containing menu items
    func getMenu(buildingId: String, districtId: String, startDate: Date, endDate: Date) async throws -> FamilyMenuResponse {
        let startDateString = apiDateFormatter.string(from: startDate)
        let endDateString = apiDateFormatter.string(from: endDate)

        let urlString = "\(baseURL)/FamilyMenu?buildingId=\(buildingId)&districtId=\(districtId)&startDate=\(startDateString)&endDate=\(endDateString)"
        logger.info("Fetching menu for building \(buildingId) from \(startDateString) to \(endDateString)")
        return try await getCached(urlString: urlString)
    }

    /// Fetches all meal types for a district
    /// - Parameter districtId: The district identifier
    /// - Returns: Array of MealItem objects
    func getMenuMeals(districtId: String) async throws -> [MealItem] {
        let urlString = "\(baseURL)/FamilyMenuMeals?districtId=\(districtId)"
        logger.info("Fetching meals for district: \(districtId)")
        return try await getCached(urlString: urlString)
    }

    // MARK: - Private Methods

    /// Generic cached GET request method
    /// - Parameter urlString: The full URL string to fetch
    /// - Returns: Decoded object of type T
    private func getCached<T: Decodable>(urlString: String) async throws -> T {
        // Check cache first
        if let cached = cache[urlString] {
            if cached.expiresAt > Date() {
                logger.debug("Cache hit for: \(urlString)")
                return try decoder.decode(T.self, from: cached.data)
            } else {
                logger.debug("Cache expired for: \(urlString)")
                cache.removeValue(forKey: urlString)
            }
        }

        // Build request
        guard let url = URL(string: urlString) else {
            logger.error("Invalid URL: \(urlString)")
            throw LinqConnectApiError.invalidURL(urlString)
        }

        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.setValue("application/json", forHTTPHeaderField: "Accept")

        logger.debug("Fetching from API: \(urlString)")

        // Perform request
        let (data, response) = try await urlSession.data(for: request)

        // Validate response
        guard let httpResponse = response as? HTTPURLResponse else {
            logger.error("Invalid response type for: \(urlString)")
            throw LinqConnectApiError.invalidResponse
        }

        guard (200...299).contains(httpResponse.statusCode) else {
            logger.error("HTTP error \(httpResponse.statusCode) for: \(urlString)")
            throw LinqConnectApiError.httpError(statusCode: httpResponse.statusCode)
        }

        // Decode response
        let decoded: T
        do {
            decoded = try decoder.decode(T.self, from: data)
        } catch {
            logger.error("Decoding error for \(urlString): \(error.localizedDescription)")
            throw LinqConnectApiError.decodingError(error)
        }

        // Cache the response
        let expiresAt = Date().addingTimeInterval(cacheDuration)
        cache[urlString] = (data: data, expiresAt: expiresAt)
        logger.debug("Cached response for: \(urlString), expires at: \(expiresAt)")

        return decoded
    }

    /// Clears all cached responses
    func clearCache() {
        cache.removeAll()
        logger.info("Cache cleared")
    }

    /// Removes expired entries from the cache
    func pruneExpiredCache() {
        let now = Date()
        let expiredKeys = cache.filter { $0.value.expiresAt <= now }.map { $0.key }
        for key in expiredKeys {
            cache.removeValue(forKey: key)
        }
        logger.info("Pruned \(expiredKeys.count) expired cache entries")
    }
}

// MARK: - Error Types

enum LinqConnectApiError: LocalizedError {
    case invalidURL(String)
    case invalidResponse
    case httpError(statusCode: Int)
    case decodingError(Error)

    var errorDescription: String? {
        switch self {
        case .invalidURL(let url):
            return "Invalid URL: \(url)"
        case .invalidResponse:
            return "Invalid response from server"
        case .httpError(let statusCode):
            return "HTTP error with status code: \(statusCode)"
        case .decodingError(let error):
            return "Failed to decode response: \(error.localizedDescription)"
        }
    }
}
