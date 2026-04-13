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
    /// Bulk inserts team-player records.
    /// </summary>
    Task BulkInsertAsync(
        List<FBTeamsPlayer> entities);
}