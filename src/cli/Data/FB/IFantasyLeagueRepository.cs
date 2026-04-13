namespace FBMngt.Data.FB;

using FBMngt.Models.FB;

/// <summary>
/// Repository for Fantasy Leagues.
/// Maps to: dbo.tblFBLeague
/// </summary>
public interface IFantasyLeagueRepository
{
    /// <summary>
    /// Gets a league by YahooLeagueKey.
    /// Returns null if not found.
    /// </summary>
    Task<FBLeague?> GetByYahooKeyAsync(
        string yahooLeagueKey);

    /// <summary>
    /// Inserts a new league.
    /// Returns the new FBLeagueID.
    /// </summary>
    Task<int> InsertAsync(
        FBLeague league);
}