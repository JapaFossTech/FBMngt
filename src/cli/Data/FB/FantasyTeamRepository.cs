namespace FBMngt.Data.FB;

using FBMngt.Models.FB;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL repository for Fantasy Teams.
/// Maps to: dbo.tblFBTeam
/// </summary>
public class FantasyTeamRepository : IFantasyTeamRepository
{
    private readonly string _connectionString;

    public FantasyTeamRepository(
        ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }

    /// <summary>
    /// Gets a team by name.
    /// </summary>
    public async Task<FBTeam?> GetByNameAsync(
        string teamName)
    {
        const string sql = @"
SELECT FBTeamID,
       TeamName,
       IsJorge,
       IsJavier
FROM dbo.tblFBTeam
WHERE TeamName = @TeamName";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(
            "@TeamName", teamName);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.Read())
            return null;

        return new FBTeam
        {
            FBTeamID = reader.GetInt32(0),
            TeamName = reader.GetString(1),
            IsJorge = reader.IsDBNull(2)
                ? null
                : reader.GetBoolean(2),
            IsJavier = reader.GetBoolean(3)
        };
    }

    /// <summary>
    /// Inserts a new team.
    /// </summary>
    public async Task<int> InsertAsync(
        FBTeam team)
    {
        const string sql = @"
INSERT INTO dbo.tblFBTeam
(
    TeamName,
    IsJorge,
    IsJavier
)
OUTPUT INSERTED.FBTeamID
VALUES
(
    @TeamName,
    @IsJorge,
    @IsJavier
)";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue(
            "@TeamName", team.TeamName);

        cmd.Parameters.AddWithValue(
            "@IsJorge",
            (object?)team.IsJorge ?? DBNull.Value);

        cmd.Parameters.AddWithValue(
            "@IsJavier", team.IsJavier);

        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }
}