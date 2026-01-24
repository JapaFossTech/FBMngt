using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services;
using FBMngt.Services.Players;
using FBMngt.Tests.TestDoubles;
using Moq;

namespace FBMngt.Tests.Services.Players;

[TestFixture]
public class ResolvePlayerIDAsyncTests
{
    private FakeAppSettings _fakeAppSettings = new();
    private List<Player> _dbPlayers;
    private Mock<IPlayerRepository> _repositoryMock;
    private ReportService _reportService;
    private PlayerResolver _playerResolver;

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
                Aka1 = "Micky"
                //BirthDate = new DateTime(1991, 8, 7)
            }
        };

        _repositoryMock = new Mock<IPlayerRepository>();

        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(_dbPlayers);

        _reportService = new ReportService(_fakeAppSettings,
                                        _repositoryMock.Object);

        _playerResolver = new PlayerResolver(
                                        _repositoryMock.Object);
    }

    [Test]
    public async Task 
    GivenExactDbMatch_WhenResolving_ThenPlayerIdIsSet()
    {
        // Arrange
        var players = new List<IPlayer>
        {
            new Player
                {
                    PlayerID = 123,
                    PlayerName = "Mike Trout"
                }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.EqualTo(123));
    }
    [Test]
    public async Task 
    GivenPlayerWithNoDbMatch_WhenResolving_ThenPlayerIdIsBlank()
    {
        // Arrange
        var players = new List<IPlayer>
        {
            new Player
                {
                    PlayerID = 1234,
                    PlayerName = "John Doe"
                }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.Null.Or.Empty);
    }
    [Test]
    public async Task
    GivenPlayerMatchingAka_WhenResolving_ThenPlayerIdIsSet()
    {
        // Arrange
        _dbPlayers[0].Aka1 = "M. Trout";

        var players = new List<IPlayer>
        {
            new Player
            {
                PlayerName = "M. Trout"
            }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.EqualTo(123));
    }
    [Test]
    public async Task
    GivenPlayerNameWithWhitespace_WhenResolving_ThenPlayerIdIsSet()
    {
        // Arrange
        var players = new List<IPlayer>
        {
            new Player
            {
                PlayerName = "  Mike Trout  "
            }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.EqualTo(123));
    }
    [Test]
    public async Task
    GivenDifferentCaseAka_WhenResolving_ThenPlayerIdIsSet()
    {
        // Arrange
        _dbPlayers[0].Aka1 = "M. Trout";

        var players = new List<IPlayer>
        {
            new Player
            {
                PlayerName = "m. trout"
            }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.EqualTo(123));
    }
    [Test]
    public async Task
    GivenDuplicateAkaInDb_WhenResolving_ThenFirstPlayerWins()
    {
        // Arrange
        _dbPlayers.Add(
            new Player
            {
                PlayerID = 999,
                PlayerName = "Fake Trout",
                Aka1 = "M. Trout"
            });

        _dbPlayers[0].Aka1 = "M. Trout"; // original player

        var players = new List<IPlayer>
        {
            new Player
            {
                PlayerName = "M. Trout"
            }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.EqualTo(123));
    }
    [Test]
    public async Task
    GivenPlayerAka_WhenResolving_ThenPlayerIdIsSet()
    {
        // Arrange
        _dbPlayers[0].Aka2 = "Micky";

        var players = new List<IPlayer>
        {
            new Player
            {
                PlayerName = "Micky"
            }
        };

        // Act
        await _playerResolver.ResolvePlayerIDAsync(players);

        // Assert
        Assert.That(players[0].PlayerID, Is.EqualTo(123));
    }

}
