using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;

namespace FBMngt.Services.Reporting.FanPros;

public sealed class FanProsDeltaReport
{
    private readonly ConfigSettings _configSettings;
    private readonly PlayerResolver _playerResolver;
    private readonly FanProsCsvReader _fanProsCsvReader;
    private readonly IPreDraftAdjustRepository _preDraftAdjustRepo;
    private readonly IFileSelectorFactory _fileSelectorFactory;

    public FanProsDeltaReport(
        ConfigSettings configSettings,
        PlayerResolver playerResolver,
        FanProsCsvReader fanProsCsvReader,
        IPreDraftAdjustRepository preDraftAdjustRepo,
        IFileSelectorFactory selectorFactory)
    {
        _configSettings = configSettings;
        _playerResolver = playerResolver;
        _fanProsCsvReader = fanProsCsvReader;
        _preDraftAdjustRepo = preDraftAdjustRepo;
        _fileSelectorFactory = selectorFactory;
    }

    public async Task<ReportResult<FanProsDeltaRow>> 
        GenerateAndWriteAsync(int rows)
    {
        Console.WriteLine("Generating FanPros Delta report...");

        // 1️ Load LATEST

        var latestReport = new FanProsCoreFieldsReport(
            _configSettings,
            _playerResolver,
            _fanProsCsvReader,
            _preDraftAdjustRepo,
            _fileSelectorFactory.CreateLatest());

        List<FanProsPlayer> latest =
            await latestReport.GenerateAsync();//rows

        // 2️ Load PREVIOUS

        var previousReport = new FanProsCoreFieldsReport(
            _configSettings,
            _playerResolver,
            _fanProsCsvReader,
            _preDraftAdjustRepo,
            _fileSelectorFactory.CreatePrevious());

        List<FanProsPlayer> previous =
            await previousReport.GenerateAsync();//rows

        // 3️ Create lookup by PlayerID

        Dictionary<int, FanProsPlayer> previousById = previous
        .Where(p => p.PlayerID.HasValue)
        .DistinctBy(p => p.PlayerID!.Value)
        .ToDictionary(p => p.PlayerID!.Value);

        // 4️ Compute movement

        List<FanProsDeltaRow> deltaRows = [];

        foreach (FanProsPlayer current in latest)
        {
            int previousRank = 0;
            int movement = 0;

            if (current.PlayerID.HasValue &&
                previousById.TryGetValue(
                    current.PlayerID.Value,
                    out var old))
            {
                previousRank = old.AdjustedRank;
                movement = previousRank - current.AdjustedRank;
            }

            deltaRows.Add(new FanProsDeltaRow
            {
                PlayerID = current.PlayerID,
                PlayerName = current.PlayerName,
                Team = current.Team,
                Position = current.Position,
                PreviousRank = previousRank,
                CurrentRank = current.AdjustedRank
            });
        }

        deltaRows = deltaRows
            .Where(r => r.PreviousRank > 0 &&
                (r.PreviousRank <=250 || r.CurrentRank <= 250))
            .OrderByDescending(r => Math.Abs(r.Movement))
            .ThenBy(r => r.CurrentRank)
            .Take(50)
            .ToList();

        // 5️ Format

        List<string> lines = new();

        lines.Add(
            "PlayerID\tPLAYER NAME\tTEAM\tPOS\tPREVIOUS" +
            "\tCURRENT\tMOVE");

        foreach (var r in deltaRows)
        {
            lines.Add(
                $"{r.PlayerID}\t{r.PlayerName}\t{r.Team}\t{r.Position}" +
                $"\t{r.PreviousRank}\t{r.CurrentRank}\t{r.Movement}");
        }

        // 6️ Write

        string path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_FanPros_Delta_" +
            $"{_configSettings.AppSettings.SeasonYear}.tsv");

        await File.WriteAllLinesAsync(path, lines);

        Console.WriteLine("FanPros Delta report generated:");
        Console.WriteLine(path);

        // Respond
        return new ReportResult<FanProsDeltaRow>() { 
            ReportRows = deltaRows,
            StringLines = lines
        };
    }
}
