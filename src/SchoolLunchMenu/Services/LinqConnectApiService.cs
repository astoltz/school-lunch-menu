namespace SchoolLunchMenu.Services;

using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SchoolLunchMenu.Models.Api;

/// <summary>
/// Fetches menu data from the LINQ Connect REST API with 1-hour response caching.
/// </summary>
public class LinqConnectApiService : ILinqConnectApiService
{
    private const string BaseUrl = "https://api.linqconnect.com/api";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly HttpClient _httpClient;
    private readonly ILogger<LinqConnectApiService> _logger;

    /// <summary>
    /// In-memory cache keyed by URL, storing the deserialized response and expiry time.
    /// </summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    /// Initializes a new instance of <see cref="LinqConnectApiService"/>.
    /// </summary>
    public LinqConnectApiService(HttpClient httpClient, ILogger<LinqConnectApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FamilyMenuIdentifierResponse> GetMenuIdentifierAsync(string identifier)
    {
        var url = $"{BaseUrl}/FamilyMenuIdentifier?identifier={identifier}";
        return await GetCachedAsync<FamilyMenuIdentifierResponse>(url)
            ?? throw new InvalidOperationException("Received null response from FamilyMenuIdentifier");
    }

    /// <inheritdoc />
    public async Task<List<AllergyItem>> GetAllergiesAsync(string districtId)
    {
        var url = $"{BaseUrl}/FamilyAllergy?districtId={districtId}";
        return await GetCachedAsync<List<AllergyItem>>(url) ?? [];
    }

    /// <inheritdoc />
    public async Task<FamilyMenuResponse> GetMenuAsync(string buildingId, string districtId, DateOnly startDate, DateOnly endDate)
    {
        // API expects M-d-yyyy format for dates
        var start = startDate.ToString("M-d-yyyy");
        var end = endDate.ToString("M-d-yyyy");
        var url = $"{BaseUrl}/FamilyMenu?buildingId={buildingId}&districtId={districtId}&startDate={start}&endDate={end}";
        return await GetCachedAsync<FamilyMenuResponse>(url)
            ?? throw new InvalidOperationException("Received null response from FamilyMenu");
    }

    /// <inheritdoc />
    public async Task<List<MealItem>> GetMenuMealsAsync(string districtId)
    {
        var url = $"{BaseUrl}/FamilyMenuMeals?districtId={districtId}";
        return await GetCachedAsync<List<MealItem>>(url) ?? [];
    }

    /// <summary>
    /// Fetches a response from the API, returning a cached copy if available and not expired.
    /// </summary>
    private async Task<T?> GetCachedAsync<T>(string url)
    {
        // Check cache first
        if (_cache.TryGetValue(url, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Cache hit for {Url} (expires {Expiry})", url, entry.ExpiresAt.ToLocalTime());
            return (T?)entry.Value;
        }

        _logger.LogInformation("Fetching from API: {Url}", url);
        var response = await _httpClient.GetFromJsonAsync<T>(url);

        // Cache the response
        _cache[url] = new CacheEntry(response, DateTime.UtcNow + CacheDuration);
        _logger.LogInformation("Cached response for {Url} (1 hour)", url);

        return response;
    }

    /// <summary>
    /// A cached API response with an expiry time.
    /// </summary>
    private record CacheEntry(object? Value, DateTime ExpiresAt);
}
