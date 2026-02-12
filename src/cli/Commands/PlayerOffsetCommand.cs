using FBMngt.Data;
using FBMngt.Services;
using FBMngt.Services.Reporting;
using FBMngt.Services.Reporting.PreDraftRanking;

namespace FBMngt.Commands;

public static class PlayerOffsetCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        var configSettings = new ConfigSettings(new AppSettings());
        var service = new PlayerOffsetService(
            configSettings,
            new PlayerRepository(configSettings.AppSettings),
            new PreDraftAdjustRepository(configSettings.AppSettings));
        var appSettings = configSettings.AppSettings;

        bool isServiceExecuted = false;
        bool doCreateReport = args.Contains("--doCreateReport");

        if (args.Contains("--initialConfiguration"))
        {
            await service.InitialConfigurationAsync();
            isServiceExecuted = true;
            //return;
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

            await service.AdjustAsync(batch);
            isServiceExecuted = true;
            //return;
        }

        if (isServiceExecuted && doCreateReport)
        {
            var reportService =
                new ReportService(
                        appSettings,
                        new PlayerRepository(appSettings),
                        new PreDraftAdjustRepository(appSettings));

            await reportService.GenerateCombinedReportAsync(
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
