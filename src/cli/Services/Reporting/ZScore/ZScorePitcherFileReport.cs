using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;

namespace FBMngt.Services.Reporting.ZScore;

public class ZScorePitcherFileReport
    : ReportBase<SteamerPitcherProjection, SteamerPitcherProjection>
{
    private readonly ConfigSettings _configSettings;
    private readonly List<FanProsPlayer> _fanProsPlayers;

    public ZScorePitcherFileReport(
        IAppSettings appSettings,
        IPlayerRepository playerRepository,
        List<FanProsPlayer> fanProsPlayers)
        : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
        _fanProsPlayers = fanProsPlayers;
    }

    protected override Task<List<SteamerPitcherProjection>> ReadAsync(int rows)
    {
        string inputPath = Path.Combine(
            _configSettings.AppSettings.ProjectionPath,
            $"{_configSettings.AppSettings.SeasonYear}" +
            "_Steamer_Projections_Pitchers.csv");

        List<SteamerPitcherProjection> items =
            CsvReader.ReadPitchers(inputPath);

        return Task.FromResult(items);
    }

    protected override Task<List<SteamerPitcherProjection>> TransformAsync(
        List<SteamerPitcherProjection> input)
    {
        // Build FanPros pitcher ID set
        HashSet<int> fanProsPitcherIds =
            _fanProsPlayers
                .Where(p => p.IsPitcher())
                .Where(p => p.PlayerID.HasValue)
                .Select(p => p.PlayerID!.Value)
                .ToHashSet();

        // Filter projections to FanPros population
        List<SteamerPitcherProjection> pitchers =
            input
                .Where(p =>
                    p.PlayerID.HasValue &&
                    fanProsPitcherIds.Contains(p.PlayerID.Value))
                .ToList();

        // Calculate Z-scores ONLY on FanPros pitchers
        ZScoreService.CalculatePitcherZScores(pitchers);

        return Task.FromResult(pitchers);
    }

    protected override List<string> FormatReport(
        List<SteamerPitcherProjection> rows)
    {
        List<string> lines = new();

        lines.Add(
            "PlayerID\tName\tIP\tW\tK\tSV\tERA\tWHIP\t" +
            "Z_W\tZ_K\tZ_SV\tZ_ERA\tZ_WHIP\tTotalZ");

        foreach (SteamerPitcherProjection p
            in rows.OrderByDescending(p => p.TotalZ))
        {
            lines.Add(
                $"{p.PlayerID}\t{p.PlayerName}\t{p.IP}\t" +
                $"{p.W}\t{p.K}\t{p.SV}\t{p.ERA:F2}\t{p.WHIP:F3}\t" +
                $"{p.Z_W:F2}\t{p.Z_K:F2}\t{p.Z_SV:F2}\t" +
                $"{p.Z_ERA:F2}\t{p.Z_WHIP:F2}\t{p.TotalZ:F2}");
        }

        return lines;
    }

    protected override Task WriteAsync(List<string> lines)
    {
        string path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Pitchers_ZScores_" +
            $"{_configSettings.AppSettings.SeasonYear}.tsv");

        File.WriteAllLines(path, lines);

        Console.WriteLine("Pitcher Z-score report generated:");
        Console.WriteLine(path);

        return Task.CompletedTask;
    }
}
