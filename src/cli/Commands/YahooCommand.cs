using FBMngt.Models;
using FBMngt.Services.Yahoo;
using FBMngt.Services.Yahoo.DailyIngest;
using Microsoft.Extensions.DependencyInjection;

namespace FBMngt.Commands;

public class YahooCommand
{
    private readonly YahooService _yahooService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigSettings _config;

    public YahooCommand(YahooService yahooService,
                        IServiceProvider serviceProvider)
    {
        _yahooService = yahooService;
        _serviceProvider = serviceProvider;
        _config = _serviceProvider
                            .GetRequiredService<ConfigSettings>();
    }

    public async Task ExecuteAsync(string[] args)
    {
        if (args.Contains("--showLoginUri"))
        {
            await _yahooService.DisplayLoginUri();
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

        if (args.Contains("--persistDaily"))
        {
            var dailyService =
                _serviceProvider.GetRequiredService<
                    YahooDailyDataService>();

            await dailyService.PersistDailyDataAsync();

            var rosterProcessor =
                _serviceProvider.GetRequiredService<
                    YahooRosterFileProcessor>();

            await rosterProcessor.ProcessDirectoryAsync(
                _config.Yahoo_Daily_Path);

            return;
        }
    }
}

// yahoo [--showLoginUri] [--getAccessToken]
// yahoo --persistInJsonFile
// yahoo --persistStatic
// yahoo --persistDaily

