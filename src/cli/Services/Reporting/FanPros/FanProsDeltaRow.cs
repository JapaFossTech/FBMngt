using FBMngt.Models;

namespace FBMngt.Services.Reporting.FanPros;

public sealed class FanProsDeltaRow: IPlayer
{
    public int? PlayerID { get; set; }
    public string? PlayerName { get; set; }
    public string? Team { get; set; }
    public string? Position { get; set; }
    public int PreviousRank { get; init; }
    public int CurrentRank { get; init; }
    public int Movement => PreviousRank - CurrentRank;
}
