using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting;
using FBMngt.Tests.TestDoubles;
using Moq;

namespace FBMngt.Tests.Services.Reporting;

[TestFixture]
public class GenerateFanProsCoreFieldsReportAsyncTests
{
    private FakeAppSettings _fakeAppSettings = new();
    private List<Player> _dbPlayers;
    private Mock<IPlayerRepository> _repositoryMock;
    private ReportService _reportService;

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
        // Act
        await _reportService
            .GenerateFanProsCoreFieldsReportAsync(10);

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
        var basePath = AppContext.BaseDirectory;

        var fanProsDir = Path.Combine(basePath, "FanPros");

        // Act
        bool exists =
            Directory.Exists(fanProsDir) &&
            Directory.GetFiles(
                fanProsDir,
                $"FantasyPros_{_fakeAppSettings.SeasonYear}_Draft_ALL_Rankings*.csv")
            .Any();

        // Assert
        Assert.That(
            exists,
            Is.True,
            $@"Expected FanPros CSV file to 
                exist in: {fanProsDir}");
    }
    [Test]
    public async Task 
    GivenCsvData_WhenPlayerHasNoDbMatch_ThenPlayerIdIsBlank()
    {
        // Arrange
        const int rows = 1;

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
        Assert.That(columns[0], Is.EqualTo(string.Empty), 
            "PlayerID should be blank");
        Assert.That(columns[1], Is.Not.Empty, 
            "Player name must be written from CSV");

    }
}
