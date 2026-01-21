using FBMngt.Models;

namespace FBMngt.Services;

public static class ZScoreService
{
    public static void CalculateHitterZScores(
                            List<SteamerBatterProjection> hitters)
    {
        var mean = new
        {
            R = hitters.Average(h => h.R),
            HR = hitters.Average(h => h.HR),
            RBI = hitters.Average(h => h.RBI),
            SB = hitters.Average(h => h.SB),
            AVG = hitters.Average(h => h.AVG),
            PA = hitters.Average(h => h.PA)
        };

        var std = new
        {
            R = StdDev(hitters.Select(h => (double)h.R)),
            HR = StdDev(hitters.Select(h => (double)h.HR)),
            RBI = StdDev(hitters.Select(h => (double)h.RBI)),
            SB = StdDev(hitters.Select(h => (double)h.SB)),
            AVG = StdDev(hitters.Select(h => h.AVG))
        };

        foreach (var h in hitters)
        {
            h.Z_R = Z(h.R, mean.R, std.R);
            h.Z_HR = Z(h.HR, mean.HR, std.HR);
            h.Z_RBI = Z(h.RBI, mean.RBI, std.RBI);
            h.Z_SB = Z(h.SB, mean.SB, std.SB);
            h.Z_AVG = ((h.AVG - mean.AVG) * h.PA)
                        / (std.AVG * mean.PA);

            h.TotalZ =
                h.Z_R +
                h.Z_HR +
                h.Z_RBI +
                h.Z_SB +
                h.Z_AVG;
        }
    }
    public static void CalculatePitcherZScores(
    List<SteamerPitcherProjection> pitchers)
    {
        var mean = new
        {
            W = pitchers.Average(p => p.W),
            K = pitchers.Average(p => p.K),
            SV = pitchers.Average(p => p.SV),
            ERA = pitchers.Average(p => p.ERA),
            WHIP = pitchers.Average(p => p.WHIP),
            IP = pitchers.Average(p => p.IP)
        };

        var std = new
        {
            W = StdDev(pitchers.Select(p => (double)p.W)),
            K = StdDev(pitchers.Select(p => (double)p.K)),
            SV = StdDev(pitchers.Select(p => (double)p.SV)),
            ERA = StdDev(pitchers.Select(p => p.ERA)),
            WHIP = StdDev(pitchers.Select(p => p.WHIP))
        };

        foreach (var p in pitchers)
        {
            p.Z_W = Z(p.W, mean.W, std.W);
            p.Z_K = Z(p.K, mean.K, std.K);
            p.Z_SV = Z(p.SV, mean.SV, std.SV);

            // Lower is better
            //p.Z_ERA = Z(mean.ERA - p.ERA, 0, std.ERA);
            p.Z_ERA = ((mean.ERA - p.ERA) * p.IP)
                        / (std.ERA * mean.IP);
            //p.Z_WHIP = Z(mean.WHIP - p.WHIP, 0, std.WHIP);
            p.Z_WHIP = ((mean.WHIP - p.WHIP) * p.IP)
                        / (std.WHIP * mean.IP);

            p.TotalZ =
                p.Z_W +
                p.Z_K +
                p.Z_SV +
                p.Z_ERA +
                p.Z_WHIP;
        }
    }

    private static double Z(double v, double mean, double std)
        => std == 0 ? 0 : (v - mean) / std;

    private static double StdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        var avg = list.Average();
        var variance = list.Sum(v => Math.Pow(v - avg, 2)) / list.Count;
        return Math.Sqrt(variance);
    }
}
