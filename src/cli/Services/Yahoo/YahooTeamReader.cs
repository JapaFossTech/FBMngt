using FBMngt.Models.Yahoo;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamReader
{
    public static YahooLeague ReadLeagueFromFile(string filePath)
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

            // Identify metadata
            if (item.TryGetProperty("league_key", out _))
            {
                leagueMetadata = item;
            }

            // Identify teams
            if (item.TryGetProperty("teams", out var t))
            {
                teamsNode = t;
            }
        }

        // 🚨 Hard validation (fail fast)
        if (leagueMetadata.ValueKind == JsonValueKind.Undefined)
        {
            throw new Exception(
                "League metadata not found in JSON.");
        }

        var league = YahooLeagueMapper.Map(leagueMetadata);

        if (teamsNode.ValueKind == JsonValueKind.Undefined)
        {
            Console.WriteLine(
                "[WARN] Teams node not found.");
            return league;
        }

        var teamNodes = YahooTeamExtractor.GetTeamNodes(
            teamsNode);

        league.Teams = teamNodes
            .Select(YahooTeamMapper.Map)
            .ToList();

        // 🔍 Summary output
        Console.WriteLine(
            $"[INFO] League: {league.LeagueKey} | " +
            $"{league.Name}");

        Console.WriteLine(
            $"[INFO] Teams loaded: {league.Teams.Count}");

        return league;
    }
}