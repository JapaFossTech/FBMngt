using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.ZScore;

namespace FBMngt.Services.Reporting;

public interface IReportBuilder
{
    Task<ReportResult<object>> GenerateAsync();
}

public sealed class FanProsCoreFieldsReportBuilder
                                : IReportBuilder
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPreDraftAdjustRepository _preDraftAdjustRepo;
    private readonly FanProsCoreFieldsReport _fanProsCoreFieldsReport;

    // Ctor
    public FanProsCoreFieldsReportBuilder(
        ConfigSettings configSettings,
        IPlayerRepository playerRepository,
        IPreDraftAdjustRepository preDraftAdjustRepo,
        FanProsCoreFieldsReport fanProsCoreFieldsReport)
    {
        _configSettings = configSettings;
        _playerRepository = playerRepository;
        _preDraftAdjustRepo = preDraftAdjustRepo;
        _fanProsCoreFieldsReport = fanProsCoreFieldsReport;
    }

    public async Task<ReportResult<object>> GenerateAsync()
    {
        ReportResult<FanProsPlayer> result =
            await _fanProsCoreFieldsReport.GenerateAndWriteAsync();

        return new ReportResult<object>
        {
            ReportRows = new List<object>(),
            StringLines = result.StringLines
        };
    }
}

public sealed class ZscoresReportBuilder
                                : IReportBuilder
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPreDraftAdjustRepository _preDraftAdjustRepo;
    private readonly FanProsCoreFieldsReport _fanProsCoreFieldsReport;

    public ZscoresReportBuilder(
        ConfigSettings configSettings,
        IPlayerRepository playerRepository,
        IPreDraftAdjustRepository preDraftAdjustRepo,
        FanProsCoreFieldsReport fanProsCoreFieldsReport)
    {
        _configSettings = configSettings;
        _playerRepository = playerRepository;
        _preDraftAdjustRepo = preDraftAdjustRepo;
        _fanProsCoreFieldsReport = fanProsCoreFieldsReport;
    }

    public async Task<ReportResult<object>> GenerateAsync()
    {
        ReportResult<FanProsPlayer> fanProsResult =
            await _fanProsCoreFieldsReport.GenerateAndWriteAsync();

        List<FanProsPlayer> fanProsPlayers =
            fanProsResult.ReportRows;

        var hitterReport =
            new ZScoreBatterFileReport(
                _configSettings.AppSettings,
                _playerRepository,
                fanProsPlayers);

        ReportResult<SteamerBatterProjection> hitterResult =
            await hitterReport.GenerateAndWriteAsync();

        var pitcherReport =
            new ZScorePitcherFileReport(
                _configSettings.AppSettings,
                _playerRepository,
                fanProsPlayers);

        ReportResult<SteamerPitcherProjection> pitcherResult =
            await pitcherReport.GenerateAndWriteAsync();

        ZScoreCombinedReport combinedReport =
            new ZScoreCombinedReport(
                _configSettings.AppSettings);

        ReportResult<CombinedZScoreRow> combinedResult =
            await combinedReport.BuildAsync(
                fanProsPlayers,
                pitcherResult.ReportRows,
                hitterResult.ReportRows);

        return new ReportResult<object>
        {
            ReportRows = new List<object>(),
            StringLines = combinedResult.StringLines
        };
    }
}
