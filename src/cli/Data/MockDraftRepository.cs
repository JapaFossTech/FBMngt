using FBMngt.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FBMngt.Data;

public interface IMockDraftRepository
{
    Task<List<MockDraftMarketStat>> GetMockMarketStatsAsync(
                                        DateTime startDate,
                                        DateTime endDate);
}

public class MockDraftRepository : IMockDraftRepository
{
    private readonly string _connectionString;

    // Ctor
    public MockDraftRepository(ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    public async Task<List<MockDraftMarketStat>> 
    GetMockMarketStatsAsync(DateTime startDate,
                            DateTime endDate)
    {
        List<MockDraftMarketStat> results = new();

        using SqlConnection connection = CreateConnection();
        await connection.OpenAsync();

        string sql = @"
        SELECT PlayerName,
           STDEV(PickNumber) AS Pick_StDev,
           COUNT(PickNumber) AS Pick_Count,
           CAST(AVG(PickNumber) AS DECIMAL(10,2)) AS Pick_Average,
           CAST(AVG([Round]) AS DECIMAL(10,2)) AS Pick_Rnd,
           MIN(PickNumber)   AS Pick_Min,
           MAX(PickNumber)   AS Pick_Max,
           MAX(PickNumber) - MIN(PickNumber) AS Pick_Diff,
           CAST(AVG(PickNumber) - MIN(PickNumber) AS DECIMAL(10,2)) 
                AS ReachIndex,
           CAST(MAX(PickNumber) - AVG(PickNumber) AS DECIMAL(10,2)) 
                AS FallIndex,
           STDEV(PickNumber) / AVG(PickNumber) AS VolatilityIndex
        FROM Draft
        WHERE DraftTypeDesc = 'Mockup'
          AND DraftDate >= @StartDate
          AND DraftDate <= @EndDate
          AND IsMyPick = 0
        GROUP BY PlayerName
        ORDER BY Pick_Average;";

        using SqlCommand command = new(sql, connection);

        command.Parameters.Add(
            new SqlParameter("@StartDate", SqlDbType.DateTime)
            {
                Value = startDate
            });

        command.Parameters.Add(
            new SqlParameter("@EndDate", SqlDbType.DateTime)
            {
                Value = endDate
            });

        using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var stat = new MockDraftMarketStat
            {
                PlayerName = reader.GetString(0),
                PickStDev = reader.IsDBNull(1) 
                            ? 0 : reader.GetDouble(1),
                PickCount = reader.GetInt32(2),
                PickAverage = reader.GetDecimal(3),
                PickRoundAverage = reader.GetDecimal(4),
                PickMin = reader.GetInt32(5),
                PickMax = reader.GetInt32(6),
                PickDiff = reader.GetInt32(7),
                ReachIndex = reader.GetDecimal(8),
                FallIndex = reader.GetDecimal(9),
                //VolatilityIndex = reader.GetDouble(10)


            };

            results.Add(stat);
        }

        return results;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}