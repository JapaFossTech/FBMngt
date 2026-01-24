using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Roster;
using FBMngt.Services.Teams;

namespace FBMngt.Services;

public class ImportService
{
    private readonly ConfigSettings _configSettings;
    public ImportService(IAppSettings appSettings)
    {
        _configSettings = new ConfigSettings(appSettings);
    }
    public async Task CheckMatchesAsync(
        string matchColumn,
        bool showPlayer,
        string? fileType,
        int? rows)
    {
        // 1. Load DB data
        var playerRepo = new PlayerRepository();
        var players = await playerRepo.GetPlayersAsync();

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
            string fullPath = Path.Combine(
                _configSettings.AppSettings.ImportedFilesPath,
                fileType,
                $"FantasyPros_{
                    _configSettings.AppSettings.SeasonYear
                    }_Draft_ALL_Rankings.csv");

            var fanProsPlayers =
                FanProsCsvReader.Read(fullPath, rows ?? 200);

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
            if (!nameLookup.Contains(csv.PlayerName.Trim()))
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
                    fanPros.PlayerName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (dbPlayer == null)
                continue;

            // Resolve CSV team
            var resolvedCsvTeam = teamResolver.Resolve(fanPros.Team);
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

            Console.WriteLine(
    $"{fanPros.PlayerName} | CSV={fanPros.Team} -> {resolvedCsvTeam.TeamId} | " +
    $"DB={dbPlayer.organization_id} -> {resolvedDbTeam.TeamId}");

            if (!resolvedDbTeam.IsResolved)
                continue;

            // Compare IDs
            if (resolvedCsvTeam.TeamId != resolvedDbTeam.TeamId)
            {
                mismatches.Add(new RosterMismatch
                {
                    PlayerName = fanPros.PlayerName,
                    CsvTeamAbbrev = teamById[resolvedCsvTeam.TeamId.Value],
                    CsvTeamId = resolvedCsvTeam.TeamId.Value,
                    DbTeamAbbrev = teamById[resolvedDbTeam.TeamId.Value],
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
}