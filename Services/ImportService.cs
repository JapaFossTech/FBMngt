using FBMngt.Data;
using FBMngt.IO.Csv;
using FBMngt.Models;
using FBMngt.Services.Roster;
using FBMngt.Services.Teams;

namespace FBMngt.Services;

public class ImportService
{
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
                AppContext.ImportedFilesPath,
                fileType,
                $"FantasyPros_{AppContext.SeasonYear}_Draft_ALL_Rankings.csv");

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

//using FBMngt.Data;
//using FBMngt.IO.Csv;
//using FBMngt.Models;
//using FBMngt.Services.Roster;
//using FBMngt.Services.Teams;

//namespace FBMngt.Services;

//public class ImportService
//{
//    public async Task CheckMatchesAsync(
//        string matchColumn,
//        bool showPlayer,
//        string? fileType,
//        int? rows)
//    {
//        // Load players from DB
//        var playerRepo = new PlayerRepository();
//        var players = await playerRepo.GetPlayersAsync();

//        // Load teams from DB
//        var teamRepo = new TeamRepository();
//        var teams = await teamRepo.GetTeamsAsync();

//        // Build name lookup
//        HashSet<string> nameLookup = BuildNameLookup(players);

//        // Build team resolver
//        ITeamResolver teamResolver = new SqlTeamResolver(teams);

//        // Build roster mismatch classifier
//        var classifier = new RosterMismatchClassifier();

//        Console.WriteLine();

//        // Determine file type based on matchColumn (or add new flag later)
//        if (fileType is not null &&
//            fileType.Equals("FanPros", AppConst.IGNORE_CASE))
//        {
//            string fullPath = Path.Combine(
//                AppContext.ImportedFilesPath,
//                fileType,
//                "FantasyPros_{AppContext.SeasonYear}_Draft_ALL_Rankings.csv");

//            List<FanProsPlayer> fanPros =
//                                FanProsCsvReader
//                                .Read(fullPath, rows ?? 200);

//            ProcessGroup(
//                "FanPros Draft Rankings",
//                fanPros,
//                nameLookup,
//                showPlayer,
//                teamResolver,
//                classifier,
//                players);
//        }
//        else
//        {
//            string fullPath = Path.Combine(
//                    AppContext.ProjectionPath,
//                    "{AppContext.SeasonYear}_Steamer_Projections_Pitchers.csv");

//            ProcessGroup(
//                "Pitchers",
//                CsvReader.ReadPitchers(fullPath),
//                nameLookup,
//                showPlayer,
//                teamResolver,
//                classifier,
//                players);

//            Console.WriteLine();

//            fullPath = Path.Combine(
//                        AppContext.ProjectionPath,
//                        "{AppContext.SeasonYear}_Steamer_Projections_Batters.csv");

//            ProcessGroup(
//                "Batters",
//                CsvReader.ReadBatters(fullPath),
//                nameLookup,
//                showPlayer,
//                teamResolver,
//                classifier,
//                players);
//        }
//    }

//    private static HashSet<string> BuildNameLookup(List<Player> players)
//    {
//        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

//        foreach (var p in players)
//        {
//            Add(set, p.PlayerName);
//            Add(set, p.Aka1);
//            Add(set, p.Aka2);
//        }

//        return set;
//    }

//    private static void Add(HashSet<string> set, string? value)
//    {
//        if (!string.IsNullOrWhiteSpace(value))
//            set.Add(value.Trim());
//    }

//    private static void ProcessGroup<T>(
//        string title,
//        List<T> csvDataLines,
//        HashSet<string> nameLookup,
//        bool showPlayer,
//        ITeamResolver teamResolver,
//        RosterMismatchClassifier classifier,
//        List<Player> dbPlayers)
//        where T : IPlayer
//    {
//        var matched = 0;
//        var unmatched = new List<string>();
//        var mismatches = new List<RosterMismatch>();

//        foreach (var csv in csvDataLines)
//        {
//            // 1. Name match
//            if (!nameLookup.Contains(csv.PlayerName.Trim()))
//            {
//                unmatched.Add(csv.PlayerName);
//                continue;
//            }

//            matched++;

//            // 2. Team logic only applies to FanPros
//            if (csv is not FanProsPlayer fanPros)
//                continue;

//            // 3. Find DB player
//            var dbPlayer = dbPlayers.FirstOrDefault(p =>
//                string.Equals(
//                    p.PlayerName?.Trim(),
//                    fanPros.PlayerName.Trim(),
//                    StringComparison.OrdinalIgnoreCase));

//            if (dbPlayer == null)
//                continue;

//            // 4. Resolve CSV team (e.g. LAD → 119)
//            var resolvedCsvTeam = teamResolver.Resolve(fanPros.Team);
//            if (!resolvedCsvTeam.IsResolved)
//                continue;

//            // 5. Resolve DB team (e.g. 112 → HOU)
//            if (string.IsNullOrWhiteSpace(dbPlayer.organization_id))
//                continue;

//            //var resolvedDbTeam = teamResolver.Resolve(dbPlayer.organization_id);
//            //if (!resolvedDbTeam.IsResolved)
//            //    continue;

//            // 6. Compare normalized IDs
//            if (!int.TryParse(dbPlayer.organization_id, out var dbTeamId))
//                continue;

//            if (resolvedCsvTeam.TeamId != dbTeamId)
//            {
//                mismatches.Add(new RosterMismatch
//                {
//                    PlayerName = fanPros.PlayerName,
//                    CsvTeamId = resolvedCsvTeam.TeamId!.Value,
//                    DbTeamId = dbTeamId
//                });
//            }

//        }

//        // -------- Output --------

//        Console.WriteLine($"=== {title} ===");
//        Console.WriteLine($"Matched        : {matched}");
//        Console.WriteLine($"Unmatched      : {unmatched.Count}");
//        Console.WriteLine($"Team mismatches: {mismatches.Count}");

//        if (showPlayer && unmatched.Any())
//        {
//            Console.WriteLine("\nUnmatched Players:");
//            foreach (var name in unmatched.OrderBy(n => n))
//                Console.WriteLine($"  - {name}");
//        }

//        if (mismatches.Any())
//        {
//            Console.WriteLine("\nRoster Mismatches:");
//            foreach (var m in mismatches)
//            {
//                var csvTeam = teamResolver.Resolve(m.CsvTeamId.ToString());
//                var dbTeam = teamResolver.Resolve(m.DbTeamId.ToString());

//                Console.WriteLine(
//                    $"  - {m.PlayerName} | " +
//                    $"CSV: {csvTeam.TeamAbbreviation} ({m.CsvTeamId}) | " +
//                    $"DB: {dbTeam.TeamAbbreviation} ({m.DbTeamId})");
//            }
//        }
//    }

//}
