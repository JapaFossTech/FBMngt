using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.PreDraftRanking;
using FBMngt.Tests.TestDoubles;
using Moq;
using NUnit.Framework;

namespace FBMngt.Tests.Services;

[TestFixture]
public class PlayerOffsetServiceTests
{
    private Mock<IPlayerRepository> _playerRepoMock;
    private Mock<IPreDraftAdjustRepository> _preDraftAdjustRepoMock = null!;
    private PlayerOffsetService _service = null!;
    private ConfigSettings _config;

    [SetUp]
    public void Setup()
    {
        _playerRepoMock = new Mock<IPlayerRepository>();
        _preDraftAdjustRepoMock = new 
                            Mock<IPreDraftAdjustRepository>();
        _config = new ConfigSettings(new FakeAppSettings());
        _service = new PlayerOffsetService(
                        _config,
                        _playerRepoMock.Object,
                        _preDraftAdjustRepoMock.Object);
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

    //[Test]
    //public async Task 
    //InitialConfigurationAsync_Sets_Catcher_And_Closer_Only()
    //{
    //    // Arrange
    //    var players = new List<Player>
    //    {
    //        new Player { PlayerID = 1, Position = "C" },
    //        new Player { PlayerID = 2, Position = "RP" },
    //        new Player { PlayerID = 3, Position = "1B" }
    //    };

    //    _playerRepoMock
    //        .Setup(r => r.GetAllAsync())
    //        .ReturnsAsync(players);

    //    // Act
    //    await _service.InitialConfigurationAsync();

    //    // Assert
    //    _preDraftAdjustRepoMock.Verify(r => r.DeleteAllAsync(), Times.Once);

    //    _preDraftAdjustRepoMock.Verify(r => r.UpsertAsync(1, 12), Times.Once);
    //    _preDraftAdjustRepoMock.Verify(r => r.UpsertAsync(2, 24), Times.Once);

    //    _preDraftAdjustRepoMock.Verify(
    //        r => r.UpsertAsync(3, It.IsAny<int>()),
    //        Times.Never);
    //}

    //[Test]
    //public async Task 
    //InitialConfigurationAsync_DeletesTableBeforeInsert()
    //{
    //    // Arrange
    //    _playerRepoMock
    //        .Setup(r => r.GetAllAsync())
    //        .ReturnsAsync(new List<Player>());

    //    // Act
    //    await _service.InitialConfigurationAsync();

    //    // Assert
    //    _preDraftAdjustRepoMock.Verify(r => 
    //                            r.DeleteAllAsync(), Times.Once);
    //}
}
