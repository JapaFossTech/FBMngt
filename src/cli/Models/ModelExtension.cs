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
    public static int? GetPositionNumber(this IPlayer player)
    {
        if (string.IsNullOrWhiteSpace(player.Position))
            return null;

        var position = player.Position;
        int i = position.Length - 1;

        // Walk backwards while digits
        while (i >= 0 && char.IsDigit(position[i]))
        {
            i--;
        }

        // If no trailing digits
        if (i == position.Length - 1)
            return null;

        var numberPart = position.Substring(i + 1);

        return int.TryParse(numberPart, out var result)
            ? result
            : null;
    }

    public static string? GetPositionCode(this IPlayer player)
    {
        if (string.IsNullOrWhiteSpace(player.Position))
            return null;

        var number = player.GetPositionNumber();

        if (number == null)
            return player.Position;

        var numberLength = number.Value.ToString().Length;

        return player.Position.Substring(0, 
                            player.Position.Length - numberLength);
    }
}
