namespace FBMngt.Models.FB;

/// <summary>
/// Represents a player assigned to a fantasy team.
/// Maps to: dbo.tblFBTeamsPlayer
/// </summary>
public class FBTeamsPlayer
{
    /// <summary>
    /// Internal database identifier (PK).
    /// </summary>
    public int? FBTeamsPlayerID { get; set; }

    /// <summary>
    /// FK to tblFBLeaguesTeam.
    /// </summary>
    public int FBLeaguesTeamID { get; set; }

    /// <summary>
    /// FK to tblPlayer.
    /// </summary>
    public int PlayerID { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime ModifiedDate { get; set; }
}