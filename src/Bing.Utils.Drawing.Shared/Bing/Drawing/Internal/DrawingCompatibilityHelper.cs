using System.Security.Cryptography;

namespace Bing.Drawing;

/// <summary>
/// Drawing 兼容性辅助操作
/// </summary>
internal static class DrawingCompatibilityHelper
{
    /// <summary>
    /// 限制整数范围
    /// </summary>
    internal static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    /// <summary>
    /// 限制字节范围
    /// </summary>
    internal static byte ClampToByte(float value)
    {
        var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return (byte)Clamp(rounded, 0, 255);
    }

    /// <summary>
    /// 生成指定上界内的随机整数
    /// </summary>
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