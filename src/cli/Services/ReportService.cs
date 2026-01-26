using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using System.Runtime;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FBMngt.Services;

public sealed class ReportResult<TReportRow>
{
    public required List<TReportRow> Rows { get; init; }
    public required List<string> Lines { get; init; }
}

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
            Rows = reportRows,
            Lines = lines
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

public class ZScorePitcherFileReport
    : ReportBase<SteamerPitcherProjection, SteamerPitcherProjection>
{
    private readonly ConfigSettings _configSettings;

    public ZScorePitcherFileReport(
        IAppSettings appSettings,
        IPlayerRepository playerRepository)
        : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
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
        // Limit population
        List<SteamerPitcherProjection> pitchers =
            input.Take(120).ToList();

        // Calculate Z-scores
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
public class ZScoreBatterFileReport
    : ReportBase<SteamerBatterProjection, SteamerBatterProjection>
{
    private readonly ConfigSettings _configSettings;

    public ZScoreBatterFileReport(
        IAppSettings appSettings,
        IPlayerRepository playerRepository)
        : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
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
        // Limit population
        List<SteamerBatterProjection> batters =
            input.Take(150).ToList();

        // Calculate Z-scores
        ZScoreService.CalculateHitterZScores(batters);

        return Task.FromResult(batters);
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
}


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
        var hitterReport = new ZScoreBatterFileReport(
                                            _configSettings.AppSettings,
                                            _playerRepository
                                            );
        ReportResult<SteamerBatterProjection> hitterResult =
            await hitterReport.GenerateAndWriteAsync();
        //
        var pitcherReport = new ZScorePitcherFileReport(
                                                _configSettings.AppSettings,
                                                _playerRepository);

        ReportResult<SteamerPitcherProjection> pitcherResult =
            await pitcherReport.GenerateAndWriteAsync();

        //await _combinedReport.WriteAsync(
        //    pitcherResult.Rows,
        //    hitterResult.Rows);
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
