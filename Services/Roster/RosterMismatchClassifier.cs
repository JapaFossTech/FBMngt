using FBMngt.Models;

namespace FBMngt.Services.Roster;

public class RosterMismatchClassifier
{
    public bool IsMismatch(FanProsPlayer csvPlayer, Player dbPlayer, ResolvedTeam resolvedCsvTeam)
    {
        // If DB player has no team, consider mismatch
        if (string.IsNullOrWhiteSpace(dbPlayer.organization_id))
            return true;

        // If CSV team can't be resolved, ignore mismatch
        if (!resolvedCsvTeam.IsResolved)
            return false;

        // Compare by TeamId (not abbreviation)
        return resolvedCsvTeam.TeamId.ToString() != dbPlayer.organization_id;
    }
}

