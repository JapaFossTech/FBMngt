namespace FBMngt.Models;

public class MatchedHitter
{
    public int PlayerID { get; set; }
    public string MatchedName { get; set; } = string.Empty;
    public SteamerBatterProjection Projection { get; set; } = null!;
}
