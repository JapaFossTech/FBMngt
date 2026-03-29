using Duende.IdentityModel.Client;
using System.Net.Http.Headers;

namespace FBMngt.Services.Yahoo;

/// <summary>
/// Responsible ONLY for:
/// - Authentication (refresh token)
/// - Executing HTTP GET requests
/// </summary>
public class YahooApiClient
{
    private readonly IAppSettings _appSettings;

    // Ctor
    public YahooApiClient(IAppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    /// <summary>
    /// Gets a valid access token using refresh token
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        var http = new HttpClient();

        var tokenResponse = await http.RequestRefreshTokenAsync(
            new RefreshTokenRequest
            {
                Address =
                    "https://api.login.yahoo.com/oauth2/get_token",

                ClientId = _appSettings.Yahoo_ClientId,
                ClientSecret = _appSettings.Yahoo_ClientSecret,
                RefreshToken = _appSettings.Yahoo_RefreshToken
            });

        if (tokenResponse.IsError)
        {
            Console.WriteLine("Error refreshing token:");
            Console.WriteLine(tokenResponse.Error);
            throw new Exception("Token refresh failed");
        }

        return tokenResponse.AccessToken!;
    }

    /// <summary>
    /// Executes GET request and returns raw JSON
    /// </summary>
    public async Task<string> GetAsync(string url)
    {
        var accessToken = await GetAccessTokenAsync();

        var http = new HttpClient();

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                accessToken);

        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Error calling Yahoo API:");
            Console.WriteLine(response.StatusCode);

            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine(err);

            throw new Exception("Yahoo API call failed");
        }

        return await response.Content.ReadAsStringAsync();
    }
}