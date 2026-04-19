using FBMngt.Models.FB;
using FBMngt.Models.SPTrending;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FBMngt.Data.SPTrending;

public class WaiverRepository
{
    private readonly IAppSettings _appSettings;

    public WaiverRepository(IAppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    private SqlConnection CreateConnection()
    {
        return null;
        //return new SqlConnection(_appSettings.ConnectionString);
    }

    public async Task<List<WaiverPitcher>> GetWaiverPitchersAsync()
    {
        var result = new List<WaiverPitcher>();

        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"-- YOUR FINAL SQL QUERY HERE";

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new WaiverPitcher
            {
                PlayerID = reader.GetInt32(0),
                PlayerName = reader.GetString(1),

                IP_Month = reader.GetDecimal(2),
                ERA = reader.GetDecimal(3),
                WHIP = reader.GetDecimal(4),
                K9 = reader.GetDecimal(5),

                Starts_Last30 = reader.GetInt32(6),

                Starts_Last3 = reader.GetInt32(8),
                ERA_Last3 = reader.GetDecimal(9),
                WHIP_Last3 = reader.GetDecimal(10),
                K9_Last3 = reader.GetDecimal(11),

                AvailableCount = reader.GetInt32(12),
                TotalLeagues = reader.GetInt32(13)
            });
        }

        return result;
    }
}
