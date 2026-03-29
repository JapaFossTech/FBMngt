using System.Text.Json;

namespace FBMngt.Services.Yahoo;

/// <summary>
/// Centralized helper for navigating Yahoo JSON quirks:
/// - Object vs Array-of-objects
/// - Safe property extraction
/// </summary>
public static class YahooJsonNavigator
{
    /// <summary>
    /// Try to get a property from:
    /// 1) Normal object
    /// 2) Yahoo array-of-objects
    /// </summary>
    public static bool TryGetProperty(
        JsonElement node,
        string propertyName,
        out JsonElement value)
    {
        // Case 1: normal object
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty(propertyName, out value))
                return true;
        }

        // Case 2: Yahoo array-of-objects
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (item.TryGetProperty(propertyName, out value))
                    return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Extract string supporting:
    /// - direct string
    /// - ["string"] (Yahoo wrap)
    /// </summary>
    public static string GetString(
        JsonElement node,
        string propertyName)
    {
        if (TryGetProperty(node, propertyName, out var value))
        {
            // direct string
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;

            // Yahoo: ["value"]
            if (value.ValueKind == JsonValueKind.Array
                && value.GetArrayLength() > 0
                && value[0].ValueKind == JsonValueKind.String)
            {
                return value[0].GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}