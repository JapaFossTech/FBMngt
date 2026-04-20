namespace FBMngt.Services.Yahoo.SPTrending;

using FBMngt.Data.SPTrending;
using FBMngt.Models.SPTrending;
using Microsoft.Extensions.DependencyInjection;

public class SPTrendingService
{
    private readonly IServiceProvider _serviceProvider;

    public SPTrendingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<List<SPTrendCandidate>> 
        GetRankedCandidatesAsync()
    {
        var repo = _serviceProvider
            .GetRequiredService<ISPTrendingRepository>();

        var players = await repo.GetCandidatesAsync();

        foreach (var p in players)
        {
            CalculateTrend(p);
            CalculateRisk(p);
            CalculateFinalScore(p);
        }

        return players
            .OrderByDescending(p => p.FinalScore)
            .ToList();
    }

    private void CalculateTrend(SPTrendCandidate p)
    {
        // Lower ERA/WHIP is better → invert
        p.ERA_TrendDelta = p.ERA - p.ERA_Last3;
        p.WHIP_TrendDelta = p.WHIP - p.WHIP_Last3;

        // Higher K9 is better
        p.K9_TrendDelta = p.K9_Last3 - p.K9;

        p.TrendScore =
            (p.ERA_TrendDelta * 2.0m) +
            (p.WHIP_TrendDelta * 2.0m) +
            (p.K9_TrendDelta * 1.5m);
    }

    private void CalculateRisk(SPTrendCandidate p)
    {
        decimal risk = 0;

        // Low sample size risk
        if (p.Starts_Last30 < 4)
            risk += 2;

        // WHIP danger
        if (p.WHIP > 1.30m)
            risk += 2;

        // ERA danger
        if (p.ERA > 4.00m)
            risk += 2;

        // Trend instability
        if (p.ERA_Last3 > p.ERA * 1.5m)
            risk += 2;

        p.RiskScore = risk;
    }

    private void CalculateFinalScore(SPTrendCandidate p)
    {
        // Availability boost (roto strategy)
        var availabilityBoost =
            (decimal)p.AvailableCount / p.TotalLeagues;

        p.FinalScore =
            p.TrendScore
            - p.RiskScore
            + (availabilityBoost * 2.0m);
    }
}