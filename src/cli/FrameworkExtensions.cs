namespace FBMngt;
public static class FrameworkExtensions
{
    // Generic - Database
    public static object ToDbValue<T>(this T? value)
    {
        if (value == null)
            return DBNull.Value;

        return value;
    }

    // String
    public static bool IsNullOrEmpty(this string? data)
        => string.IsNullOrEmpty(data);
    public static bool HasString(this string? data)
        => !string.IsNullOrEmpty(data);
}

