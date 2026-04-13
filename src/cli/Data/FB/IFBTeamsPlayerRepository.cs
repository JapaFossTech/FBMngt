namespace FBMngt.Data.FB;

using FBMngt.Models.FB;

/// <summary>
/// Repository for team-player relationships.
/// Maps to: dbo.tblFBTeamsPlayer
/// </summary>
public interface IFBTeamsPlayerRepository
{
    /// <summary>
    /// Deletes all players for a team.
    /// </summary>
    Task DeleteByLeagueTeamIdAsync(
        int fbLeaguesTeamID);

    /// <summary>
    /// Inserts a team-player record.
    /// </summary>
    Task InsertAsync(
        FBTeamsPlayer entity);
}