using FBMngt.Models;

namespace FBMngt.Services.Teams
{
    public interface ITeamResolver
    {
        ResolvedTeam Resolve(string? csvTeam);
    }
}
