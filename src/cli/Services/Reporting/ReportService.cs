using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.ZScore;

namespace FBMngt.Services.Reporting;
public class ReportService
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;

    public ReportService(IAppSettings appSettings,
                        IPlayerRepository playerRepository)
    {
        _configSettings = new ConfigSettings(appSettings);
        _playerRepository = playerRepository;
    }
    // ZScoreReports
    public async Task GenerateZScoreReportsAsync()
    {
        // Generate FanPros report FIRST (source of truth)
        FanProsCoreFieldsReport fanProsReport =
            new FanProsCoreFieldsReport(
                _configSettings.AppSettings,
                _playerRepository);

        ReportResult<FanProsPlayer> fanProsResult =
            await fanProsReport.GenerateAndWriteAsync();

        List<FanProsPlayer> fanProsPlayers =
            fanProsResult.ReportRows;

        // Generate hitter Z-scores (FanPros-driven)
        ZScoreBatterFileReport hitterReport =
            new ZScoreBatterFileReport(
                _configSettings.AppSettings,
                _playerRepository,
                fanProsPlayers);

        ReportResult<SteamerBatterProjection> hitterResult =
            await hitterReport.GenerateAndWriteAsync();

        // Generate pitcher Z-scores (FanPros-driven)
        ZScorePitcherFileReport pitcherReport =
            new ZScorePitcherFileReport(
                _configSettings.AppSettings,
                _playerRepository,
                fanProsPlayers);

        ReportResult<SteamerPitcherProjection> pitcherResult =
            await pitcherReport.GenerateAndWriteAsync();

        // Generate combined report
        ZScoreCombinedReport combinedReport =
            new ZScoreCombinedReport(
                _configSettings.AppSettings);

        await combinedReport.WriteAsync(
            fanProsPlayers,
            pitcherResult.ReportRows,
            hitterResult.ReportRows);
    }

    // FanProsCoreFields
    public async Task GenerateFanProsCoreFieldsReportAsync(int rows)
    {
        var report = new FanProsCoreFieldsReport(
                                                _configSettings.AppSettings,
                                                _playerRepository
                                                );
        await report.GenerateAndWriteAsync(rows);
    }
}
