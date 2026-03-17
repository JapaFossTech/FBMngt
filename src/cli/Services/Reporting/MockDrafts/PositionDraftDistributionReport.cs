using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.FanPros;
using System.Text;

namespace FBMngt.Services.Reporting.MockDrafts;

public class PositionDraftDistributionReport
{
    private readonly ConfigSettings _configSettings;
    private readonly IMockDraftRepository _mockDraftRepository;
    private readonly PlayerResolver _playerResolver;
    private readonly FanProsCoreFieldsReport _fanProsReport;

    public sealed class PositionDraftDistributionRow
    {
        public required string Position { get; init; }

        public int PickCount { get; init; }

        public decimal PickAverage { get; init; }

        public double PickStDev { get; init; }

        public int PickMin { get; init; }

        public int PickMax { get; init; }

        public double PlayersPerDraft { get; init; }

        public double PlayersPerTeam { get; init; }
    }

    // Ctor
    public PositionDraftDistributionReport(
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

    public async Task<ReportResult<PositionDraftDistributionRow>>
        GenerateAsync(int days)
    {
        var result = new ReportResult<PositionDraftDistributionRow>()
        {
            ReportRows = [],
            StringLines = []
        };

        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-days);

        // 1️⃣ Get raw mock draft picks
        List<MockDraftPick> picks =
            await _mockDraftRepository
                .GetMockDraftPicksAsync(startDate, endDate);

        if (picks.Count == 0)
        {
            Console.WriteLine("No mock draft data found.");
            return result;
        }

        // 2️⃣ Resolve PlayerIDs
        await _playerResolver.ResolvePlayerIDAsync(
            picks.Cast<IPlayer>().ToList());

        // 3️⃣ Load FanPros positions
        List<FanProsPlayer> fanPros =
            await _fanProsReport.GenerateAsync();

        Dictionary<int, FanProsPlayer> fanProsLookup =
            fanPros
                .Where(p => p.PlayerID.HasValue)
                .DistinctBy(p => p.PlayerID)
                .ToDictionary(p => p.PlayerID!.Value);

        // 4️⃣ Enrich picks with positions
        foreach (var pick in picks)
        {
            if (pick.PlayerID.HasValue &&
                fanProsLookup.TryGetValue(pick.PlayerID.Value, out var fp))
            {
                pick.Position = fp.Position;
            }
        }

        // 5️⃣ Filter valid picks
        var validPicks =
            picks
                .Where(p => !string.IsNullOrWhiteSpace(p.Position))
                .ToList();

        if (validPicks.Count == 0)
        {
            Console.WriteLine("No picks with position data.");
            return result;
        }

        // 6️ Normalize positions
        foreach (var pick in validPicks)
        {
            string? pos = pick.GetPositionCode();

            if (string.IsNullOrWhiteSpace(pos))
                continue;

            if (pos == "CF" || pos == "RF" || pos == "LF")
                pos = "OF";

            if (pos == "SP,RP")
                pos = "SP";

            pick.Position = pos;
        }

        int totalValidPicks = validPicks.Count;

        int picksPerDraft = 23 * 12;

        double draftCount =
            (double)totalValidPicks / picksPerDraft;

        // 7️ Group by normalized position
        var grouped =
            validPicks
                .GroupBy(p => p.Position);

        foreach (var group in grouped)
        {
            var pickNumbers =
                group
                    .Select(p => p.PickNumber)
                    .ToList();

            double avg = pickNumbers.Average();

            double variance =
                pickNumbers
                    .Select(p => Math.Pow(p - avg, 2))
                    .Average();

            double stdev = Math.Sqrt(variance);

            int pickCount = pickNumbers.Count;

            var row = new PositionDraftDistributionRow
            {
                Position = group.Key ?? "UNK",
                PickCount = pickCount,
                PickAverage = (decimal)avg,
                PickStDev = stdev,
                PickMin = pickNumbers.Min(),
                PickMax = pickNumbers.Max(),
                PlayersPerDraft = pickCount / draftCount,
                PlayersPerTeam = pickCount / draftCount / 12.0
            };

            result.ReportRows.Add(row);
        }

        // 8️ Sort rows
        var sortedRows =
            result.ReportRows
                .OrderBy(r => r.PickAverage)
                .ToList();

        // 9️ Build TSV
        var sb = new StringBuilder();

        sb.AppendLine(
            "Position\tPick Count\tPick Average\tRound Average" +
            "\tPick StDev\tPlayers Per Draft\tPlayers Per Team" +
            "\tMIN\tMAX");

        foreach (PositionDraftDistributionRow row in sortedRows)
        {
            sb.AppendLine(
                $"{row.Position}\t" +
                $"{row.PickCount}\t" +
                $"{row.PickAverage:F0}\t" +
                $"{row.PickAverage / 12:F1}\t" +
                $"{row.PickStDev:F0}\t" +
                $"{row.PlayersPerDraft:F2}\t" +
                $"{row.PlayersPerTeam:F2}\t" +
                $"{row.PickMin}\t" +
                $"{row.PickMax}");
        }

        result.StringLines.AddRange(
            sb.ToString()
            .Split(Environment.NewLine,
                   StringSplitOptions.RemoveEmptyEntries)
            .ToList()
        );

        return result;
    }
}