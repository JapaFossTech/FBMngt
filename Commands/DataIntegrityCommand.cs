using FBMngt.Services;

namespace FBMngt.Commands;

public static class DataIntegrityCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(@"Usage: FBMngt data-integrity 
                                players [--dry-run]");
            return;
        }

        string target = args[0].ToLowerInvariant();
        bool dryRun = args.Any(a => 
                                a.Equals("--dry-run", 
                                         AppConst.IGNORE_CASE));

        switch (target)
        {
            case "players":
                var service = new PlayerIntegrityService();
                await service.RunAllChecksAsync(dryRun);
                break;

            default:
                Console.WriteLine($@"Unknown data-integrity 
                                    target: {target}");
                break;
        }
    }
}
