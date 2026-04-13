namespace FBMngt.Data.FB;

using FBMngt.Models.FB;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL repository for Fantasy Leagues.
/// Maps to: dbo.tblFBLeague
/// </summary>
public class FantasyLeagueRepository : IFantasyLeagueRepository
{
    private readonly string _connectionString;

    public FantasyLeagueRepository(
        ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    /// <summary>
    /// Gets a league by YahooLeagueKey.
    /// </summary>
    public async Task<FBLeague?> GetByYahooKeyAsync(
        string yahooLeagueKey)
    {
        const string sql = @"
SELECT FBLeagueID,
       YahooLeagueKey,
       Season,
       LeagueName,
       Short
FROM dbo.tblFBLeague
WHERE YahooLeagueKey = @YahooLeagueKey";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(
            "@YahooLeagueKey", yahooLeagueKey);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.Read())
            return null;

        return new FBLeague
        {
            FBLeagueID = reader.GetInt32(0),
            YahooLeagueKey = reader.GetString(1),
            Season = reader.GetInt32(2),
            LeagueName = reader.GetString(3),
            Short = reader.IsDBNull(4)
                ? null
                : reader.GetString(4)
        };
    }

    /// <summary>
    /// Inserts a new league.
    /// </summary>
    public async Task<int> InsertAsync(
        FBLeague league)
    {
        const string sql = @"
INSERT INTO dbo.tblFBLeague
(
    YahooLeagueKey,
    Season,
    LeagueName,
    Short
)
OUTPUT INSERTED.FBLeagueID
VALUES
(
    @YahooLeagueKey,
    @Season,
    @LeagueName,
    @Short
)";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@YahooLeagueKey", league.YahooLeagueKey);

        cmd.Parameters.AddWithValue(
            "@Season", league.Season);

        cmd.Parameters.AddWithValue(
            "@LeagueName", league.LeagueName);

        cmd.Parameters.AddWithValue(
            "@Short",
            (object?)league.Short ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }
}