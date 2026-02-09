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
                _configSettings,
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
                _configSettings,
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
