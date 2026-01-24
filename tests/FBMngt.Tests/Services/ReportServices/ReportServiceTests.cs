using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services;
using FBMngt.Services.Players;
using FBMngt.Tests.TestDoubles;
using Moq;
using NUnit.Framework;

namespace FBMngt.Tests.Services.ReportServices;

[TestFixture]
public class GenerateFanProsCoreFieldsReportAsyncTests
{
    private FakeAppSettings _fakeAppSettings = new();
    private List<Player> _dbPlayers;
    private Mock<IPlayerRepository> _repositoryMock;
    private ReportService _reportService;
    //private PlayerResolver _resolver;

    [SetUp]
    public void SetUp()
    {
        // DB has ONE known player
        _dbPlayers = new List<Player>
        {
            new Player
            {
                PlayerID = 123,
                PlayerName = "Mike Trout",
                //PrimaryPosition = "OF",
                //BirthDate = new DateTime(1991, 8, 7)
            }
        };

        _repositoryMock = new Mock<IPlayerRepository>();

        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(_dbPlayers);

        _reportService = new ReportService(_fakeAppSettings,
                                        _repositoryMock.Object);

        //_resolver = new PlayerResolver(_repository);
    }

    [Test]
    public async Task 
    GivenCsvData_WhenReportCreated_ThenReportShouldHaveCorrectHeaders()
    {
        // Arrange

        //var fakeAppSettings = new FakeAppSettings();

        //var service = new ReportService(fakeAppSettings,
        //                                _repositoryMock.Object);

        // Act
        await _reportService.GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var filePath = Path.Combine(
            _fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                _fakeAppSettings.SeasonYear}.tsv");

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

        //var fakeAppSettings = new FakeAppSettings();

        //var service = new ReportService(fakeAppSettings,
        //                                _repositoryMock);

        // Act
        await _reportService
                .GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var files = Directory.GetFiles(
            _fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                _fakeAppSettings.SeasonYear}.tsv");

        Assert.That(files.Length, Is.EqualTo(1));
    }
    [Test]
    public async Task 
    GivenCsvData_When5Rows_ThenReport6Rows()
    {
        // Arrange
        const int rows = 5;

        //var fakeAppSettings = new FakeAppSettings();

        //var service = new ReportService(fakeAppSettings,
        //    _repositoryMock);

        // Act
        await _reportService
            .GenerateFanProsCoreFieldsReportAsync(rows);

        // Assert
        var filePath = Path.Combine(
            _fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                _fakeAppSettings.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        // 1 header + N data rows
        Assert.That(lines.Length, Is.EqualTo(rows + 1));
    }
    [Test]
    public void GivenCsvFile_WhenPathProvided_ThenFileExist()
    {
        // Arrange
        //var fakeAppSettings = new FakeAppSettings();
        var basePath = System.AppContext.BaseDirectory;

        var relativePath = Path.Combine(
            "FanPros",
            $"FantasyPros_{
                _fakeAppSettings.SeasonYear
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

        //var fakeAppSettings = new FakeAppSettings();

        //var service =
        //    new ReportService(fakeAppSettings, _repositoryMock);

        // Act
        await _reportService
            .GenerateFanProsCoreFieldsReportAsync(rows);

        // Assert
        var filePath = Path.Combine(
            _fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{
                _fakeAppSettings.SeasonYear}.tsv");

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
