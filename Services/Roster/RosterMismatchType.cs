namespace FBMngt.Services.Roster;

public enum RosterMismatchType
{
    Match,                // CSV team matches DB team
    TeamMismatch,         // Different team values
    UnknownCsvTeam,       // CSV team not resolvable
    UnknownDbTeam,        // DB team not resolvable
    UnknownBothTeams      // Neither team resolvable
}
