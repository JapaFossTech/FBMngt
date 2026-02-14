using FBMngt.Services;
using FBMngt.Services.Players;

namespace FBMngt.Commands;

public class DataIntegrityCommand
{
    private readonly PlayerIntegrityService _playerIntegrityService;

    public DataIntegrityCommand(PlayerIntegrityService service)
    {
        _playerIntegrityService = service;
    }

    public async Task ExecuteAsync(string[] args)
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
                await _playerIntegrityService.RunAllChecksAsync(dryRun);
                break;

            default:
                Console.WriteLine($@"Unknown data-integrity 
                                    target: {target}");
                break;
        }
    }
}
