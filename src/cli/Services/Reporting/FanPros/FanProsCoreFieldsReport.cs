using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;

namespace FBMngt.Services.Reporting.FanPros;

// FanPros

public class FanProsCoreFieldsReport
    : ReportBase<FanProsPlayer, FanProsPlayer>
{
    private readonly ConfigSettings _configSettings;
    private readonly FanProsCsvReader _fanProsCsvReader;
    private readonly IPreDraftAdjustRepository _preDraftAdjustRepo;

    // Ctor
    public FanProsCoreFieldsReport(
                    ConfigSettings configSettings,
                    PlayerResolver playerResolver,
                    FanProsCsvReader fanProsCsvReader,
                    IPreDraftAdjustRepository preDraftAdjustRepo)
                    : base(playerResolver)
    {
        _configSettings = configSettings;
        _fanProsCsvReader = fanProsCsvReader;
        _preDraftAdjustRepo = preDraftAdjustRepo;
    }


    protected override Task<List<FanProsPlayer>> ReadAsync(
                                                    int rows)
    {
        List<FanProsPlayer> items =
            _fanProsCsvReader.Read(
                _configSettings.FanPros_Rankings_InputCsv_Path,
                rows);

        return Task.FromResult(items);
    }

    protected override async Task<List<FanProsPlayer>> TransformAsync(List<FanProsPlayer> input)
    {
        var offsets = await _preDraftAdjustRepo.GetAllAsync();

        foreach (FanProsPlayer p in input)
        {
            if (p.PlayerID.HasValue &&
                offsets.TryGetValue(p.PlayerID.Value, out int off))
                p.Offset = off;
            else
                p.Offset = 0;
        }

        // 1️ Sort by Rank - Offset, tie-break by Rank
        List<FanProsPlayer> sorted = input
            .OrderBy(p => p.Rank - p.Offset)
            .ThenBy(p => p.Rank)
            .ToList();

        // 2️ Assign unique AdjustedRank sequentially
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].AdjustedRank = i + 1; // 1-based
        }

        return sorted;
    }

    // Convert rows → TSV lines
    protected override List<string> FormatReport(
        List<FanProsPlayer> rows)
    {
        List<string> lines = new();

        lines.Add(
            "PlayerID\tPLAYER NAME\tTEAM\tPOS\tRANK\tOFFSET\tADJUSTED");

        foreach (FanProsPlayer p in rows)
        {
            lines.Add(
                $"{p.PlayerID}\t{p.PlayerName}\t{p.Team}\t{p.Position}" +
                $"\t{p.Rank}\t{p.Offset}\t{p.AdjustedRank}");
        }

        return lines;
    }

    protected override Task WriteAsync(List<string> lines)
    {
        File.WriteAllLines(
            _configSettings.FanPros_OutReport_Path,
            lines);

        Console.WriteLine("FanPros Core Fields report generated:");
        Console.WriteLine(_configSettings.FanPros_OutReport_Path);

        return Task.CompletedTask;
    }
}
