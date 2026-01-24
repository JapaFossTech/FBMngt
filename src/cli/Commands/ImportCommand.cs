using FBMngt.Services;
using Microsoft.Identity.Client;

namespace FBMngt.Commands;

public static class ImportCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        string? matchColumn = null;
        bool showPlayer = false;
        string? fileType = null;
        int? rows = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--match-column", 
                                AppConst.IGNORE_CASE)
                && i + 1 < args.Length)
            {
                matchColumn = args[i + 1];
            }

            if (args[i].Equals("--show-player",
                                AppConst.IGNORE_CASE))
            {
                showPlayer = true;
            }

            if (args[i].Equals("--file-Type",
                                AppConst.IGNORE_CASE)
                && i + 1 < args.Length)
            {
                fileType = args[i + 1];
            }

            if (args[i].Equals("--rows", AppConst.IGNORE_CASE)
                && i + 1 < args.Length
                && int.TryParse(args[i + 1], out var r))
            {
                rows = r;
            }
        }

        if (string.IsNullOrWhiteSpace(matchColumn))
        {
            Console.WriteLine("ERROR: Missing required option --match-column");
            return;
        }

        var service = new ImportService(new AppSettings());
        await service.CheckMatchesAsync(matchColumn,
                                        showPlayer,
                                        fileType,
                                        rows
                                        );
    }
}
