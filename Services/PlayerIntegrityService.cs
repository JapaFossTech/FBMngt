using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FBMngt.Services;

public class PlayerIntegrityService
{
    private readonly string _connStr =
        Program.Configuration.GetConnectionString("MLB")!;

    public async Task RunAllChecksAsync(bool dryRun)
    {
        Console.WriteLine("[FBMngt] Data Integrity – Players");

        await CheckAccentedNamesMissingAkasAsync(dryRun);
        await ReportCrossFieldNameCollisionsAsync();
        await ReportRedundantAkasAsync();
        await ReportAccentInsensitivePlayerNameDuplicatesAsync();
    }

    private async Task CheckAccentedNamesMissingAkasAsync(
                                                bool dryRun)
    {
        Console.WriteLine();
        Console.WriteLine("Check 1: Accented names missing AKAs");
        Console.WriteLine("Check: Accented names missing AKAs");
        Console.WriteLine(dryRun
            ? "Mode: DRY RUN (no updates)"
            : "Mode: APPLY CHANGES");
        Console.WriteLine();

        using var conn = new SqlConnection(
            Program.Configuration.GetConnectionString("MLB"));

        await conn.OpenAsync();

        var sql = dryRun
            ? GetSelectSql()
            : GetUpdateSql();

        using var cmd = new SqlCommand(sql, conn);

        if (dryRun)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;

            Console.WriteLine();
            Console.WriteLine("PlayerID | PlayerName | CanonicalName");

            while (await reader.ReadAsync())
            {
                count++;
                Console.WriteLine(
                    $"{reader.GetInt32(0),8} | " +
                    $"{reader.GetString(1)} | " +
                    $"{reader.GetString(4)}");
            }

            Console.WriteLine();
            Console.WriteLine($"Found {count} players");
            Console.WriteLine("No database changes were made.");
        }
        else
        {
            var affected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine();
            Console.WriteLine($"Rows updated: {affected}");
        }
    }
    private static string GetSelectSql() => @"
SELECT
    p.PlayerID,
    p.PlayerName,
    p.Aka1,
    p.Aka2,
    c.CanonicalName
FROM tblPlayer p
CROSS APPLY (
    SELECT
        TRANSLATE(
            p.PlayerName,
            N'áéíóúÁÉÍÓÚñÑüÜ',
            N'aeiouAEIOUnNuU'
        ) COLLATE Latin1_General_CI_AI AS CanonicalName
) c
WHERE
    p.PlayerName COLLATE Latin1_General_BIN2
        <> c.CanonicalName COLLATE Latin1_General_BIN2
AND p.Aka1 IS NULL
AND p.Aka2 IS NULL;
";
    private static string GetUpdateSql() => @"
UPDATE p
SET Aka1 = c.CanonicalName
FROM tblPlayer p
CROSS APPLY (
    SELECT
        TRANSLATE(
            p.PlayerName,
            N'áéíóúÁÉÍÓÚñÑüÜ',
            N'aeiouAEIOUnNuU'
        ) COLLATE Latin1_General_CI_AI AS CanonicalName
) c
WHERE
    p.PlayerName COLLATE Latin1_General_BIN2
        <> c.CanonicalName COLLATE Latin1_General_BIN2
AND p.Aka1 IS NULL
AND p.Aka2 IS NULL;
";

    private async Task ReportCrossFieldNameCollisionsAsync()
    {
        Console.WriteLine();
        Console.WriteLine("Check 2: Cross-field duplicate names");
        Console.WriteLine("(Same name mapped to multiple PlayerIDs)");
        Console.WriteLine();

        using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(@"
        WITH Names AS
        (
            SELECT PlayerID, PlayerName AS Name FROM dbo.tblPlayer WHERE PlayerName IS NOT NULL
            UNION ALL
            SELECT PlayerID, Aka1 FROM dbo.tblPlayer WHERE Aka1 IS NOT NULL
            UNION ALL
            SELECT PlayerID, Aka2 FROM dbo.tblPlayer WHERE Aka2 IS NOT NULL
        )
        SELECT
            Name,
            COUNT(DISTINCT PlayerID) AS PlayerCount
        FROM Names
        GROUP BY Name
        HAVING COUNT(DISTINCT PlayerID) > 1
        ORDER BY PlayerCount DESC, Name;
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        Console.WriteLine("Name | PlayerCount");

        while (await reader.ReadAsync())
        {
            Console.WriteLine(
                $"{reader.GetString(0)} | {reader.GetInt32(1)}");
        }

        Console.WriteLine();
    }

    private async Task ReportRedundantAkasAsync()
    {
        Console.WriteLine();
        Console.WriteLine("Check 3: Redundant AKAs");
        Console.WriteLine("(AKA equals PlayerName or duplicate AKA)");
        Console.WriteLine();

        using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(@"
        SELECT
            PlayerID,
            PlayerName,
            Aka1,
            Aka2
        FROM dbo.tblPlayer
        WHERE
            (PlayerName IS NOT NULL AND Aka1 = PlayerName)
         OR (PlayerName IS NOT NULL AND Aka2 = PlayerName)
         OR (Aka1 IS NOT NULL AND Aka2 = Aka1);
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        Console.WriteLine("PlayerID | PlayerName | Aka1 | Aka2");

        while (await reader.ReadAsync())
        {
            var playerId = reader.GetInt32(0);
            var playerName = GetNullableString(reader, 1);
            var aka1 = GetNullableString(reader, 2);
            var aka2 = GetNullableString(reader, 3);

            Console.WriteLine(
                $"{playerId} | " +
                $"{playerName ?? "(null)"} | " +
                $"{aka1 ?? "(null)"} | " +
                $"{aka2 ?? "(null)"}");
        }

        Console.WriteLine();
    }

    private static string? GetNullableString(
                        SqlDataReader reader, int index)
    {
        return reader.IsDBNull(index)
            ? null
            : reader.GetString(index);
    }

    private async Task ReportAccentInsensitivePlayerNameDuplicatesAsync()
    {
        Console.WriteLine("Check 4: Suspected same human (accent-insensitive PlayerName match)");
        Console.WriteLine();

        using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(@"
        SELECT
            p1.PlayerID AS PlayerID1,
            p1.PlayerName AS Name1,
            p2.PlayerID AS PlayerID2,
            p2.PlayerName AS Name2
        FROM dbo.tblPlayer p1
        JOIN dbo.tblPlayer p2
            ON p1.PlayerID < p2.PlayerID
           AND p1.PlayerName COLLATE Latin1_General_CI_AI
               = p2.PlayerName COLLATE Latin1_General_CI_AI
        ORDER BY Name1;
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        Console.WriteLine("PlayerID1 | Name1 | PlayerID2 | Name2");

        var count = 0;

        while (await reader.ReadAsync())
        {
            count++;

            Console.WriteLine(
                $"{reader.GetInt32(0),9} | " +
                $"{reader.GetString(1)} | " +
                $"{reader.GetInt32(2),9} | " +
                $"{reader.GetString(3)}");
        }

        Console.WriteLine();
        Console.WriteLine($"Suspected duplicate humans: {count}");
        Console.WriteLine();
    }

}