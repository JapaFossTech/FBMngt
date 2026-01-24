using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FBMngt.Services;

public class ReportService
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;

    public ReportService(IAppSettings appSettings, 
                        IPlayerRepository playerRepository)
    {
        _configSettings = new ConfigSettings(appSettings);
        _playerRepository = playerRepository;
    }

    // ZScoreReports
    public async Task GenerateZScoreReportsAsync()
    {
        await GenerateHitterZScoreReportAsync();
        await GeneratePitcherZScoreReportAsync();
    }

    private async Task GenerateHitterZScoreReportAsync()
    {
        // 1. Load DB players
        var repo = new PlayerRepository();
        var players = await repo.GetAllAsync();

        var lookup = new Dictionary<string, Player>(
                                StringComparer.OrdinalIgnoreCase);

        foreach (var p in players)
        {
            AddLookup(lookup, p.PlayerName, p);
            AddLookup(lookup, p.Aka1, p);
            AddLookup(lookup, p.Aka2, p);
        }

        // 2. Load hitter projections
        string fullPath = Path.Combine(
                    _configSettings.AppSettings.ProjectionPath,
                    $"{_configSettings.AppSettings.SeasonYear}_Steamer_Projections_Batters.csv");
        var hitters = CsvReader.ReadBatters(fullPath);

        var draftPool = ProjectionPoolService
            .GetDraftableHitters(hitters);

        ZScoreService.CalculateHitterZScores(draftPool);

        var top = draftPool
            .OrderByDescending(h => h.TotalZ)
            .Take(10)
            .ToList();

        Console.WriteLine("Top 10 Hitters by Z-score:");
        foreach (var h in top)
        {
            Console.WriteLine(
                $"{h.PlayerName,-22} Z={h.TotalZ,6:F2}");
        }

        // 3. Attach PlayerID + matched name
        var matched = new List<MatchedHitter>();

        foreach (var h in hitters)
        {
            if (lookup.TryGetValue(h.PlayerName.Trim(), 
                                   out var dbPlayer))
            {
                matched.Add(new MatchedHitter
                {
                    PlayerID = dbPlayer.PlayerID!.Value,
                    MatchedName = h.PlayerName,
                    Projection = h
                });
            }
        }

        // 4. Limit to draft-relevant pool
        matched = matched
            .OrderByDescending(m => m.Projection.HR + m.Projection.RBI)
            .Take(150)
            .ToList();

        // 5. Calculate Z-scores
        ZScoreService.CalculateHitterZScores(
            matched.Select(m => m.Projection).ToList());

        // 6. Write report
        WriteHitterReport(matched);
    }
    private async Task GeneratePitcherZScoreReportAsync()
    {
        var repo = new PlayerRepository();
        var players = await repo.GetAllAsync();

        Dictionary<string, Player> lookup = new(
                                    StringComparer.OrdinalIgnoreCase);

        foreach (var p in players)
        {
            AddLookup(lookup, p.PlayerName, p);
            AddLookup(lookup, p.Aka1, p);
            AddLookup(lookup, p.Aka2, p);
        }

        var pitchers = CsvReader.ReadPitchers(
            Path.Combine(
                _configSettings.AppSettings.ProjectionPath,
                $"{_configSettings.AppSettings.SeasonYear}_Steamer_Projections_Pitchers.csv"));

        var matched = pitchers
            .Where(p => lookup.ContainsKey(p.PlayerName.Trim()))
            .Select(p => new MatchedPitcher
            {
                PlayerID = lookup[p.PlayerName.Trim()].PlayerID!.Value,
                MatchedName = p.PlayerName,
                Projection = p
            })
            .ToList();

        var pool = ProjectionPoolService
            .GetDraftablePitchers(
                matched.Select(m => m.Projection).ToList());

        ZScoreService.CalculatePitcherZScores(pool);

        WritePitcherReport(
            matched
                .Where(m => pool.Contains(m.Projection))
                .ToList());
    }

    private void WriteHitterReport(List<MatchedHitter> hitters)
    {
        var outputPath = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Hitters_ZScores_{
                _configSettings.AppSettings.SeasonYear}.tsv");


        using var writer = new StreamWriter(outputPath);

        writer.WriteLine(
            "PlayerID\tName\tPA\tR\tHR\tRBI\tSB\tAVG\t" +
            "Z_R\tZ_HR\tZ_RBI\tZ_SB\tZ_AVG\tTotalZ");

        foreach (var h in hitters.OrderByDescending(
                                        h => h.Projection.TotalZ))
        {
            var p = h.Projection;

            writer.WriteLine(
                $"{h.PlayerID}\t{h.MatchedName}\t{p.PA}\t" +
                $"{p.R}\t{p.HR}\t{p.RBI}\t{p.SB}\t{p.AVG:F3}\t" +
                $"{p.Z_R:F2}\t{p.Z_HR:F2}\t{p.Z_RBI:F2}\t{p.Z_SB:F2}\t{p.Z_AVG:F2}\t" +
                $"{p.TotalZ:F2}");
        }

        Console.WriteLine($"Z-score report generated:");
        Console.WriteLine(outputPath);
    }
    private void WritePitcherReport(
                                List<MatchedPitcher> pitchers)
    {
        var path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Pitchers_ZScores_{
                    _configSettings.AppSettings.SeasonYear}.tsv");

        using var writer = new StreamWriter(path);

        writer.WriteLine(
            "PlayerID\tName\tIP\tW\tK\tSV\tERA\tWHIP\t" +
            "Z_W\tZ_K\tZ_SV\tZ_ERA\tZ_WHIP\tTotalZ");

        foreach (var p in pitchers
            .OrderByDescending(p => p.Projection.TotalZ))
        {
            var s = p.Projection;

            writer.WriteLine(
                $"{p.PlayerID}\t{p.MatchedName}\t{s.IP}\t" +
                $"{s.W}\t{s.K}\t{s.SV}\t{s.ERA:F2}\t{s.WHIP:F3}\t" +
                $"{s.Z_W:F2}\t{s.Z_K:F2}\t{s.Z_SV:F2}\t" +
                $"{s.Z_ERA:F2}\t{s.Z_WHIP:F2}\t{s.TotalZ:F2}");
        }

        Console.WriteLine("Pitcher Z-score report generated:");
        Console.WriteLine(path);
    }
    private static void AddLookup(
                            Dictionary<string, Player> lookup,
                            string? name,
                            Player player)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var key = name.Trim();

        // FIRST one wins
        if (lookup.ContainsKey(key))
        {
            // TEMP: debug only
            Console.WriteLine($"Duplicate name ignored: {key}");
            return;
        }
        else
        {
            lookup[key] = player;
        }
    }

    // FanProsCoreFields
    public async Task GenerateFanProsCoreFieldsReportAsync(int rows)
    {
        // Read from FanPros CSV file

        List<FanProsPlayer> fanProsPlayers = FanProsCsvReader.Read(
                            _configSettings.FanPros_Rankings_InputCsv_Path, 
                            rows);

        // Resolve PlayerIDs (batch, once)
        var resolver = new PlayerResolver(_playerRepository);

        await resolver.ResolvePlayerIDAsync(
                        fanProsPlayers.Cast<IPlayer>().ToList());

        // Write the output file
        WriteFanProsReport(fanProsPlayers);
    }
    private void WriteFanProsReport(List<FanProsPlayer> fanProsPlayers)
    {
        using var writer = new StreamWriter(_configSettings.FanPros_OutReport_Path);

        writer.WriteLine("PlayerID\tPLAYER NAME\tTEAM\tPOS");

        foreach (var p in fanProsPlayers.OrderBy(p => p.Rank))
        {
            writer.WriteLine(
                $"{p.PlayerID}\t{p.PlayerName}\t{p.Team}\t{p.Position}");
        }

        Console.WriteLine("Pitcher Z-score report generated:");
        Console.WriteLine(_configSettings.FanPros_OutReport_Path);
    }
}
