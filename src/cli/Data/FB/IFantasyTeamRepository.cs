namespace FBMngt.Data.FB;

using FBMngt.Models.FB;

/// <summary>
/// Repository for Fantasy Teams.
/// Maps to: dbo.tblFBTeam
/// </summary>
public interface IFantasyTeamRepository
{
    /// <summary>
    /// Gets a team by TeamName.
    /// Returns null if not found.
    /// </summary>
    Task<FBTeam?> GetByNameAsync(
        string teamName);

    /// <summary>
    /// Inserts a new team.
    /// Returns the new FBTeamID.
    /// </summary>
    Task<int> InsertAsync(
        FBTeam team);
}