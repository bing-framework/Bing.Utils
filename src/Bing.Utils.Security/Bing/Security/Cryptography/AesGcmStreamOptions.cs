#if NET6_0_OR_GREATER
namespace Bing.Security.Cryptography;

/// <summary>
/// 配置 AES-GCM 分块认证流加密参数。
/// </summary>
public sealed class AesGcmStreamOptions
{
    /// <summary>
    /// 默认明文块大小，单位为字节。
    /// </summary>
    public const int DefaultBlockSize = 65536;

    /// <summary>
    /// 每个认证加密块的最大明文字节数，必须介于 4096 和 1048576 之间。
    /// </summary>
    public int BlockSize { get; init; } = DefaultBlockSize;
}
#endif