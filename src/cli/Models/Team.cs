namespace FBMngt.Models;

public class Team
{
    public int TeamID { get; set; }

    // Add these:
    public string? MlbOrgId { get; set; }
    public string? MlbOrgAbbrev { get; set; }

    // Keep this if needed
    public string? mlb_org_abbrev { get; set; }
}
public class FBTeam
{
    public string? TeamKey { get; set; }
    public string? Name { get; set; }
}