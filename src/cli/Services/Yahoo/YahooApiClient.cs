using Duende.IdentityModel.Client;
using System.Net.Http.Headers;

namespace FBMngt.Services.Yahoo;

public class YahooApiClient
{
    private readonly HttpClient _http;
    private readonly ConfigSettings _config;

    // Cached token (per execution)
    private string? _accessToken;

    public YahooApiClient(HttpClient http,
                          ConfigSettings config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> GetAsync(string url)
    {
        // Ensure we have a token (ONLY ONCE)
        if (string.IsNullOrEmpty(_accessToken))
        {
            _accessToken = await GetAccessTokenAsync();
        }

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _accessToken);

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("[ERROR] Yahoo API call failed");
            Console.WriteLine(response.StatusCode);

            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine(err);

            throw new Exception("Yahoo API call failed");
        }

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        Console.WriteLine("[INFO] Retrieving access token...");

        var tokenResponse =
            await _http.RequestRefreshTokenAsync(
                new RefreshTokenRequest
                {
                    Address =
                        "https://api.login.yahoo.com/oauth2/get_token",
                    ClientId =
                        _config.AppSettings.Yahoo_ClientId,
                    ClientSecret =
                        _config.AppSettings.Yahoo_ClientSecret,
                    RefreshToken =
                        _config.AppSettings.Yahoo_RefreshToken
                });

        if (tokenResponse.IsError)
        {
            Console.WriteLine("[ERROR] Failed to retrieve access token");
            Console.WriteLine(tokenResponse.Raw);

            throw new Exception("OAuth token request failed");
        }

        return tokenResponse.AccessToken!;
    }
}