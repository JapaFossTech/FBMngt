using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamReader
{
    public static List<FBTeam> ReadTeamsFromFile(string filePath)
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

            // Identify metadata object
            if (item.TryGetProperty("league_key", out _))
            {
                leagueMetadata = item;
            }

            // Identify teams object
            if (item.TryGetProperty("teams", out var t))
            {
                teamsNode = t;
            }
        }

        // Map league metadata
        var league = YahooLeagueMapper.Map(leagueMetadata);

        // Map teams and attach
        if (teamsNode.ValueKind != JsonValueKind.Undefined)
        {
            var teamNodes = YahooTeamExtractor.GetTeamNodes(teamsNode);

            league.Teams = teamNodes
                .Select(YahooTeamMapper.Map)
                .ToList();
        }

        return league.Teams;
    }
}