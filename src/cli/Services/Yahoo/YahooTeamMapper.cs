using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamMapper
{
    public static FBTeam Map(JsonElement teamNode)
    {
        return new FBTeam
        {
            TeamKey = YahooJsonNavigator.GetString(
                teamNode, "team_key"),

            Name = YahooJsonNavigator.GetString(
                teamNode, "name")
        };
    }
}