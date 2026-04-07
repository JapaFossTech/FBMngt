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

    ///// <summary>
    ///// EXISTING METHOD (UNCHANGED BEHAVIOR)
    ///// </summary>
    //public async Task<PlayerPersistenceStats> ProcessRosterAsync(
    //    JsonElement root)
    //{
    //    var playerNodes =
    //        _extractor.ExtractPlayers(root);

    //    var mappedPlayers = new List<Player>();

    //    foreach (var node in playerNodes)
    //    {
    //        var player = _mapper.Map(node);
    //        mappedPlayers.Add(player);
    //    }

    //    var dedupedPlayers =
    //        Deduplicate(mappedPlayers);

    //    await _resolver.ResolvePlayerIDAsync(
    //        dedupedPlayers.Cast<IPlayer>().ToList());

    //    var stats = await _persistenceService
    //        .PersistAsync(dedupedPlayers);

    //    return stats;
    //}

    /// <summary>
    /// NEW METHOD — BATCH OPTIMIZED
    /// </summary>
    public async Task<PlayerPersistenceStats> ProcessRosterAsync(
        JsonElement root,
        List<Player> dbPlayers,
        Dictionary<int, int> yahooMap)
    {
        var playerNodes =
            _extractor.ExtractPlayers(root);

        var mappedPlayers = new List<Player>();

        foreach (var node in playerNodes)
        {
            var player = _mapper.Map(node);
            mappedPlayers.Add(player);
        }

        var dedupedPlayers =
            Deduplicate(mappedPlayers);

        await _resolver.ResolvePlayerIDAsync(
            dedupedPlayers.Cast<IPlayer>().ToList(),
            dbPlayers);

        var stats = await _persistenceService
            .PersistAsync(dedupedPlayers, yahooMap);

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
                noExternalId.Add(p);
            }
        }

        return dict.Values
            .Concat(noExternalId)
            .ToList();
    }
}