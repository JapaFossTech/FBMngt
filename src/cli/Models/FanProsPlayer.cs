namespace FBMngt.Models;

public class FanProsPlayer : IPlayer
{
    public int? PlayerID { get; set; }
    public string? PlayerName { get; set; }
    public string? Team { get; set; }

    //Non IPlayer
    public string? Position { get; set; }
    public int Rank { get; set; }

    public override string ToString()
    {
        string id = PlayerID.HasValue ? PlayerID.Value.ToString() : "ID?";
        return $"{id}: {PlayerName ?? "Name?"}";
    }
}
