using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.FanPros;
using System.Text;

namespace FBMngt.Services.Reporting.MockDrafts;

public class PositionRunDetectionReport
{
    private readonly ConfigSettings _configSettings;
    private readonly IMockDraftRepository _mockDraftRepository;
    private readonly PlayerResolver _playerResolver;
    private readonly FanProsCoreFieldsReport _fanProsReport;

    public sealed class PositionRunDetectionRow
    {
        public required string Position { get; init; }

        public int RunStartPick { get; init; }

        public int RunEndPick { get; init; }

        public int RunSize { get; init; }

        public int RunLength { get; init; }

        public decimal RunStartRound { get; init; }
    }

    public PositionRunDetectionReport(
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

    public async Task<ReportResult<PositionRunDetectionRow>>
    GenerateAsync(int days)
    {
        var result = new ReportResult<PositionRunDetectionRow>()
        {
            ReportRows = [],
            StringLines = []
        };

        // =========================================================
        // 1️⃣ LOAD DATA
        // =========================================================
        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-days);

        var picks = await _mockDraftRepository
            .GetMockDraftPicksAsync(startDate, endDate);

        if (picks.Count == 0)
            return result;

        await _playerResolver.ResolvePlayerIDAsync(
            picks.Cast<IPlayer>().ToList());

        var fanPros = await _fanProsReport.GenerateAsync();

        var fanProsLookup =
            fanPros
                .Where(p => p.PlayerID.HasValue)
                .DistinctBy(p => p.PlayerID)
                .ToDictionary(p => p.PlayerID!.Value);

        // =========================================================
        // 2️⃣ ENRICH POSITIONS
        // =========================================================
        foreach (var pick in picks)
        {
            if (pick.PlayerID.HasValue &&
                fanProsLookup.TryGetValue(pick.PlayerID.Value, out var fp))
            {
                pick.Position = fp.Position;
            }
        }

        var validPicks =
            picks
                .Where(p => !string.IsNullOrWhiteSpace(p.Position))
                .ToList();

        if (validPicks.Count == 0)
            return result;

        // =========================================================
        // 3️⃣ NORMALIZE POSITIONS
        // =========================================================
        foreach (var pick in validPicks)
        {
            var pos = pick.GetPositionCode();

            if (string.IsNullOrWhiteSpace(pos))
                continue;

            if (pos is "CF" or "RF" or "LF")
                pos = "OF";

            if (pos == "SP,RP")
                pos = "SP";

            pick.Position = pos;
        }

        // =========================================================
        // 4️⃣ RUN DETECTION (EVENT LEVEL)
        // =========================================================
        var drafts = validPicks.GroupBy(p => p.DraftID);

        const int runThreshold = 3;

        var detectedRuns = new List<PositionRunDetectionRow>();

        foreach (var draft in drafts)
        {
            var ordered =
                draft
                    .OrderBy(p => p.PickNumber)
                    .ToList();

            int runStartIndex = 0;

            for (int i = 1; i <= ordered.Count; i++)
            {
                bool isEnd =
                    i == ordered.Count ||
                    ordered[i].Position != ordered[runStartIndex].Position;

                if (!isEnd)
                    continue;

                int runLength = i - runStartIndex;

                if (runLength >= runThreshold)
                {
                    var startPick = ordered[runStartIndex];
                    var endPick = ordered[i - 1];

                    detectedRuns.Add(new PositionRunDetectionRow
                    {
                        Position = startPick.Position!,
                        RunStartPick = startPick.PickNumber,
                        RunEndPick = endPick.PickNumber,
                        RunSize = runLength,
                        RunLength = endPick.PickNumber - startPick.PickNumber,
                        RunStartRound = (decimal)startPick.PickNumber / 12
                    });
                }

                runStartIndex = i;
            }
        }

        // =========================================================
        // 5️⃣ SAVE RAW RUNS (OPTIONAL DEBUG OUTPUT)
        // =========================================================
        result.ReportRows.AddRange(detectedRuns);

        // =========================================================
        // 6️⃣ RUN AGGREGATION (🔥 THIS IS THE KEY PART)
        // =========================================================
        int bucketSize = 12; // 1 round

        var aggregated =
            detectedRuns
                .GroupBy(r => new
                {
                    r.Position,
                    r.RunSize,
                    Bucket = (r.RunStartPick / bucketSize) * bucketSize
                })
                .Select(g => new
                {
                    Position = g.Key.Position,
                    RunSize = g.Key.RunSize,
                    StartPickMin = g.Key.Bucket,
                    StartPickMax = g.Key.Bucket + bucketSize - 1,

                    StartRoundMin = (g.Key.Bucket / (decimal)bucketSize) + 1,
                    StartRoundMax = ((g.Key.Bucket + bucketSize - 1) / (decimal)bucketSize) + 1,

                    Count = g.Count(),
                    AvgStartPick = g.Average(x => x.RunStartPick),
                    AvgStartRound = g.Average(x => x.RunStartPick / 12m)
                })
                .OrderBy(x => x.Position)
                .ThenBy(x => x.StartPickMin)
                .ToList();

        // =========================================================
        // 7️⃣ BUILD FINAL OUTPUT (AGGREGATED VIEW)
        // =========================================================
        var sb = new StringBuilder();

        sb.AppendLine(
        "Position\tRun Size\tStart Pick Range\tStart Round Range\tCount\tAvg Pick\tAvg Round");

        foreach (var row in aggregated)
        {
            sb.AppendLine(
                $"{row.Position}\t" +
                $"{row.RunSize}\t" +
                $"{row.StartPickMin}-{row.StartPickMax}\t" +
                $"{row.StartRoundMin:F1}-{row.StartRoundMax:F1}\t" +
                $"{row.Count}\t" +
                $"{row.AvgStartPick:F1}\t" +
                $"{row.AvgStartRound:F1}");
        }

        result.StringLines.AddRange(
            sb.ToString()
            .Split(Environment.NewLine,
                   StringSplitOptions.RemoveEmptyEntries)
            .ToList());

        return result;
    }
}