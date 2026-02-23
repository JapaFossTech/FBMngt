using FBMngt.Models;
using FBMngt.Services.Reporting.PreDraftRanking;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FBMngt.Tests.Services.Reporting.PreDraftRanking;

[TestFixture]
public class PreDraftRankingMovementCalculatorTests
{
    private PreDraftRankingMovementCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator =
            new PreDraftRankingMovementCalculator();
    }

    private FanProsPlayer P(int id, string name)
    {
        return new FanProsPlayer
        {
            PlayerID = id,
            PlayerName = name
        };
    }

    [Test]
    public void 
    When_Lists_Are_Identical_Returns_All_Zero_Movements()
    {
        var yahoo = new List<FanProsPlayer>
        {
            P(1, "Acuna"),
            P(2, "Julio"),
            P(3, "Witt")
        };

        var target = new List<FanProsPlayer>
        {
            P(1, "Acuna"),
            P(2, "Julio"),
            P(3, "Witt")
        };

        var result =
            _calculator.CalculateMovement(yahoo, target);

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.All(r => r.Movement == 0));
    }

    [Test]
    public void Simple_Swap_Calculates_Correct_Movement()
    {
        var yahoo = new List<FanProsPlayer>
    {
        P(1, "Judge"),
        P(2, "Acuna"),
        P(3, "Witt"),
        P(4, "Julio")
    };

        var target = new List<FanProsPlayer>
    {
        P(2, "Acuna"),
        P(4, "Julio"),
        P(3, "Witt"),
        P(1, "Judge")
    };

        var result =
            _calculator.CalculateMovement(yahoo, target);

        var acuna =
            result.First(r => r.PlayerName == "Acuna");

        var julio =
            result.First(r => r.PlayerName == "Julio");

        var witt =
            result.First(r => r.PlayerName == "Witt");

        var judge =
            result.First(r => r.PlayerName == "Judge");

        Assert.That(acuna.Movement, Is.EqualTo(+1));
        Assert.That(julio.Movement, Is.EqualTo(+2));
        Assert.That(witt.Movement, Is.EqualTo(+1));
        Assert.That(judge.Movement, Is.EqualTo(0));
    }
    [Test]
    public void Move_Player_Up_By_One()
    {
        var yahoo = new List<FanProsPlayer>
    {
        P(1, "A"),
        P(2, "B"),
        P(3, "C")
    };

        var target = new List<FanProsPlayer>
    {
        P(2, "B"),
        P(1, "A"),
        P(3, "C")
    };

        List<PreDraftMovementRow> result =
            _calculator.CalculateMovement(yahoo, target);

        PreDraftMovementRow b =
            result.First(r => r.PlayerName == "B");

        Assert.That(b.CurrentRank, Is.EqualTo(2));
        Assert.That(b.TargetRank, Is.EqualTo(1));
        Assert.That(b.Movement, Is.EqualTo(+1));
    }

    [Test]
    public void 
    Filters_Target_Players_Not_In_Yahoo_Universe()
    {
        var yahoo = new List<FanProsPlayer>
        {
            P(1, "A"),
            P(2, "B")
        };

        var target = new List<FanProsPlayer>
        {
            P(1, "A"),
            P(2, "B"),
            P(99, "NotInYahoo")
        };

        var result =
            _calculator.CalculateMovement(yahoo, target);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(r => r.PlayerID != 99));
    }

    [Test]
    public void 
    Current_And_Target_Rank_Are_Correctly_Tracked()
    {
        var yahoo = new List<FanProsPlayer>
        {
            P(1, "A"),
            P(2, "B"),
            P(3, "C")
        };

        var target = new List<FanProsPlayer>
        {
            P(2, "B"),
            P(1, "A"),
            P(3, "C")
        };

        var result =
            _calculator.CalculateMovement(yahoo, target);

        var b =
            result.First(r => r.PlayerName == "B");

        Assert.That(b.CurrentRank, Is.EqualTo(2));
        Assert.That(b.TargetRank, Is.EqualTo(1));
    }

    [Test]
    public void Handles_Player_Not_Found_In_Simulation()
    {
        var yahoo = new List<FanProsPlayer>
        {
            P(1, "A"),
            P(2, "B")
        };

        var target = new List<FanProsPlayer>
        {
            P(1, "A"),
            P(3, "Ghost"),
            P(2, "B")
        };

        var result =
            _calculator.CalculateMovement(yahoo, target);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(r => r.PlayerID != 3));
    }
}