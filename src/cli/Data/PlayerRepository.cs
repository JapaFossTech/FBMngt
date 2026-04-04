using FBMngt.Commands;
using FBMngt.Models;
using Microsoft.Data.SqlClient;

namespace FBMngt.Data;

public interface IPlayerRepository
{
    Task<List<Player>> GetAllAsync();
    Task InsertAsync(Player player);
}

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

    public async Task InsertAsync(Player player)
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
        await cmd.ExecuteNonQueryAsync();
    }
}