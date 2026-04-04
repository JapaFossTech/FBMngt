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
    /// Persists a collection of players after resolution.
    /// </summary>
    public async Task PersistAsync(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            await ProcessPlayerAsync(player);
        }
    }

    /// <summary>
    /// Core persistence logic per player.
    /// </summary>
    private async Task ProcessPlayerAsync(Player player)
    {
        if (player == null)
            return;

        // --------------------------------------------------------
        // CASE 1: PLAYER NOT FOUND → INSERT
        // --------------------------------------------------------
        if (!player.PlayerID.HasValue)
        {
            await HandleInsertAsync(player);
            return;
        }

        // --------------------------------------------------------
        // CASE 2: PLAYER FOUND → UPDATE YahooPlayerID if needed
        // --------------------------------------------------------
        if (player.ExternalPlayerID.HasValue)
        {
            await HandleUpdateAsync(player);
        }
    }

    // ------------------------------------------------------------
    // INSERT FLOW
    // ------------------------------------------------------------
    private async Task HandleInsertAsync(Player player)
    {
        // --------------------------------------------------------
        // SAFETY: avoid duplicate YahooPlayerID
        // --------------------------------------------------------
        if (player.ExternalPlayerID.HasValue)
        {
            var exists = await _playerRepository
                .ExistsByYahooPlayerIdAsync(
                    player.ExternalPlayerID.Value);

            if (exists)
            {
                // TODO: log conflict
                return;
            }
        }

        var newPlayerId = await _playerRepository
            .InsertAsync(player);

        // --------------------------------------------------------
        // OPTIONAL: set ID back into object (useful later)
        // --------------------------------------------------------
        player.PlayerID = newPlayerId;
    }

    // ------------------------------------------------------------
    // UPDATE FLOW
    // ------------------------------------------------------------
    private async Task HandleUpdateAsync(Player player)
    {
        var yahooId = player.ExternalPlayerID!.Value;

        // --------------------------------------------------------
        // SAFETY: avoid duplicate YahooPlayerID
        // --------------------------------------------------------
        var exists = await _playerRepository
            .ExistsByYahooPlayerIdAsync(yahooId);

        if (exists)
        {
            // TODO: log conflict
            return;
        }

        var updated = await _playerRepository
            .UpdateYahooPlayerIdAsync(
                player.PlayerID!.Value,
                yahooId);

        if (!updated)
        {
            // TODO: log skipped (already set)
        }
    }
}