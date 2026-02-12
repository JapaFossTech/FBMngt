using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting;
using FBMngt.Tests.TestDoubles;
using Moq;
using NUnit.Framework;

namespace FBMngt.Tests.Services.Reporting;

[TestFixture]
public class GenerateFanProsCoreFieldsReportAsyncTests
{
    private FakeAppSettings _fakeAppSettings = new();
    private List<Player> _dbPlayers = default!;
    private Mock<IPlayerRepository> _playerRepositoryMock = default!;
    private Mock<IPreDraftAdjustRepository> 
                            _preDraftAdjustRepoMock = default!;
    private ReportService _reportService = default!;

    [SetUp]
    public void SetUp()
    {
        // Arrange
        _dbPlayers = new List<Player>
        {
            new Player
            {
                PlayerID = 123,
                PlayerName = "Mike Trout",
            }
        };

        _playerRepositoryMock = new Mock<IPlayerRepository>();

        _playerRepositoryMock
                        .Setup(r => r.GetAllAsync())
                        .ReturnsAsync(_dbPlayers);

        _preDraftAdjustRepoMock = 
                        new Mock<IPreDraftAdjustRepository>();
        _preDraftAdjustRepoMock
                    .Setup(x => x.GetAllAsync())
                    .ReturnsAsync(new Dictionary<int, int>());

        _reportService =
            new ReportService(
                _fakeAppSettings,
                _playerRepositoryMock.Object,
                _preDraftAdjustRepoMock.Object);
    }

    [Test]
    public async Task
    GivenCsvData_WhenReportCreated_ThenReportShouldHaveCorrectHeaders()
    {
        // Act
        await _reportService
            .GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var filePath = Path.Combine(
            _fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{_fakeAppSettings.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        Assert.That(lines.Length, Is.GreaterThan(0));
        Assert.That(
            lines[0],
            Is.EqualTo("PlayerID\tPLAYER NAME\tTEAM\tPOS\tRANK\tOFFSET\tADJUSTED"));
    }

    [Test]
    public async Task
    GivenCsvData_WhenReportGenerated_ThenFileCreated()
    {
        // Act
        await _reportService
            .GenerateFanProsCoreFieldsReportAsync(10);

        // Assert
        var files = Directory.GetFiles(
            _fakeAppSettings.ReportPath,
            $"FBMngt_FanPros_CoreFields_{_fakeAppSettings.SeasonYear}.tsv");

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
            $"FBMngt_FanPros_CoreFields_{_fakeAppSettings.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        // 1 header + N data rows
        Assert.That(lines.Length, Is.EqualTo(rows + 1));
    }

    [Test]
    public void
    GivenCsvFile_WhenResolvedViaRepoRoot_ThenFileExists()
    {
        // Arrange
        var rawFanProsDir = Path.Combine(
            RepoPath.Root,
            "rawData",
            "FanPros");

        // Act
        bool exists =
            Directory.Exists(rawFanProsDir) &&
            Directory.GetFiles(
                rawFanProsDir,
                $"FantasyPros_{_fakeAppSettings.SeasonYear}_Draft_ALL_Rankings*.csv")
            .Any();

        // Assert
        Assert.That(
            exists,
            Is.True,
            $@"Expected FanPros CSV file to exist in: {rawFanProsDir}");
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
            $"FBMngt_FanPros_CoreFields_{_fakeAppSettings.SeasonYear}.tsv");

        var lines = File.ReadAllLines(filePath);

        Assert.That(lines.Length, Is.EqualTo(2));

        var dataRow = lines[1];
        var columns = dataRow.Split('\t');

        Assert.That(columns.Length, Is.GreaterThanOrEqualTo(4));
        Assert.That(columns[0], Is.EqualTo(string.Empty));
        Assert.That(columns[1], Is.Not.Empty);
    }
}
