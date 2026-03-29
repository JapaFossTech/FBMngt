using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMngt.Models;

public class League
{
    public string LeagueKey { get; set; } = default!;
    public string Name { get; set; } = default!;
    public List<FBTeam> Teams { get; set; } = new();
}
