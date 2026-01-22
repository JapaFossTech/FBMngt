using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Reporting;
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FBMngt.Services;

public class ReportService
{
    private readonly IReportPathProvider _reportPathProvider;

    public ReportService(IReportPathProvider reportPathProvider)
    {
        _reportPathProvider = reportPathProvider;
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
        var players = await repo.GetPlayersAsync();

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
                    AppContext.ProjectionPath,
                    $"{AppContext.SeasonYear}_Steamer_Projections_Batters.csv");
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
                    PlayerID = dbPlayer.PlayerID,
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
        var players = await repo.GetPlayersAsync();

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
                AppContext.ProjectionPath,
                $"{AppContext.SeasonYear}_Steamer_Projections_Pitchers.csv"));

        var matched = pitchers
            .Where(p => lookup.ContainsKey(p.PlayerName.Trim()))
            .Select(p => new MatchedPitcher
            {
                PlayerID = lookup[p.PlayerName.Trim()].PlayerID,
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

    private static void WriteHitterReport(List<MatchedHitter> hitters)
    {
        var outputPath = Path.Combine(
    AppContext.ReportPath,
    $"{AppConst.APP_NAME}_Hitters_ZScores_{AppContext.SeasonYear}.tsv");


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
    private static void WritePitcherReport(
                                List<MatchedPitcher> pitchers)
    {
        var path = Path.Combine(
            AppContext.ReportPath,
            $"{AppConst.APP_NAME}_Pitchers_ZScores_{
                                    AppContext.SeasonYear}.tsv");

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
    public Task GenerateFanProsCoreFieldsReportAsync(int rows)
    {
        //// 1) Find the FanPros CSV file
        //    pathToCsv = resolve CSV location(possibly from config or convention)

        //// 2) Parse the CSV file into structured rows
        ////    – CSV has columns: RK, PLAYER NAME, TEAM, POS, BEST, WORST, AVG, etc.
        //rawRows = CsvParser.Read(pathToCsv)

        //// 3) Normalize the raw rows
        ////    – Remove header row from CSV
        ////    – Normalize player name (strip parenthesis, trim, etc.)
        //normalizedRows = rawRows.Select(row => new
        //{
        //    PlayerName = Normalize(row["PLAYER NAME"]),
        //    Team = row["TEAM"],
        //    Pos = row["POS"]
        //})

        //// 4) Limit to top N (rows parameter)
        //selectedRows = normalizedRows.Take(rows)

        //// 5) Resolve PlayerID for each selected row
        ////    – Use IPlayerIdResolver (abstracted service)
        //enrichedRows = For each selected row:
        //    {
        //        Id = playerResolver.ResolvePlayerId(row.PlayerName)
        //        PlayerName = row.PlayerName
        //        Team = row.Team
        //        Pos = row.Pos
        //    }

        //    // 6) Write the output file
        //    //    – First line = tab delimited header:
        //    //         PlayerID   PLAYER NAME   TEAM   POS
        //    //    – Then one line per enriched row:
        //    //         {PlayerID}\t{PlayerName}\t{Team}\t{Pos}
        //    //    – Use AppContext.ReportPath via injected provider
        //    outputFilePath = combine ReportPath with $"FBMngt_FanPros_CoreFields_{AppContext.SeasonYear}.tsv"
        //open file for writing
        //write header line
        //for each enriched row:
        //    write row data

        //// 7) Return
        //return
        var reportPath = _reportPathProvider.ReportPath;

        if (!Directory.Exists(reportPath))
        {
            Directory.CreateDirectory(reportPath);
        }

        var filePath = Path.Combine(
            reportPath,
            $"FBMngt_FanPros_CoreFields_{AppContext.SeasonYear}.tsv");

        var lines = new List<string>
    {
        "PlayerID\tPLAYER NAME\tTEAM\tPOS"
    };

        for (int i = 0; i < rows; i++)
        {
            lines.Add(string.Empty); // placeholder data row
        }

        File.WriteAllLines(filePath, lines);

        return Task.CompletedTask;
    }

}
