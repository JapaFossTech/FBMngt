using FBMngt.Data;
using FBMngt.Data.FB;
using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo.DailyIngest;

/// <summary>
/// Loads roster JSON files from disk and feeds them into
/// the player ingestion pipeline.
///
/// This bridges:
/// File system → Json → Ingestion pipeline
/// </summary>
public class YahooRosterFileProcessor
{
    private readonly YahooPlayerIngestionService _ingestionService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IFantasyLeagueTeamRepository _leagueTeamRepo;
    private readonly IFBTeamsPlayerRepository _teamsPlayerRepo;

    public YahooRosterFileProcessor(
        YahooPlayerIngestionService ingestionService,
        IPlayerRepository playerRepository,
        IFantasyLeagueTeamRepository leagueTeamRepo,
        IFBTeamsPlayerRepository teamsPlayerRepo)
    {
        _ingestionService = ingestionService;
        _playerRepository = playerRepository;
        _leagueTeamRepo = leagueTeamRepo;
        _teamsPlayerRepo = teamsPlayerRepo;
    }

    /// <summary>
    /// Processes all roster JSON files in a directory.
    /// BATCH OPTIMIZED:
    /// - Loads DB players once
    /// - Loads YahooPlayerIDs once
    /// </summary>
    public async Task ProcessDirectoryAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine(
                $"Directory not found: {directoryPath}");
            return;
        }

        var files = Directory.GetFiles(
            directoryPath,
            "*_roster.json",
            SearchOption.TopDirectoryOnly);

        Console.WriteLine(
            $"Found {files.Length} roster files");

        // --------------------------------------------------------
        // LOAD SHARED DATA ONCE (KEY OPTIMIZATION)
        // --------------------------------------------------------
        var dbPlayers = await _playerRepository.GetAllAsync();

        Dictionary<int, int> yahooMap = await _playerRepository
                        .GetYahooPlayerIdMapAsync();

        // --------------------------------------------------------
        // AGGREGATED STATS
        // --------------------------------------------------------
        var totalStats = new PlayerPersistenceStats();

        foreach (var file in files)
        {
            var stats = await ProcessFileAsync(
                file,
                dbPlayers,
                yahooMap);

            AggregateStats(totalStats, stats);
        }

        // --------------------------------------------------------
        // FINAL SUMMARY
        // --------------------------------------------------------
        PrintSummary(totalStats);
    }

    /// <summary>
    /// Processes a single roster JSON file.
    /// </summary>
    private async Task<PlayerPersistenceStats> ProcessFileAsync(
        string filePath,
        List<Player> dbPlayers,
        Dictionary<int, int> yahooMap)
    {
        Console.WriteLine();
        Console.WriteLine($"Processing file: {filePath}");

        try
        {
            var json = await File.ReadAllTextAsync(filePath);

            using var doc = JsonDocument.Parse(json);

            var stats = await _ingestionService
                .ProcessRosterAsync(
                    doc.RootElement,
                    dbPlayers,
                    yahooMap);

            PrintFileStats(stats, filePath);

            // --------------------------------------------------------
            // FANTASY ROSTER PERSISTENCE (SMART UPDATE)
            // --------------------------------------------------------
            string fileName = Path.GetFileName(filePath);

            // yahoo_469.l.33371.t.10_roster.json
            string[] parts = fileName.Split('_');

            if (parts.Length < 3)
            {
                Console.WriteLine(
                    $"[FB] Invalid file name: {fileName}");
                return stats;
            }

            // ✅ Extract team key correctly
            string teamKey = parts[1];

            // Resolve FBLeaguesTeamID
            var leagueTeam = await _leagueTeamRepo
                .GetByYahooTeamKeyAsync(teamKey);

            if (leagueTeam == null)
            {
                Console.WriteLine(
                    $"[FB] LeaguesTeam not found for {teamKey}");
                return stats;
            }

            int leagueTeamId =
                leagueTeam.FBLeaguesTeamID!.Value;

            // --------------------------------------------
            // CURRENT DB STATE
            // --------------------------------------------
            var currentPlayerIds =
                await _teamsPlayerRepo
                    .GetPlayerIdsByLeagueTeamIdAsync(
                        leagueTeamId);

            var currentSet =
                new HashSet<int>(currentPlayerIds);

            // --------------------------------------------
            // INCOMING STATE (FROM JSON)
            // --------------------------------------------
            var yahooPlayerIds =
                ExtractYahooPlayerIds(doc.RootElement);

            var incomingSet = new HashSet<int>();

            foreach (var yahooPlayerId in yahooPlayerIds)
            {
                if (!yahooMap.TryGetValue(
                    yahooPlayerId,
                    out int playerId))
                {
                    Console.WriteLine(
                        "[FB] Player not resolved: " +
                        yahooPlayerId);
                    continue;
                }

                incomingSet.Add(playerId);
            }

            // --------------------------------------------
            // DIFF CALCULATION
            // --------------------------------------------

            // Players to INSERT
            var toInsert = incomingSet
                .Except(currentSet)
                .ToList();

            // Players to DELETE
            var toDelete = currentSet
                .Except(incomingSet)
                .ToList();

            DateTime now = DateTime.Now;

            // --------------------------------------------
            // INSERT NEW PLAYERS (BULK)
            // --------------------------------------------
            var insertEntities =
                new List<Models.FB.FBTeamsPlayer>();

            foreach (var playerId in toInsert)
            {
                insertEntities.Add(
                    new Models.FB.FBTeamsPlayer
                    {
                        FBLeaguesTeamID = leagueTeamId,
                        PlayerID = playerId,
                        ModifiedDate = now
                    });
            }

            if (insertEntities.Count > 0)
            {
                await _teamsPlayerRepo
                    .BulkInsertAsync(insertEntities);
            }

            // --------------------------------------------
            // DELETE REMOVED PLAYERS
            // --------------------------------------------
            foreach (var playerId in toDelete)
            {
                await _teamsPlayerRepo.DeleteAsync(
                    leagueTeamId,
                    playerId);
            }

            // --------------------------------------------
            // LOGGING
            // --------------------------------------------
            Console.WriteLine(
                $"[FB] Roster sync for {teamKey} | " +
                $"Add: {toInsert.Count}, " +
                $"Remove: {toDelete.Count}, " +
                $"Keep: {incomingSet.Count - toInsert.Count}");

            return stats;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error processing file: {filePath}");
            Console.WriteLine(ex.Message);

            return new PlayerPersistenceStats
            {
                Errors = 1
            };
        }
    }
    // ------------------------------------------------------------
    // AGGREGATION
    // ------------------------------------------------------------
    private void AggregateStats(
        PlayerPersistenceStats total,
        PlayerPersistenceStats current)
    {
        total.Inserted += current.Inserted;
        total.Updated += current.Updated;
        total.Skipped += current.Skipped;
        total.Conflicts += current.Conflicts;
        total.Errors += current.Errors;

        total.ConflictDetails.AddRange(
            current.ConflictDetails);

        total.ErrorDetails.AddRange(
            current.ErrorDetails);
    }

    // ------------------------------------------------------------
    // LOGGING (PER FILE)
    // ------------------------------------------------------------
    private void PrintFileStats(
        PlayerPersistenceStats stats,
        string filePath)
    {
        Console.WriteLine("----- File Summary -----");
        Console.WriteLine($"File: {Path.GetFileName(filePath)}");
        Console.WriteLine($"Inserted : {stats.Inserted}");
        Console.WriteLine($"Updated  : {stats.Updated}");
        Console.WriteLine($"Skipped  : {stats.Skipped}");
        Console.WriteLine($"Conflicts: {stats.Conflicts}");
        Console.WriteLine($"Errors   : {stats.Errors}");
    }

    // ------------------------------------------------------------
    // LOGGING (FINAL)
    // ------------------------------------------------------------
    private void PrintSummary(
        PlayerPersistenceStats stats)
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("TOTAL PLAYER PERSISTENCE SUMMARY");
        Console.WriteLine("=================================");
        Console.WriteLine($"Inserted : {stats.Inserted}");
        Console.WriteLine($"Updated  : {stats.Updated}");
        Console.WriteLine($"Skipped  : {stats.Skipped}");
        Console.WriteLine($"Conflicts: {stats.Conflicts}");
        Console.WriteLine($"Errors   : {stats.Errors}");
        Console.WriteLine("=================================");
    }

    /// <summary>
    /// Extracts Yahoo Player IDs from roster JSON.
    /// Uses recursive traversal (no assumptions).
    /// </summary>
    private List<int> ExtractYahooPlayerIds(
        JsonElement element)
    {
        var result = new List<int>();

        ExtractRecursive(element, result);

        return result;
    }

    private void ExtractRecursive(
        JsonElement element,
        List<int> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                // Yahoo uses "player_id"
                if (prop.NameEquals("player_id"))
                {
                    if (int.TryParse(
                        prop.Value.ToString(),
                        out int id))
                    {
                        result.Add(id);
                    }
                }

                ExtractRecursive(prop.Value, result);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ExtractRecursive(item, result);
            }
        }
    }
}