using FBMngt.Data;
using FBMngt.Services;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.PreDraftRanking;

namespace FBMngt.Commands;

public class PlayerOffsetCommand
{
    private readonly PlayerOffsetService _playerOffsetService;
    private readonly ReportService _reportService;

    // Ctor
    public PlayerOffsetCommand(PlayerOffsetService playerOffsetService,
                               ReportService reportService)
    {
        _playerOffsetService = playerOffsetService;
        _reportService = reportService;
    }
    public async Task ExecuteAsync(string[] args)
    {
        bool isServiceExecuted = false;
        bool doCreateReport = args.Contains("--doCreateReport");

        if (args.Contains("--initialConfiguration"))
        {
            await _playerOffsetService.InitialConfigurationAsync();
            isServiceExecuted = true;
        }

        if (args.Contains("--adjust"))
        {
            int idx = Array.IndexOf(args, "--adjust");

            if (idx + 1 >= args.Length)
            {
                Console.WriteLine("Missing batch value.");
                return;
            }

            string batch = args[idx + 1];

            await _playerOffsetService.AdjustAsync(batch);
            isServiceExecuted = true;
        }

        if (isServiceExecuted && doCreateReport)
        {
            await _reportService.GenerateCombinedReportAsync(
                new[] { "FanProsCoreFields", "zscores" });
        }

        if (!isServiceExecuted)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  FBMngt playerOffset --initialConfiguration [--doCreateReport]");
            Console.WriteLine("  FBMngt playerOffset --adjust playerId,10|playerId2,-5 [--doCreateReport]");
        }
    }
}
