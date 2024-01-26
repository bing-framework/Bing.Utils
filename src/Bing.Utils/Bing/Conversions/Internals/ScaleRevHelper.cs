namespace Bing.Conversions.Internals;

internal static class ScaleRevHelper
{
    public static string Reverse(string val, int bitLength)
    {
        if (string.IsNullOrWhiteSpace(val))
            return val;
        var left = val.Length % bitLength;
        if (left > 0)
            val = $"{'0'.Repeat(left)}{val}";
    }
}