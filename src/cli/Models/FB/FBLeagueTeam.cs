namespace FBMngt.Models.FB;

/// <summary>
/// Represents a team participating in a league.
/// Maps to: dbo.tblFBLeaguesTeam
/// </summary>
public class FBLeagueTeam
{
    /// <summary>
    /// Internal database identifier (PK).
    /// </summary>
    public int? FBLeaguesTeamID { get; set; }

    /// <summary>
    /// Foreign key to FantasyLeague.
    /// </summary>
    public int FBLeagueID { get; set; }

    /// <summary>
    /// Foreign key to FantasyTeam.
    /// </summary>
    public int FBTeamID { get; set; }

    /// <summary>
    /// Yahoo Team Key (e.g. 469.l.7042.t.1).
    /// REQUIRED and UNIQUE.
    /// </summary>
    public string YahooTeamKey { get; set; } = string.Empty;
}