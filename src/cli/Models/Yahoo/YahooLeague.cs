using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Models.Yahoo;

public class YahooLeague
{
    public string LeagueKey { get; set; } = default!;
    public string Name { get; set; } = default!;
    public List<YahooTeam> Teams { get; set; } = new();
}
