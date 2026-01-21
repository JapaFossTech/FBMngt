namespace FBMngt.Models
{
    public class RosterMismatch
    {
        public string PlayerName { get; set; } = string.Empty;

        public string CsvTeamAbbrev { get; set; } = string.Empty;
        public int CsvTeamId { get; set; }

        public string DbTeamAbbrev { get; set; } = string.Empty;
        public int DbTeamId { get; set; }
    }
}


//public sealed class RosterMismatch
//{
//    public string PlayerName { get; init; } = string.Empty;
//    public string CsvTeamAbbrev { get; init; } = string.Empty;
//    public string DbTeamAbbrev { get; init; } = string.Empty;
//    public int CsvRank { get; init; }
//    public int DbRank { get; init; }
//}

//namespace FBMngt.Models;

//public class RosterMismatch
//{
//    public string PlayerName { get; set; } = string.Empty;

//    // CSV side
//    public string CsvTeamAbbrev { get; set; } = string.Empty;
//    public int CsvTeamId { get; set; }

//    // DB side
//    public string DbTeamAbbrev { get; set; } = string.Empty;
//    public int DbTeamId { get; set; }
//}
