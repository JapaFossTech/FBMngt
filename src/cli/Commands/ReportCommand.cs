using FBMngt.Data;
using FBMngt.Services;
using FBMngt.Services.Reporting;
using FBMngt.Services.Yahoo;

namespace FBMngt.Commands;

public class ReportCommand
{
    private readonly ReportService _reportService;

    public ReportCommand(ReportService reportService)
    {
        _reportService = reportService;
    }
    public async Task ExecuteAsync(string[] args)
    {
        var appSettings = new AppSettings();
        var configSettings = new ConfigSettings(appSettings);

        // Combined report
        if (args.Length > 0 &&
            args[0].Equals("--combine", AppConst.IGNORE_CASE))
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  FBMngt report --combine FanProsCoreFields,zscores");
                return;
            }

            string combinedValue = args[1];

            IEnumerable<string> reportNames =
                combinedValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

            await _reportService.GenerateCombinedReportAsync(reportNames);
            return;
        }

        // Z-Score report
        if (args.Length > 0 &&
            args[0].Equals("--zscores", AppConst.IGNORE_CASE))
        {
            await _reportService.GenerateZScoreReportsAsync();
            return;
        }

        // FanPros Core Fields report
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
            await _reportService.GenerateFanProsCoreFieldsReportAsync(rows);
            return;
        }

        // No recognized argument -> show help
        Console.WriteLine("Usage:");
        Console.WriteLine("  FBMngt report --zscores");
        Console.WriteLine("  FBMngt report --FanProsCoreFields [--rows 250]");
        Console.WriteLine("  FBMngt report --combine FanProsCoreFields,zscores");
    }
}
