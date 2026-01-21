namespace FBMngt.Models;

public class FanProsPlayer : IPlayer
{
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Rank { get; set; }
    //public string? OrganizationId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
