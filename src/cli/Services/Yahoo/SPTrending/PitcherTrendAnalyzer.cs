using FBMngt.Models.FB;
using FBMngt.Models.SPTrending;

namespace FBMngt.Services.Yahoo.SPTrending;

public class PitcherTrendAnalyzer
{
    public List<WaiverPitcher> Analyze(List<WaiverPitcher> pitchers)
    {
        foreach (var p in pitchers)
        {
            ApplyTrend(p);
            ApplyScore(p);
            ApplyRecommendation(p);
        }

        return pitchers
            .OrderByDescending(p => p.Score)
            .ToList();
    }

    private void ApplyTrend(WaiverPitcher p)
    {
        var eraDiff = p.ERA_Last3 - p.ERA;
        var whipDiff = p.WHIP_Last3 - p.WHIP;
        var k9Diff = p.K9_Last3 - p.K9;

        // Weighted trend signal
        var trendScore = (-eraDiff * 2m) + (-whipDiff * 3m) + (k9Diff * 1.5m);

        if (trendScore >= 1.5m)
            p.Trend = "🔥 Breakout";
        else if (trendScore <= -1.5m)
            p.Trend = "⚠️ Regression";
        else
            p.Trend = "Stable";
    }

    private void ApplyScore(WaiverPitcher p)
    {
        decimal score = 0;

        // --- BASELINE PERFORMANCE (CORE) ---
        score += NormalizeInverse(p.ERA, 2.00m, 5.00m) * 30;   // lower ERA better
        score += NormalizeInverse(p.WHIP, 0.90m, 1.50m) * 30; // lower WHIP better
        score += Normalize(p.K9, 6.0m, 11.0m) * 20;           // higher K9 better

        // --- TREND BONUS ---
        var eraTrend = p.ERA - p.ERA_Last3;
        var whipTrend = p.WHIP - p.WHIP_Last3;
        var k9Trend = p.K9_Last3 - p.K9;

        score += eraTrend * 5;
        score += whipTrend * 8;
        score += k9Trend * 3;

        // --- VOLUME / TRUST ---
        score += Math.Min(p.Starts_Last30, 6) * 2;  // stability bonus

        // --- AVAILABILITY BONUS ---
        score += p.AvailableCount * 2;

        p.Score = Math.Round(score, 2);
    }

    private void ApplyRecommendation(WaiverPitcher p)
    {
        if (p.Score >= 75)
            p.Recommendation = "🔥 MUST ADD";
        else if (p.Score >= 60)
            p.Recommendation = "👍 STRONG ADD";
        else if (p.Score >= 50)
            p.Recommendation = "🎯 STREAM";
        else
            p.Recommendation = "❌ AVOID";
    }

    // ---------------------------
    // Helpers
    // ---------------------------

    private decimal Normalize(decimal value, decimal min, decimal max)
    {
        if (value <= min) return 0;
        if (value >= max) return 1;
        return (value - min) / (max - min);
    }

    private decimal NormalizeInverse(decimal value, decimal min, decimal max)
    {
        if (value <= min) return 1;
        if (value >= max) return 0;
        return 1 - ((value - min) / (max - min));
    }
}
