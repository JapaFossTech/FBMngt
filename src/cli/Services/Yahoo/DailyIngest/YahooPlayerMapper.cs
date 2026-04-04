using System.Text.Json;
using FBMngt.Models;

namespace FBMngt.Services.Yahoo.DailyIngest;

/// <summary>
/// Maps Yahoo player JsonElement into Player domain model.
///
/// IMPORTANT:
/// - Aka fields are RESERVED for name variations ONLY
/// - ExternalPlayerID stores Yahoo player_id (int)
/// - player_key is ignored (league-specific)
/// </summary>
public class YahooPlayerMapper
{
    /// <summary>
    /// Maps a JsonElement representing a player into Player.
    /// </summary>
    public Player Map(JsonElement playerElement)
    {
        var player = new Player
        {
            PlayerName = GetFullName(playerElement),

            Team = GetString(playerElement,
                "editorial_team_abbr"),

            Position = GetString(playerElement,
                "primary_position"),

            ExternalPlayerID =
                GetInt(playerElement, "player_id")
        };

        return player;
    }

    /// <summary>
    /// Extracts full name safely from nested "name".
    /// </summary>
    private string? GetFullName(JsonElement element)
    {
        if (element.TryGetProperty("name",
            out var nameElement))
        {
            return GetString(nameElement, "full");
        }

        return null;
    }

    /// <summary>
    /// Safely retrieves string property.
    /// </summary>
    private string? GetString(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(propertyName,
            out var value))
        {
            return value.ValueKind switch
            {
                JsonValueKind.String =>
                    value.GetString(),

                JsonValueKind.Number =>
                    value.ToString(),

                _ => null
            };
        }

        return null;
    }

    /// <summary>
    /// Safely retrieves int property.
    /// </summary>
    private int? GetInt(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(propertyName,
            out var value))
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(),
                    out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}