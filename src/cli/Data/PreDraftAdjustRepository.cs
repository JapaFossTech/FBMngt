using Microsoft.Data.SqlClient;

namespace FBMngt.Data;

public interface IPreDraftAdjustRepository
{
    Task DeleteAllAsync();
    Task<Dictionary<int, int>> GetAllAsync();
    Task InsertAsync(int playerId, int offset);
    Task UpsertAsync(int playerId, int offset);
}

public class PreDraftAdjustRepository : IPreDraftAdjustRepository
{
    private readonly string _connectionString;

    private SqlConnection CreateConnection()
        => new SqlConnection(_connectionString);

    // Ctor
    public PreDraftAdjustRepository(ConfigSettings configSettings)
    {
        _connectionString = configSettings.MLB_ConnString;
    }
    public async Task<Dictionary<int, int>> GetAllAsync()
    {
        using var cn = CreateConnection();
        await cn.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT PlayerID, Offset FROM dbo.tblPreDraftAdjust", cn);

        var dict = new Dictionary<int, int>();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            dict[reader.GetInt32(0)] = reader.GetInt32(1);
        }

        return dict;
    }
    public async Task InsertAsync(int playerId, int offset)
    {
        using var cn = CreateConnection();
        await cn.OpenAsync();

        var cmd = new SqlCommand(@"
            INSERT INTO dbo.tblPreDraftAdjust(PlayerID, Offset)
            VALUES(@playerId, @offset)", cn);

        cmd.Parameters.AddWithValue("@playerId", playerId);
        cmd.Parameters.AddWithValue("@offset", offset);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task UpsertAsync(int playerId, int offset)
    {
        using var cn = CreateConnection();
        await cn.OpenAsync();

        var cmd = new SqlCommand(@"
        MERGE dbo.tblPreDraftAdjust AS target
        USING (SELECT @playerId AS PlayerID) AS src
        ON target.PlayerID = src.PlayerID
        WHEN MATCHED THEN
            UPDATE SET Offset = @offset
        WHEN NOT MATCHED THEN
            INSERT (PlayerID, Offset) VALUES (@playerId, @offset);", cn);

        cmd.Parameters.AddWithValue("@playerId", playerId);
        cmd.Parameters.AddWithValue("@offset", offset);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task DeleteAllAsync()
    {
        using var cn = CreateConnection();
        await cn.OpenAsync();

        var cmd = new SqlCommand(
            "DELETE FROM dbo.tblPreDraftAdjust", cn);

        await cmd.ExecuteNonQueryAsync();
    }

}
