namespace FBMngt.Models.SPTrending;

public class WaiverPitcher
{
    public int PlayerID { get; set; }
    public string PlayerName { get; set; } = string.Empty;

    // 30-day baseline
    public decimal ERA { get; set; }
    public decimal WHIP { get; set; }
    public decimal K9 { get; set; }
    public decimal IP_Month { get; set; }
    public int Starts_Last30 { get; set; }

    // Last 3 starts
    public int Starts_Last3 { get; set; }
    public decimal ERA_Last3 { get; set; }
    public decimal WHIP_Last3 { get; set; }
    public decimal K9_Last3 { get; set; }

    // Availability
    public int AvailableCount { get; set; }
    public int TotalLeagues { get; set; }

    // Computed
    public decimal Score { get; set; }
    public string Trend { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}