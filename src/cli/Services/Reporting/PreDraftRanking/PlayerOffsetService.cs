using FBMngt.Data;
using FBMngt.Models;
using FBMngt.Services.Reporting.FanPros;

namespace FBMngt.Services.Reporting.PreDraftRanking;

public class PlayerOffsetService : IPlayerOffsetService
{
    private readonly ConfigSettings _configSettings;
    private readonly IPlayerRepository _playerRepository;
    private readonly IPreDraftAdjustRepository _preDraftAdjustRepo;
    private readonly FanProsCoreFieldsReport _fanProsCoreFieldsReport;

    // Ctor
    public PlayerOffsetService(
                ConfigSettings configSettings,
                IPlayerRepository playerRepository,
                IPreDraftAdjustRepository preDraftAdjustRepository,
                FanProsCoreFieldsReport fanProsCoreFieldsReport)
    {
        _configSettings = configSettings;
        _playerRepository = playerRepository;
        _preDraftAdjustRepo = preDraftAdjustRepository;
        _fanProsCoreFieldsReport = fanProsCoreFieldsReport;
    }
    public async Task InitialConfigurationAsync()
    {
        Console.WriteLine("Loading FanPros players...");

        var players = await _fanProsCoreFieldsReport.GenerateAsync();

        Console.WriteLine($"Players loaded: {players.Count}");

        Console.WriteLine("Resetting offsets...");
        await _preDraftAdjustRepo.DeleteAllAsync();

        int inserted = 0;

        foreach (FanProsPlayer player in players)
        {
            if (player.PlayerID <= 0)
                continue;

            if (player.IsCatcher())
            {
                await _preDraftAdjustRepo.UpsertAsync(
                                    player.PlayerID!.Value, 12);
                inserted++;
                continue;
            }

            if (player.IsCloser())
            {
                await _preDraftAdjustRepo.UpsertAsync(
                                    player.PlayerID!.Value, 24);
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

            await _preDraftAdjustRepo.UpsertAsync(playerId, offset);

            Console.WriteLine($"Player {playerId} → {offset}");
        }

        Console.WriteLine("Done.");
    }

}
