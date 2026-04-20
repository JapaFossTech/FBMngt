namespace FBMngt.Data.SPTrending;

using System.Data;
using Microsoft.Data.SqlClient;
using FBMngt.Models.SPTrending;

public class SPTrendingRepository : ISPTrendingRepository
{
    private readonly string _connectionString;

    public SPTrendingRepository(ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<List<SPTrendCandidate>> GetCandidatesAsync()
    {
        var result = new List<SPTrendCandidate>();

        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();

        // 🔥 IMPORTANT: This should point to your FINAL query/view
        cmd.CommandText = @"
            SELECT 
                PlayerID,
                SP AS Player,
                ERA,
                WHIP,
                K9,
                ERA_Last3,
                WHIP_Last3,
                K9_Last3,
                Starts_Last30,
                Starts_Last3,
                AvailableCount,
                TotalLeagues
            FROM dbo.YourFinalWaiverSPView
        ";

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new SPTrendCandidate
            {
                PlayerID = reader.GetInt32(0),
                Player = reader.GetString(1),

                ERA = reader.GetDecimal(2),
                WHIP = reader.GetDecimal(3),
                K9 = reader.GetDecimal(4),

                ERA_Last3 = reader.GetDecimal(5),
                WHIP_Last3 = reader.GetDecimal(6),
                K9_Last3 = reader.GetDecimal(7),

                Starts_Last30 = reader.GetInt32(8),
                Starts_Last3 = reader.GetInt32(9),

                AvailableCount = reader.GetInt32(10),
                TotalLeagues = reader.GetInt32(11)
            });
        }

        return result;
    }
}