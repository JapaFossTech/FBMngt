using FBMngt.Data.FB;
using FBMngt.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public class YahooService
{
    private readonly YahooApiClient _apiClient;
    private readonly ConfigSettings _config;
    private readonly IServiceProvider _serviceProvider;

    //Ctor
    public YahooService(YahooApiClient apiClient,
                        ConfigSettings config,
                        IServiceProvider serviceProvider)
    {
        _apiClient = apiClient;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    //----------------------------------------------------------
    // Add back DisplayLoginUri (used by command)
    //----------------------------------------------------------
    public Task DisplayLoginUri()
    {
        var authorizeUrl =
            "https://api.login.yahoo.com/oauth2/request_auth" +
            $"?client_id={_config.AppSettings.Yahoo_ClientId}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(
                _config.AppSettings.Yahoo_RedirectUri)}";

        Console.WriteLine("Open this URL in your browser:");
        Console.WriteLine();
        Console.WriteLine(authorizeUrl);
        Console.WriteLine();

        return Task.CompletedTask;
    }

    //----------------------------------------------------------
    // Persist STATIC data
    //----------------------------------------------------------
    public async Task PersistInJsonFileAsync()
    {
        Console.WriteLine("[INFO] Starting Static Data Ingest...");

        // 1. Resolve season key
        var seasonKey = await GetSeasonKeyAsync();

        Console.WriteLine($"[INFO] Season Key: {seasonKey}");

        // 2. Get leagues
        var leaguesUrl =
            $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/users;use_login=1/games;game_keys={seasonKey}" +
            $"/leagues?format=json";

        var leaguesJson = await _apiClient.GetAsync(leaguesUrl);

        SaveToFile($"{seasonKey}_leagues", leaguesJson);

        var leagueKeys = ExtractLeagueKeys(leaguesJson);

        Console.WriteLine($"[INFO] Leagues found: {leagueKeys.Count}");

        // 3. Loop leagues
        foreach (var leagueKey in leagueKeys)
        {
            Console.WriteLine($"[INFO] Processing League: {leagueKey}");

            // --- Teams ---
            var teamsUrl =
                $"https://fantasysports.yahooapis.com/fantasy/v2" +
                $"/league/{leagueKey}/teams?format=json";

            var teamsJson = await _apiClient.GetAsync(teamsUrl);

            SaveToFile($"{leagueKey}_teams", teamsJson);

            // --- Settings ---
            var settingsUrl =
                $"https://fantasysports.yahooapis.com/fantasy/v2" +
                $"/league/{leagueKey}/settings?format=json";

            var settingsJson = await _apiClient.GetAsync(settingsUrl);

            SaveToFile($"{leagueKey}_settings", settingsJson);

            // --- Draft ---
            var draftUrl =
                $"https://fantasysports.yahooapis.com/fantasy/v2" +
                $"/league/{leagueKey}/draftresults?format=json";

            var draftJson = await _apiClient.GetAsync(draftUrl);

            SaveToFile($"{leagueKey}_draft", draftJson);
        }

        Console.WriteLine("[INFO] Static Data Ingest Completed.");
    }

    //----------------------------------------------------------
    // Extract league keys (UPDATED: uses proper extractor)
    //----------------------------------------------------------
    public List<string> ExtractLeagueKeys(string json)
    {
        return YahooLeagueExtractor.ExtractLeagueKeys(json);
    }

    //----------------------------------------------------------
    // ExtractTeamKeys (used by Daily Service)
    //----------------------------------------------------------
    public List<string> ExtractTeamKeys(string json)
    {
        var tempPath = Path.GetTempFileName();

        File.WriteAllText(tempPath, json);

        var league = YahooTeamReader.ReadLeagueFromFile(tempPath);

        return league.Teams
            .Where(t => !string.IsNullOrWhiteSpace(t.TeamKey))
            .Select(t => t.TeamKey!)
            .ToList();
    }

    //----------------------------------------------------------
    // Season Key (unchanged working version)
    //----------------------------------------------------------
    internal async Task<string> GetSeasonKeyAsync()
    {
        var url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            "/users;use_login=1/games?format=json";

        var json = await _apiClient.GetAsync(url);

        SaveToFile("season", json);

        using var doc = JsonDocument.Parse(json);

        var games = doc.RootElement
            .GetProperty("fantasy_content")
            .GetProperty("users")
            .GetProperty("0")
            .GetProperty("user")[1]
            .GetProperty("games");

        var currentYear = DateTime.Now.Year.ToString();

        foreach (var gameEntry in games.EnumerateObject())
        {
            if (gameEntry.Name == "count")
                continue;

            var game = gameEntry.Value
                .GetProperty("game")[0];

            var season =
                game.GetProperty("season").GetString();

            if (season == currentYear)
            {
                return game
                    .GetProperty("game_key")
                    .GetString()!;
            }
        }

        throw new Exception(
            $"MLB season key not found for year {currentYear}");
    }

    //----------------------------------------------------------
    // Save helper
    //----------------------------------------------------------
    private void SaveToFile(string reportId, string content)
    {
        string path = Path.Combine(
            _config.AppSettings.ReportPath,
            $@"yahoo\yahoo_{reportId}.json");

        File.WriteAllText(path, content);

        Console.WriteLine($"[INFO] Saved: {reportId}");
    }

    //----------------------------------------------------------
    // Existing static read
    //----------------------------------------------------------
    public async Task PersistStaticAsync()
    {
        var yahooDir = Path.Combine(
            _config.AppSettings.ReportPath,
            "yahoo");

        if (!Directory.Exists(yahooDir))
        {
            Console.WriteLine(
                $"Directory not found: {yahooDir}");
            return;
        }

        var files = Directory.GetFiles(
            yahooDir,
            "yahoo_*_teams.json",
            SearchOption.TopDirectoryOnly);

        Console.WriteLine(
            $"Found {files.Length} league files");

        int season = DateTime.Now.Year;

        // --------------------------------------------------------
        // RESOLVE REPOSITORIES
        // --------------------------------------------------------
        var leagueRepo =
            _serviceProvider.GetRequiredService<
                IFantasyLeagueRepository>();

        var teamRepo =
            _serviceProvider.GetRequiredService<
                IFantasyTeamRepository>();

        var leagueTeamRepo =
            _serviceProvider.GetRequiredService<
                IFantasyLeagueTeamRepository>();

        foreach (var file in files)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Processing: {Path.GetFileName(file)}");

            var league =
                YahooTeamReader.ReadLeagueFromFile(file);

            Console.WriteLine(
                $"{league.LeagueKey} | {league.Name}");

            // ----------------------------------------------------
            // UPSERT LEAGUE
            // ----------------------------------------------------
            var dbLeague = await leagueRepo
                .GetByYahooKeyAsync(league.LeagueKey);

            if (dbLeague == null)
            {
                dbLeague = new Models.FB.FBLeague
                {
                    YahooLeagueKey = league.LeagueKey,
                    LeagueName = league.Name,
                    Season = season
                };

                dbLeague.FBLeagueID =
                    await leagueRepo.InsertAsync(dbLeague);

                Console.WriteLine(
                    $"[FB] Inserted League: {dbLeague.LeagueName}");
            }
            else
            {
                Console.WriteLine(
                    $"[FB] League exists: {dbLeague.LeagueName}");
            }

            // ----------------------------------------------------
            // TEAMS
            // ----------------------------------------------------
            foreach (var team in league.Teams)
            {
                Console.WriteLine(
                    $"{team.TeamKey} | {team.Name}");

                if (string.IsNullOrWhiteSpace(team.TeamKey))
                {
                    Console.WriteLine(
                        "[FB] Missing TeamKey. Skipping.");
                    continue;
                }

                // --------------------------------------------
                // UPSERT TEAM
                // --------------------------------------------
                var dbTeam = await teamRepo
                    .GetByNameAsync(team.Name ?? "UNKNOWN");

                if (dbTeam == null)
                {
                    dbTeam = new Models.FB.FBTeam
                    {
                        TeamName = team.Name ?? "UNKNOWN",
                        IsJavier = false
                    };

                    dbTeam.FBTeamID =
                        await teamRepo.InsertAsync(dbTeam);

                    Console.WriteLine(
                        $"[FB] Inserted Team: {dbTeam.TeamName}");
                }
                else
                {
                    Console.WriteLine(
                        $"[FB] Team exists: {dbTeam.TeamName}");
                }

                // --------------------------------------------
                // UPSERT LEAGUE-TEAM
                // --------------------------------------------
                var dbLeagueTeam =
                    await leagueTeamRepo
                        .GetByYahooTeamKeyAsync(team.TeamKey);

                if (dbLeagueTeam == null)
                {
                    var leagueTeam =
                        new Models.FB.FBLeagueTeam
                        {
                            FBLeagueID =
                            dbLeague.FBLeagueID!.Value,
                            FBTeamID =
                            dbTeam.FBTeamID!.Value,
                            YahooTeamKey = team.TeamKey
                        };

                    await leagueTeamRepo
                        .InsertAsync(leagueTeam);

                    Console.WriteLine(
                        "[FB] Linked Team to League");
                }
                else
                {
                    Console.WriteLine(
                        "[FB] League-Team exists");
                }
            }
        }
    }
}