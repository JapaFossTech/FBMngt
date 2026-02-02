using FBMngt.Models;
using FBMngt.Services.Importing;
using Microsoft.VisualBasic.FileIO;
using System.Text;

namespace FBMngt.IO.Csv;

public static class FanProsCsvReader
{
    public static List<FanProsPlayer> Read(
                                string path, int? maxRows = 400)
    {
        //Get the file path for the newest csv file
        //TODO: Consider decoupling here because Filename normalization is not part of csv read
        var importFileResolver = new ImportFileResolver();
        path = importFileResolver.ResolveNewestFilePath(path,
                                    ImportNormalizationMode.ResolveOnly);

        Console.WriteLine($@"Using FanPros file: 
                                        {Path.GetFileName(path)}");

        //Continue
        var result = new List<FanProsPlayer>();

        using var parser = new TextFieldParser(path, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = new[] { "," },
            HasFieldsEnclosedInQuotes = true
        };

        // Read header
        var header = parser.ReadFields();
        if (header is null)
            return result;

        var colIndex = header
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name.Trim('"'), x => x.index,
                          StringComparer.OrdinalIgnoreCase);

        var count = 0;

        while (!parser.EndOfData)
        {
            if (maxRows.HasValue && maxRows.Value > 0 
                                    && count >= maxRows.Value)
                break;

            var cols = parser.ReadFields();
            if (cols == null || cols.Length == 0)
                continue;

            var player = new FanProsPlayer
            {
                PlayerName = cols[colIndex["PLAYER NAME"]].Trim(),
                Team = cols[colIndex["TEAM"]].Trim(),
                Position = cols[colIndex["POS"]].Trim(),
                Rank = int.TryParse(cols[colIndex["RK"]], out var r) ? r : 0
            };

            result.Add(player);
            count++;
        }

        return result;
    }
}
