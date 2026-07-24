using System.Security.Cryptography;
using Bing.Security.Encoding;

namespace Bing.Security.Randomness;

/// <summary>
/// 提供基于系统密码学随机数生成器的随机数据。
/// </summary>
public static class SecurityRandom
{
    /// <summary>
    /// 生成指定长度的密码学安全随机字节。
    /// </summary>
    /// <param name="length">要生成的字节数，必须大于或等于零。</param>
    /// <returns>包含随机字节的新数组。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 小于零时抛出。</exception>
    public static byte[] GetBytes(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, "随机字节长度不能小于零。");

        var result = new byte[length];
        Fill(result);
        return result;
    }

    /// <summary>
    /// 使用密码学安全随机字节填充目标缓冲区。
    /// </summary>
    /// <param name="destination">要填充的目标缓冲区；空缓冲区不会执行任何操作。</param>
    public static void Fill(Span<byte> destination)
    {
        if (destination.IsEmpty)
            return;

#if NETSTANDARD2_0
        using var random = RandomNumberGenerator.Create();
        var buffer = new byte[destination.Length];
        random.GetBytes(buffer);
        buffer.AsSpan().CopyTo(destination);
        CryptographicOperationsCompat.ZeroMemory(buffer);
#else
        RandomNumberGenerator.Fill(destination);
#endif
    }

    /// <summary>
    /// 生成指定字节长度的小写十六进制随机值。
    /// </summary>
    /// <param name="byteLength">随机字节数，必须大于或等于零。</param>
    /// <returns>由小写十六进制字符组成的随机值。</returns>
    public static string GetHex(int byteLength) => HexEncoding.Encode(GetBytes(byteLength));

    /// <summary>
    /// 生成指定字节长度、无填充的 Base64Url 随机值。
    /// </summary>
    /// <param name="byteLength">随机字节数，必须大于或等于零。</param>
    /// <returns>无填充的 Base64Url 随机值。</returns>
    public static string GetBase64Url(int byteLength) => Base64UrlEncoding.Encode(GetBytes(byteLength));
}