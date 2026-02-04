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
    private FanProsCsvReader _fanProsCsvReader { get; init; }

    public FanProsCoreFieldsReport(
        IAppSettings appSettings,
        IPlayerRepository playerRepository)
        : base(new PlayerResolver(playerRepository))
    {
        _configSettings = new ConfigSettings(appSettings);
        _fanProsCsvReader = new FanProsCsvReader();
    }

    protected override Task<List<FanProsPlayer>> ReadAsync(int rows)
    {
        List<FanProsPlayer> items =
            _fanProsCsvReader.Read(
                _configSettings.FanPros_Rankings_InputCsv_Path,
                rows);

        return Task.FromResult(items);
    }

    // Convert rows → TSV lines
    protected override List<string> FormatReport(
        List<FanProsPlayer> rows)
    {
        List<string> lines = new();

        lines.Add("PlayerID\tPLAYER NAME\tTEAM\tPOS");

        foreach (FanProsPlayer p in rows.OrderBy(p => p.Rank))
        {
            lines.Add(
                $"{p.PlayerID}\t{p.PlayerName}\t{p.Team}\t{p.Position}");
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
