using FBMngt.Services;
using FBMngt.Services.Reporting;

namespace FBMngt.Commands;

public static class ReportCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        var service = new ReportService(
                            new AppContextReportPathProvider());

        // Z-Score report (existing functionality)
        if (args.Length > 0 &&
            args[0].Equals("--zscores", AppConst.IGNORE_CASE))
        {
            await service.GenerateZScoreReportsAsync();
            return;
        }

        // FanPros Core Fields report (new functionality)
        bool isFanProsCoreFields = args.Contains("--FanProsCoreFields");
        int rows = 250;

        if (args.Contains("--rows"))
        {
            int idx = Array.IndexOf(args, "--rows");
            if (idx + 1 < args.Length)
            {
                int.TryParse(args[idx + 1], out rows);
            }
        }

        if (isFanProsCoreFields)
        {
            
            await service.GenerateFanProsCoreFieldsReportAsync(rows);
            return;
        }

        // No recognized argument -> show help
        Console.WriteLine("Usage:");
        Console.WriteLine("  FBMngt report --zscores");
        Console.WriteLine("  FBMngt report --FanProsCoreFields [--rows 250]");
    }
}
