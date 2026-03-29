using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamReader
{
    public static League ReadLeagueFromFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var document = JsonDocument.Parse(stream);

        var root = document.RootElement;

        JsonElement fantasyContent = root.GetProperty(
                                                "fantasy_content");

        JsonElement leagueArray = fantasyContent.GetProperty(
                                                "league");

        JsonElement leagueMetadata = default;
        JsonElement teamsNode = default;

        foreach (var item in leagueArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            // metadata
            if (item.TryGetProperty("league_key", out _))
            {
                leagueMetadata = item;
            }

            // teams
            if (item.TryGetProperty("teams", out var t))
            {
                teamsNode = t;
            }
        }

        var league = YahooLeagueMapper.Map(leagueMetadata);

        if (teamsNode.ValueKind != JsonValueKind.Undefined)
        {
            var teamNodes = YahooTeamExtractor.GetTeamNodes(
                                                        teamsNode);

            league.Teams = teamNodes
                .Select(YahooTeamMapper.Map)
                .ToList();
        }

        return league;
    }
}
