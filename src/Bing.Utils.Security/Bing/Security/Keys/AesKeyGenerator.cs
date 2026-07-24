using Bing.Security.Randomness;

namespace Bing.Security.Keys;

/// <summary>
/// 提供 AES 对称密钥生成操作。
/// </summary>
public static class AesKeyGenerator
{
    /// <summary>
    /// 生成指定长度的随机 AES 密钥。
    /// </summary>
    /// <param name="keySize">AES 密钥位长度，默认使用 256 位。</param>
    /// <returns>随机 AES 密钥字节。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="keySize"/> 不受支持时抛出。</exception>
    public static byte[] Generate(AesKeySize keySize = AesKeySize.Size256)
    {
        return keySize switch
        {
            AesKeySize.Size128 => SecurityRandom.GetBytes(16),
            AesKeySize.Size192 => SecurityRandom.GetBytes(24),
            AesKeySize.Size256 => SecurityRandom.GetBytes(32),
            _ => throw new ArgumentOutOfRangeException(nameof(keySize), keySize, "不支持的 AES 密钥长度。")
        };
    }
}