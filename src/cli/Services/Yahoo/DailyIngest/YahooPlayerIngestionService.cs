using FBMngt.Models;
using FBMngt.Services.Players;
using System.Text.Json;

namespace FBMngt.Services.Yahoo.DailyIngest;

/// <summary>
/// Orchestrates player ingestion from Yahoo roster JSON files.
///
/// PIPELINE:
/// JSON → Extract → Map → Deduplicate → Resolve → Persist
/// </summary>
public class YahooPlayerIngestionService
{
    private readonly YahooPlayerExtractor _extractor;
    private readonly YahooPlayerMapper _mapper;
    private readonly PlayerResolver _resolver;
    private readonly YahooPlayerPersistenceService _persistenceService;

    public YahooPlayerIngestionService(
        YahooPlayerExtractor extractor,
        YahooPlayerMapper mapper,
        PlayerResolver resolver,
        YahooPlayerPersistenceService persistenceService)
    {
        _extractor = extractor;
        _mapper = mapper;
        _resolver = resolver;
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Processes a roster JSON document.
    /// </summary>
    public async Task<PlayerPersistenceStats> ProcessRosterAsync(
        JsonElement root)
    {
        // 1. Extract raw player nodes
        var playerNodes =
            _extractor.ExtractPlayers(root);

        Console.WriteLine(
            $"Extractor found {playerNodes.Count} players");

        // 2. Map to domain model
        var mappedPlayers = new List<Player>();

        foreach (var node in playerNodes)
        {
            var player = _mapper.Map(node);

            mappedPlayers.Add(player);
        }

        Console.WriteLine(
            $"Mapped players: {mappedPlayers.Count}");

        // 3. Deduplicate by ExternalPlayerID
        var dedupedPlayers =
            Deduplicate(mappedPlayers);

        Console.WriteLine(
            $"Unique players after dedup: " +
            $"{dedupedPlayers.Count}");

        // 4. Resolve PlayerID against DB
        await _resolver.ResolvePlayerIDAsync(
            dedupedPlayers.Cast<IPlayer>().ToList());

        // --------------------------------------------------------
        // 5. PERSIST (NEW)
        // --------------------------------------------------------
        var stats = await _persistenceService
            .PersistAsync(dedupedPlayers);

        // --------------------------------------------------------
        // 6. RETURN DIAGNOSTICS
        // --------------------------------------------------------
        return stats;
    }

    /// <summary>
    /// Deduplicates players by ExternalPlayerID.
    /// </summary>
    private List<Player> Deduplicate(
        List<Player> players)
    {
        var dict = new Dictionary<int, Player>();

        var noExternalId = new List<Player>();

        foreach (var p in players)
        {
            if (p.ExternalPlayerID.HasValue)
            {
                var key = p.ExternalPlayerID.Value;

                if (!dict.ContainsKey(key))
                {
                    dict[key] = p;
                }
            }
            else
            {
                // fallback (rare case)
                noExternalId.Add(p);
            }
        }

        // Add players without External ID (no dedup possible)
        return dict.Values
            .Concat(noExternalId)
            .ToList();
    }
}