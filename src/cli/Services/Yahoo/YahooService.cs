using Duende.IdentityModel.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Services.Yahoo;

public class YahooService
{
    private readonly YahooApiClient _apiClient;
    private readonly ConfigSettings _configSettings;
    private IAppSettings AppSettings { get 
                                    => _configSettings.AppSettings; }
    //Ctor
    public YahooService(ConfigSettings configSettings)
    {
        _configSettings = configSettings;

        _apiClient = new YahooApiClient(configSettings.AppSettings);
    }

    public Task DisplayLoginUri()
    {
        var authorizeUrl =
            "https://api.login.yahoo.com/oauth2/request_auth" +
            $"?client_id={AppSettings.Yahoo_ClientId}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(
                AppSettings.Yahoo_RedirectUri)}";

        Console.WriteLine("Open this URL in your browser:");
        Console.WriteLine();
        Console.WriteLine(authorizeUrl);
        Console.WriteLine();
        return Task.CompletedTask;
    }

    public async Task GetAccessToken()
    {
        Console.Write("Paste the code: ");
        var code = Console.ReadLine();
        if (string.IsNullOrEmpty(code))
            throw new Exception("Invalid code!");

        var http = new HttpClient();

        var tokenResponse = await http.RequestAuthorizationCodeTokenAsync(
            new AuthorizationCodeTokenRequest
            {
                Address = "https://api.login.yahoo.com/oauth2/get_token",
                ClientId = AppSettings.Yahoo_ClientId,
                ClientSecret = AppSettings.Yahoo_ClientSecret,
                Code = code,
                RedirectUri = AppSettings.Yahoo_RedirectUri
            });

        if (tokenResponse.IsError)
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine(tokenResponse.Error);
            return;
        }

        Console.WriteLine();
        Console.WriteLine("ACCESS TOKEN:");
        Console.WriteLine(tokenResponse.AccessToken);
        Console.WriteLine();
        Console.WriteLine("REFRESH TOKEN:");
        Console.WriteLine(tokenResponse.RefreshToken);
    }

    #region PersistInJsonFileAsync()
    public async Task PersistInJsonFileAsync_old()
    {
        var http = new HttpClient();

        var tokenResponse = await http.RequestRefreshTokenAsync(
            new RefreshTokenRequest
            {
                Address = "https://api.login.yahoo.com/oauth2" +
                                                        "/get_token",
                ClientId = AppSettings.Yahoo_ClientId,
                ClientSecret = AppSettings.Yahoo_ClientSecret,
                RefreshToken = AppSettings.Yahoo_RefreshToken
            });

        if (tokenResponse.IsError)
        {
            Console.WriteLine("Error refreshing token:");
            Console.WriteLine(tokenResponse.Error);
            return;
        }

        var newAccessToken = tokenResponse.AccessToken;
        Console.WriteLine("New Access Token:");
        Console.WriteLine(newAccessToken);

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                                        newAccessToken);
        string url = "https://fantasysports.yahooapis.com/fantasy/v2" +
            "/users;use_login=1/games?format=json";
        string seasonKey = "469"; // 2026 Baseball season
        string leagueKey = "469.l.7042"; // Kantuta_2026
        string playerKey = "469.p.9097"; // Gary Sánchez
        string teamKey = "469.l.7042.t.8";
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/users;use_login=1/games;game_keys={seasonKey}" +
            $"/leagues?format=json";
        //teams
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/469.l.7042/teams?format=json";
        //team's roster
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/team/469.l.7042.t.1/roster?format=json";
        //league setting
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/settings?format=json";
        //league standings
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/standings?format=json";
        //league scoreboard
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/scoreboard?format=json";
        //league players
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/players;start=25;count=50?format=json";
        //league player's Stats
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/players;player_keys={playerKey}" +
            $"/stats?format=json"; Console.WriteLine(url);
        //league transactions
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/transactions?format=json";
        //league draft result
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/draftresults?format=json";
        //league draft result
        url = $"https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/team/{teamKey}?format=json";

        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Error calling Yahoo API:");
            Console.WriteLine(response.StatusCode);
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine(err);
            return;
        }

        var content = await response.Content.ReadAsStringAsync();

        // Print everything
        Console.WriteLine(content);
        // Save to file if too big
        string reportID = "leagues";
        reportID = leagueKey + "_teams";
        reportID = leagueKey + ".t.1_roster";
        reportID = leagueKey + "_leagueSetting";
        reportID = leagueKey + "_leagueStanding";
        reportID = leagueKey + "_leagueScoreboard";
        reportID = leagueKey + "_players";
        reportID = leagueKey + "_playerStats";
        reportID = leagueKey + "_leagueTransactions";
        reportID = leagueKey + "_leagueDraftResult";
        reportID = teamKey + "_team";
        string yahooPath = Path.Combine(
                        AppSettings.ReportPath,
                        $@"yahoo\yahoo_{reportID}.json");
        //System.IO.File.WriteAllText(yahooPath, content);
        Console.WriteLine(
            "Full JSON saved to yahoo_leagues.json");

        
    }
    private async Task<string> GetLeaguesJsonAsync(string seasonKey)
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/users;use_login=1/games;game_keys={seasonKey}" +
            "/leagues?format=json";

        return await _apiClient.GetAsync(url);
    }
    private async Task<string> GetTeamsJsonAsync(string leagueKey)
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/league/{leagueKey}/teams?format=json";

        return await _apiClient.GetAsync(url);
    }
    private async Task<string> GetRosterJsonAsync(string teamKey)
    {
        string url =
            "https://fantasysports.yahooapis.com/fantasy/v2" +
            $"/team/{teamKey}/roster?format=json";

        return await _apiClient.GetAsync(url);
    }
    private void SaveToFile(string reportId, string content)
    {
        string yahooPath = Path.Combine(
            AppSettings.ReportPath,
            $@"yahoo\yahoo_{reportId}.json");

        File.WriteAllText(yahooPath, content);

        Console.WriteLine($"[INFO] Saved: {reportId}");
    }
    public async Task PersistInJsonFileAsync()
    {
        // STEP 1: Get season (game) data
        string seasonKey = "469"; // TODO: resolve dynamically

        Console.WriteLine($"[INFO] SeasonKey: {seasonKey}");

        // STEP 2: Get leagues for season
        var leaguesJson = await GetLeaguesJsonAsync(seasonKey);

        SaveToFile($"season_{seasonKey}_leagues", leaguesJson);

        // TODO: parse league keys from JSON (next step)
        // For now, keep using known league key
        string leagueKey = "469.l.7042";

        Console.WriteLine($"[INFO] LeagueKey: {leagueKey}");

        // STEP 3: Get teams for league
        var teamsJson = await GetTeamsJsonAsync(leagueKey);

        SaveToFile($"{leagueKey}_teams", teamsJson);

        // STEP 4: Get rosters (example: first 3 teams only)
        var teamKeys = new List<string>
    {
        $"{leagueKey}.t.1",
        $"{leagueKey}.t.2",
        $"{leagueKey}.t.3"
    };

        foreach (var teamKey in teamKeys)
        {
            var rosterJson = await GetRosterJsonAsync(teamKey);

            SaveToFile($"{teamKey}_roster", rosterJson);
        }

        Console.WriteLine("[INFO] Persist completed.");
    }
    #endregion
    public Task PersistStaticAsync()
    {
        //reading from json file
        string leagueKey = "469.l.7042"; // Kantuta_2026
        string reportID = leagueKey + "_teams";
        string yahooPath = Path.Combine(
                        AppSettings.ReportPath,
                        $@"yahoo\yahoo_{reportID}.json");

        var league = YahooTeamReader.ReadLeagueFromFile(yahooPath);

        Console.WriteLine($"{league.LeagueKey} | {league.Name}");

        foreach (var team in league.Teams)
        {
            Console.WriteLine($"{team.TeamKey} | {team.Name}");
        }

        return Task.CompletedTask;
    }
}
