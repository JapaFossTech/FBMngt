using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using System.Runtime;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FBMngt.Services;

// Model output
public sealed class ReportResult<TReportRow>
{
    public required List<TReportRow> ReportRows { get; init; }
    public required List<string> StringLines { get; init; }
}

// Base pipeline
public abstract class ReportBase<TInput, TReportRow>
    where TInput : IPlayer
{
    private readonly PlayerResolver _playerResolver;

    protected ReportBase(PlayerResolver playerResolver)
    {
        _playerResolver = playerResolver;
    }

    public async Task<ReportResult<TReportRow>> GenerateAndWriteAsync(
        int rows = 0)
    {
        // 1️ Read
        List<TInput> input = await ReadAsync(rows);

        // 2️ Resolve PlayerIDs (ONCE)
        await _playerResolver.ResolvePlayerIDAsync(
            input.Cast<IPlayer>().ToList());

        // 3️ Transform / calculate
        List<TReportRow> reportRows =
            await TransformAsync(input);

        // 4️ Format
        List<string> lines =
            FormatReport(reportRows);

        // 5️ Persist
        await WriteAsync(lines);

        return new ReportResult<TReportRow>
        {
            ReportRows = reportRows,
            StringLines = lines
        };
    }

    protected abstract Task<List<TInput>> ReadAsync(int rows);

    // DEFAULT: identity transform when possible
    protected virtual Task<List<TReportRow>> TransformAsync(List<TInput> input)
    {
        if (typeof(TReportRow) == typeof(TInput))
        {
            return Task.FromResult(
                input.Cast<TReportRow>().ToList());
        }

        throw new NotSupportedException(
            $"{GetType().Name} must override TransformAsync because " +
            $"{typeof(TInput).Name} ≠ {typeof(TReportRow).Name}");
    }
    protected abstract List<string> FormatReport(List<TReportRow> rows);
    protected abstract Task WriteAsync(List<string> lines);
}

// FanPros
public sealed class FanProsPopulationBuilder
{
    public (
        List<SteamerPitcherProjection> Pitchers,
        List<SteamerBatterProjection> Hitters
    )
    BuildPopulation(
        List<FanProsPlayer> fanProsPlayers,
        List<SteamerPitcherProjection> steamerPitchers,
        List<SteamerBatterProjection> steamerHitters)
    {
        Dictionary<int, SteamerPitcherProjection> pitcherMap =
            steamerPitchers.ToDictionary(p => p.PlayerID.Value);

        Dictionary<int, SteamerBatterProjection> hitterMap =
            steamerHitters.ToDictionary(h => h.PlayerID.Value);

        List<SteamerPitcherProjection> pitchers = new();
        List<SteamerBatterProjection> hitters = new();

        foreach (FanProsPlayer fp in fanProsPlayers)
        {
            if (fp.IsPitcher)
            {
                pitchers.Add(
                    pitcherMap.TryGetValue(fp.PlayerID.Value, out var p)
                        ? p
                        : CreateEmptyPitcher(fp));
            }
            else
            {
                hitters.Add(
                    hitterMap.TryGetValue(fp.PlayerID.Value, out var h)
                        ? h
                        : CreateEmptyHitter(fp));
            }
        }

        return (pitchers, hitters);
    }

    // ----------------------------
    // ZERO-FILL GUARDRAILS
    // ----------------------------

    private static SteamerPitcherProjection CreateEmptyPitcher(
        FanProsPlayer fp)
    {
        return new SteamerPitcherProjection
        {
            PlayerID = fp.PlayerID,
            PlayerName = fp.PlayerName,
            W = 0,
            SV = 0,
            K = 0,
            ERA = 0,
            WHIP = 0
        };
    }

    private static SteamerBatterProjection CreateEmptyHitter(
        FanProsPlayer fp)
    {
        return new SteamerBatterProjection
        {
            PlayerID = fp.PlayerID,
            PlayerName = fp.PlayerName,
            R = 0,
            HR = 0,
            RBI = 0,
            SB = 0,
            AVG = 0
        };
    }
}

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

// Z-scores helper classes
public class CombinedZScoreRow
{
    public int? PlayerID { get; set; }
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";

    public double ZR_ZW { get; set; }
    public double ZHR_ZSV { get; set; }
    public double ZRBI_ZK { get; set; }
    public double ZSB_ZERA { get; set; }
    public double ZAVG_ZWHIP { get; set; }

    public double TotalZ { get; set; }
}
public static class ZScorePopulationValidator
{
    public static void ValidateHitters(
        IEnumerable<SteamerBatterProjection> hitters)
    {
        var invalid = hitters
            .Where(h =>
                h.Z_R == 0 ||
                h.Z_HR == 0 ||
                h.Z_RBI == 0 ||
                h.Z_SB == 0 ||
                h.Z_AVG == 0 ||
                h.TotalZ == 0)
            .ToList();

        if (invalid.Any())
        {
            throw new InvalidOperationException(
                $"Z-score validation failed for {invalid.Count} hitters. " +
                "FanPros population integrity violated.");
        }
    }

    public static void ValidatePitchers(
        IEnumerable<SteamerPitcherProjection> pitchers)
    {
        var invalid = pitchers
            .Where(p =>
                p.Z_W == 0 ||
                p.Z_SV == 0 ||
                p.Z_K == 0 ||
                p.Z_ERA == 0 ||
                p.Z_WHIP == 0 ||
                p.TotalZ == 0)
            .ToList();

        if (invalid.Any())
        {
            throw new InvalidOperationException(
                $"Z-score validation failed for {invalid.Count} pitchers. " +
                "FanPros population integrity violated.");
        }
    }
}

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
                .Where(p => IsPitcherRole(p.Position))
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
    private static bool IsPitcherRole(string? position)
    {
        if (string.IsNullOrWhiteSpace(position))
            return false;

        return position.StartsWith("SP", AppConst.IGNORE_CASE)
            || position.StartsWith("RP", AppConst.IGNORE_CASE)
            || position.Equals("P", AppConst.IGNORE_CASE);
    }

}
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
            bool isPitcher = IsPitcherRole(fanPros.Position);

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
    private static bool IsPitcherRole(string? fanProsPosition)
    {
        if (string.IsNullOrWhiteSpace(fanProsPosition))
            return false;

        return fanProsPosition.StartsWith("SP", AppConst.IGNORE_CASE)
            || fanProsPosition.StartsWith("RP", AppConst.IGNORE_CASE)
            || fanProsPosition.Equals("P", AppConst.IGNORE_CASE);
    }

}

// Service
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
        // Generate FanPros report FIRST (source of truth)
        FanProsCoreFieldsReport fanProsReport =
            new FanProsCoreFieldsReport(
                _configSettings.AppSettings,
                _playerRepository);

        ReportResult<FanProsPlayer> fanProsResult =
            await fanProsReport.GenerateAndWriteAsync();

        List<FanProsPlayer> fanProsPlayers =
            fanProsResult.ReportRows;

        // Generate hitter Z-scores (FanPros-driven)
        ZScoreBatterFileReport hitterReport =
            new ZScoreBatterFileReport(
                _configSettings.AppSettings,
                _playerRepository,
                fanProsPlayers);

        ReportResult<SteamerBatterProjection> hitterResult =
            await hitterReport.GenerateAndWriteAsync();

        // Generate pitcher Z-scores (FanPros-driven)
        ZScorePitcherFileReport pitcherReport =
            new ZScorePitcherFileReport(
                _configSettings.AppSettings,
                _playerRepository,
                fanProsPlayers);

        ReportResult<SteamerPitcherProjection> pitcherResult =
            await pitcherReport.GenerateAndWriteAsync();

        // Generate combined report
        ZScoreCombinedReport combinedReport =
            new ZScoreCombinedReport(
                _configSettings.AppSettings);

        await combinedReport.WriteAsync(
            fanProsPlayers,
            pitcherResult.ReportRows,
            hitterResult.ReportRows);
    }

    // FanProsCoreFields
    public async Task GenerateFanProsCoreFieldsReportAsync(int rows)
    {
        var report = new FanProsCoreFieldsReport(
                                                _configSettings.AppSettings,
                                                _playerRepository
                                                );
        await report.GenerateAndWriteAsync(rows);
    }
}
