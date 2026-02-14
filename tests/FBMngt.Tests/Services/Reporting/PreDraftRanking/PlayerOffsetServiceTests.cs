using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.PreDraftRanking;
using FBMngt.Tests.TestDoubles;
using Moq;
using NUnit.Framework;

namespace FBMngt.Tests.Services;

[TestFixture]
public class PlayerOffsetServiceTests
{
    private Mock<IPlayerRepository> _playerRepositoryMock;
    private Mock<IPreDraftAdjustRepository> _preDraftAdjustRepoMock = null!;
    private PlayerOffsetService _service = null!;
    private ConfigSettings configSettings;
    private Mock<FanProsCoreFieldsReport> _fanProsReportMock;
    private Mock<PlayerResolver> _playerResolverMock;

    [SetUp]
    public void Setup()
    {
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _preDraftAdjustRepoMock = new 
                            Mock<IPreDraftAdjustRepository>();
        configSettings = new ConfigSettings(new FakeAppSettings());
        _playerResolverMock = new Mock<PlayerResolver>(
                        _playerRepositoryMock.Object);
        _fanProsReportMock = new Mock<FanProsCoreFieldsReport>(
                        configSettings,
                        _playerResolverMock.Object,
                        new FanProsCsvReader(),
                        _preDraftAdjustRepoMock.Object);
        _service = new PlayerOffsetService(
                        configSettings,
                        _playerRepositoryMock.Object,
                        _preDraftAdjustRepoMock.Object,
                        _fanProsReportMock.Object);
    }

    // AdjustAsync

    [Test]
    public async Task AdjustAsync_ParsesBatch_And_UpsertsPlayers()
    {
        // Arrange
        string batch = "10,12|20,-5";

        // Act
        await _service.AdjustAsync(batch);

        // Assert
        _preDraftAdjustRepoMock.Verify(r => r.UpsertAsync(10, 12), Times.Once);
        _preDraftAdjustRepoMock.Verify(r => r.UpsertAsync(20, -5), Times.Once);
    }

    [Test]
    public async Task AdjustAsync_EmptyBatch_DoesNothing()
    {
        // Act
        await _service.AdjustAsync("");

        // Assert
        _preDraftAdjustRepoMock.Verify(
            r => r.UpsertAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    // InitialConfigurationAsync

    [Test]
    public async Task
    InitialConfigurationAsync_Sets_Catcher_And_Closer_Only()
    {
        // CSV truth
        var csvPlayers = new List<FanProsPlayer>
    {
        new FanProsPlayer { PlayerID = 1, Position = "C" },
        new FanProsPlayer { PlayerID = 2, Position = "RP" },
        new FanProsPlayer { PlayerID = 3, Position = "1B" }
    };

        _fanProsReportMock
            .Setup(r => r.GenerateAsync(It.IsAny<int>()))
            .ReturnsAsync(csvPlayers);

        await _service.InitialConfigurationAsync();

        _preDraftAdjustRepoMock.Verify(r => r.DeleteAllAsync(), Times.Once);

        _preDraftAdjustRepoMock.Verify(r => r.UpsertAsync(1, 12), Times.Once);
        _preDraftAdjustRepoMock.Verify(r => r.UpsertAsync(2, 24), Times.Once);

        _preDraftAdjustRepoMock.Verify(
            r => r.UpsertAsync(3, It.IsAny<int>()),
            Times.Never);
    }

    [Test]
    public async Task
    InitialConfigurationAsync_DeletesTableBeforeInsert()
    {
        _fanProsReportMock
            .Setup(r => r.GenerateAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<FanProsPlayer>());

        await _service.InitialConfigurationAsync();

        _preDraftAdjustRepoMock.Verify(r => r.DeleteAllAsync(), Times.Once);
    }

}
