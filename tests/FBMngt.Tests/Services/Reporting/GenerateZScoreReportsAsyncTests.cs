using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.ZScore;
using FBMngt.Tests.TestDoubles;
using NUnit.Framework;

namespace FBMngt.Tests.Services.Reporting;

[TestFixture]
public sealed class GenerateZScoreReportsAsyncTests
{
    private FakeAppSettings _fakeAppSettings = new();
    private ZScoreCombinedReport _combinedZScoreReport;
    private ReportResult<SteamerPitcherProjection> _pitcherResult;
    private ReportResult<SteamerBatterProjection> _hitterResult;
    private List<FanProsPlayer> _fanProsPlayers;

    public static class TestPitchers
    {
        public static List<SteamerPitcherProjection> WithZScores()
        {
            return new List<SteamerPitcherProjection>
            {
                new SteamerPitcherProjection
                {
                    PlayerID = 1,
                    PlayerName = "Andrés Muñoz",
                    Z_W = 1.8,
                    Z_SV = 2.1,
                    Z_K = 0.9,
                    Z_ERA = 0.5,
                    Z_WHIP = 0.7,
                    TotalZ = 6.0
                }
            };
        }
    }

    public static class TestHitters
    {
        public static List<SteamerBatterProjection> WithZScores()
        {
            return new List<SteamerBatterProjection>
            {
                new SteamerBatterProjection
                {
                    PlayerID = 2,
                    PlayerName = "Jurickson Profar",
                    Z_R = 1.8,
                    Z_HR = 2.1,
                    Z_RBI = 0.9,
                    Z_SB = 0.5,
                    Z_AVG = 0.7,
                    TotalZ = 6.0
                }
            };
        }
    }

    [SetUp]
    public void Setup()
    {
        _combinedZScoreReport =
            new ZScoreCombinedReport(_fakeAppSettings);

        _pitcherResult =
            new ReportResult<SteamerPitcherProjection>
            {
                ReportRows = TestPitchers.WithZScores(),
                StringLines = new List<string>()
            };

        _hitterResult =
            new ReportResult<SteamerBatterProjection>
            {
                ReportRows = TestHitters.WithZScores(),
                StringLines = new List<string>()
            };

        _fanProsPlayers =
            new List<FanProsPlayer>
            {
                new FanProsPlayer
                {
                    PlayerID = 1,
                    PlayerName = "Andres Munoz"
                },
                new FanProsPlayer
                {
                    PlayerID = 2,
                    PlayerName = "Jurickson Profar"
                }
            };
    }

    [Test]
    public async Task
    GivenPitcherWithZScores_WhenBuildingCombinedReport_ThenTotalZIsNonZero()
    {
        // Act
        ReportResult<CombinedZScoreRow> result =
            await _combinedZScoreReport.BuildAsync(
                _fanProsPlayers,
                _pitcherResult.ReportRows,
                _hitterResult.ReportRows);

        // Assert
        CombinedZScoreRow row =
            result.ReportRows.Single(r => r.PlayerID == 1);

        Assert.That(row.TotalZ, Is.Not.EqualTo(0));
    }

    [Test]
    [Ignore("FanPros population not yet integrated into Z-score pipeline")]
    public async Task
    GivenFanProsAndSteamerOrderMismatch_WhenGeneratingZScores_ThenAllFanProsPlayersAreScored()
    {
        // Act
        ReportResult<CombinedZScoreRow> result =
            await _combinedZScoreReport.BuildAsync(
                _fanProsPlayers,
                _pitcherResult.ReportRows,
                _hitterResult.ReportRows);

        // Assert
        foreach (FanProsPlayer fanPros in _fanProsPlayers)
        {
            Assert.That(
                result.ReportRows.Any(r => r.PlayerID == fanPros.PlayerID),
                Is.True,
                $"Missing Z-score for FanPros player {fanPros.PlayerName}");
        }
    }

    [Test]
    public async Task
    GivenFanProsPlayers_WhenGeneratingCombinedZScoreReport_ThenNoZScoresAreMissing()
    {
        // Act
        ReportResult<CombinedZScoreRow> result =
            await _combinedZScoreReport.BuildAsync(
                _fanProsPlayers,
                _pitcherResult.ReportRows,
                _hitterResult.ReportRows);

        // Assert
        List<CombinedZScoreRow> missing =
            result.ReportRows
                .Where(r => r.TotalZ == 0)
                .ToList();

        Assert.That(
            missing,
            Is.Empty,
            "Combined report must not introduce zeroed Z-scores");
    }
}
