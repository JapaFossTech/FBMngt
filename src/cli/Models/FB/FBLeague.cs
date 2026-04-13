namespace FBMngt.Models.FB;

/// <summary>
/// Represents a Fantasy Baseball League.
/// Maps to: dbo.tblFBLeague
/// </summary>
public class FBLeague
{
    /// <summary>
    /// Internal database identifier (PK).
    /// </summary>
    public int? FBLeagueID { get; set; }

    /// <summary>
    /// Yahoo League Key (e.g. 469.l.7042).
    /// REQUIRED and UNIQUE.
    /// </summary>
    public string YahooLeagueKey { get; set; } = string.Empty;

    /// <summary>
    /// Season (e.g. 2026).
    /// </summary>
    public int Season { get; set; }

    /// <summary>
    /// Full league name.
    /// </summary>
    public string LeagueName { get; set; } = string.Empty;

    /// <summary>
    /// Optional short name (e.g. "KNT2026").
    /// </summary>
    public string? Short { get; set; }
}