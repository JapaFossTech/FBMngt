using FBMngt.Services.Roster;

namespace FBMngt.Models;

//public enum RosterMismatchType
//{
//    Match = 0,
//    TeamMismatch,
//    UnknownCsvTeam,
//    MissingDbTeam,
//    UnknownBothTeams,
//    UnmatchedPlayer
//}


public sealed class RosterCheckResult
{
    public int? PlayerId { get; }
    public string PlayerName { get; }

    public string? CsvTeamRaw { get; }
    public ResolvedTeam CsvTeam { get; }

    public ResolvedTeam DbTeam { get; }

    public RosterMismatchType MismatchType { get; }

    public RosterCheckResult(
        int? playerId,
        string playerName,
        string? csvTeamRaw,
        ResolvedTeam csvTeam,
        ResolvedTeam dbTeam,
        RosterMismatchType mismatchType)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        CsvTeamRaw = csvTeamRaw;
        CsvTeam = csvTeam;
        DbTeam = dbTeam;
        MismatchType = mismatchType;
    }
}
