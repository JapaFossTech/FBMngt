namespace FBMngt.Data.FB;

using FBMngt.Models.FB;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL repository for team-player relationships.
/// </summary>
public class FBTeamsPlayerRepository
    : IFBTeamsPlayerRepository
{
    private readonly string _connectionString;

    public FBTeamsPlayerRepository(
        ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    /// <summary>
    /// Deletes all players for a team.
    /// </summary>
    public async Task DeleteByLeagueTeamIdAsync(
        int fbLeaguesTeamID)
    {
        const string sql = @"
DELETE FROM dbo.tblFBTeamsPlayer
WHERE FBLeaguesTeamID = @FBLeaguesTeamID";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@FBLeaguesTeamID", fbLeaguesTeamID);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Inserts a team-player record.
    /// </summary>
    public async Task InsertAsync(
        FBTeamsPlayer entity)
    {
        const string sql = @"
INSERT INTO dbo.tblFBTeamsPlayer
(
    FBLeaguesTeamID,
    PlayerID,
    ModifiedDate
)
VALUES
(
    @FBLeaguesTeamID,
    @PlayerID,
    @ModifiedDate
)";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@FBLeaguesTeamID", entity.FBLeaguesTeamID);

        cmd.Parameters.AddWithValue(
            "@PlayerID", entity.PlayerID);

        cmd.Parameters.AddWithValue(
            "@ModifiedDate", entity.ModifiedDate);

        await cmd.ExecuteNonQueryAsync();
    }
}