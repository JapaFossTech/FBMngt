using FBMngt.Services.Importing;

namespace FBMngt.Commands;

public class ImportCommand
{
    private readonly ImportService _importService;

    public ImportCommand(ImportService service)
    {
        _importService = service;
    }

    public async Task ExecuteAsync(string[] args)
    {
        string? matchColumn = null;
        bool showPlayer = false;
        string? fileType = null;
        int? rows = null;

        // Extract parameter values
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--match-column", AppConst.IGNORE_CASE)
                && i + 1 < args.Length)
            {
                matchColumn = args[i + 1];
            }

            if (args[i].Equals("--show-player", AppConst.IGNORE_CASE))
            {
                showPlayer = true;
            }

            if (args[i].Equals("--file-Type", AppConst.IGNORE_CASE)
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

        // Import path (authoritative)
        await _importService.ImportPlayersAsync(
            fileType,
            rows);

        //Call Check Matches
        // TODO: Move CheckMatchesAsync() to data-integrity command

        if (matchColumn.HasString())
        {
            await _importService.CheckMatchesAsync(matchColumn!,
                                            showPlayer,
                                            fileType,
                                            rows);
        }
    }
}
