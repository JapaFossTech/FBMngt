using FBMngt.Services.Yahoo;

namespace FBMngt.Commands;

public static class YahooCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        var configSettings = new ConfigSettings(new AppSettings());
        var service = new YahooService(configSettings);

        if (args.Contains("--showLoginUri"))
        {
            await service.DisplayLoginUri();
            return;
        }
        if (args.Contains("--getAccessToken"))
        {
            await service.GetAccessToken();
            return;
        }

        await service.RunAsync();
    }
}
