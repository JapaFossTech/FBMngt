using FBMngt.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FBMngt.Data;

public class PlayerRepository
{
    private readonly string _connectionString;

    public PlayerRepository()
    {
        _connectionString =
            Program.Configuration.GetConnectionString("MLB")
            ?? throw new Exception("Missing connection string 'MLB'");
    }

    public async Task<List<Player>> GetPlayersAsync()
    {
        var players = new List<Player>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "SELECT PlayerID, PlayerName, Aka1, Aka2, organization_id FROM tblPlayer",
            conn);

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            players.Add(new Player
            {
                PlayerID = reader.GetInt32(0),
                PlayerName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Aka1 = reader.IsDBNull(2) ? null : reader.GetString(2),
                Aka2 = reader.IsDBNull(3) ? null : reader.GetString(3),
                organization_id = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return players;
    }
}
