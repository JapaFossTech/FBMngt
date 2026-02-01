namespace FBMngt;
public static class FrameworkExtensions
{
    public static bool IsNullOrEmpty(this string? data)
        => string.IsNullOrEmpty(data);
    public static bool HasString(this string? data)
        => !string.IsNullOrEmpty(data);
}

