using System.Text;
using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting.FanPros;

namespace FBMngt.Services.Reporting.MockDrafts;

public class MockMarketDeltaReport
{
    private sealed class MarketIntelligenceRow
    {
        public int PlayerID { get; init; }
        public string PlayerName { get; init; }
                                        = string.Empty;

        public int MyRank { get; init; }
        public decimal MarketADP { get; init; }

        public decimal MissDelta { get; init; }

        // absolute difference used for threshold comparison
        public decimal AbsMissDelta { get; init; }

        // statistical strength of the signal
        public decimal MarketDisagreement { get; init; }

        // MarketDisagreement classification for Excel coloring
        public string DisagreementTier { get; init; } = string.Empty;

        // threshold tier used based on MyRank
        public int ThresholdUsed { get; init; }

        // classification column
        public string SignalType { get; init; } = string.Empty;

        public double AdoptionRate { get; init; }

        // liquidity pressure metric
        public decimal DraftPressureScore { get; init; }

        // pressure classification
        public string PressureTier { get; init; } = string.Empty;

        public MockDraftMarketStat Source { get; init; }
                                            = default!;
    }

    private readonly ConfigSettings _configSettings;
    private readonly IMockDraftRepository _mockDraftRepository;
    private readonly PlayerResolver _playerResolver;
    private readonly FanProsCoreFieldsReport _fanProsReport;

    private const decimal SyntheticRank = 400m;
    private const double AdoptionThreshold = 0.33;

    // Ctor
    public MockMarketDeltaReport(
        ConfigSettings configSettings,
        IMockDraftRepository mockDraftRepository,
        PlayerResolver playerResolver,
        FanProsCoreFieldsReport fanProsReport)
    {
        _configSettings = configSettings;
        _mockDraftRepository = mockDraftRepository;
        _playerResolver = playerResolver;
        _fanProsReport = fanProsReport;
    }

    public async Task GenerateAsync(int days)
    {
        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-days);

        // 1️ Get market stats
        List<MockDraftMarketStat> marketStats =
            await _mockDraftRepository
                .GetMockMarketStatsAsync(startDate, endDate);

        if (marketStats.Count == 0)
        {
            Console.WriteLine("No mock draft data found.");
            return;
        }

        // 2️ Resolve PlayerIDs
        await _playerResolver.ResolvePlayerIDAsync(
            marketStats.Cast<IPlayer>().ToList());

        // 3️ Determine total drafts
        // First player always has 100% pick rate.
        int totalDrafts = marketStats.Max(x => x.PickCount);

        if (totalDrafts == 0)
        {
            Console.WriteLine("No drafts detected.");
            return;
        }

        Console.WriteLine($"Detected {totalDrafts} mock drafts.");

        // 4️ Load FanPros ranks
        List<FanProsPlayer> fanPros =
            await _fanProsReport.GenerateAsync();

        Dictionary<int, FanProsPlayer> fanProsLookup =
            fanPros
                .OrderBy(p => p.AdjustedRank)
                .Where(p => p.PlayerID.HasValue)
                .DistinctBy(p => p.PlayerID)
                .ToDictionary(p => p.PlayerID!.Value);

        // 5️ Build Intelligence Rows + Build TSV
        var sb = new StringBuilder();

        sb.AppendLine(
            "PlayerID\tPlayerName\tPick_StDev\tPick_Count" +
            "\tPick_Average\tPick_Rnd\tMIN\tMAX\tDiff" +
            "\tReach Index\tFall Index" +
            "\tMiss Delta\tAbs Miss Delta\tMarket Disagreement" +
            "\tDisagreement Tier" +
            "\tThreshold Used" +
            "\tAdoption Rate\tSignal Type" +
            "\tDraft Pressure Score\tPressure Tier");

        var intelligenceRows = new List<MarketIntelligenceRow>();

        foreach (MockDraftMarketStat stat in marketStats)
        {
            double adoptionRate =
                (double)stat.PickCount / totalDrafts;

            bool doConsider =
                adoptionRate >= AdoptionThreshold;

            int myRank;

            // -----------------------------------
            // Not resolved to DB
            // -----------------------------------
            if (!stat.PlayerID.HasValue)
            {
                Console.ForegroundColor = doConsider ?
                        ConsoleColor.Red :
                        ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[WARN] '{stat.PlayerName}' not found " +
                    $"in DB. Adoption: {adoptionRate:P0}");
                Console.ResetColor();

                if (!doConsider)
                    continue;

                // synthetic negative ID
                stat.PlayerID =
                    -Math.Abs(
                        stat.PlayerName!.GetHashCode());

                myRank = (int)SyntheticRank;
            }
            else
            {
                // -----------------------------------
                // Not found in FanPros
                // -----------------------------------
                if (!fanProsLookup.TryGetValue(
                        stat.PlayerID.Value,
                        out FanProsPlayer? fp))
                {
                    Console.ForegroundColor = doConsider ?
                        ConsoleColor.Red :
                        ConsoleColor.Yellow;
                    Console.WriteLine(
                        $"[WARN] '{stat.PlayerName}' " +
                        $"(ID {stat.PlayerID}) not in " +
                        $"FanPros. Adoption: " +
                        $"{adoptionRate:P0}");
                    Console.ResetColor();

                    if (!doConsider)
                        continue;

                    myRank = (int)SyntheticRank;
                }
                else
                {
                    myRank = fp.AdjustedRank;
                }
            }

            decimal marketAdp = stat.PickAverage;
            decimal missDelta = myRank - marketAdp;

            // structural signal detection
            decimal absMissDelta = Math.Abs(missDelta);
            int threshold = GetThresholdForRank(myRank);

            // Calculate MarketDisagreement
            decimal marketDisagreement = 0m;

            if (stat.PickStDev > 0)
            {
                marketDisagreement =
                    missDelta / (decimal)stat.PickStDev;
            }
            string disagreementTier = GetDisagreementTier(marketDisagreement);

            // Determine Signal Type
            string signalType;

            if (myRank > 276)
            {
                signalType = "Ignored";
            }
            else if (absMissDelta >= threshold)
            {
                signalType = missDelta > 0
                    ? "MarketPush"
                    : "MarketFade";
            }
            else
            {
                signalType = "Aligned";
            }

            // Apply liquidity pressure ONLY to structural signals
            decimal draftPressureScore = 0m;
            string pressureTier = "None";

            if (signalType == "MarketPush" ||
                signalType == "MarketFade")
            {
                draftPressureScore =
                    absMissDelta * (decimal)adoptionRate;

                if (draftPressureScore >= 17m)
                    pressureTier = "High";
                else if (draftPressureScore >= 12m)
                    pressureTier = "Medium";
                else if (draftPressureScore >= 7m)
                    pressureTier = "Low";
                else
                    pressureTier = "Minimal";
            }

            if (doConsider)
            {
                intelligenceRows.Add(
                    new MarketIntelligenceRow
                    {
                        PlayerID = stat.PlayerID!.Value,
                        PlayerName = stat.PlayerName ?? string.Empty,
                        MyRank = myRank,
                        MarketADP = marketAdp,
                        MissDelta = missDelta,
                        AbsMissDelta = absMissDelta,
                        MarketDisagreement = marketDisagreement,
                        DisagreementTier = disagreementTier,
                        ThresholdUsed = threshold,
                        AdoptionRate = adoptionRate,
                        SignalType = signalType,
                        DraftPressureScore = draftPressureScore,
                        PressureTier = pressureTier,
                        Source = stat
                    });
            }
        }

        // 6️ Sort by MyRank
        var sortedRows =
            intelligenceRows
                .OrderBy(x => x.MyRank)
                .ToList();

        foreach (var row in sortedRows)
        {
            var r = row.Source;

            sb.AppendLine(
                $"{row.PlayerID}\t" +
                $"{row.PlayerName}\t" +
                $"{r.PickStDev:F2}\t" +
                $"{r.PickCount}\t" +
                $"{r.PickAverage:F2}\t" +
                $"{r.PickRoundAverage:F2}\t" +
                $"{r.PickMin}\t" +
                $"{r.PickMax}\t" +
                $"{r.PickDiff}\t" +
                $"{r.ReachIndex:F2}\t" +
                $"{r.FallIndex:F2}\t" +
                $"{row.MissDelta:F2}\t" +
                $"{row.AbsMissDelta:F2}\t" +
                $"{row.MarketDisagreement:F2}\t" +
                $"{row.DisagreementTier:F2}\t" +
                $"{row.ThresholdUsed}\t" +
                $"{row.AdoptionRate:P2}\t" +
                $"{row.SignalType}\t" +
                $"{row.DraftPressureScore:F2}\t" +
                $"{row.PressureTier}");
        }

        string fileName = _configSettings
                          .Mock_Market_Delta_Path;

        File.WriteAllText(fileName, sb.ToString());

        Console.WriteLine($"Report written: {fileName}");
    }

    private string GetDisagreementTier(decimal marketDisagreement)
    {
        string disagreementTier;

        decimal absMarketDisagreement = Math.Abs(marketDisagreement);

        if (absMarketDisagreement < 0.5m)
            disagreementTier = "Noise";
        else if (absMarketDisagreement < 1.0m)
            disagreementTier = "Mild";
        else if (absMarketDisagreement < 1.5m)
            disagreementTier = "Meaningful";
        else if (absMarketDisagreement < 2.0m)
            disagreementTier = "Strong";
        else
            disagreementTier = "Extreme";

        return disagreementTier;
    }

    // Threshold tier logic based on MyRank
    private static int GetThresholdForRank(int myRank)
    {
        if (myRank <= 120) return 12;
        if (myRank <= 180) return 18;
        if (myRank <= 240) return 24;
        if (myRank <= 276) return 36;

        return int.MaxValue;
    }
}