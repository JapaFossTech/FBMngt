namespace FBMngt.Data.FB;

using FBMngt.Models.FB;

/// <summary>
/// Repository for League-Team relationships.
/// Maps to: dbo.tblFBLeaguesTeam
/// </summary>
public interface IFantasyLeagueTeamRepository
{
    /// <summary>
    /// Gets a league-team by YahooTeamKey.
    /// Returns null if not found.
    /// </summary>
    Task<FBLeagueTeam?> GetByYahooTeamKeyAsync(
        string yahooTeamKey);

    /// <summary>
    /// Inserts a new league-team relationship.
    /// Returns the new FBLeaguesTeamID.
    /// </summary>
    Task<int> InsertAsync(
        FBLeagueTeam leagueTeam);
}