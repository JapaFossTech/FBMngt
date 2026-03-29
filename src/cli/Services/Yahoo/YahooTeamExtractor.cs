using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamExtractor
{
    public static IEnumerable<JsonElement> GetTeamNodes(
                                            JsonElement teamsNode)
    {
        foreach (JsonElement teamWrapper in YahooJsonHelper
                                    .GetYahooCollection(teamsNode))
        {
            // each wrapper looks like: { "team": [ { ... } ] }

            if (!teamWrapper.TryGetProperty("team", 
                                            out JsonElement teamArray))
                continue;

            if (teamArray.ValueKind != JsonValueKind.Array)
                continue;

            // Yahoo wraps actual object inside an array → take first element
            if (teamArray.GetArrayLength() == 0)
                continue;

            yield return teamArray[0];
        }
    }

}
