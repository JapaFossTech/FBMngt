using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using System.Runtime;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FBMngt.Services;

public abstract class ReportBase<TInput, TOutput>
{
    protected readonly PlayerResolver _playerResolver;

    protected ReportBase(PlayerResolver playerResolver)
    {
        _playerResolver = playerResolver;
    }

    public async Task GenerateAsync()
    {
        await GenerateAsync(0);
    }
    public async Task GenerateAsync(int rows)
    {
        // 1. Read
        var input = await ReadAsync(rows);

        // 2. Resolve PlayerIDs (shared)
        await _playerResolver.ResolvePlayerIDAsync(
            input.Cast<IPlayer>().ToList());

        // 3. Transform (optional)
        List<TOutput> output = await TransformAsync(input);

        // 4. Write
        await WriteAsync(output);
    }

    protected abstract Task<List<TInput>> ReadAsync(int rows);

    protected virtual Task<List<TOutput>> TransformAsync(List<TInput> input)
        => Task.FromResult(input.Cast<TOutput>().ToList());

    protected abstract Task WriteAsync(List<TOutput> output);
}
public class FanProsCoreFieldsReport : ReportBase<FanProsPlayer, FanProsPlayer>
{
    private readonly ConfigSettings _configSettings;

    public FanProsCoreFieldsReport(
                                    IAppSettings appSettings,
                                    IPlayerRepository playerRepository
                                    )
                                    : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
    }

    protected override Task<List<FanProsPlayer>> ReadAsync(int rows)
    {
        //no async because Read() is synchronous
        var items = FanProsCsvReader.Read(
            _configSettings.FanPros_Rankings_InputCsv_Path, rows);
        return Task.FromResult(items);
    }

    protected override async Task WriteAsync(List<FanProsPlayer> players)
    {
        // delegation to existing method
        await WriteFanProsReport(players);
    }

    private Task WriteFanProsReport(List<FanProsPlayer> fanProsPlayers)
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
        return Task.CompletedTask;
    }
}
public class ZScoreReport_Pitcher
               : ReportBase<SteamerPitcherProjection, SteamerPitcherProjection>
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;

    public ZScoreReport_Pitcher(
                                IAppSettings appSettings,
                                IPlayerRepository playerRepository
                                )
                                : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
        _playerRepository = playerRepository;
    }

    protected override Task<List<SteamerPitcherProjection>> ReadAsync(int rows)
    {
        string inputPath = Path.Combine(
                _configSettings.AppSettings.ProjectionPath,
                $"{_configSettings.AppSettings.SeasonYear
                    }_Steamer_Projections_Pitchers.csv");

        var items = CsvReader.ReadPitchers(inputPath);
    
        return Task.FromResult(items);
    }

    protected override async Task<List<SteamerPitcherProjection>> 
                    TransformAsync( List<SteamerPitcherProjection> input)
    {
        // Read from database

        var players = await _playerRepository.GetAllAsync();

        // Create a PlayerName lookup including AKAs

        Dictionary<string, Player> lookup = new(
                                    StringComparer.OrdinalIgnoreCase);

        foreach (var p in players)
        {
            AddLookup(lookup, p.PlayerName, p);
            AddLookup(lookup, p.Aka1, p);
            AddLookup(lookup, p.Aka2, p);
        }

        // Take 120 players to be considered for zscore calculation

        input = input
            //.OrderByDescending(p => p.K + p.SV)
            .Take(120)
            .ToList();

        // Calculate Zscore

        ZScoreService.CalculatePitcherZScores(input);

        return input;
    }

    protected override Task WriteAsync(List<SteamerPitcherProjection> rows)
    {
        WritePitcherReport(rows);
        return Task.CompletedTask;
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

    private void WritePitcherReport(List<SteamerPitcherProjection> pitchers)
    {
        var path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Pitchers_ZScores_{_configSettings.AppSettings.SeasonYear}.tsv");

        using var writer = new StreamWriter(path);

        writer.WriteLine(
            "PlayerID\tName\tIP\tW\tK\tSV\tERA\tWHIP\t" +
            "Z_W\tZ_K\tZ_SV\tZ_ERA\tZ_WHIP\tTotalZ");

        foreach (SteamerPitcherProjection p in pitchers
            .OrderByDescending(p => p.TotalZ))
        {
            //var s = p.Projection;

            writer.WriteLine(
                $"{p.PlayerID}\t{p.PlayerName}\t{p.IP}\t" +
                $"{p.W}\t{p.K}\t{p.SV}\t{p.ERA:F2}\t{p.WHIP:F3}\t" +
                $"{p.Z_W:F2}\t{p.Z_K:F2}\t{p.Z_SV:F2}\t" +
                $"{p.Z_ERA:F2}\t{p.Z_WHIP:F2}\t{p.TotalZ:F2}");
        }

        Console.WriteLine("Pitcher Z-score report generated:");
        Console.WriteLine(path);
    }
}
public class ZScoreReport_Batter
               : ReportBase<SteamerBatterProjection, SteamerBatterProjection>
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;

    public ZScoreReport_Batter(
                                IAppSettings appSettings,
                                IPlayerRepository playerRepository
                                )
                                : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
        _playerRepository = playerRepository;
    }

    protected override Task<List<SteamerBatterProjection>> ReadAsync(int rows)
    {
        string inputPath = Path.Combine(
                _configSettings.AppSettings.ProjectionPath,
                $"{_configSettings.AppSettings.SeasonYear
                    }_Steamer_Projections_Batters.csv");

        var items = CsvReader.ReadBatters(inputPath);

        return Task.FromResult(items);
    }

    protected override async Task<List<SteamerBatterProjection>>
                    TransformAsync(List<SteamerBatterProjection> input)
    {
        // Read from database

        var players = await _playerRepository.GetAllAsync();

        // Create a PlayerName lookup including AKAs

        Dictionary<string, Player> lookup = new(
                                    StringComparer.OrdinalIgnoreCase);

        foreach (var p in players)
        {
            AddLookup(lookup, p.PlayerName, p);
            AddLookup(lookup, p.Aka1, p);
            AddLookup(lookup, p.Aka2, p);
        }

        // Take 120 players to be considered for zscore calculation

        input = input
            //.OrderByDescending(h => h.HR + h.RBI)
            .Take(150)
            .ToList();

        // Calculate Zscore

        ZScoreService.CalculateHitterZScores(input);

        return input;
    }

    protected override Task WriteAsync(List<SteamerBatterProjection> rows)
    {
        WriteHitterReport(rows);
        return Task.CompletedTask;
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
    private void WriteHitterReport(List<SteamerBatterProjection> pitchers)
    {
        var path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Hitters_ZScores_{_configSettings
                                                .AppSettings.SeasonYear}.tsv");

        using var writer = new StreamWriter(path);

        writer.WriteLine(
            "PlayerID\tName\tPA\tR\tHR\tRBI\tSB\tAVG\t" +
            "Z_R\tZ_HR\tZ_RBI\tZ_SB\tZ_AVG\tTotalZ");

        foreach (SteamerBatterProjection b in pitchers
            .OrderByDescending(b => b.TotalZ))
        {
            writer.WriteLine(
                $"{b.PlayerID}\t{b.PlayerName}\t{b.PA}\t" +
                $"{b.R}\t{b.HR}\t{b.RBI}\t{b.SB}\t{b.AVG:F3}\t" +
                $"{b.Z_R:F2}\t{b.Z_HR:F2}\t{b.Z_RBI:F2}\t{b.Z_SB:F2}\t{b.Z_AVG:F2}\t" +
                $"{b.TotalZ:F2}");
        }

        Console.WriteLine("Batter Z-score report generated:");
        Console.WriteLine(path);
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
        await GenerateHitterZScoreReportAsync();
        await GeneratePitcherZScoreReportAsync();
    }

    private async Task GenerateHitterZScoreReportAsync()
    {
        var report = new ZScoreReport_Batter(
                                            _configSettings.AppSettings,
                                            _playerRepository
                                            );
        await report.GenerateAsync();
    }
    private async Task GeneratePitcherZScoreReportAsync()
    {
        var report = new ZScoreReport_Pitcher(
                                                _configSettings.AppSettings,
                                                _playerRepository
                                                );
        await report.GenerateAsync();
    }

    // FanProsCoreFields
    public async Task GenerateFanProsCoreFieldsReportAsync(int rows)
    {
        var report = new FanProsCoreFieldsReport(
                                                _configSettings.AppSettings,
                                                _playerRepository
                                                );
        await report.GenerateAsync(rows);
    }
}
