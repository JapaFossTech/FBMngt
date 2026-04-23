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

        cmd.CommandText = @"
        SELECT
            PlayerID,
            SP,
            ERA,
            WHIP,
            K9,
            ERA_Last3,
            WHIP_Last3,
            K9_Last3,
            Starts_Last30,
            Starts_Last3,
            AvailableCount,
            TotalLeagues,
            MaxERA_Last3,
            MaxWHIP_Last3,
            MinIP_Last3,
            NextGameDate,
            Opponent,
            RunsPerGame,
            K_Percentage
        FROM dbo.GamesSPs_TrendCandidates
    ";

        using var reader = await cmd.ExecuteReaderAsync();

        // Column ordinals (performance + readability)
        var ordPlayerID = reader.GetOrdinal("PlayerID");
        var ordSP = reader.GetOrdinal("SP");

        var ordERA = reader.GetOrdinal("ERA");
        var ordWHIP = reader.GetOrdinal("WHIP");
        var ordK9 = reader.GetOrdinal("K9");

        var ordERALast3 = reader.GetOrdinal("ERA_Last3");
        var ordWHIPLast3 = reader.GetOrdinal("WHIP_Last3");
        var ordK9Last3 = reader.GetOrdinal("K9_Last3");

        var ordStartsLast30 = reader.GetOrdinal("Starts_Last30");
        var ordStartsLast3 = reader.GetOrdinal("Starts_Last3");

        var ordAvailableCount = reader.GetOrdinal("AvailableCount");
        var ordTotalLeagues = reader.GetOrdinal("TotalLeagues");

        var ordMaxERALast3 = reader.GetOrdinal("MaxERA_Last3");
        var ordMaxWHIPLast3 = reader.GetOrdinal("MaxWHIP_Last3");
        var ordMinIPLast3 = reader.GetOrdinal("MinIP_Last3");

        var ordNextGameDate = reader.GetOrdinal("NextGameDate");
        var ordOpponent = reader.GetOrdinal("Opponent");

        var ordRunsPerGame = reader.GetOrdinal("RunsPerGame");
        var ordKPercentage = reader.GetOrdinal("K_Percentage");

        while (await reader.ReadAsync())
        {
            var candidate = new SPTrendCandidate
            {
                PlayerID = reader.GetInt32(ordPlayerID),
                SP = reader.GetString(ordSP),

                ERA = reader.GetDecimal(ordERA),
                WHIP = reader.GetDecimal(ordWHIP),
                K9 = reader.GetDecimal(ordK9),

                ERA_Last3 = reader.IsDBNull(ordERALast3)
                    ? null
                    : reader.GetDecimal(ordERALast3),

                WHIP_Last3 = reader.IsDBNull(ordWHIPLast3)
                    ? null
                    : reader.GetDecimal(ordWHIPLast3),

                K9_Last3 = reader.IsDBNull(ordK9Last3)
                    ? null
                    : reader.GetDecimal(ordK9Last3),

                Starts_Last30 = reader.GetInt32(ordStartsLast30),

                Starts_Last3 = reader.IsDBNull(ordStartsLast3)
                    ? 0
                    : reader.GetInt32(ordStartsLast3),

                AvailableCount = reader.GetInt32(ordAvailableCount),
                TotalLeagues = reader.GetInt32(ordTotalLeagues),

                MaxERA_Last3 = reader.IsDBNull(ordMaxERALast3)
                    ? null
                    : reader.GetDecimal(ordMaxERALast3),

                MaxWHIP_Last3 = reader.IsDBNull(ordMaxWHIPLast3)
                    ? null
                    : reader.GetDecimal(ordMaxWHIPLast3),

                MinIP_Last3 = reader.IsDBNull(ordMinIPLast3)
                    ? null
                    : reader.GetDecimal(ordMinIPLast3),

                NextGameDate = reader.IsDBNull(ordNextGameDate)
                    ? null
                    : reader.GetDateTime(ordNextGameDate),

                Opponent = reader.IsDBNull(ordOpponent)
                    ? null
                    : reader.GetString(ordOpponent),

                RunsPerGame = reader.IsDBNull(ordRunsPerGame)
                    ? null
                    : reader.GetDecimal(ordRunsPerGame),

                K_Percentage = reader.IsDBNull(ordKPercentage)
                    ? null
                    : reader.GetDecimal(ordKPercentage)
            };

            result.Add(candidate);
        }

        return result;
    }
}