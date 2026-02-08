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
    private readonly ConfigSettings _configSettings;
    private IAppSettings AppSettings { get => _configSettings.AppSettings; }
    //Ctor
    public YahooService(ConfigSettings configSettings)
    {
        _configSettings = configSettings;
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

    internal async Task RunAsync()
    {
        var http = new HttpClient();

        var tokenResponse = await http.RequestRefreshTokenAsync(
            new RefreshTokenRequest
            {
                Address = "https://api.login.yahoo.com/oauth2/get_token",
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
        string url = "https://fantasysports.yahooapis.com/fantasy/v2/users;use_login=1/games?format=json";
        var gameKey = "469"; // 2026 Baseball
        url = $"https://fantasysports.yahooapis.com/fantasy/v2/users;use_login=1/games;game_keys={gameKey}/leagues?format=json";
        Console.WriteLine(url);

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
        //// Save to file if too big
        //string yahooPath = Path.Combine(
        //                AppSettings.ReportPath,
        //                $"yahoo_leagues.json");
        //System.IO.File.WriteAllText(yahooPath, content);
        //Console.WriteLine(
        //    "Full JSON saved to yahoo_raw.json");

        //var root = JsonDocument.Parse(content).RootElement;

        //var users = root.GetProperty("fantasy_content").GetProperty("users").GetProperty("0");
        //var gamesContainer = users.GetProperty("user")[1].GetProperty("games");

        //foreach (var gameKey in gamesContainer.EnumerateObject())
        //{
        //    // Skip keys that aren't game objects (like "count")
        //    if (!gameKey.Value.TryGetProperty("game", out var gameArray))
        //        continue;

        //    var game = gameArray[0];

        //    if (game.TryGetProperty("season", out var seasonElem) && seasonElem.GetString() == "2026")
        //    {
        //        Console.WriteLine("Game: " + game.GetProperty("name").GetString());

        //        if (game.TryGetProperty("leagues", out var leagues))
        //        {
        //            foreach (var leagueKey in leagues.EnumerateObject())
        //            {
        //                var league = leagueKey.Value.GetProperty("league")[0];
        //                Console.WriteLine("League: " + league.GetProperty("name").GetString());
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("No leagues yet for this season.");
        //        }
        //    }
        //}
    }
}
