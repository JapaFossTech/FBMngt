using FBMngt.Models;

namespace FBMngt.Services;

public static class ProjectionPoolService
{
    public static List<SteamerBatterProjection> GetDraftableHitters(
            List<SteamerBatterProjection> hitters,
            int poolSize = 150)
    {
        return hitters
            .OrderByDescending(h => h.HR + h.RBI)
            .Take(poolSize)
            .ToList();
    }
    public static List<SteamerPitcherProjection> GetDraftablePitchers(
            List<SteamerPitcherProjection> pitchers,
            int poolSize = 120)
    {
        return pitchers
            .OrderByDescending(p => p.K + p.SV)
            .Take(poolSize)
            .ToList();
    }

}
