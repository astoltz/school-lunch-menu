namespace SchoolLunchMenu.Services;

using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Models.Api;

/// <summary>
/// Parses LINQ Connect API responses from a HAR (HTTP Archive) file for offline use.
/// </summary>
public class HarFileService : IHarFileService
{
    private readonly ILogger<HarFileService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="HarFileService"/>.
    /// </summary>
    public HarFileService(ILogger<HarFileService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(FamilyMenuResponse Menu, List<AllergyItem> Allergies, FamilyMenuIdentifierResponse Identifier, string? UserAgent)> LoadFromHarFileAsync(string harFilePath)
    {
        _logger.LogInformation("Loading HAR file from {Path}", harFilePath);

        // Read file content asynchronously, then parse on a background thread
        // since HAR files can be large (400KB+) and JSON parsing is CPU-bound
        var json = await File.ReadAllTextAsync(harFilePath).ConfigureAwait(false);

        return await Task.Run(() => ParseHarJson(json)).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the HAR JSON content and extracts API responses. Runs on a background thread.
    /// </summary>
    private (FamilyMenuResponse Menu, List<AllergyItem> Allergies, FamilyMenuIdentifierResponse Identifier, string? UserAgent) ParseHarJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var entries = doc.RootElement.GetProperty("log").GetProperty("entries");

        FamilyMenuResponse? menu = null;
        List<AllergyItem>? allergies = null;
        FamilyMenuIdentifierResponse? identifier = null;
        string? userAgent = null;

        foreach (var entry in entries.EnumerateArray())
        {
            var request = entry.GetProperty("request");
            var url = request.GetProperty("url").GetString() ?? "";

            // Extract User-Agent from the first LinqConnect API request
            if (userAgent is null && url.Contains("linqconnect.com", StringComparison.OrdinalIgnoreCase)
                && request.TryGetProperty("headers", out var headers))
            {
                foreach (var header in headers.EnumerateArray())
                {
                    var name = header.GetProperty("name").GetString();
                    if (string.Equals(name, "User-Agent", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = header.GetProperty("value").GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            userAgent = value;
                            _logger.LogInformation("Extracted User-Agent from HAR: {UserAgent}", value);
                        }
                        break;
                    }
                }
            }

            var responseContent = entry.GetProperty("response").GetProperty("content");

            if (!responseContent.TryGetProperty("text", out var textElement))
                continue;

            var text = textElement.GetString();
            if (string.IsNullOrEmpty(text))
                continue;

            try
            {
                if (url.Contains("FamilyMenu?") && url.Contains("startDate") && menu is null)
                {
                    menu = JsonSerializer.Deserialize<FamilyMenuResponse>(text);
                    _logger.LogInformation("Parsed FamilyMenu response ({Length} chars)", text.Length);
                }
                else if (url.Contains("FamilyAllergy") && url.Contains("districtId") && allergies is null)
                {
                    allergies = JsonSerializer.Deserialize<List<AllergyItem>>(text);
                    _logger.LogInformation("Parsed FamilyAllergy response with {Count} allergens", allergies?.Count ?? 0);
                }
                else if (url.Contains("FamilyMenuIdentifier") && identifier is null)
                {
                    identifier = JsonSerializer.Deserialize<FamilyMenuIdentifierResponse>(text);
                    _logger.LogInformation("Parsed FamilyMenuIdentifier response for {District}", identifier?.DistrictName);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse HAR entry for URL {Url}", url);
            }
        }

        if (menu is null)
            throw new InvalidOperationException("HAR file does not contain a FamilyMenu response");
        if (allergies is null)
            throw new InvalidOperationException("HAR file does not contain a FamilyAllergy response");
        if (identifier is null)
            throw new InvalidOperationException("HAR file does not contain a FamilyMenuIdentifier response");

        return (menu, allergies, identifier, userAgent);
    }
}
