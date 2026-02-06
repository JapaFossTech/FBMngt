using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.FanPros;
using FBMngt.Services.Reporting.ZScore;

namespace FBMngt.Services.Reporting.Display;

public interface IReportBuilder
{
    //bool IsSupported(string reportName);

    //Task<ReportResult<object>> GenerateAsync(
    //                    string reportName,
    //                    ConfigSettings configSettings,
    //                    IPlayerRepository playerRepository);
    Task<ReportResult<object>> GenerateAsync();
}

//public sealed class ReportRegistry : IReportBuilder
//{
//    public bool IsSupported(string reportName)
//    {
//        bool isFanProsCoreFields = reportName
//                .Equals("FanProsCoreFields", AppConst.IGNORE_CASE);
//        bool isZscores = reportName
//                .Equals("zscores", AppConst.IGNORE_CASE);

//        return isFanProsCoreFields || isZscores;
//    }

//    public async Task<ReportResult<object>> GenerateAsync(
//                        string reportName,
//                        ConfigSettings configSettings,
//                        IPlayerRepository playerRepository)
//    {
//        if (reportName.Equals("FanProsCoreFields", AppConst.IGNORE_CASE))
//        {
//            var report =
//                new FanProsCoreFieldsReport(
//                    configSettings.AppSettings,
//                    playerRepository);

//            ReportResult<FanProsPlayer> result =
//                await report.GenerateAndWriteAsync();

//            return new ReportResult<object>
//            {
//                ReportRows = new List<object>(),
//                StringLines = result.StringLines
//            };
//        }

//        if (reportName.Equals("zscores", AppConst.IGNORE_CASE))
//        {
//            var fanProsReport =
//                new FanProsCoreFieldsReport(
//                    configSettings.AppSettings,
//                    playerRepository);

//            ReportResult<FanProsPlayer> fanProsResult =
//                await fanProsReport.GenerateAndWriteAsync();

//            List<FanProsPlayer> fanProsPlayers =
//                fanProsResult.ReportRows;

//            var hitterReport =
//                new ZScoreBatterFileReport(
//                    configSettings.AppSettings,
//                    playerRepository,
//                    fanProsPlayers);

//            ReportResult<SteamerBatterProjection> hitterResult =
//                await hitterReport.GenerateAndWriteAsync();

//            var pitcherReport =
//                new ZScorePitcherFileReport(
//                    configSettings.AppSettings,
//                    playerRepository,
//                    fanProsPlayers);

//            ReportResult<SteamerPitcherProjection> pitcherResult =
//                await pitcherReport.GenerateAndWriteAsync();

//            ZScoreCombinedReport combinedReport =
//                new ZScoreCombinedReport(
//                    configSettings.AppSettings);

//            ReportResult<CombinedZScoreRow> combinedResult =
//                await combinedReport.BuildAsync(
//                    fanProsPlayers,
//                    pitcherResult.ReportRows,
//                    hitterResult.ReportRows);

//            return new ReportResult<object>
//            {
//                ReportRows = new List<object>(),
//                StringLines = combinedResult.StringLines
//            };
//        }

//        throw new InvalidOperationException(
//            $"Unknown report name: {reportName}");
//    }
//}

public sealed class FanProsCoreFieldsReportBuilder
                                : IReportBuilder
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;

    public FanProsCoreFieldsReportBuilder(
        ConfigSettings configSettings,
        IPlayerRepository playerRepository)
    {
        _configSettings = configSettings;
        _playerRepository = playerRepository;
    }

    public async Task<ReportResult<object>> GenerateAsync()
    {
        var report =
            new FanProsCoreFieldsReport(
                _configSettings.AppSettings,
                _playerRepository);

        ReportResult<FanProsPlayer> result =
            await report.GenerateAndWriteAsync();

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

    public ZscoresReportBuilder(
        ConfigSettings configSettings,
        IPlayerRepository playerRepository)
    {
        _configSettings = configSettings;
        _playerRepository = playerRepository;
    }

    public async Task<ReportResult<object>> GenerateAsync()
    {
        var fanProsReport =
            new FanProsCoreFieldsReport(
                _configSettings.AppSettings,
                _playerRepository);

        ReportResult<FanProsPlayer> fanProsResult =
            await fanProsReport.GenerateAndWriteAsync();

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
