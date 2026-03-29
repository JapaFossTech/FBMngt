using FBMngt.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FBMngt.Services.Yahoo;

public static class YahooTeamMapper
{
    public static FBTeam Map(JsonElement teamNode)
    {
        return new FBTeam
        {
            TeamKey = GetString(teamNode, "team_key"),
            Name = GetString(teamNode, "name")
        };
    }
    private static string GetString(JsonElement node, 
                                    string propertyName)
    {
        // Case 1: Normal object
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty(propertyName, 
                                            out JsonElement value))
            {
                return ExtractString(value);
            }
        }

        // Case 2: Yahoo "array of key-value objects"
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in node.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (item.TryGetProperty(propertyName, out var value))
                {
                    return ExtractString(value);
                }
            }
        }

        return string.Empty;
    }

    private static string ExtractString(JsonElement value)
    => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()
                                ?? string.Empty,

        JsonValueKind.Array when value.GetArrayLength() > 0
            && value[0].ValueKind == JsonValueKind.String
                => value[0].GetString() ?? string.Empty,

        _ => string.Empty
    };
}
