namespace FBMngt.Services.Yahoo.DailyIngest;

using FBMngt.Models;
using FBMngt.Data;

public class YahooPlayerPersistenceService
{
    private readonly IPlayerRepository _playerRepository;

    public YahooPlayerPersistenceService(
        IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<PlayerPersistenceStats> PersistAsync(
        IEnumerable<Player> players,
        Dictionary<int, int> yahooMap)
    {
        var stats = new PlayerPersistenceStats();

        foreach (var player in players)
        {
            try
            {
                await ProcessPlayerAsync(player, stats, yahooMap);
            }
            catch (Exception ex)
            {
                stats.Errors++;

                stats.ErrorDetails.Add(
                    $"{player?.PlayerName} → {ex.Message}");
            }
        }

        return stats;
    }

    private async Task ProcessPlayerAsync(
        Player player,
        PlayerPersistenceStats stats,
        Dictionary<int, int> yahooMap)
    {
        if (player == null)
        {
            stats.Skipped++;
            return;
        }

        if (!player.PlayerID.HasValue)
        {
            await HandleInsertAsync(player, stats, yahooMap);
            return;
        }

        if (player.ExternalPlayerID.HasValue)
        {
            await HandleUpdateAsync(player, stats, yahooMap);
        }
        else
        {
            stats.Skipped++;
        }
    }

    private async Task HandleInsertAsync(
        Player player,
        PlayerPersistenceStats stats,
        Dictionary<int, int> yahooMap)
    {
        var yahooId = player.ExternalPlayerID;

        if (yahooId.HasValue &&
            yahooMap.ContainsKey(yahooId.Value))
        {
            stats.Conflicts++;
            return;
        }

        var newPlayerId = await _playerRepository
            .InsertAsync(player);

        player.PlayerID = newPlayerId;

        if (yahooId.HasValue)
        {
            yahooMap[yahooId.Value] = newPlayerId;
        }

        stats.Inserted++;
    }

    private async Task HandleUpdateAsync(
        Player player,
        PlayerPersistenceStats stats,
        Dictionary<int, int> yahooMap)
    {
        var yahooId = player.ExternalPlayerID!.Value;

        if (yahooMap.TryGetValue(yahooId, out var existingPlayerId))
        {
            if (existingPlayerId == player.PlayerID)
            {
                stats.Skipped++;


                return;
            }
            else
            {
                stats.Conflicts++;

                Console.WriteLine(
                    $"CONFLICT: {player.PlayerName} " +
                    $"YahooID={yahooId} belongs to " +
                    $"PlayerID={existingPlayerId}");

                return;
            }
        }

        var updated = await _playerRepository
            .UpdateYahooPlayerIdAsync(
                player.PlayerID!.Value,
                yahooId);

        if (updated)
        {
            yahooMap[yahooId] = player.PlayerID.Value;
            stats.Updated++;

            Console.WriteLine(
                $"UPDATED: {player.PlayerName}");
        }
        else
        {
            stats.Skipped++;
        }
    }
}