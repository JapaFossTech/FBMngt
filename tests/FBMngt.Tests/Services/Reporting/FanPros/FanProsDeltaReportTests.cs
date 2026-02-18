using FBMngt;
using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Tests.TestDoubles;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FBMngt.Tests.Services.Reporting.FanPros;

[TestFixture]
public class FanProsDeltaReportTests
{
    private Mock<PlayerResolver> _playerResolverMock = null!;
    private Mock<FanProsCsvReader> _csvReaderMock = null!;
    private Mock<IPreDraftAdjustRepository> _adjustRepoMock = null!;
    private Mock<IFileSelectorFactory> _selectorFactoryMock = null!;
    private Mock<IFileSelector> _latestSelectorMock = null!;
    private Mock<IFileSelector> _previousSelectorMock = null!;

    private ConfigSettings _configSettings = null!;
    private FanProsDeltaReport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _configSettings = new ConfigSettings(new FakeAppSettings());

        _csvReaderMock = new Mock<FanProsCsvReader>();
        _adjustRepoMock = new Mock<IPreDraftAdjustRepository>();
        _selectorFactoryMock = new Mock<IFileSelectorFactory>();
        _latestSelectorMock = new Mock<IFileSelector>();
        _previousSelectorMock = new Mock<IFileSelector>();

        // PlayerResolver requires IPlayerRepository
        var playerRepoMock = new Mock<IPlayerRepository>();

        _playerResolverMock =
            new Mock<PlayerResolver>(playerRepoMock.Object)
            { CallBase = false };

        _playerResolverMock
            .Setup(x => x.ResolvePlayerIDAsync(
                It.IsAny<List<IPlayer>>()))
            .Returns<List<IPlayer>>(players =>
            {
                int nextId = 1000;

                foreach (var p in players)
                {
                    p.PlayerID = p.PlayerName switch
                    {
                        "A" => 1,
                        "B" => 2,
                        "C" => 3,
                        _ => nextId++
                    };
                }

                return Task.CompletedTask;
            });




        _adjustRepoMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new Dictionary<int, int>());

        // Factory behavior
        _selectorFactoryMock
            .Setup(x => x.CreateLatest())
            .Returns(_latestSelectorMock.Object);

        _selectorFactoryMock
            .Setup(x => x.CreatePrevious())
            .Returns(_previousSelectorMock.Object);

        _latestSelectorMock
            .Setup(x => x.GetFilePath(It.IsAny<string>()))
            .Returns("latest.csv");

        _previousSelectorMock
            .Setup(x => x.GetFilePath(It.IsAny<string>()))
            .Returns("previous.csv");

        _sut = new FanProsDeltaReport(
            _configSettings,
            _playerResolverMock.Object,
            _csvReaderMock.Object,
            _adjustRepoMock.Object,
            _selectorFactoryMock.Object);
    }

    // ==========================================================
    // 1️⃣ Sorts by absolute move descending
    // ==========================================================
    [Test]
    public async Task GenerateAndWriteAsync_SortsByAbsoluteMoveDescending()
    {
        SetupCsvData(
            current: new[]
            {
                Player("A", 1),
                Player("B", 20),
                Player("C", 10)
            },
            previous: new[]
            {
                Player("A", 3),   // +2
                Player("B", 5),   // -15
                Player("C", 17)   // +7
            });

        var result = await _sut.GenerateAndWriteAsync(0);

        var ordered = result.ReportRows.ToList();

        Assert.That(ordered.Select(x => x.PlayerName),
            Is.EqualTo(new[] { "C", "B", "A" }));
    }

    // ==========================================================
    // 2️⃣ Tie breaker = better current rank first
    // ==========================================================
    [Test]
    public async Task GenerateAndWriteAsync_WhenMovesEqual_SortsByCurrentRank()
    {
        SetupCsvData(
            current: new[]
            {
                Player("A", 5),
                Player("B", 2)
            },
            previous: new[]
            {
                Player("A", 15),
                Player("B", 12)
            });

        var result = await _sut.GenerateAndWriteAsync(0);

        var rows = result.ReportRows.ToList();

        Assert.That(rows[0].PlayerName, Is.EqualTo("B"));
        Assert.That(rows[1].PlayerName, Is.EqualTo("A"));
    }

    // ==========================================================
    // 3️⃣ Zero move last
    // ==========================================================
    [Test]
    public async Task GenerateAndWriteAsync_ZeroMovesAreLast()
    {
        SetupCsvData(
            current: new[]
            {
                Player("Mover", 1),
                Player("Flat", 5)
            },
            previous: new[]
            {
                Player("Mover", 10),
                Player("Flat", 5)
            });

        var result = await _sut.GenerateAndWriteAsync(0);

        var rows = result.ReportRows.ToList();

        Assert.That(rows.Last().PlayerName, Is.EqualTo("Flat"));
    }

    // ==========================================================
    // ======================= Helpers ===========================
    // ==========================================================

    private void SetupCsvData(
    IEnumerable<FanProsPlayer> current,
    IEnumerable<FanProsPlayer> previous)
    {
        _csvReaderMock
            .SetupSequence(x => x.Read(It.IsAny<string>(), It.IsAny<int?>()))
            .Returns(current.ToList())
            .Returns(previous.ToList());
    }


    private static FanProsPlayer Player(string name, int rank)
    {
        return new FanProsPlayer
        {
            PlayerName = name,
            Rank = rank,
            AdjustedRank = rank   // IMPORTANT
        };
    }
}
