using FBMngt.Models;

namespace FBMngt.IO;
public class YahooPreDraftRankingReader
    {
        public List<FanProsPlayer> Read(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"Yahoo ranking file not found: {filePath}");
            }

            string[] lines = File.ReadAllLines(filePath);

            var players = new List<FanProsPlayer>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Skip header lines like "Your Rankings"
                if (!char.IsDigit(line[0]))
                {
                    continue;
                }

                int dotIndex = line.IndexOf('.');
                if (dotIndex <= 0)
                {
                    continue;
                }

                string rankPart = line.Substring(0, dotIndex);
                string namePart = line.Substring(dotIndex + 1).Trim();

                if (!int.TryParse(rankPart, out var rank))
                {
                    continue;
                }

                var player = new FanProsPlayer
                {
                    Rank = rank,
                    PlayerName = namePart
                };

                players.Add(player);
            }

            return players;
        }
    }


