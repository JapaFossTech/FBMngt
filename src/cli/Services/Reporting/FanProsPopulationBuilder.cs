using FBMngt.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Services.Reporting;

public sealed class FanProsPopulation
{
    public required List<SteamerPitcherProjection> Pitchers { get; init; }
    public required List<SteamerBatterProjection> Hitters { get; init; }
}

public static class FanProsPopulationBuilder
{
    public static FanProsPopulation Build(
        List<FanProsPlayer> fanProsPlayers,
        List<SteamerPitcherProjection> allPitchers,
        List<SteamerBatterProjection> allHitters)
    {
        // Build projection lookups by PlayerID
        Dictionary<int, SteamerPitcherProjection> pitcherLookup =
            allPitchers
                .Where(p => p.PlayerID.HasValue)
                .ToDictionary(p => p.PlayerID!.Value);

        Dictionary<int, SteamerBatterProjection> hitterLookup =
            allHitters
                .Where(h => h.PlayerID.HasValue)
                .ToDictionary(h => h.PlayerID!.Value);

        List<SteamerPitcherProjection> selectedPitchers = new();
        List<SteamerBatterProjection> selectedHitters = new();

        foreach (FanProsPlayer fanPros in fanProsPlayers)
        {
            if (!fanPros.PlayerID.HasValue)
            {
                Console.WriteLine(
                    $"[WARN] FanPros player '{fanPros.PlayerName}' has null PlayerID – skipped");
                continue;
            }

            int playerId = fanPros.PlayerID.Value;

            bool isPitcher = IsPitcherRole(fanPros.Position);

            if (isPitcher)
            {
                if (pitcherLookup.TryGetValue(playerId, out SteamerPitcherProjection? pitcher))
                {
                    selectedPitchers.Add(pitcher);
                }
                else
                {
                    Console.WriteLine(
                        $"[WARN] FanPros pitcher '{fanPros.PlayerName}' (ID {playerId}) " +
                        $"has no pitcher projections – skipped");
                }
            }
            else
            {
                if (hitterLookup.TryGetValue(playerId, out SteamerBatterProjection? hitter))
                {
                    selectedHitters.Add(hitter);
                }
                else
                {
                    Console.WriteLine(
                        $"[WARN] FanPros hitter '{fanPros.PlayerName}' (ID {playerId}) " +
                        $"has no hitter projections – skipped");
                }
            }
        }

        return new FanProsPopulation
        {
            Pitchers = selectedPitchers,
            Hitters = selectedHitters
        };
    }

    private static bool IsPitcherRole(string? fanProsPosition)
    {
        if (string.IsNullOrWhiteSpace(fanProsPosition))
            return false;

        return fanProsPosition.StartsWith("SP", AppConst.IGNORE_CASE)
            || fanProsPosition.StartsWith("RP", AppConst.IGNORE_CASE)
            || fanProsPosition.Equals("P", AppConst.IGNORE_CASE);
    }
}

