using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Importing;
using FBMngt.Services.Players;
using FBMngt.Tests.TestDoubles;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FBMngt.Tests.Services.Importing;

[TestFixture]
public class ImportServiceTests
{
    [Test]
    public async Task ImportPlayersAsync_InsertsMissingFanProsPlayers()
    {
        // Arrange
        var fanProsPlayers = new List<FanProsPlayer>
    {
        new FanProsPlayer { PlayerName = "Aaron Judge" },
        new FanProsPlayer { PlayerName = "Paul Skenes" }
    };

        var csvReaderMock = new Mock<FanProsCsvReader>();
        csvReaderMock
            .Setup(r => r.Read(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(fanProsPlayers);

        var dbPlayers = new List<Player>
    {
        new Player
        {
            PlayerID = 1,
            PlayerName = "Aaron Judge"
        }
    };

        var playerRepoMock = new Mock<IPlayerRepository>();
        playerRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(dbPlayers);

        playerRepoMock
            .Setup(r => r.InsertAsync(It.IsAny<Player>()))
            .Returns(Task.CompletedTask);

        var resolver = new PlayerResolver(playerRepoMock.Object);
        var importer = new PlayerImportService(playerRepoMock.Object);
        var config = new ConfigSettings(new FakeAppSettings());

        var service =
            new ImportService(
                config,
                playerRepoMock.Object,
                resolver,
                importer,
                csvReaderMock.Object);

        // Act
        await service.ImportPlayersAsync("FanPros", null);

        // Assert
        playerRepoMock.Verify(
            r => r.InsertAsync(
                It.Is<Player>(p =>
                    p.PlayerName == "Paul Skenes")),
            Times.Once);

        csvReaderMock.Verify(
            r => r.Read(It.IsAny<string>(), It.IsAny<int>()),
            Times.Once);
    }
}
