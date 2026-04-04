using FBMngt.Data;
using FBMngt.Models;

namespace FBMngt.Services.Players;

/// <summary>
/// Resolves PlayerID using Name (and Akas).
///
/// ENHANCED:
/// - Detects NOT FOUND players
/// - Detects MULTIPLE MATCHES (data integrity issue)
/// - Logs results to CLI
/// </summary>
public class PlayerResolver
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerResolver(
        IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async virtual Task ResolvePlayerIDAsync(
        List<IPlayer> inputPlayers)
    {
        if (inputPlayers.Count == 0)
            return;

        // 1️ Load ALL DB players once
        List<Player> dbPlayers = await _playerRepository
                                    .GetAllAsync();

        int notFoundCount = 0;
        int multipleCount = 0;

        Console.WriteLine();
        Console.WriteLine("=== Player Resolver Summary ===");
        Console.WriteLine($"Total Input Players: {inputPlayers.Count}");

        foreach (IPlayer player in inputPlayers)
        {
            var name = player.PlayerName?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                player.PlayerID = null;
                continue;
            }

            // STEP 1: Match by name (PlayerName + Aka)
            var matches = dbPlayers
                .Where(p =>
                    string.Equals(p.PlayerName, name,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Aka1, name,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Aka2, name,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                notFoundCount++;

                Console.WriteLine(
                    $"PlayerResolver: NOT FOUND: {name}");

                player.PlayerID = null;
                continue;
            }

            if (matches.Count == 1)
            {
                player.PlayerID = matches[0].PlayerID;
                continue;
            }

            // ⚠️ MULTIPLE MATCHES → DISAMBIGUATE

            var filtered = matches;

            // STEP 2: Filter by Team (NOW using Team directly)
            if (!string.IsNullOrWhiteSpace(player.Team))
            {
                filtered = filtered
                    .Where(p =>
                        string.Equals(
                            p.Team,
                            player.Team,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // STEP 3: Filter by Position
            if (filtered.Count > 1 &&
                !string.IsNullOrWhiteSpace(player.Position))
            {
                filtered = filtered
                    .Where(p =>
                        string.Equals(
                            p.Position,
                            player.Position,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (filtered.Count == 1)
            {
                player.PlayerID = filtered[0].PlayerID;
                continue;
            }

            // STILL ambiguous
            multipleCount++;

            Console.WriteLine();
            Console.WriteLine(
                $"---- Players with MULTIPLE MATCHES ----");
            Console.WriteLine(
                $"Input: {player.PlayerName} | Team: {player.Team} | Pos: {player.Position}");

            foreach (var m in matches)
            {
                Console.WriteLine(
                    $"   -> DB PlayerID: {m.PlayerID} | Name: {m.PlayerName} | Org: {m.organization_id}");
            }

            player.PlayerID = null;
        }

        Console.WriteLine($"Not Found: {notFoundCount}");
        Console.WriteLine($"Multiple Matches: {multipleCount}");
    }
    /// <summary>
    /// Adds player to lookup (supports multiple matches).
    /// </summary>
    private static void AddLookup(
        Dictionary<string, List<Player>> lookup,
        string? name,
        Player player)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var key = name.Trim();

        if (!lookup.ContainsKey(key))
        {
            lookup[key] = new List<Player>();
        }

        lookup[key].Add(player);
    }
}