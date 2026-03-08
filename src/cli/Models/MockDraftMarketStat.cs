namespace FBMngt.Models;

public class MockDraftMarketStat : IPlayer
{
    // IPlayer
    public int? PlayerID { get; set; }
    public string? PlayerName { get; set; }
    public string? Team { get; set; }
    public string? Position { get; set; }

    // Market Data
    public double PickStDev { get; set; }
    public int PickCount { get; set; }
    public decimal PickAverage { get; set; }
    public decimal PickRoundAverage { get; set; }
    public int PickMin { get; set; }
    public int PickMax { get; set; }
    public int PickDiff { get; set; }
    public decimal ReachIndex { get; set; }
    public decimal FallIndex { get; set; }
    public double VolatilityIndex { get; set; }
}