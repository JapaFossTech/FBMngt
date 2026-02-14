using FBMngt.Services.Players;
using FBMngt.Services.Yahoo;

namespace FBMngt.Commands;

public class YahooCommand
{
    private readonly YahooService _yahooService;

    public YahooCommand(YahooService yahooService)
    {
        _yahooService = yahooService;
    }
    public async Task ExecuteAsync(string[] args)
    {
        if (args.Contains("--showLoginUri"))
        {
            await _yahooService.DisplayLoginUri();
            return;
        }
        if (args.Contains("--getAccessToken"))
        {
            await _yahooService.GetAccessToken();
            return;
        }

        await _yahooService.RunAsync();
    }
}
