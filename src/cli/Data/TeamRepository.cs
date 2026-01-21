namespace FBMngt.Data;

using FBMngt.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class TeamRepository
{
    private readonly string _connectionString;

    public TeamRepository()
    {
        _connectionString =
            Program.Configuration.GetConnectionString("MLB")
            ?? throw new Exception("Missing connection string 'MLB'");
    }

    public async Task<List<Team>> GetTeamsAsync()
    {
        var teams = new List<Team>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "SELECT TeamID, Team, mlb_org_id, mlb_org_abbrev FROM lktblTeam",
            conn);

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            teams.Add(new Team
            {
                TeamID = reader.GetInt32(0),
                //TeamName = reader.IsDBNull(1) ? null : reader.GetString(1),
                MlbOrgId = reader.IsDBNull(2) ? null : reader.GetString(2),
                MlbOrgAbbrev = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return teams;
    }

}
