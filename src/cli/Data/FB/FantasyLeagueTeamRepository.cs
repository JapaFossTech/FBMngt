namespace FBMngt.Data.FB;

using FBMngt.Models.FB;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL repository for League-Team relationships.
/// Maps to: dbo.tblFBLeaguesTeam
/// </summary>
public class FantasyLeagueTeamRepository
    : IFantasyLeagueTeamRepository
{
    private readonly string _connectionString;

    public FantasyLeagueTeamRepository(
        ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    /// <summary>
    /// Gets a league-team by YahooTeamKey.
    /// </summary>
    public async Task<FBLeagueTeam?>
        GetByYahooTeamKeyAsync(string yahooTeamKey)
    {
        const string sql = @"
SELECT FBLeaguesTeamID,
       FBLeagueID,
       FBTeamID,
       YahooTeamKey
FROM dbo.tblFBLeaguesTeam
WHERE YahooTeamKey = @YahooTeamKey";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(
            "@YahooTeamKey", yahooTeamKey);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.Read())
            return null;

        return new FBLeagueTeam
        {
            FBLeaguesTeamID = reader.GetInt32(0),
            FBLeagueID = reader.GetInt32(1),
            FBTeamID = reader.GetInt32(2),
            YahooTeamKey = reader.GetString(3)
        };
    }

    /// <summary>
    /// Inserts a new league-team relationship.
    /// </summary>
    public async Task<int> InsertAsync(
        FBLeagueTeam leagueTeam)
    {
        const string sql = @"
INSERT INTO dbo.tblFBLeaguesTeam
(
    FBLeagueID,
    FBTeamID,
    YahooTeamKey
)
OUTPUT INSERTED.FBLeaguesTeamID
VALUES
(
    @FBLeagueID,
    @FBTeamID,
    @YahooTeamKey
)";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@FBLeagueID", leagueTeam.FBLeagueID);

        cmd.Parameters.AddWithValue(
            "@FBTeamID", leagueTeam.FBTeamID);

        cmd.Parameters.AddWithValue(
            "@YahooTeamKey", leagueTeam.YahooTeamKey);

        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }
}