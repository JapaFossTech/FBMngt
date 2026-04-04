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
        List<Player> dbPlayers =
            await _playerRepository.GetAllAsync();

        // 2️ Build lookup: Name -> List<Player>
        var lookup = new Dictionary<string, List<Player>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var p in dbPlayers)
        {
            AddLookup(lookup, p.PlayerName, p);
            AddLookup(lookup, p.Aka1, p);
            AddLookup(lookup, p.Aka2, p);
        }

        var notFound = new List<IPlayer>();
        var multipleMatches = new List<(IPlayer, List<Player>)>();

        //int lineCount = 1;

        // 3️ Resolve PlayerID
        foreach (IPlayer player in inputPlayers)
        {
            var key = player.PlayerName?.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                notFound.Add(player);
                continue;
            }

            if (lookup.TryGetValue(key, out var matches))
            {
                // Get DISTINCT PlayerIDs
                var distinctIds = matches
                    .Where(m => m.PlayerID.HasValue)
                    .Select(m => m.PlayerID!.Value)
                    .Distinct()
                    .ToList();

                if (distinctIds.Count == 1)
                {
                    // ✅ All matches point to SAME player
                    player.PlayerID = distinctIds[0];
                }
                else if (distinctIds.Count > 1)
                {
                    // ⚠️ TRUE ambiguity (different players)
                    player.PlayerID = distinctIds[0]; // FIRST wins

                    multipleMatches.Add((player, matches));
                }
                else
                {
                    // Edge case: no valid IDs
                    player.PlayerID = null;
                    notFound.Add(player);
                }
            }
            else
            {
                // ❌ NOT FOUND
                player.PlayerID = null;
                notFound.Add(player);
            }
        }

        // 4️ CLI OUTPUT

        Console.WriteLine();
        Console.WriteLine("=== Player Resolver Summary ===");

        Console.WriteLine(
            $"Total Input Players: {inputPlayers.Count}");

        Console.WriteLine(
            $"Not Found: {notFound.Count}");

        Console.WriteLine(
            $"Multiple Matches: {multipleMatches.Count}");

        // Detailed logs

        if (notFound.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("---- Players NOT FOUND ----");

            foreach (var p in notFound)
            {
                Console.WriteLine(
                    $"{p.PlayerName} | Team: {p.Team} " +
                    $"| Pos: {p.Position}");
            }
        }

        if (multipleMatches.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                "---- Players with MULTIPLE MATCHES ----");

            foreach (var (input, matches)
                     in multipleMatches)
            {
                Console.WriteLine(
                    $"Input: {input.PlayerName} | " +
                    $"Team: {input.Team} | Pos: {input.Position}");

                foreach (var m in matches)
                {
                    Console.WriteLine(
                        $"   -> DB PlayerID: {m.PlayerID} " +
                        $"| Name: {m.PlayerName} " +
                        $"| Org: {m.organization_id}");
                }
            }
        }
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