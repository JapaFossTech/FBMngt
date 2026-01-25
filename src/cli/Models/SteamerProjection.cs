using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Models;

public class SteamerBatterProjection : IPlayer
{
    public int? PlayerID { get; set; }
    public string PlayerName { get; set; } = string.Empty;

    public int PA { get; set; }
    public int R { get; set; }
    public int HR { get; set; }
    public int RBI { get; set; }
    public int SB { get; set; }
    public double AVG { get; set; }


    // Z-scores per category
    public double Z_R { get; set; }
    public double Z_HR { get; set; }
    public double Z_RBI { get; set; }
    public double Z_SB { get; set; }
    public double Z_AVG { get; set; }

    public double TotalZ { get; set; }
    public string? Team { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}

public class SteamerPitcherProjection : IPlayer
{
    public int? PlayerID { get; set; }
    public string PlayerName { get; set; } = string.Empty;

    public double IP { get; set; }
    public int W { get; set; }
    public int K { get; set; }
    public int SV { get; set; }
    public int H { get; set; }
    public int BB { get; set; }
    public double ERA { get; set; }
    public double WHIP { get; set; }

    // Z-scores
    public double Z_W { get; set; }
    public double Z_K { get; set; }
    public double Z_SV { get; set; }
    public double Z_ERA { get; set; }
    public double Z_WHIP { get; set; }

    public double TotalZ { get; set; }
    public string? Team { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}


