using System;
using System.Security.Cryptography;

namespace Bing.IdUtils;

/// <summary>
/// 按 RFC 9562 生成并读取 UUIDv7。
/// </summary>
/// <remarks>
/// 时间戳使用 Unix 毫秒，随机字段使用密码学安全随机数；不保证同一毫秒内单调递增。
/// </remarks>
public static class Uuid7
{
    /// <summary>
    /// UUIDv7 时间戳字段允许的最大 Unix 毫秒值。
    /// </summary>
    private const long MaximumTimestamp = 0xFFFFFFFFFFFF;

    /// <summary>
    /// 生成包含当前 UTC 时间戳和随机字段的 UUIDv7。
    /// </summary>
    /// <returns>符合 RFC 9562 布局的 UUIDv7。</returns>
    public static Guid Generate() => Generate(DateTimeOffset.UtcNow);

    /// <summary>
    /// 使用指定时刻生成 UUIDv7。
    /// </summary>
    /// <param name="timestamp">编码为 Unix 毫秒的时刻；毫秒以下精度会被舍弃。</param>
    /// <returns>符合 RFC 9562 布局且包含指定 Unix 毫秒时间戳的 UUIDv7。</returns>
    /// <exception cref="ArgumentOutOfRangeException">指定时刻早于 Unix Epoch，或时间戳超出 48 位范围。</exception>
    public static Guid Generate(DateTimeOffset timestamp)
    {
        var unixTimeMilliseconds = timestamp.ToUnixTimeMilliseconds();
        if (unixTimeMilliseconds < 0 || unixTimeMilliseconds > MaximumTimestamp)
            throw new ArgumentOutOfRangeException(nameof(timestamp), timestamp, "UUIDv7 时间戳必须位于 48 位 Unix 毫秒范围内。");

        var networkBytes = new byte[16];
        var randomBytes = new byte[10];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(randomBytes);

        var timestampValue = unixTimeMilliseconds;
        for (var index = 5; index >= 0; index--)
        {
            networkBytes[index] = (byte)(timestampValue & 0xFF);
            timestampValue >>= 8;
        }

        networkBytes[6] = (byte)(0x70 | (randomBytes[0] & 0x0F));
        networkBytes[7] = randomBytes[1];
        networkBytes[8] = (byte)(0x80 | (randomBytes[2] & 0x3F));
        Array.Copy(randomBytes, 3, networkBytes, 9, 7);

        return FromNetworkOrderBytes(networkBytes);
    }

    /// <summary>
    /// 尝试从 RFC 9562 UUIDv7 中读取 Unix 毫秒时间戳。
    /// </summary>
    /// <param name="value">待检查的标识。</param>
    /// <param name="unixTimeMilliseconds">成功时为 48 位 Unix 毫秒时间戳；失败时为零。</param>
    /// <returns>标识具有 UUIDv7 版本和 RFC 变体时返回 <see langword="true"/>。</returns>
    public static bool TryGetTimestamp(Guid value, out long unixTimeMilliseconds)
    {
        var networkBytes = ToNetworkOrderBytes(value);
        if ((networkBytes[6] >> 4) != 7 || (networkBytes[8] & 0xC0) != 0x80)
        {
            unixTimeMilliseconds = 0;
            return false;
        }

        unixTimeMilliseconds = 0;
        for (var index = 0; index < 6; index++)
            unixTimeMilliseconds = (unixTimeMilliseconds << 8) | networkBytes[index];

        return true;
    }

    /// <summary>
    /// 将 RFC 网络字节序转换为 .NET <see cref="Guid"/> 构造函数使用的混合端序。
    /// </summary>
    /// <param name="networkBytes">按 RFC 9562 顺序排列的 16 字节 UUID。</param>
    /// <returns>表示相同 UUID 的 .NET <see cref="Guid"/>。</returns>
    private static Guid FromNetworkOrderBytes(byte[] networkBytes)
    {
        var guidBytes = new byte[16];
        guidBytes[0] = networkBytes[3];
        guidBytes[1] = networkBytes[2];
        guidBytes[2] = networkBytes[1];
        guidBytes[3] = networkBytes[0];
        guidBytes[4] = networkBytes[5];
        guidBytes[5] = networkBytes[4];
        guidBytes[6] = networkBytes[7];
        guidBytes[7] = networkBytes[6];
        Array.Copy(networkBytes, 8, guidBytes, 8, 8);
        return new Guid(guidBytes);
    }

    /// <summary>
    /// 将 .NET <see cref="Guid"/> 的混合端序转换为 RFC 网络字节序。
    /// </summary>
    /// <param name="value">待转换的标识。</param>
    /// <returns>按 RFC 9562 字段顺序排列的 16 字节 UUID。</returns>
    private static byte[] ToNetworkOrderBytes(Guid value)
    {
        var guidBytes = value.ToByteArray();
        var networkBytes = new byte[16];
        networkBytes[0] = guidBytes[3];
        networkBytes[1] = guidBytes[2];
        networkBytes[2] = guidBytes[1];
        networkBytes[3] = guidBytes[0];
        networkBytes[4] = guidBytes[5];
        networkBytes[5] = guidBytes[4];
        networkBytes[6] = guidBytes[7];
        networkBytes[7] = guidBytes[6];
        Array.Copy(guidBytes, 8, networkBytes, 8, 8);
        return networkBytes;
    }
}
