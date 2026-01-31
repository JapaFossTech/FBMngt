using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;

namespace FBMngt.Services.Reporting.FanPros;

// FanPros
//public sealed class FanProsPopulationBuilder
//{
//    public (
//        List<SteamerPitcherProjection> Pitchers,
//        List<SteamerBatterProjection> Hitters
//    )
//    BuildPopulation(
//        List<FanProsPlayer> fanProsPlayers,
//        List<SteamerPitcherProjection> steamerPitchers,
//        List<SteamerBatterProjection> steamerHitters)
//    {
//        Dictionary<int, SteamerPitcherProjection> pitcherMap =
//            steamerPitchers.ToDictionary(p => p.PlayerID.Value);

//        Dictionary<int, SteamerBatterProjection> hitterMap =
//            steamerHitters.ToDictionary(h => h.PlayerID.Value);

//        List<SteamerPitcherProjection> pitchers = new();
//        List<SteamerBatterProjection> hitters = new();

//        foreach (FanProsPlayer fp in fanProsPlayers)
//        {
//            if (fp.IsPitcher)
//            {
//                pitchers.Add(
//                    pitcherMap.TryGetValue(fp.PlayerID.Value, out var p)
//                        ? p
//                        : CreateEmptyPitcher(fp));
//            }
//            else
//            {
//                hitters.Add(
//                    hitterMap.TryGetValue(fp.PlayerID.Value, out var h)
//                        ? h
//                        : CreateEmptyHitter(fp));
//            }
//        }

//        return (pitchers, hitters);
//    }

//    // ZERO-FILL GUARDRAILS

//    private static SteamerPitcherProjection CreateEmptyPitcher(
//        FanProsPlayer fp)
//    {
//        return new SteamerPitcherProjection
//        {
//            PlayerID = fp.PlayerID,
//            PlayerName = fp.PlayerName,
//            W = 0,
//            SV = 0,
//            K = 0,
//            ERA = 0,
//            WHIP = 0
//        };
//    }

//    private static SteamerBatterProjection CreateEmptyHitter(
//        FanProsPlayer fp)
//    {
//        return new SteamerBatterProjection
//        {
//            PlayerID = fp.PlayerID,
//            PlayerName = fp.PlayerName,
//            R = 0,
//            HR = 0,
//            RBI = 0,
//            SB = 0,
//            AVG = 0
//        };
//    }
//}

public class FanProsCoreFieldsReport
    : ReportBase<FanProsPlayer, FanProsPlayer>
{
    private readonly ConfigSettings _configSettings;

    public FanProsCoreFieldsReport(
        IAppSettings appSettings,
        IPlayerRepository playerRepository)
        : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
    }

    protected override Task<List<FanProsPlayer>> ReadAsync(int rows)
    {
        List<FanProsPlayer> items =
            FanProsCsvReader.Read(
                _configSettings.FanPros_Rankings_InputCsv_Path,
                rows);

        return Task.FromResult(items);
    }

    // Convert rows → TSV lines
    protected override List<string> FormatReport(
        List<FanProsPlayer> rows)
    {
        List<string> lines = new();

        lines.Add("PlayerID\tPLAYER NAME\tTEAM\tPOS");

        foreach (FanProsPlayer p in rows.OrderBy(p => p.Rank))
        {
            lines.Add(
                $"{p.PlayerID}\t{p.PlayerName}\t{p.Team}\t{p.Position}");
        }

        return lines;
    }

    protected override Task WriteAsync(List<string> lines)
    {
        File.WriteAllLines(
            _configSettings.FanPros_OutReport_Path,
            lines);

        Console.WriteLine("FanPros Core Fields report generated:");
        Console.WriteLine(_configSettings.FanPros_OutReport_Path);

        return Task.CompletedTask;
    }
}
