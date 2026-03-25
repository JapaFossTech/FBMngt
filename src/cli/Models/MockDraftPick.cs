namespace FBMngt.Models;

public sealed class MockDraftPick : IPlayer
{
    public int? PlayerID { get; set; }

    public string? PlayerName { get; set; }

    public string? Team { get; set; }

    public string? Position { get; set; }
    public int? DraftID { get; set; }

    public int PickNumber { get; init; }

    public int RoundNumber { get; init; }

    public DateTime DraftDate { get; init; }
}