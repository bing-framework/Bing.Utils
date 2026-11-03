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

#if NET6_0_OR_GREATER
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
#else
        var bytes = new byte[16];
        using (var random = RandomNumberGenerator.Create())
            random.GetBytes(bytes);
#endif

        // 直接写入 Guid 构造函数使用的混合端序，保留 74 位密码学随机数据。
        bytes[3] = (byte)(unixTimeMilliseconds >> 40);
        bytes[2] = (byte)(unixTimeMilliseconds >> 32);
        bytes[1] = (byte)(unixTimeMilliseconds >> 24);
        bytes[0] = (byte)(unixTimeMilliseconds >> 16);
        bytes[5] = (byte)(unixTimeMilliseconds >> 8);
        bytes[4] = (byte)unixTimeMilliseconds;
        bytes[7] = (byte)(0x70 | (bytes[7] & 0x0F));
        bytes[8] = (byte)(0x80 | (bytes[8] & 0x3F));
        return new Guid(bytes);
    }

    /// <summary>
    /// 尝试从 RFC 9562 UUIDv7 中读取 Unix 毫秒时间戳。
    /// </summary>
    /// <param name="value">待检查的标识。</param>
    /// <param name="unixTimeMilliseconds">成功时为 48 位 Unix 毫秒时间戳；失败时为零。</param>
    /// <returns>标识具有 UUIDv7 版本和 RFC 变体时返回 <see langword="true"/>。</returns>
    public static bool TryGetTimestamp(Guid value, out long unixTimeMilliseconds)
    {
#if NET6_0_OR_GREATER
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
#else
        var bytes = value.ToByteArray();
#endif
        if ((bytes[7] >> 4) != 7 || (bytes[8] & 0xC0) != 0x80)
        {
            unixTimeMilliseconds = 0;
            return false;
        }

        unixTimeMilliseconds = ((long)bytes[3] << 40)
                             | ((long)bytes[2] << 32)
                             | ((long)bytes[1] << 24)
                             | ((long)bytes[0] << 16)
                             | ((long)bytes[5] << 8)
                             | bytes[4];
        return true;
    }
}
