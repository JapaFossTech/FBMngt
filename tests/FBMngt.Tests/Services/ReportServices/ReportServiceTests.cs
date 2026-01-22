using FBMngt.Services;
using FBMngt.Tests.TestDoubles;
using NUnit.Framework;

namespace FBMngt.Tests.Services.ReportServices;

[TestFixture]
public class GenerateFanProsCoreFieldsReportAsyncTests
{
    private const string FB_LOGS_PATH = 
        "C:\\Users\\Master2022\\Documents\\Javier\\FantasyBaseball\\Logs";

    [Test]
    public async Task 
    GivenCsvData_WhenReportCreated_ThenReportShouldHaveCorrectHeaders()
    {
        // Arrange
        
        var reportPathProvider = new FakeReportPathProvider(FB_LOGS_PATH);

        var service = new ReportService(reportPathProvider);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var filePath = Path.Combine(
            FB_LOGS_PATH,
            $"FBMngt_FanPros_CoreFields_{AppContext.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        Assert.That(lines.Length, Is.GreaterThan(0));

        Assert.That(
            lines[0],
            Is.EqualTo("PlayerID\tPLAYER NAME\tTEAM\tPOS"));
    }
    [Test]
    public async Task 
    GivenCsvData_WhenReportGenerated_ThenFileCreated()
    {
        // Arrange

        var reportPathProvider = new FakeReportPathProvider(FB_LOGS_PATH);

        var service = new ReportService(reportPathProvider);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var files = Directory.GetFiles(
            FB_LOGS_PATH,
            $"FBMngt_FanPros_CoreFields_{AppContext.SeasonYear}.tsv");

        Assert.That(files.Length, Is.EqualTo(1));
    }
    [Test]
    public async Task 
    GivenCsvData_When5Rows_ThenReport6Rows()
    {
        // Arrange
        const int rows = 5;

        var reportPathProvider =
            new FakeReportPathProvider(FB_LOGS_PATH);

        var service = new ReportService(reportPathProvider);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(rows);

        // Assert
        var filePath = Path.Combine(
            FB_LOGS_PATH,
            $"FBMngt_FanPros_CoreFields_{AppContext.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        // 1 header + N data rows
        Assert.That(lines.Length, Is.EqualTo(rows + 1));
    }


}
