using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooLeagueExtractor
{
    // ------------------------------------------------------------
    // Extract ALL league keys from ANY Yahoo JSON shape
    // ------------------------------------------------------------
    public static List<string> ExtractLeagueKeys(string json)
    {
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        var leagueKeys = new List<string>();

        // Start recursive search
        ExtractLeagueKeysRecursive(root, leagueKeys);

        if (leagueKeys.Count == 0)
        {
            throw new Exception(
                "No league keys found in JSON.");
        }

        Console.WriteLine(
            $"[INFO] Leagues found: {leagueKeys.Count}");

        return leagueKeys;
    }

    // ------------------------------------------------------------
    // Recursive traversal (SAFE against Yahoo weird structure)
    // ------------------------------------------------------------
    private static void ExtractLeagueKeysRecursive(
        JsonElement element,
        List<string> results)
    {
        // If this node contains league_key → capture it
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("league_key", out var keyProp))
            {
                var key = keyProp.GetString();

                if (!string.IsNullOrWhiteSpace(key))
                {
                    results.Add(key);
                }
            }

            // Traverse all properties
            foreach (var prop in element.EnumerateObject())
            {
                ExtractLeagueKeysRecursive(
                    prop.Value,
                    results);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            // Traverse array items
            foreach (var item in element.EnumerateArray())
            {
                ExtractLeagueKeysRecursive(
                    item,
                    results);
            }
        }
    }
}