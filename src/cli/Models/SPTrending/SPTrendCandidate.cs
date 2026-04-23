namespace FBMngt.Models.SPTrending;

public class SPTrendCandidate
{
    public int PlayerID { get; set; }
    public string SP { get; set; } = default!;

    public decimal IP_Month { get; set; }
    public decimal ERA { get; set; }
    public decimal WHIP { get; set; }
    public decimal K9 { get; set; }

    public int Starts_Last30 { get; set; }
    public DateTime LastStartDate { get; set; }

    public int? Starts_Last3 { get; set; }
    public decimal? ERA_Last3 { get; set; }
    public decimal? WHIP_Last3 { get; set; }
    public decimal? K9_Last3 { get; set; }

    // 🔥 NEW
    public decimal? MaxERA_Last3 { get; set; }
    public decimal? MaxWHIP_Last3 { get; set; }
    public decimal? MinIP_Last3 { get; set; }

    public int AvailableCount { get; set; }
    public int TotalLeagues { get; set; }

    // Existing scoring fields...
    public decimal ERA_TrendDelta { get; set; }
    public decimal WHIP_TrendDelta { get; set; }
    public decimal K9_TrendDelta { get; set; }
    public decimal TrendScore { get; set; }
    public decimal RiskScore { get; set; }
    public decimal FinalScore { get; set; }

    // Opponent
    public DateTime? NextGameDate { get; set; }

    public string? Opponent { get; set; }

    public decimal? RunsPerGame { get; set; }

    public decimal? K_Percentage { get; set; }

    public decimal MatchupScore { get; set; }
}