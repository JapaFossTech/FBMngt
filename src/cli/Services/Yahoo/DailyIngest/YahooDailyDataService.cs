using FBMngt.Models;
using System.Text.Json;

namespace FBMngt.Services.Yahoo.DailyIngest;

/// <summary>
/// Handles DAILY Yahoo data (changes frequently)
/// - rosters
/// - standings (future)
/// - transactions (future)
/// </summary>
public class YahooDailyDataService
{
    private readonly YahooApiClient _apiClient;
    private readonly YahooService _yahooService;
    private readonly ConfigSettings _config;

    public YahooDailyDataService(
        YahooApiClient apiClient,
        YahooService yahooService,
        ConfigSettings config)
    {
        _apiClient = apiClient;
        _yahooService = yahooService;
        _config = config;
    }

    /// <summary>
    /// Entry point for daily ingestion
    /// </summary>
    public async Task PersistDailyDataAsync()
    {
        Console.WriteLine("[INFO] Starting Daily Data Ingest...");

        // ------------------------------------------------------------
        // Step 1: Get current season key (reuse logic)
        // ------------------------------------------------------------
        string seasonKey = await _yahooService.GetSeasonKeyAsync();

        Console.WriteLine($"[INFO] Season Key: {seasonKey}");

        // ------------------------------------------------------------
        // Step 2: Get leagues for season
        // ------------------------------------------------------------
        string leaguesUrl =
            $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/users;use_login=1/games;game_keys={seasonKey}" +
            $"/leagues?format=json";

        var leaguesJson = await _apiClient.GetAsync(leaguesUrl);

        var leagueKeys = _yahooService.ExtractLeagueKeys(leaguesJson);

        Console.WriteLine($"[INFO] Leagues found: {leagueKeys.Count}");

        // ------------------------------------------------------------
        // Step 3: Loop leagues
        // ------------------------------------------------------------
        foreach (var leagueKey in leagueKeys)
        {
            Console.WriteLine($"[INFO] Processing League: {leagueKey}");

            // --------------------------------------------------------
            // Step 3.1: Get teams
            // --------------------------------------------------------
            string teamsUrl =
                $"https://fantasysports.yahooapis.com/fantasy/v2" +
                $"/league/{leagueKey}/teams?format=json";

            var teamsJson = await _apiClient.GetAsync(teamsUrl);

            var teamKeys = _yahooService.ExtractTeamKeys(teamsJson);

            Console.WriteLine(
                $"[INFO] Teams found: {teamKeys.Count}");

            // --------------------------------------------------------
            // Step 3.2: Loop teams → download roster
            // --------------------------------------------------------
            foreach (var teamKey in teamKeys)
            {
                Console.WriteLine(
                    $"[INFO] Downloading roster: {teamKey}");

                try
                {
                    string rosterUrl =
                        $"https://fantasysports.yahooapis.com/fantasy/v2" +
                        $"/team/{teamKey}/roster?format=json";

                    var rosterJson =
                        await _apiClient.GetAsync(rosterUrl);

                    SaveToFile($"{teamKey}_roster", rosterJson);
                }
                catch (Exception ex)
                {
                    // IMPORTANT: do NOT fail entire run
                    Console.WriteLine(
                        $"[ERROR] Failed roster for {teamKey}");

                    Console.WriteLine(ex.Message);
                }
            }
        }

        Console.WriteLine("[INFO] Daily Data Ingest Completed");
    }

    // -------------------------------
    // API CALLS
    // -------------------------------

    private async Task<string> GetGamesJsonAsync()
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            "/users;use_login=1/games?format=json";

        return await _apiClient.GetAsync(url);
    }

    private async Task<string> GetLeaguesJsonAsync(
        string seasonKey)
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/users;use_login=1/games;game_keys={seasonKey}" +
            "/leagues?format=json";

        return await _apiClient.GetAsync(url);
    }

    private async Task<string> GetTeamsJsonAsync(
        string leagueKey)
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/teams?format=json";

        return await _apiClient.GetAsync(url);
    }

    private async Task<string> GetRosterJsonAsync(
        string teamKey)
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/team/{teamKey}/roster?format=json";

        return await _apiClient.GetAsync(url);
    }

    // -------------------------------
    // EXTRACTION (reuse logic)
    // -------------------------------

    private string ExtractSeasonKey(string json)
    {
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        var fantasyContent =
            root.GetProperty("fantasy_content");

        var users = fantasyContent.GetProperty("users");

        var userWrapper = users.GetProperty("0");

        var userArray = userWrapper.GetProperty("user");

        string currentYear = DateTime.Now.Year.ToString();

        foreach (var item in userArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            if (!item.TryGetProperty("games", out var games))
                continue;

            foreach (var gameWrapper in YahooJsonHelper
                .GetYahooCollection(games))
            {
                if (!gameWrapper.TryGetProperty("game",
                    out var gameArray))
                    continue;

                if (gameArray.ValueKind != JsonValueKind.Array
                    || gameArray.GetArrayLength() == 0)
                    continue;

                var game = gameArray[0];

                var code = YahooJsonNavigator.GetString(
                    game, "code");

                if (code != "mlb")
                    continue;

                var season = YahooJsonNavigator.GetString(
                    game, "season");

                if (season != currentYear)
                    continue;

                var seasonKey = YahooJsonNavigator.GetString(
                    game, "game_key");

                if (!string.IsNullOrWhiteSpace(seasonKey))
                    return seasonKey;
            }
        }

        throw new Exception(
            $"Season key not found for {currentYear}");
    }

    private List<string> ExtractLeagueKeys(string json)
    {
        // reuse your existing logic (copy from YahooService)
        return _yahooService.ExtractLeagueKeys(json);
    }

    // -------------------------------
    // FILE SAVE
    // -------------------------------

    private void SaveToFile(string reportId, string content)
    {
        string path = Path.Combine(
            _config.AppSettings.ReportPath,
            $@"yahoo\Daily\yahoo_{reportId}.json");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        File.WriteAllText(path, content);

        Console.WriteLine($"[INFO] Saved: {reportId}");
    }
}
