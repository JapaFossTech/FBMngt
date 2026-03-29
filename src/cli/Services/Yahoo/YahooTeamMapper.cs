using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamMapper
{
    public static FBTeam Map(JsonElement teamNode)
    {
        var team = new FBTeam
        {
            TeamKey = YahooJsonNavigator.GetString(
                teamNode, "team_key"),

            Name = YahooJsonNavigator.GetString(
                teamNode, "name")
        };

        // 🔍 Validation (basic, explicit)
        if (string.IsNullOrWhiteSpace(team.TeamKey))
        {
            Console.WriteLine(
                "[WARN] Missing TeamKey in teamNode");
        }

        if (string.IsNullOrWhiteSpace(team.Name))
        {
            Console.WriteLine(
                $"[WARN] Missing Name for TeamKey: " +
                $"{team.TeamKey}");
        }

        return team;
    }
}