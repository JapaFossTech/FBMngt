using FBMngt.Services;

namespace FBMngt.Commands;

public static class ReportCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        if (args.Length > 0 &&
            args[0].Equals("--zscores", AppConst.IGNORE_CASE))
        {
            var service = new ReportService();
            await service.GenerateZScoreReportsAsync();
            return;
        }

    }
}
