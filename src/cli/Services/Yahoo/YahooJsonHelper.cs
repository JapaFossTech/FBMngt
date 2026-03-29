using System.Text.Json;

namespace FBMngt.Services.Yahoo;

public static class YahooJsonHelper
{
    public static IEnumerable<JsonElement> GetYahooCollection(
                                                JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (JsonProperty property in node.EnumerateObject())
        {
            if (property.NameEquals("count"))
                continue;

            yield return property.Value;
        }
    }

}
