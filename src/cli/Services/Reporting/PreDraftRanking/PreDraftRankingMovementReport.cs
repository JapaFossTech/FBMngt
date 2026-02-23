using FBMngt.Data;
using FBMngt.IO;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.PreDraftRanking;

namespace FBMngt.Services.Reporting.PreDraftRanking;

public class PreDraftMovementRow
{
    public int PlayerID { get; set; }
    public string PlayerName { get; set; } = default!;
    public int CurrentRank { get; set; }
    public int TargetRank { get; set; }
    public int Movement { get; set; }
}

public class PreDraftRankingMovementReport
{
    private readonly ConfigSettings _configSettings;
    private readonly YahooPreDraftRankingReader _yahooReader;
    private readonly PlayerResolver _playerResolver;
    private readonly FanProsCoreFieldsReport
                                        _fanProsCoreFieldsReport;
    private readonly IPreDraftRankingMovementCalculator
                                        _movementCalculator;

    public PreDraftRankingMovementReport(
        ConfigSettings configSettings,
        YahooPreDraftRankingReader yahooReader,
        PlayerResolver playerResolver,
        FanProsCoreFieldsReport fanProsCoreFieldsReport,
        IPreDraftRankingMovementCalculator movementCalculator
        )
    {
        _configSettings = configSettings;
        _yahooReader = yahooReader;
        _playerResolver = playerResolver;
        _fanProsCoreFieldsReport = fanProsCoreFieldsReport;
        _movementCalculator = movementCalculator;
    }

    public async Task GenerateAndWriteAsync()
    {
        // Read text file

        string path =
            _configSettings.Yahoo_PreDraftRankings_Path;

        List<FanProsPlayer> startPlayers = _yahooReader.Read(path);

        if (startPlayers.Count == 0)
        {
            Console.WriteLine("No Yahoo pre-draft rankings found.");
            return;
        }

        // Resolve PlayerID

        await _playerResolver
            .ResolvePlayerIDAsync(startPlayers.Cast<IPlayer>().ToList());

        Console.WriteLine(
            $"Resolved {startPlayers.Count} Yahoo players.");

        int unresolvedCount =
            startPlayers.Count(p => !p.PlayerID.HasValue);

        Console.WriteLine(
            $"Unresolved players: {unresolvedCount}");

        // Get the Goal (target) State

        List<FanProsPlayer> targetPlayers =
            await _fanProsCoreFieldsReport.GenerateAsync();

        // At this point, starting and Target
        // have their PlayerIDs resolved.

        // Move all needed players

        var movementRows = _movementCalculator
                .CalculateMovement(startPlayers, targetPlayers);

        // Write to TSV

        Console.WriteLine();
        Console.WriteLine("Movement Report (Non-Zero Only)");
        Console.WriteLine("--------------------------------");

        var outputLines = new List<string>();
        outputLines.Add("PlayerName\tMovement");

        foreach (var row in movementRows)
        {
            if (row.Movement == 0)
                continue;

            outputLines.Add(
                $"{row.PlayerName}\t{row.Movement}");

            Console.WriteLine(
                $"{row.PlayerName} {row.Movement:+#;-#;0}");
        }

        string outputPath = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"FBMngt_PreDraftMovement_" +
                $"{_configSettings.AppSettings.SeasonYear}.tsv");

        await File.WriteAllLinesAsync(
            outputPath,
            outputLines);

        Console.WriteLine();
        Console.WriteLine(
            $"TSV written to: {outputPath}");
    }
}