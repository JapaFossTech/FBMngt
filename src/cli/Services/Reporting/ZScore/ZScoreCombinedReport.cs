using FBMngt.Models;

namespace FBMngt.Services.Reporting.ZScore;

public sealed class ZScoreCombinedReport
{
    private readonly ConfigSettings _configSettings;

    public ZScoreCombinedReport(IAppSettings appSettings)
    {
        _configSettings = new ConfigSettings(appSettings);
    }

    // TEST-FRIENDLY ENTRY POINT
    public Task<ReportResult<CombinedZScoreRow>> BuildAsync(
        List<FanProsPlayer> fanProsPlayers,
        List<SteamerPitcherProjection> pitchers,
        List<SteamerBatterProjection> hitters)
    {
        List<CombinedZScoreRow> rows =
            BuildCombinedRows(
                fanProsPlayers,
                pitchers,
                hitters);

        List<string> lines =
            FormatCombinedReport(rows);

        ReportResult<CombinedZScoreRow> result =
            new ReportResult<CombinedZScoreRow>
            {
                ReportRows = rows,
                StringLines = lines
            };

        return Task.FromResult(result);
    }

    // CLI ENTRY POINT
    public async Task WriteAsync(
        List<FanProsPlayer> fanProsPlayers,
        List<SteamerPitcherProjection> pitchers,
        List<SteamerBatterProjection> hitters)
    {
        ReportResult<CombinedZScoreRow> result =
            await BuildAsync(
                fanProsPlayers,
                pitchers,
                hitters);

        string path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Combined_ZScores_" +
            $"{_configSettings.AppSettings.SeasonYear}.tsv");

        await File.WriteAllLinesAsync(
            path,
            result.StringLines);

        Console.WriteLine("Combined Z-score report generated:");
        Console.WriteLine(path);
    }

    // FanPros defines population, Steamer defines metrics
    private static List<CombinedZScoreRow> BuildCombinedRows(
        List<FanProsPlayer> fanProsPlayers,
        List<SteamerPitcherProjection> pitchers,
        List<SteamerBatterProjection> hitters)
    {
        Dictionary<int, SteamerPitcherProjection> pitcherLookup =
            pitchers
                .Where(p => p.PlayerID.HasValue)
                .ToDictionary(
                    p => p.PlayerID!.Value,
                    p => p);

        //var x = hitters.Where(p => p.PlayerID == 1944);

        Dictionary<int, SteamerBatterProjection> hitterLookup =
            hitters
                .Where(h => h.PlayerID.HasValue)
                .ToDictionary(
                    h => h.PlayerID!.Value,
                    h => h);

        List<CombinedZScoreRow> rows = new();

        foreach (FanProsPlayer fanPros in fanProsPlayers)
        {
            if (!fanPros.PlayerID.HasValue)
            {
                Console.WriteLine(
                    $"FanPros player '{fanPros.PlayerName}' has "
                    +"null PlayerID. Is not included in combined report.");
                continue;
            }

            int playerId = fanPros.PlayerID.Value;

            // FanPros roster slot defines role (SP1, RP2, Util, etc.)
            bool isPitcher = fanPros.IsPitcher();

            CombinedZScoreRow? row = null;

            if (isPitcher)
            {
                if (pitcherLookup.TryGetValue(playerId, out SteamerPitcherProjection? pitcher))
                {
                    row = FromPitcher(pitcher);
                }
            }
            else
            {
                if (hitterLookup.TryGetValue(playerId, out SteamerBatterProjection? hitter))
                {
                    row = FromHitter(hitter);
                }
            }

            // Projection coverage gap → skip but continue
            if (row == null)
            {
                // TODO: replace with ILogger once wired
                Console.WriteLine(
                    $"[WARN] FanPros player '{fanPros.PlayerName}' (ID {playerId}) " +
                    $"has no {(isPitcher ? "pitcher" : "hitter")} projections — skipped");

                continue;
            }

            rows.Add(row);
        }

        return rows
            .OrderByDescending(r => r.TotalZ)
            .ToList();
    }

    // Pitcher mapper
    private static CombinedZScoreRow FromPitcher(
        SteamerPitcherProjection p)
    {
        return new CombinedZScoreRow
        {
            PlayerID = p.PlayerID!.Value,
            PlayerName = p.PlayerName,
            Position = "P",

            ZR_ZW = p.Z_W,
            ZHR_ZSV = p.Z_SV,
            ZRBI_ZK = p.Z_K,
            ZSB_ZERA = p.Z_ERA,
            ZAVG_ZWHIP = p.Z_WHIP,

            TotalZ = p.TotalZ
        };
    }

    // Hitter mapper
    private static CombinedZScoreRow FromHitter(
        SteamerBatterProjection h)
    {
        return new CombinedZScoreRow
        {
            PlayerID = h.PlayerID!.Value,
            PlayerName = h.PlayerName,
            Position = "B",

            ZR_ZW = h.Z_R,
            ZHR_ZSV = h.Z_HR,
            ZRBI_ZK = h.Z_RBI,
            ZSB_ZERA = h.Z_SB,
            ZAVG_ZWHIP = h.Z_AVG,

            TotalZ = h.TotalZ
        };
    }

    // TSV formatting
    private static List<string> FormatCombinedReport(
        List<CombinedZScoreRow> rows)
    {
        List<string> lines = new();

        lines.Add(
            "PlayerID\tName\tPos\t" +
            "ZR_ZW\tZHR_ZSV\tZRBI_ZK\tZSB_ZERA\tZAVG_ZWHIP\tTotalZ");

        foreach (CombinedZScoreRow r in rows)
        {
            lines.Add(
                $"{r.PlayerID}\t{r.PlayerName}\t{r.Position}\t" +
                $"{r.ZR_ZW:F2}\t{r.ZHR_ZSV:F2}\t{r.ZRBI_ZK:F2}\t" +
                $"{r.ZSB_ZERA:F2}\t{r.ZAVG_ZWHIP:F2}\t{r.TotalZ:F2}");
        }

        return lines;
    }
}
