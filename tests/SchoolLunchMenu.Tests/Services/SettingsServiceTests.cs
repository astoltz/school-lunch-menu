using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolLunchMenu.Models;
using SchoolLunchMenu.Services;
using Xunit;

namespace SchoolLunchMenu.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _service;

    private readonly string _settingsPath;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SchoolLunchMenuTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // SettingsService uses AppContext.BaseDirectory; we'll test the actual service
        // by creating a helper that uses a custom path
        _service = new SettingsService(NullLogger<SettingsService>.Instance);

        // Clean up any settings file left by prior test runs
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (File.Exists(_settingsPath))
            File.Delete(_settingsPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_settingsPath))
                File.Delete(_settingsPath);
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Cleanup is best-effort
        }
    }

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsDefaults()
    {
        var result = await _service.LoadAsync();

        result.Should().NotBeNull();
        result.Identifier.Should().Be("YVAM38");
        result.SelectedSessionName.Should().Be("Lunch");
        result.CrossOutPastDays.Should().BeTrue();
        result.LayoutMode.Should().Be("IconsLeft");
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips()
    {
        var settings = new AppSettings
        {
            Identifier = "TEST123",
            SelectedAllergenIds = ["allergen-1", "allergen-2"],
            SelectedThemeName = "Valentines",
            CrossOutPastDays = false,
            ShowShareFooter = true,
            DayLabelCorner = "BottomLeft"
        };

        await _service.SaveAsync(settings);
        var loaded = await _service.LoadAsync();

        loaded.Identifier.Should().Be("TEST123");
        loaded.SelectedAllergenIds.Should().BeEquivalentTo(["allergen-1", "allergen-2"]);
        loaded.SelectedThemeName.Should().Be("Valentines");
        loaded.CrossOutPastDays.Should().BeFalse();
        loaded.ShowShareFooter.Should().BeTrue();
        loaded.DayLabelCorner.Should().Be("BottomLeft");
    }

    [Fact]
    public void DefaultSettings_HaveSensibleValues()
    {
        var settings = new AppSettings();

        settings.SelectedAllergenIds.Should().BeEmpty();
        settings.DayLabelCycle.Should().HaveCount(2);
        settings.DayLabelCycle[0].Label.Should().Be("Red");
        settings.DayLabelCycle[1].Label.Should().Be("White");
        settings.ShowUnsafeLines.Should().BeTrue();
        settings.UnsafeLineMessage.Should().Be("No safe options");
        settings.DistrictName.Should().Be("Lakeville Area Schools");
    }
}
