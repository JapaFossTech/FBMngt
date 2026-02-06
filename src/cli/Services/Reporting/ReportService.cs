using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.Display;
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

    //public async Task GenerateCombinedReportAsync_del(
    //                                IEnumerable<string> reportNames)
    //{
    //    // Registry (explicit for now; DI can come later)
    //    IReportBuilder reportRegistry = new ReportRegistry();

    //    // Collect individual report outputs
    //    var reportResults = new List<ReportResult<object>>();

    //    foreach (string reportName in reportNames)
    //    {
    //        if (!reportRegistry.IsSupported(reportName))
    //        {
    //            throw new InvalidOperationException(
    //                $"Unknown report name: {reportName}");
    //        }

    //        ReportResult<object> result =
    //            await reportRegistry.GenerateAsync(
    //                    reportName,
    //                    _configSettings,
    //                    _playerRepository);

    //        reportResults.Add(result);
    //    }

    //    // Horizontally append reports
    //    IHorizontalReportAppender horizontalAppender =
    //        new HorizontalReportAppender();

    //    List<string> combinedLines =
    //        horizontalAppender.Append(reportResults);

    //    // Write final combined output
    //    string path = Path.Combine(
    //        _configSettings.AppSettings.ReportPath,
    //        $"{AppConst.APP_NAME}_Combined_Report_" +
    //        $"{_configSettings.AppSettings.SeasonYear}.tsv");

    //    await File.WriteAllLinesAsync(
    //        path,
    //        combinedLines);
    //}
    public async Task GenerateCombinedReportAsync(
                                    IEnumerable<string> reportNames)
    {
        List<IReportBuilder> reportBuilders = 
                            GetReportBuilders(reportNames);

        // Collect individual report outputs
        var reportResults = new List<ReportResult<object>>();

        foreach (IReportBuilder reportBuilder in reportBuilders)
        {
            ReportResult<object> result = await reportBuilder
                .GenerateAsync();

            reportResults.Add(result);
        }

        // Horizontally append reports
        IHorizontalReportAppender horizontalAppender =
            new HorizontalReportAppender();

        List<string> combinedLines =
            horizontalAppender.Append(reportResults);

        // Write final combined output
        string path = Path.Combine(
            _configSettings.AppSettings.ReportPath,
            $"{AppConst.APP_NAME}_Combined_Report_" +
            $"{_configSettings.AppSettings.SeasonYear}.tsv");

        await File.WriteAllLinesAsync(
            path,
            combinedLines);
    }

    private List<IReportBuilder> GetReportBuilders(
                            IEnumerable<string> reportNames)
    {
        var builders = new List<IReportBuilder>();

        foreach (string reportName in reportNames)
        {
            switch (reportName.ToLowerInvariant())
            {
                case "fanproscorefields":
                    builders.Add(
                        new FanProsCoreFieldsReportBuilder(
                            _configSettings,
                            _playerRepository));
                    break;

                case "zscores":
                    builders.Add(
                        new ZscoresReportBuilder(
                            _configSettings,
                            _playerRepository));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown report name: {reportName}");
            }
        }

        return builders;
    }

}
