using FBMngt.Models;

namespace FBMngt.Data;

public interface IPlayerRepository
{
    Task<List<Player>> GetAllAsync();
    Task<int> InsertAsync(Player player);
    Task<bool> ExistsByYahooPlayerIdAsync(int yahooPlayerId);
    Task<bool> UpdateYahooPlayerIdAsync(
                                int playerId, int yahooPlayerId);
    Task<HashSet<int>> GetAllYahooPlayerIdsAsync();
}
