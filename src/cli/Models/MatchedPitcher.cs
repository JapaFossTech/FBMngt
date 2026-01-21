namespace FBMngt.Models;

public class MatchedPitcher
{
    public int PlayerID { get; set; }
    public string MatchedName { get; set; } = string.Empty;
    public SteamerPitcherProjection Projection { get; set; } = null!;
}
