using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Tests.TestDoubles;
using Moq;

namespace FBMngt.Tests.Services.Players;

[TestFixture]
public class ResolvePlayerIDAsyncTests
{
    private FakeAppSettings _fakeAppSettings = new();
    private List<Player> _dbPlayers;
    private Mock<IPlayerRepository> _playerRepoMock;
    private Mock<IPreDraftAdjustRepository> _preDraftAdjustRepoMock;
    private ReportService _reportService;
    private PlayerResolver _playerResolver;
    private Mock<FanProsCoreFieldsReport> _fanProsReportMock;


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

        _playerRepoMock = new Mock<IPlayerRepository>();

        _playerRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(_dbPlayers);

        _preDraftAdjustRepoMock =
                        new Mock<IPreDraftAdjustRepository>();
        _preDraftAdjustRepoMock
                    .Setup(x => x.GetAllAsync())
                    .ReturnsAsync(new Dictionary<int, int>());

        var configSettings = new ConfigSettings(_fakeAppSettings);

        _fanProsReportMock = new Mock<FanProsCoreFieldsReport>(
            configSettings,
            _playerResolver,           // or mock if needed
            new FanProsCsvReader(),
            _preDraftAdjustRepoMock.Object);


        _reportService = new ReportService(
                            configSettings,
                            _playerRepoMock.Object,
                            _preDraftAdjustRepoMock.Object,
                            _fanProsReportMock.Object);

        _playerResolver = new PlayerResolver(
                                        _playerRepoMock.Object);
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
