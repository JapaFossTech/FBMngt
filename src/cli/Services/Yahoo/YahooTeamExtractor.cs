using System.Text.Json;

namespace FBMngt.Services.Yahoo;

/// <summary>
/// Extracts team nodes from Yahoo JSON structure
/// </summary>
public static class YahooTeamExtractor
{
    public static IEnumerable<JsonElement> GetTeamNodes(
        JsonElement teamsNode)
    {
        foreach (var teamWrapper in YahooJsonHelper
            .GetYahooCollection(teamsNode))
        {
            // Each wrapper:
            // { "team": [ [ {...}, {...} ] ] }

            if (!teamWrapper.TryGetProperty("team", 
                                            out var teamArray))
                continue;

            if (teamArray.ValueKind != JsonValueKind.Array)
                continue;

            if (teamArray.GetArrayLength() == 0)
                continue;

            var firstLevel = teamArray[0];

            // 🔥 FIX: handle double array
            if (firstLevel.ValueKind == JsonValueKind.Array)
            {
                if (firstLevel.GetArrayLength() == 0)
                    continue;

                // actual team data (array-of-objects)
                yield return firstLevel;
            }
            else
            {
                // fallback (rare case)
                yield return firstLevel;
            }
        }
    }
}