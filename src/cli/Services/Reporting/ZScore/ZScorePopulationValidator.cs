//using FBMngt.Models;

//namespace FBMngt.Services.Reporting.ZScore;

//public static class ZScorePopulationValidator
//{
//    public static void ValidateHitters(
//        IEnumerable<SteamerBatterProjection> hitters)
//    {
//        var invalid = hitters
//            .Where(h =>
//                h.Z_R == 0 ||
//                h.Z_HR == 0 ||
//                h.Z_RBI == 0 ||
//                h.Z_SB == 0 ||
//                h.Z_AVG == 0 ||
//                h.TotalZ == 0)
//            .ToList();

//        if (invalid.Any())
//        {
//            throw new InvalidOperationException(
//                $"Z-score validation failed for {invalid.Count} hitters. " +
//                "FanPros population integrity violated.");
//        }
//    }

//    public static void ValidatePitchers(
//        IEnumerable<SteamerPitcherProjection> pitchers)
//    {
//        var invalid = pitchers
//            .Where(p =>
//                p.Z_W == 0 ||
//                p.Z_SV == 0 ||
//                p.Z_K == 0 ||
//                p.Z_ERA == 0 ||
//                p.Z_WHIP == 0 ||
//                p.TotalZ == 0)
//            .ToList();

//        if (invalid.Any())
//        {
//            throw new InvalidOperationException(
//                $"Z-score validation failed for {invalid.Count} pitchers. " +
//                "FanPros population integrity violated.");
//        }
//    }
//}
