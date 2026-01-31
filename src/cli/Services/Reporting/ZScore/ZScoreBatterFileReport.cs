using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;

namespace FBMngt.Services.Reporting.ZScore;

public class ZScoreBatterFileReport
    : ReportBase<SteamerBatterProjection, SteamerBatterProjection>
{
    private readonly ConfigSettings _configSettings;
    private readonly List<FanProsPlayer> _fanProsPlayers;

    public ZScoreBatterFileReport(
        IAppSettings appSettings,
        IPlayerRepository playerRepository,
        List<FanProsPlayer> fanProsPlayers)
        : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
        _fanProsPlayers = fanProsPlayers;
    }

    protected override Task<List<SteamerBatterProjection>> ReadAsync(int rows)
    {
        string inputPath = Path.Combine(
            _configSettings.AppSettings.ProjectionPath,
            $"{_configSettings.AppSettings.SeasonYear}" +
            "_Steamer_Projections_Batters.csv");

        List<SteamerBatterProjection> items =
            CsvReader.ReadBatters(inputPath);

        return Task.FromResult(items);
    }

    protected override Task<List<SteamerBatterProjection>> TransformAsync(
        List<SteamerBatterProjection> input)
    {
        // Build FanPros hitter ID set
        HashSet<int> fanProsHitterIds =
            _fanProsPlayers
                .Where(p => !IsPitcherRole(p.Position))
                .Where(p => p.PlayerID.HasValue)
                .Select(p => p.PlayerID!.Value)
                .ToHashSet();

        // Filter projections to FanPros population
        List<SteamerBatterProjection> hitters =
            input
                .Where(h =>
                    h.PlayerID.HasValue &&
                    fanProsHitterIds.Contains(h.PlayerID.Value))
                .ToList();

        // Calculate Z-scores ONLY on FanPros hitters
        ZScoreService.CalculateHitterZScores(hitters);

        return Task.FromResult(hitters);
    }

    protected override List<string> FormatReport(
        List<SteamerBatterProjection> rows)
    {
        List<string> lines = new();

        lines.Add(
            "PlayerID\tName\tPA\tR\tHR\tRBI\tSB\tAVG\t" +
            "Z_R\tZ_HR\tZ_RBI\tZ_SB\tZ_AVG\tTotalZ");

        foreach (SteamerBatterProjection b
            in rows.OrderByDescending(b => b.TotalZ))
        {
            lines.Add(
                $"{b.PlayerID}\t{b.PlayerName}\t{b.PA}\t" +
                $"{b.R}\t{b.HR}\t{b.RBI}\t{b.SB}\t{b.AVG:F3}\t" +
                $"{b.Z_R:F2}\t{b.Z_HR:F2}\t{b.Z_RBI:F2}\t" +
                $"{b.Z_SB:F2}\t{b.Z_AVG:F2}\t{b.TotalZ:F2}");
        }

        return lines;
    }

    protected override Task WriteAsync(List<string> lines)
    {
        string path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Hitters_ZScores_" +
            $"{_configSettings.AppSettings.SeasonYear}.tsv");

        File.WriteAllLines(path, lines);

        Console.WriteLine("Batter Z-score report generated:");
        Console.WriteLine(path);

        return Task.CompletedTask;
    }
    private static bool IsPitcherRole(string? position)
    {
        if (string.IsNullOrWhiteSpace(position))
            return false;

        return position.StartsWith("SP", AppConst.IGNORE_CASE)
            || position.StartsWith("RP", AppConst.IGNORE_CASE)
            || position.Equals("P", AppConst.IGNORE_CASE);
    }
}
