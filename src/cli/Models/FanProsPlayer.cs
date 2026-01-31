namespace FBMngt.Models;

public class FanProsPlayer : IPlayer
{
    public int? PlayerID { get; set; }
    public string PlayerName { get; set; } = default!;
    public string? Team { get; set; }

    //Non IPlayer
    public string? Position { get; set; }
    public int Rank { get; set; }
    public bool IsPitcher { get; init; }
}
