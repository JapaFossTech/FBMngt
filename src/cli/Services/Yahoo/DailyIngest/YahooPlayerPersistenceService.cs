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

    /// <summary>
    /// Persists players with optimized YahooPlayerID lookup.
    /// </summary>
    public async Task<PlayerPersistenceStats> PersistAsync(
        IEnumerable<Player> players)
    {
        var stats = new PlayerPersistenceStats();

        // --------------------------------------------------------
        // LOAD ALL YahooPlayerIDs ONCE
        // --------------------------------------------------------
        var yahooIds = await _playerRepository
            .GetAllYahooPlayerIdsAsync();

        foreach (var player in players)
        {
            try
            {
                await ProcessPlayerAsync(player, stats, yahooIds);
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
        HashSet<int> yahooIds)
    {
        if (player == null)
        {
            stats.Skipped++;
            return;
        }

        if (!player.PlayerID.HasValue)
        {
            await HandleInsertAsync(player, stats, yahooIds);
            return;
        }

        if (player.ExternalPlayerID.HasValue)
        {
            await HandleUpdateAsync(player, stats, yahooIds);
        }
        else
        {
            stats.Skipped++;
        }
    }

    private async Task HandleInsertAsync(
        Player player,
        PlayerPersistenceStats stats,
        HashSet<int> yahooIds)
    {
        if (player.ExternalPlayerID.HasValue &&
            yahooIds.Contains(player.ExternalPlayerID.Value))
        {
            stats.Conflicts++;

            stats.ConflictDetails.Add(
                $"INSERT CONFLICT: {player.PlayerName} " +
                $"YahooID={player.ExternalPlayerID}");

            return;
        }

        var newPlayerId = await _playerRepository
            .InsertAsync(player);

        player.PlayerID = newPlayerId;

        // --------------------------------------------------------
        // KEEP MEMORY IN SYNC
        // --------------------------------------------------------
        if (player.ExternalPlayerID.HasValue)
        {
            yahooIds.Add(player.ExternalPlayerID.Value);
        }

        stats.Inserted++;
    }

    private async Task HandleUpdateAsync(
        Player player,
        PlayerPersistenceStats stats,
        HashSet<int> yahooIds)
    {
        var yahooId = player.ExternalPlayerID!.Value;

        if (yahooIds.Contains(yahooId))
        {
            stats.Conflicts++;

            stats.ConflictDetails.Add(
                $"UPDATE CONFLICT: {player.PlayerName} " +
                $"YahooID={yahooId}");

            return;
        }

        var updated = await _playerRepository
            .UpdateYahooPlayerIdAsync(
                player.PlayerID!.Value,
                yahooId);

        if (updated)
        {
            yahooIds.Add(yahooId);

            stats.Updated++;
        }
        else
        {
            stats.Skipped++;
        }
    }
}