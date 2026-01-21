using FBMngt.Services;
using FBMngt.Tests.TestDoubles;
using NUnit.Framework;

namespace FBMngt.Tests.Services;

[TestFixture]
public class ReportServiceTests
{
    private const string FB_LOGS_PATH = 
        "C:\\Users\\Master2022\\Documents\\Javier\\FantasyBaseball\\Logs";

    [Test]
    public async Task 
    T01_GenerateFanProsCoreFieldsReportAsync_WritesHeaderRow()
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
    T02_GenerateFanProsCoreFieldsReportAsync_CreatesOutputFile_InReportPath()
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
    T03_GenerateFanProsCoreFieldsReportAsync_WhenCalled_CreatesOutputFile()
    {
        // Arrange

        var reportPathProvider = new FakeReportPathProvider(FB_LOGS_PATH);

        var service = new ReportService(reportPathProvider);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        Assert.That(false, Is.True, "Expected output file to be created (not implemented yet).");
    }


}
