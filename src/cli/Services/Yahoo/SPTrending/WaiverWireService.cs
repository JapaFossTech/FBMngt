using FBMngt.Data.SPTrending;
using FBMngt.Models.FB;
using FBMngt.Models.SPTrending;
using FBMngt.Services.Yahoo.SPTrending;
using Microsoft.Extensions.DependencyInjection;

namespace FBMngt.Services.Yahoo.SPTrending;

public class WaiverWireService
{
    private readonly IServiceProvider _serviceProvider;

    public WaiverWireService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<List<WaiverPitcher>> GetRankedPitchersAsync()
    {
        //var repo = (WaiverRepository)_serviceProvider
        //           .GetService(typeof(WaiverRepository))!;
        var repo = _serviceProvider
                   .GetRequiredService<WaiverRepository>();

        var analyzer = new PitcherTrendAnalyzer();

        var pitchers = await repo.GetWaiverPitchersAsync();

        return analyzer.Analyze(pitchers);
    }
}