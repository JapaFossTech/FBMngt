using FBMngt.Services;
using FBMngt.Tests.TestDoubles;
using NUnit.Framework;

namespace FBMngt.Tests.Services.ReportServices;

[TestFixture]
public class GenerateFanProsCoreFieldsReportAsyncTests
{
    //private const string FB_LOGS_PATH = 
    //    "C:\\Users\\Master2022\\Documents\\Javier\\FantasyBaseball\\Logs";
    //private string FANPROS_FILE_PATH = 
    //    $@"FanPros\FantasyPros_{AppSettings.SeasonYear}_Draft_ALL_Rankings.csv";

    [Test]
    public async Task 
    GivenCsvData_WhenReportCreated_ThenReportShouldHaveCorrectHeaders()
    {
        // Arrange

        var fakeAppSettings = new FakeAppSettings();

        var service = new ReportService(fakeAppSettings);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var filePath = Path.Combine(
            fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                        fakeAppSettings.SeasonYear}.tsv");

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

        var fakeAppSettings = new FakeAppSettings();

        var service = new ReportService(fakeAppSettings);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var files = Directory.GetFiles(
            fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                        fakeAppSettings.SeasonYear}.tsv");

        Assert.That(files.Length, Is.EqualTo(1));
    }
    [Test]
    public async Task 
    GivenCsvData_When5Rows_ThenReport6Rows()
    {
        // Arrange
        const int rows = 5;

        var fakeAppSettings = new FakeAppSettings();

        var service = new ReportService(fakeAppSettings);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(rows);

        // Assert
        var filePath = Path.Combine(
            fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                        fakeAppSettings.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        // 1 header + N data rows
        Assert.That(lines.Length, Is.EqualTo(rows + 1));
    }
    [Test]
    public void GivenCsvFile_WhenPathProvided_ThenFileExist()
    {
        // Arrange
        var fakeAppSettings = new FakeAppSettings();
        var basePath = System.AppContext.BaseDirectory;

        var relativePath = Path.Combine(
            "FanPros",
            $"FantasyPros_{fakeAppSettings.SeasonYear
                }_Draft_ALL_Rankings.csv");

        var fullPath = Path.Combine(basePath, relativePath);

        // Act
        var exists = File.Exists(fullPath);

        // Assert
        Assert.That(
            exists,
            Is.True,
            $"Expected CSV file to exist at: {fullPath}");
    }
    [Test]
    public async Task 
    GivenCsvData_WhenPlayerHasNoDbMatch_ThenPlayerIdIsBlank()
    {
        // Arrange
        const int rows = 1;

        var fakeAppSettings = new FakeAppSettings();

        var service =
            new ReportService(fakeAppSettings);

        // Act
        await service.GenerateFanProsCoreFieldsReportAsync(rows);

        // Assert
        var filePath = Path.Combine(
            fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                    fakeAppSettings.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        // header + 1 data row
        Assert.That(lines.Length, Is.EqualTo(2));

        var dataRow = lines[1];

        var columns = dataRow.Split('\t');

        // report row must have at least 4 columns
        Assert.That(columns.Length, Is.GreaterThanOrEqualTo(4));

        // core behavior
        Assert.That(columns[0], Is.EqualTo(string.Empty), "PlayerID should be blank");
        Assert.That(columns[1], Is.Not.Empty, "Player name must be written from CSV");

    }
}
