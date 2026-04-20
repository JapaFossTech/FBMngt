namespace FBMngt.Data.SPTrending;

using FBMngt.Models.SPTrending;

public interface ISPTrendingRepository
{
    Task<List<SPTrendCandidate>> GetCandidatesAsync();
}