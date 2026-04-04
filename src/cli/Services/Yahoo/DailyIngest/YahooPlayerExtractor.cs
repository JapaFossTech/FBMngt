using System.Text.Json;

namespace FBMngt.Services.Yahoo.DailyIngest;

/// <summary>
/// Extracts FULL player objects from Yahoo roster JSON.
/// Handles fragmented "player" arrays.
/// </summary>
public class YahooPlayerExtractor
{
    public List<JsonElement> ExtractPlayers(JsonElement root)
    {
        var players = new List<JsonElement>();

        Traverse(root, players);

        return players;
    }

    private void Traverse(
        JsonElement element,
        List<JsonElement> players)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            // Detect "player" property
            if (element.TryGetProperty("player",
                out var playerArray) &&
                playerArray.ValueKind == JsonValueKind.Array)
            {
                var merged = MergePlayerArray(playerArray);

                if (merged.HasValue)
                {
                    players.Add(merged.Value);
                }
            }

            foreach (var prop in element.EnumerateObject())
            {
                Traverse(prop.Value, players);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Traverse(item, players);
            }
        }
    }

    /// <summary>
    /// Extracts and merges the TRUE player payload from Yahoo.
    /// Handles nested array structure:
    /// "player": [ [ {...}, {...} ], {...}, {...} ]
    /// </summary>
    private JsonElement? MergePlayerArray(
        JsonElement playerArray)
    {
        // Step 1: Validate structure
        if (playerArray.ValueKind != JsonValueKind.Array)
            return null;

        // Step 2: FIRST element should be the player array
        if (playerArray.GetArrayLength() == 0)
            return null;

        var firstElement = playerArray[0];

        if (firstElement.ValueKind != JsonValueKind.Array)
            return null;

        // Step 3: Merge ONLY the inner array (real player data)
        var dict = new Dictionary<string, JsonElement>();

        foreach (var item in firstElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var prop in item.EnumerateObject())
            {
                dict[prop.Name] = prop.Value;
            }
        }

        if (dict.Count == 0)
            return null;

        // Step 4: Rebuild JSON object
        var json = JsonSerializer.Serialize(dict);

        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.Clone();
    }
}