namespace FBMngt.Models;

public sealed class ResolvedTeam
{
    public bool IsResolved { get; private set; }
    public int? TeamId { get; private set; }
    public string? TeamAbbreviation { get; private set; }

    public ResolvedTeam() { }

    private ResolvedTeam(bool isResolved, int? teamId, string? teamAbbreviation)
    {
        IsResolved = isResolved;
        TeamId = teamId;
        TeamAbbreviation = teamAbbreviation;
    }

    public static ResolvedTeam Unresolved()
        => new(false, null, null);

    public static ResolvedTeam Resolved(int teamId, string? teamAbbreviation)
        => new(true, teamId, teamAbbreviation);
}