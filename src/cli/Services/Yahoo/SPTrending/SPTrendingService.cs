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
            CalculateMatchup(p);
            CalculateFinalScore(p);
        }

        return players
            .OrderByDescending(p => p.FinalScore)
            .ToList();
    }

    private void CalculateTrend(SPTrendCandidate p)
    {
        // Lower ERA/WHIP is better → invert
        p.ERA_TrendDelta = p.ERA - (p.ERA_Last3 ?? p.ERA);
        p.WHIP_TrendDelta = p.WHIP - (p.WHIP_Last3 ?? p.WHIP);

        // Higher K9 is better
        p.K9_TrendDelta = (p.K9_Last3 ?? p.K9) - p.K9;

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
        if (p.ERA_Last3.HasValue && p.ERA_Last3 > p.ERA * 1.5m)
            risk += 2;

        // 🔥 NEW — Blowup detection
        if (p.MaxERA_Last3.HasValue && p.MaxERA_Last3 >= 7.0m)
            risk += 2;

        // 🔥 NEW — Control issues
        if (p.MaxWHIP_Last3.HasValue && p.MaxWHIP_Last3 >= 1.80m)
            risk += 2;

        // 🔥 NEW — Short outing (durability risk)
        if (p.MinIP_Last3.HasValue && p.MinIP_Last3 <= 3.0m)
            risk += 1;

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
            + p.MatchupScore
            + (availabilityBoost * 2.0m);
    }
    private void CalculateMatchup(SPTrendCandidate p)
    {
        decimal matchupScore = 0;

        // High strikeout opponent
        if (p.K_Percentage.HasValue)
        {
            if (p.K_Percentage >= 0.250m)
                matchupScore += 2.0m;
            else if (p.K_Percentage >= 0.230m)
                matchupScore += 1.0m;
        }

        // Weak offense
        if (p.RunsPerGame.HasValue)
        {
            if (p.RunsPerGame <= 4.0m)
                matchupScore += 2.0m;
            else if (p.RunsPerGame <= 4.5m)
                matchupScore += 1.0m;

            if (p.RunsPerGame >= 5.2m)
                matchupScore -= 2.0m;
        }

        p.MatchupScore = matchupScore;
    }
}