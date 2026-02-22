using FBMngt.Data;
using FBMngt.IO;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting.FanPros;

namespace FBMngt.Services.Reporting.PreDraft;

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

    public PreDraftRankingMovementReport(
        ConfigSettings configSettings,
        YahooPreDraftRankingReader yahooReader,
        PlayerResolver playerResolver,
        FanProsCoreFieldsReport fanProsCoreFieldsReport
        )
    {
        _configSettings = configSettings;
        _yahooReader = yahooReader;
        _playerResolver = playerResolver;
        _fanProsCoreFieldsReport = fanProsCoreFieldsReport;
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

        // Prepare simulation list

        List<FanProsPlayer> simulationPlayerList = startPlayers
                .Where(p => p.PlayerID.HasValue)
                .ToList();

        List<int> originalYahooOrder_PlayerID =
            simulationPlayerList
                .Select(p => p.PlayerID!.Value)
                .ToList();
        // Reduce the target list to only have the original list

        targetPlayers = targetPlayers
            .Where(p =>
                p.PlayerID.HasValue &&
                originalYahooOrder_PlayerID.Contains(
                    p.PlayerID.Value))
            .ToList();

        if (simulationPlayerList.Count != targetPlayers.Count)
        {
            Console.WriteLine(
                "Warning: Player universe mismatch detected.");
        }

        var movementByPlayerId = new Dictionary<int, int>();

        // Loop target playes to move them if necessary

        for (int targetIndex = 0;
             targetIndex < targetPlayers.Count;
             targetIndex++)
        {
            FanProsPlayer targetPlayer = targetPlayers[targetIndex];

            if (!targetPlayer.PlayerID.HasValue)
            {
                if (targetIndex <= 350)
                    Console.WriteLine("");
                continue;
            }

            int targetPlayerId = targetPlayer.PlayerID.Value;

            // Get player index from the simulation, from the start
            // list that is changing with previous moves
            int playerIndexFromSimulation =
                simulationPlayerList
                    .FindIndex(p =>
                        p.PlayerID == targetPlayerId);

            if (playerIndexFromSimulation == -1)
            {
                if (targetIndex <= 350)
                    Console.WriteLine($"Target player '{targetPlayer}' " +
                    $"(with target index: {targetIndex}) " +
                    $"not found in original start list");
                continue; // not in original start list
            }

            // IMPORTANT:
            // Clamp targetIndex to valid range
            int safeTargetIndex =
                Math.Min(targetIndex,
                         simulationPlayerList.Count - 1);

            int movement = playerIndexFromSimulation 
                            - safeTargetIndex;

            movementByPlayerId[targetPlayerId] = movement;

            if (movement != 0)
            {
                FanProsPlayer playerToMove =
                    simulationPlayerList[playerIndexFromSimulation];

                // Target position removed from working list,
                // All below players move up 1
                simulationPlayerList
                    .RemoveAt(playerIndexFromSimulation);

                // Because we removed 1, when moving down, make
                // sure the target index is within the simulation
                if (playerIndexFromSimulation < safeTargetIndex)
                    safeTargetIndex--;

                // Target position inserted into working list using
                // his safeTargetIndex.Players move down 1 poistion.
                simulationPlayerList
                    .Insert(safeTargetIndex, playerToMove);
            }
        }
        Console.WriteLine();
        Console.WriteLine("Movement Report (Non-Zero Only)");
        Console.WriteLine("--------------------------------");

        // Write to TSV

        var outputLines = new List<string>();
        outputLines.Add("PlayerName\tMovement");

        foreach (int originalPlayerId in originalYahooOrder_PlayerID)
        {
            FanProsPlayer player =
                startPlayers.First(p =>
                    p.PlayerID == originalPlayerId);

            int movement =
                movementByPlayerId
                    .ContainsKey(originalPlayerId)
                ? movementByPlayerId[originalPlayerId]
                : 0;

            if (movement == 0)
                continue;

            outputLines.Add($"{player.PlayerName}\t{movement}");

            Console.WriteLine(
                $"{player.PlayerName} {movement:+#;-#;0}");
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
