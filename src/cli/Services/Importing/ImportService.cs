using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Players;
using FBMngt.Services.Roster;
using FBMngt.Services.Teams;

namespace FBMngt.Services.Importing;

public class ImportService
{
    private readonly ConfigSettings _configSettings;
    private IAppSettings AppSettings 
                                => _configSettings.AppSettings;
    private IPlayerRepository _playerRepository { get; init; }
    private PlayerResolver _playerResolver { get; init; }
    private PlayerImportService _playerImportService { get; init; }
    private FanProsCsvReader _fanProsCsvReader { get; init; }

    //Ctor
    public ImportService(IAppSettings appSettings)
    {
        _configSettings = new ConfigSettings(appSettings);
        _playerRepository = new PlayerRepository(appSettings);
        _playerResolver = new PlayerResolver(_playerRepository);
        _playerImportService = new PlayerImportService(_playerRepository);
        _fanProsCsvReader = new FanProsCsvReader();
    }
    public ImportService(
        ConfigSettings configSettings,
        IPlayerRepository playerRepository,
        PlayerResolver playerResolver,
        PlayerImportService playerImportService,
        FanProsCsvReader fanProsCsvReader)
    {
        _configSettings = configSettings;
        _playerRepository = playerRepository;
        _playerResolver = playerResolver;
        _playerImportService = playerImportService;
        _fanProsCsvReader = fanProsCsvReader;
    }


    #region CheckMatchesAsync
    // TODO: ImportPlayersAsync is the only method that should remain here long-term
    public async Task CheckMatchesAsync(
        string matchColumn,
        bool showPlayer,
        string? fileType,
        int? rows)
    {
        // 1. Load DB data
        var playerRepo = new PlayerRepository(AppSettings);
        var players = await playerRepo.GetAllAsync();

        var teamRepo = new TeamRepository();
        var teams = await teamRepo.GetTeamsAsync();

        // 2. Build lookups
        var nameLookup = BuildNameLookup(players);

        var teamResolver = new SqlTeamResolver(teams);
        var classifier = new RosterMismatchClassifier();

        // mlb_org_id -> Abbreviation (for display)
        var teamById = teams
            .Where(t => !string.IsNullOrWhiteSpace(t.MlbOrgId)
                        && !string.IsNullOrWhiteSpace(t.MlbOrgAbbrev))
            .ToDictionary(
                t => int.Parse(t.MlbOrgId!),
                t => t.MlbOrgAbbrev!);


        Console.WriteLine();

        // 3. FanPros flow
        if (fileType != null &&
            fileType.Equals("FanPros", AppConst.IGNORE_CASE))
        {
            string fullPath = _configSettings.FanPros_Rankings_InputCsv_Path;

            var fanProsPlayers = _fanProsCsvReader.Read(fullPath, rows ?? 200);

            ProcessGroup(
                title: "FanPros Draft Rankings",
                csvDataLines: fanProsPlayers,
                nameLookup: nameLookup,
                showPlayer: showPlayer,
                teamResolver: teamResolver,
                classifier: classifier,
                dbPlayers: players,
                teamById: teamById);
        }
    }

    // ---------------- helpers ----------------

    private static HashSet<string> BuildNameLookup(List<Player> players)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in players)
        {
            Add(set, p.PlayerName);
            Add(set, p.Aka1);
            Add(set, p.Aka2);
        }

        return set;
    }

    private static void Add(HashSet<string> set, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            set.Add(value.Trim());
    }

    // ---------------- core logic ----------------

    private static void ProcessGroup<T>(
        string title,
        List<T> csvDataLines,
        HashSet<string> nameLookup,
        bool showPlayer,
        ITeamResolver teamResolver,
        RosterMismatchClassifier classifier,
        List<Player> dbPlayers,
        Dictionary<int, string> teamById)
        where T : IPlayer
    {
        int matched = 0;
        var unmatched = new List<string>();
        var mismatches = new List<RosterMismatch>();

        foreach (var csv in csvDataLines)
        {
            if (!nameLookup.Contains(csv.PlayerName!.Trim()))
            {
                unmatched.Add(csv.PlayerName);
                continue;
            }

            matched++;

            if (csv is not FanProsPlayer fanPros)
                continue;

            var dbPlayer = dbPlayers.FirstOrDefault(p =>
                string.Equals(
                    p.PlayerName?.Trim(),
                    fanPros.PlayerName!.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (dbPlayer == null)
                continue;

            // Resolve CSV team
            var resolvedCsvTeam = teamResolver
                                    .Resolve(fanPros.Team);
            if (!resolvedCsvTeam.IsResolved)
                continue;

            // Resolve DB team (works if DB stores numeric or abbreviation)
            ResolvedTeam resolvedDbTeam;

            if (int.TryParse(dbPlayer.organization_id, out var dbId))
            {
                resolvedDbTeam = ResolvedTeam.Resolved(dbId, teamById.ContainsKey(dbId) ? teamById[dbId] : "UNK");
            }
            else
            {
                resolvedDbTeam = teamResolver.Resolve(dbPlayer.organization_id);
            }

            //        Console.WriteLine(
            //$"{fanPros.PlayerName} | CSV={fanPros.Team} -> {resolvedCsvTeam.TeamId} | " +
            //$"DB={dbPlayer.organization_id} -> {resolvedDbTeam.TeamId}");

            if (!resolvedDbTeam.IsResolved)
                continue;

            // Compare IDs
            if (resolvedCsvTeam.TeamId != resolvedDbTeam.TeamId)
            {
                mismatches.Add(new RosterMismatch
                {
                    PlayerName = fanPros.PlayerName!,
                    CsvTeamAbbrev =
                        teamById[resolvedCsvTeam.TeamId!.Value],
                    CsvTeamId = resolvedCsvTeam.TeamId.Value,
                    DbTeamAbbrev =
                        teamById[resolvedDbTeam.TeamId!.Value],
                    DbTeamId = resolvedDbTeam.TeamId.Value
                });
            }

        }

        Console.WriteLine($"=== {title} ===");
        Console.WriteLine($"Matched        : {matched}");
        Console.WriteLine($"Unmatched      : {unmatched.Count}");
        Console.WriteLine($"Team mismatches: {mismatches.Count}");

        if (showPlayer && unmatched.Any())
        {
            Console.WriteLine("\nUnmatched Players:");
            foreach (var name in unmatched.OrderBy(n => n))
                Console.WriteLine($"  - {name}");
        }

        if (mismatches.Any())
        {
            Console.WriteLine("\nRoster Mismatches:");
            foreach (var m in mismatches)
            {
                Console.WriteLine(
                    $"  - {m.PlayerName} | " +
                    $"CSV: {m.CsvTeamAbbrev} ({m.CsvTeamId}) | " +
                    $"DB: {m.DbTeamAbbrev} ({m.DbTeamId})");
            }
        }
    }

    #endregion

    #region ImportPlayersAsync
    public async Task ImportPlayersAsync(string? fileType, int? rows)
    {
        // Guard for FanPros only
        if (fileType == null ||
            !fileType.Equals("FanPros", AppConst.IGNORE_CASE))
        {
            Console.WriteLine("ImportPlayers currently supports FanPros only.");
            return;
        }

        // 1️ Load FanPros CSV players
        string fullPath = _configSettings.FanPros_Rankings_InputCsv_Path;

        List<FanProsPlayer> players =
                            _fanProsCsvReader.Read(fullPath, rows ?? 400);

        if (players.Count == 0)
            return;

        // 2️ Resolve PlayerIDs
        await _playerResolver.ResolvePlayerIDAsync(
                            players.Cast<IPlayer>().ToList());

        // 3️ Extract missing players
        List<string> missingPlayers = players
            .Where(p => p.PlayerID == null
                     && p.PlayerName.HasString())
            .Select(p => p.PlayerName!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 4 Import missing players

        await _playerImportService.InsertPlayersAsync(missingPlayers);

        // 5 Re-run resolution after import
        await _playerResolver.ResolvePlayerIDAsync(
                            players.Cast<IPlayer>().ToList());
    }
    #endregion
}