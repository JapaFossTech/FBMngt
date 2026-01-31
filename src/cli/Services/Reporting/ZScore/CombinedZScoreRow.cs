namespace FBMngt.Services.Reporting.ZScore;

// Z-scores helper classes
public class CombinedZScoreRow
{
    public int? PlayerID { get; set; }
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";

    public double ZR_ZW { get; set; }
    public double ZHR_ZSV { get; set; }
    public double ZRBI_ZK { get; set; }
    public double ZSB_ZERA { get; set; }
    public double ZAVG_ZWHIP { get; set; }

    public double TotalZ { get; set; }
}
