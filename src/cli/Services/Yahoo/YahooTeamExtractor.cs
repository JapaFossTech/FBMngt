using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamExtractor
{
    // ------------------------------------------------------------
    // Entry point: extract all team nodes safely
    // ------------------------------------------------------------
    public static IEnumerable<JsonElement> GetTeamNodes(
        JsonElement root)
    {
        var results = new List<JsonElement>();

        ExtractTeamNodesRecursive(root, results);

        return results;
    }

    // ------------------------------------------------------------
    // Recursive traversal to find "team" nodes
    // ------------------------------------------------------------
    private static void ExtractTeamNodesRecursive(
        JsonElement element,
        List<JsonElement> results)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            // If this object contains "team"
            if (element.TryGetProperty("team", out var teamNode))
            {
                if (teamNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in teamNode.EnumerateArray())
                    {
                        results.Add(t);
                    }
                }
                else if (teamNode.ValueKind ==
                         JsonValueKind.Object)
                {
                    results.Add(teamNode);
                }
            }

            // Traverse all properties
            foreach (var prop in element.EnumerateObject())
            {
                ExtractTeamNodesRecursive(
                    prop.Value,
                    results);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ExtractTeamNodesRecursive(
                    item,
                    results);
            }
        }
    }
}