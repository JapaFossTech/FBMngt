namespace FBMngt.Services.Yahoo.DailyIngest;

/// <summary>
/// Tracks persistence outcomes for diagnostics.
/// </summary>
public class PlayerPersistenceStats
{
    public int Inserted { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public int Conflicts { get; set; }

    public int Errors { get; set; }

    // Detailed tracking
    public List<string> ConflictDetails { get; } = new();
    public List<string> ErrorDetails { get; } = new();
    public List<string> SkippedDetails { get; } = new();
}