using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooLeagueMapper
{
    public static League Map(JsonElement leagueNode)
    {
        // Map only metadata here (teams handled outside)
        return new League
        {
            LeagueKey = YahooJsonNavigator.GetString(
                leagueNode, "league_key"),

            Name = YahooJsonNavigator.GetString(
                leagueNode, "name"),

            Teams = new List<FBTeam>()
        };
    }
}