namespace FBMngt.Models
{
    public class Team
    {
        public int TeamID { get; set; }

        // Add these:
        public string? MlbOrgId { get; set; }
        public string? MlbOrgAbbrev { get; set; }

        // Keep this if needed
        public string? mlb_org_abbrev { get; set; }
    }
}

//namespace FBMngt.Models;

//public sealed class Team
//{
//    public int TeamID { get; set; }
//    public string? mlb_org_id { get; set; }
//    public string? mlb_org_abbrev { get; set; }
//}