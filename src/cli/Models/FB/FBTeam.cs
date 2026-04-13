namespace FBMngt.Models.FB;

/// <summary>
/// Represents a Fantasy Team (global, not league-specific).
/// Maps to: dbo.tblFBTeam
/// </summary>
public class FBTeam
{
    /// <summary>
    /// Internal database identifier (PK).
    /// </summary>
    public int? FBTeamID { get; set; }

    /// <summary>
    /// Team display name.
    /// </summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating Jorge's team.
    /// </summary>
    public bool? IsJorge { get; set; }

    /// <summary>
    /// Flag indicating Javier's team.
    /// </summary>
    public bool IsJavier { get; set; }
}