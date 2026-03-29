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
        if (args.Contains("--persistStatic"))
        {
            await _yahooService.PersistStaticAsync();
            return;
        }

        if (args.Contains("--persistInJsonFile"))
        {
            await _yahooService.PersistInJsonFileAsync();
        }
    }
}

// yahoo [--showLoginUri] [--getAccessToken]
// yahoo --persistInJsonFile
// yahoo --persistStatic
