using FBMngt.Commands;
using FBMngt.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FBMngt.Data;

/// <summary>
/// Player repository using Player VIEW (not tblPlayer directly).
/// This provides Team and Position already resolved.
/// </summary>
public class PlayerRepository : IPlayerRepository
{
    private readonly string _connectionString;

    public PlayerRepository(ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    /// <summary>
    /// Loads ALL players from DB using Player view.
    /// </summary>
    public async Task<List<Player>> GetAllAsync()
    {
        var players = new List<Player>();

        using var conn = new SqlConnection(_connectionString);

        using var cmd = new SqlCommand(
            @"
            SELECT 
                PlayerID,
                PlayerName,
                Aka1,
                Aka2,
                Team_3Letter,
                primary_position
            FROM dbo.Player
            ",
            conn);

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            players.Add(new Player
            {
                PlayerID = reader.GetInt32(0),

                PlayerName = reader.IsDBNull(1)
                    ? null : reader.GetString(1),

                Aka1 = reader.IsDBNull(2)
                    ? null : reader.GetString(2),

                Aka2 = reader.IsDBNull(3)
                    ? null : reader.GetString(3),

                Team = reader.IsDBNull(4)
                    ? null : reader.GetString(4),

                Position = reader.IsDBNull(5)
                    ? null : reader.GetString(5)
            });
        }

        return players;
    }

    public async Task<int> InsertAsync(Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        using var conn = new SqlConnection(_connectionString);

        using var cmd = new SqlCommand(
            @"
        INSERT INTO tblPlayer
        (
            PlayerName,
            Aka1,
            Aka2,
            ModifiedDate
        )
        OUTPUT INSERTED.PlayerID
        VALUES
        (
            @PlayerName,
            @Aka1,
            @Aka2,
            GETUTCDATE()
        );
        ",
            conn);

        cmd.Parameters.AddWithValue(
            "@PlayerName",
            player.PlayerName.ToDbValue());

        cmd.Parameters.AddWithValue(
            "@Aka1",
            player.Aka1.ToDbValue());

        cmd.Parameters.AddWithValue(
            "@Aka2",
            player.Aka2.ToDbValue());

        await conn.OpenAsync();

        var result = await cmd.ExecuteScalarAsync();

        return result != null ? (int)result : 0;
    }

    /// <summary>
    /// Loads YahooPlayerID → PlayerID mapping.
    /// </summary>
    public async Task<Dictionary<int, int>> GetYahooPlayerIdMapAsync()
    {
        var map = new Dictionary<int, int>();

        using var conn = new SqlConnection(_connectionString);

        using var cmd = new SqlCommand(
            @"
            SELECT PlayerID, YahooPlayerID
            FROM dbo.tblPlayer
            WHERE YahooPlayerID IS NOT NULL
            ",
            conn);

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var playerId = reader.GetInt32(0);
            var yahooId = reader.GetInt32(1);

            if (!map.ContainsKey(yahooId))
            {
                map[yahooId] = playerId;
            }
        }

        return map;
    }

    public async Task<bool> UpdateYahooPlayerIdAsync(
        int playerId,
        int yahooPlayerId)
    {
        const string sql = @"
UPDATE dbo.tblPlayer
SET YahooPlayerID = @YahooPlayerID
WHERE PlayerID = @PlayerID
  AND (YahooPlayerID IS NULL)";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter("@YahooPlayerID", SqlDbType.Int)
            {
                Value = yahooPlayerId
            });

        command.Parameters.Add(
            new SqlParameter("@PlayerID", SqlDbType.Int)
            {
                Value = playerId
            });

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }
}