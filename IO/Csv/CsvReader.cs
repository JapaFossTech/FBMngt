using FBMngt.Models;
using System.Globalization;
using System.Text;

namespace FBMngt.IO.Csv;

public static class CsvReader
{
    private static List<T> Read<T>(string path)
    where T : IPlayer, new()
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);

        var header = lines[0].Split(',');

        var colIndex = header
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index,
                          StringComparer.OrdinalIgnoreCase);

        var result = new List<T>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');

            var projection = new T
            {
                PlayerName = cols[colIndex["Name"]]
            };

            // Batter-specific
            if (projection is SteamerBatterProjection b)
            {
                b.R = ParseInt(cols, colIndex, "R");
                b.HR = ParseInt(cols, colIndex, "HR");
                b.RBI = ParseInt(cols, colIndex, "RBI");
                b.SB = ParseInt(cols, colIndex, "SB");
                b.AVG = ParseDouble(cols, colIndex, "AVG");
                b.PA = ParseInt(cols, colIndex, "PA");
            }

            // Pitcher-specific
            if (projection is SteamerPitcherProjection p)
            {
                p.W = ParseInt(cols, colIndex, "W");
                p.K = ParseInt(cols, colIndex, "SO");   // FanGraphs uses SO
                p.SV = ParseInt(cols, colIndex, "SV");
                p.H = ParseInt(cols, colIndex, "H");
                p.BB = ParseInt(cols, colIndex, "BB");
                p.ERA = ParseDouble(cols, colIndex, "ERA");
                p.IP = ParseDouble(cols, colIndex, "IP");
                p.WHIP = p.IP > 0 ? (p.H + p.BB) / p.IP : 0;
            }

            result.Add(projection);
        }

        return result;
    }
    private static int ParseInt(
        string[] cols,
        Dictionary<string, int> map,
        string key)
    {
        return map.ContainsKey(key)
            ? int.TryParse(cols[map[key]], out var v) ? v : 0
            : 0;
    }

    private static double ParseDouble(
        string[] cols,
        Dictionary<string, int> map,
        string key)
    {
        return map.ContainsKey(key)
            ? double.TryParse(cols[map[key]],
                CultureInfo.InvariantCulture,
                out var v) ? v : 0
            : 0;
    }

    public static List<SteamerPitcherProjection> ReadPitchers(string path)
        => Read<SteamerPitcherProjection>(path);

    public static List<SteamerBatterProjection> ReadBatters(string path)
        => Read<SteamerBatterProjection>(path);

}

