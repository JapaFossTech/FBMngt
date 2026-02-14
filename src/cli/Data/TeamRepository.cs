namespace FBMngt.Data;

using FBMngt.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class TeamRepository
{
    private readonly ConfigSettings _configSettings;
    private string _connStr => _configSettings.MLB_ConnString;

    //Ctor
    public TeamRepository(ConfigSettings configSettings)
    {
        _configSettings = configSettings;
    }

    public async Task<List<Team>> GetTeamsAsync()
    {
        var teams = new List<Team>();

        using var conn = new SqlConnection(_connStr);
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
