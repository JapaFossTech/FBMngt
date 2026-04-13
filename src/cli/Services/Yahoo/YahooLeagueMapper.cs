using FBMngt.Models.Yahoo;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooLeagueMapper
{
    public static YahooLeague Map(JsonElement leagueNode)
    {
        // Map only metadata here (teams handled outside)
        var league = new YahooLeague
        {
            LeagueKey = YahooJsonNavigator.GetString(
                leagueNode, "league_key"),

            Name = YahooJsonNavigator.GetString(
                leagueNode, "name"),

            Teams = new List<YahooTeam>()
        };

        // 🔍 Validation
        if (string.IsNullOrWhiteSpace(league.LeagueKey))
        {
            Console.WriteLine(
                "[WARN] Missing LeagueKey");
        }

        if (string.IsNullOrWhiteSpace(league.Name))
        {
            Console.WriteLine(
                "[WARN] Missing League Name");
        }

        return league;
    }
}