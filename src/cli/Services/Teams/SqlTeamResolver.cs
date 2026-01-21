using FBMngt.Models;
using FBMngt.Services.Teams;

public sealed class SqlTeamResolver : ITeamResolver
{
    private readonly List<Team> _teams;

    public SqlTeamResolver(List<Team> teams)
    {
        _teams = teams;
    }

    public ResolvedTeam Resolve(string teamAbbrev)
    {
        if (string.IsNullOrWhiteSpace(teamAbbrev))
            return ResolvedTeam.Unresolved();

        var t = _teams.FirstOrDefault(x =>
            string.Equals(x.MlbOrgAbbrev, teamAbbrev, StringComparison.OrdinalIgnoreCase));

        if (t == null || string.IsNullOrWhiteSpace(t.MlbOrgId))
            return ResolvedTeam.Unresolved();

        return ResolvedTeam.Resolved(int.Parse(t.MlbOrgId), t.MlbOrgAbbrev);
    }
}

//using FBMngt.Models;

//namespace FBMngt.Services.Teams;

//public class SqlTeamResolver : ITeamResolver
//{
//    private readonly Dictionary<string, Team> _lookup;

//    public SqlTeamResolver(List<Team> teams)
//    {
//        _lookup = teams
//            .Where(t => !string.IsNullOrWhiteSpace(t.mlb_org_abbrev))
//            .ToDictionary(t => t.mlb_org_abbrev!.Trim().ToUpper(), t => t);
//    }

//    public ResolvedTeam Resolve(string teamAbbrev)
//    {
//        if (string.IsNullOrWhiteSpace(teamAbbrev))
//            return ResolvedTeam.Unresolved();

//        var key = teamAbbrev.Trim().ToUpper();

//        if (!_lookup.TryGetValue(key, out var team))
//            return ResolvedTeam.Unresolved();

//        // HERE is the fix: return mlb_org_id instead of TeamID
//        if (int.TryParse(team.MlbOrgId, out var orgId))
//            return ResolvedTeam.Resolved(orgId, team.mlb_org_abbrev!);

//        return ResolvedTeam.Unresolved();
//    }
//}

