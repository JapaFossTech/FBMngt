using System.Text.Json;
using FBMngt.Data;
using FBMngt.Models;

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

    public YahooRosterFileProcessor(
        YahooPlayerIngestionService ingestionService,
        IPlayerRepository playerRepository)
    {
        _ingestionService = ingestionService;
        _playerRepository = playerRepository;
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
}