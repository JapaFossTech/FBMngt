namespace FBMngt.Data.FB;

using FBMngt.Models.FB;
using Microsoft.Data.SqlClient;
using System.Data;

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
    /// Bulk inserts team-player records.
    /// Uses SqlBulkCopy for performance.
    /// </summary>
    public async Task BulkInsertAsync(
        List<FBTeamsPlayer> entities)
    {
        if (entities == null || entities.Count == 0)
        {
            return;
        }

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var bulk = new SqlBulkCopy(conn);

        bulk.DestinationTableName = "dbo.tblFBTeamsPlayer";

        // --------------------------------------------
        // CREATE DATATABLE
        // --------------------------------------------
        var table = new DataTable();

        table.Columns.Add("FBLeaguesTeamID", typeof(int));
        table.Columns.Add("PlayerID", typeof(int));
        table.Columns.Add("ModifiedDate", typeof(DateTime));

        foreach (var e in entities)
        {
            table.Rows.Add(
                e.FBLeaguesTeamID,
                e.PlayerID,
                e.ModifiedDate);
        }

        // --------------------------------------------
        // MAP COLUMNS
        // --------------------------------------------
        bulk.ColumnMappings.Add(
            "FBLeaguesTeamID", "FBLeaguesTeamID");

        bulk.ColumnMappings.Add(
            "PlayerID", "PlayerID");

        bulk.ColumnMappings.Add(
            "ModifiedDate", "ModifiedDate");

        await bulk.WriteToServerAsync(table);
    }

    public async Task<List<int>>
    GetPlayerIdsByLeagueTeamIdAsync(
        int fbLeaguesTeamID)
    {
        const string sql = @"
        SELECT PlayerID
        FROM dbo.tblFBTeamsPlayer
        WHERE FBLeaguesTeamID = @FBLeaguesTeamID";

        var result = new List<int>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@FBLeaguesTeamID", fbLeaguesTeamID);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(reader.GetInt32(0));
        }

        return result;
    }
    public async Task DeleteAsync(
    int fbLeaguesTeamID,
    int playerID)
    {
        const string sql = @"
        DELETE FROM dbo.tblFBTeamsPlayer
        WHERE FBLeaguesTeamID = @FBLeaguesTeamID
          AND PlayerID = @PlayerID";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@FBLeaguesTeamID", fbLeaguesTeamID);

        cmd.Parameters.AddWithValue(
            "@PlayerID", playerID);

        await cmd.ExecuteNonQueryAsync();
    }
}