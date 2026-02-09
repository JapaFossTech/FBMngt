using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.FanPros;

namespace FBMngt.Services.Reporting.PreDraftRanking;

public class PlayerOffsetService : IPlayerOffsetService
{
    private readonly ConfigSettings _configSettings;
    private IAppSettings AppSettings 
                                => _configSettings.AppSettings;
    private readonly PreDraftAdjustRepository _repo;


    // Ctor
    public PlayerOffsetService(ConfigSettings configSettings)
    {
        _configSettings = configSettings;
        _repo = new PreDraftAdjustRepository(AppSettings);
    }
    public async Task InitialConfigurationAsync()
    {
        Console.WriteLine("Loading FanPros players...");

        var report = new FanProsCoreFieldsReport(
                                    _configSettings,
                                    new PlayerRepository(_configSettings.AppSettings));

        var players = await report.GenerateAsync();

        Console.WriteLine($"Players loaded: {players.Count}");

        Console.WriteLine("Resetting offsets...");
        await _repo.DeleteAllAsync();

        int inserted = 0;

        foreach (FanProsPlayer player in players)
        {
            if (player.PlayerID <= 0)
                continue;

            if (player.IsCatcher())
            {
                await _repo.UpsertAsync(player.PlayerID!.Value, 12);
                inserted++;
                continue;
            }

            if (player.IsCloser())
            {
                await _repo.UpsertAsync(player.PlayerID!.Value, 24);
                inserted++;
                continue;
            }
        }

        Console.WriteLine($"Baseline offsets inserted: {inserted}");
    }
    public async Task AdjustAsync(string batch)
    {
        Console.WriteLine("Applying manual offsets...");

        string[] pairs = batch.Split('|', 
                                StringSplitOptions.RemoveEmptyEntries);

        foreach (string p in pairs)
        {
            string[] parts = p.Split(',', 
                                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                continue;

            if (!int.TryParse(parts[0], out int playerId))
                continue;

            if (!int.TryParse(parts[1], out int offset))
                continue;

            await _repo.UpsertAsync(playerId, offset);

            Console.WriteLine($"Player {playerId} → {offset}");
        }

        Console.WriteLine("Done.");
    }

}
