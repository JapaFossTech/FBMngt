namespace FBMngt.Models;

public interface IPlayer
{
    int? PlayerID { get; set; }
    string PlayerName { get; set; }
    string? Team { get; set; }
}
public class Player: IPlayer
{
    public int? PlayerID { get; set; }
    public string? PlayerName { get; set; }
    public string? Aka1 { get; set; }
    public string? Aka2 { get; set; }

    public string? organization_id { get; set; }
    public string? Team { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
