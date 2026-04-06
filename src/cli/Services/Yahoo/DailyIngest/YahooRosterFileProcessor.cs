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

    public YahooRosterFileProcessor(
        YahooPlayerIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    /// <summary>
    /// Processes all roster JSON files in a directory.
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

        foreach (var file in files)
        {
            await ProcessFileAsync(file);
        }
    }

    /// <summary>
    /// Processes a single roster JSON file.
    /// </summary>
    private async Task ProcessFileAsync(string filePath)
    {
        Console.WriteLine();
        Console.WriteLine($"Processing file: {filePath}");

        try
        {
            var json = await File.ReadAllTextAsync(filePath);

            using var doc = JsonDocument.Parse(json);

            var stats = await _ingestionService
                .ProcessRosterAsync(doc.RootElement);

            Console.WriteLine(
                $"Inserted={stats.Inserted}, " +
                $"Updated={stats.Updated}, " +
                $"Conflicts={stats.Conflicts}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error processing file: {filePath}");
            Console.WriteLine(ex.Message);
        }
    }
}