import Foundation
import os

/// Error types that can occur when parsing HAR files.
enum HarFileError: LocalizedError {
    case fileReadFailed(URL, Error)
    case invalidJsonStructure(String)
    case missingFamilyMenuResponse
    case missingFamilyAllergyResponse
    case missingFamilyMenuIdentifierResponse
    case jsonParsingFailed(String, Error)

    var errorDescription: String? {
        switch self {
        case .fileReadFailed(let url, let error):
            return "Failed to read HAR file at \(url.path): \(error.localizedDescription)"
        case .invalidJsonStructure(let details):
            return "Invalid HAR file structure: \(details)"
        case .missingFamilyMenuResponse:
            return "HAR file does not contain a FamilyMenu response"
        case .missingFamilyAllergyResponse:
            return "HAR file does not contain a FamilyAllergy response"
        case .missingFamilyMenuIdentifierResponse:
            return "HAR file does not contain a FamilyMenuIdentifier response"
        case .jsonParsingFailed(let url, let error):
            return "Failed to parse JSON response for \(url): \(error.localizedDescription)"
        }
    }
}

/// Parses LINQ Connect API responses from a HAR (HTTP Archive) file for offline use.
class HarFileService {
    private let logger = Logger(subsystem: "com.schoollunchmenu", category: "HarFileService")

    /// Result containing all parsed API responses from a HAR file.
    struct HarParseResult {
        /// The family menu response containing sessions, plans, and days.
        let menu: FamilyMenuResponse

        /// The list of available allergens.
        let allergies: [AllergyItem]

        /// The menu identifier response containing district and building info.
        let identifier: FamilyMenuIdentifierResponse

        /// The User-Agent string extracted from the first LinqConnect API request, if found.
        let userAgent: String?
    }

    /// Loads and parses LINQ Connect API responses from a HAR file.
    /// - Parameter url: The file URL of the HAR file to parse.
    /// - Returns: A HarParseResult containing the parsed menu, allergies, and identifier.
    /// - Throws: HarFileError if the file cannot be read or parsed.
    func loadFromHarFile(at url: URL) async throws -> HarParseResult {
        logger.info("Loading HAR file from \(url.path)")

        // Read file content
        let data: Data
        do {
            data = try Data(contentsOf: url)
        } catch {
            logger.error("Failed to read HAR file: \(error.localizedDescription)")
            throw HarFileError.fileReadFailed(url, error)
        }

        // Parse on background thread since HAR files can be large (400KB+)
        return try await Task.detached(priority: .userInitiated) { [self] in
            try self.parseHarData(data)
        }.value
    }

    // MARK: - Private Methods

    /// Parses the HAR data and extracts API responses.
    private func parseHarData(_ data: Data) throws -> HarParseResult {
        // Parse the HAR JSON structure
        let harObject: Any
        do {
            harObject = try JSONSerialization.jsonObject(with: data, options: [])
        } catch {
            logger.error("Failed to parse HAR JSON: \(error.localizedDescription)")
            throw HarFileError.invalidJsonStructure("Invalid JSON: \(error.localizedDescription)")
        }

        guard let harDict = harObject as? [String: Any],
              let logDict = harDict["log"] as? [String: Any],
              let entries = logDict["entries"] as? [[String: Any]] else {
            throw HarFileError.invalidJsonStructure("Missing log.entries array")
        }

        var menu: FamilyMenuResponse?
        var allergies: [AllergyItem]?
        var identifier: FamilyMenuIdentifierResponse?
        var userAgent: String?

        let decoder = JSONDecoder()

        for entry in entries {
            guard let request = entry["request"] as? [String: Any],
                  let urlString = request["url"] as? String,
                  let response = entry["response"] as? [String: Any],
                  let content = response["content"] as? [String: Any],
                  let text = content["text"] as? String,
                  !text.isEmpty else {
                continue
            }

            // Extract User-Agent from the first LinqConnect API request
            if userAgent == nil && urlString.contains("linqconnect.com"),
               let headers = request["headers"] as? [[String: Any]] {
                for header in headers {
                    if let name = header["name"] as? String,
                       name.caseInsensitiveCompare("User-Agent") == .orderedSame,
                       let value = header["value"] as? String,
                       !value.isEmpty {
                        userAgent = value
                        logger.info("Extracted User-Agent from HAR: \(value)")
                        break
                    }
                }
            }

            guard let textData = text.data(using: .utf8) else {
                continue
            }

            do {
                // Check for FamilyMenu response
                if urlString.contains("FamilyMenu?") && urlString.contains("startDate") && menu == nil {
                    menu = try decoder.decode(FamilyMenuResponse.self, from: textData)
                    logger.info("Parsed FamilyMenu response (\(text.count) chars)")
                }
                // Check for FamilyAllergy response
                else if urlString.contains("FamilyAllergy") && urlString.contains("districtId") && allergies == nil {
                    allergies = try decoder.decode([AllergyItem].self, from: textData)
                    logger.info("Parsed FamilyAllergy response with \(allergies?.count ?? 0) allergens")
                }
                // Check for FamilyMenuIdentifier response
                else if urlString.contains("FamilyMenuIdentifier") && identifier == nil {
                    identifier = try decoder.decode(FamilyMenuIdentifierResponse.self, from: textData)
                    logger.info("Parsed FamilyMenuIdentifier response for \(identifier?.districtName ?? "unknown")")
                }
            } catch {
                logger.warning("Failed to parse HAR entry for URL \(urlString): \(error.localizedDescription)")
                // Continue to next entry rather than failing entirely
            }
        }

        // Validate all required responses were found
        guard let menuResult = menu else {
            throw HarFileError.missingFamilyMenuResponse
        }
        guard let allergiesResult = allergies else {
            throw HarFileError.missingFamilyAllergyResponse
        }
        guard let identifierResult = identifier else {
            throw HarFileError.missingFamilyMenuIdentifierResponse
        }

        return HarParseResult(
            menu: menuResult,
            allergies: allergiesResult,
            identifier: identifierResult,
            userAgent: userAgent
        )
    }
}
