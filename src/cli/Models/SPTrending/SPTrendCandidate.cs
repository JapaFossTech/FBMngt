namespace FBMngt.Models.SPTrending;

public class SPTrendCandidate
{
    public int PlayerID { get; set; }
    public string Player { get; set; } = string.Empty;

    // Baseline (30-day)
    public decimal ERA { get; set; }
    public decimal WHIP { get; set; }
    public decimal K9 { get; set; }

    // Trend (last 3)
    public decimal ERA_Last3 { get; set; }
    public decimal WHIP_Last3 { get; set; }
    public decimal K9_Last3 { get; set; }

    // Volume
    public int Starts_Last30 { get; set; }
    public int Starts_Last3 { get; set; }

    // Availability
    public int AvailableCount { get; set; }
    public int TotalLeagues { get; set; }

    // Derived (C#)
    public decimal ERA_TrendDelta { get; set; }
    public decimal WHIP_TrendDelta { get; set; }
    public decimal K9_TrendDelta { get; set; }

    public decimal TrendScore { get; set; }
    public decimal RiskScore { get; set; }
    public decimal FinalScore { get; set; }
}