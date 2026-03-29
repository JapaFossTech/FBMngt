using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooLeagueMapper
{
    public static League Map(JsonElement leagueNode)
    {
        return new League
        {
            LeagueKey = GetString(leagueNode, "league_key"),
            Name = GetString(leagueNode, "name"),
            Teams = new List<FBTeam>()
        };
    }

    private static string GetString(JsonElement node, 
                                    string propertyName)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty(propertyName, out var value))
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString() ?? 
                                            string.Empty,
                    _ => string.Empty
                };
            }
        }

        return string.Empty;
    }
}
