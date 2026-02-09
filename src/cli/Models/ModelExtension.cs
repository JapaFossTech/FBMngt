namespace FBMngt.Models;

public static class IPlayerExtension
{
    public static bool IsPitcher(this IPlayer player)
    {
        string? pos = player.Position;

        if (string.IsNullOrWhiteSpace(pos))
            return false;

        return pos.StartsWith("SP", AppConst.IGNORE_CASE)
            || pos.StartsWith("RP", AppConst.IGNORE_CASE)
            || pos.Equals("P", AppConst.IGNORE_CASE);
    }
    public static bool IsBatter(this IPlayer player)
        => !player.IsPitcher();
    public static bool IsCatcher(this IPlayer player)
    {
        string? pos = player.Position;

        if (string.IsNullOrWhiteSpace(pos))
            return false;

        return pos.StartsWith("C", AppConst.IGNORE_CASE);
    }

    public static bool IsCloser(this IPlayer player)
    {
        string? pos = player.Position;

        if (string.IsNullOrWhiteSpace(pos))
            return false;

        return pos.StartsWith("RP", AppConst.IGNORE_CASE);
    }

}
