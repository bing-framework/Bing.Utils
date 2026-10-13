using System.Security.Cryptography;

namespace Bing.Drawing;

/// <summary>
/// Drawing 兼容性辅助操作
/// </summary>
internal static class DrawingCompatibilityHelper
{
    /// <summary>
    /// 将整数限制在指定范围内。
    /// </summary>
    /// <param name="value">待限制的整数。</param>
    /// <param name="min">允许的最小值。</param>
    /// <param name="max">允许的最大值。</param>
    /// <returns>限制后的整数。</returns>
    internal static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    /// <summary>
    /// 将数值限制在字节可表示的范围内。
    /// </summary>
    /// <param name="value">待限制的数值。</param>
    /// <returns>限制到 0 到 255 的字节值。</returns>
    internal static byte ClampToByte(float value)
    {
        var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return (byte)Clamp(rounded, 0, 255);
    }

    /// <summary>
    /// 生成小于上限的随机整数。
    /// </summary>
    /// <param name="maxExclusive">随机数的独占上限。</param>
    /// <returns>生成的随机整数。</returns>
    internal static int GetRandomInt32(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));

        var upperBound = (uint)maxExclusive;
        const ulong maxValueExclusive = 1UL + uint.MaxValue;
        var threshold = maxValueExclusive - maxValueExclusive % upperBound;
        var buffer = new byte[sizeof(uint)];

        using var randomNumberGenerator = RandomNumberGenerator.Create();

        uint value;
        do
        {
            randomNumberGenerator.GetBytes(buffer);
            value = BitConverter.ToUInt32(buffer, 0);
        } while ((ulong)value >= threshold);

        return (int)(value % upperBound);
    }
}
