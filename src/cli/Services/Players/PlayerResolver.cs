using FBMngt.Data;
using FBMngt.Models;

namespace FBMngt.Services.Players;

public class PlayerResolver
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerResolver(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task ResolvePlayerIDAsync(
                                List<IPlayer> inputPlayers)
    {
        if (inputPlayers.Count == 0)
            return;

        // 1️ Load ALL DB players once
        var dbPlayers = await _playerRepository.GetAllAsync();

        // 2️ Build lookup
        var lookup = new Dictionary<string, Player>(
                            StringComparer.OrdinalIgnoreCase);

        foreach (var p in dbPlayers)
        {
            AddLookup(lookup, p.PlayerName, p);
            AddLookup(lookup, p.Aka1, p);
            AddLookup(lookup, p.Aka2, p);
        }

        // 3️ Resolve PlayerID for each input player
        foreach (IPlayer player in inputPlayers)
        {
            var key = player.PlayerName!.Trim();

            if (lookup.TryGetValue(key, out var dbPlayer))
            {
                player.PlayerID = dbPlayer.PlayerID;
            }
            else
            {
                player.PlayerID = null; // explicit
                Console.WriteLine("PlayerResolver: "
                        +$"Player not found in db: {key}");
            }
        }
    }
    private static void AddLookup(
                            Dictionary<string, Player> lookup,
                            string? name,
                            Player player)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var key = name.Trim();

        // FIRST one wins
        if (lookup.ContainsKey(key))
        {
            // TEMP: debug only
            Console.WriteLine($"Duplicate name ignored: {key}");
            return;
        }
        else
        {
            lookup[key] = player;
        }
    }
}
