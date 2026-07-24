using System;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace Bing.Security.Gm;

/// <summary>
/// 提供 SM3 摘要和 HMAC-SM3 消息认证码计算。
/// </summary>
public static class Sm3
{
    /// <summary>
    /// SM3 摘要长度，单位为字节。
    /// </summary>
    public const int DigestSize = 32;

    /// <summary>
    /// 计算数据的 SM3 摘要。
    /// </summary>
    /// <param name="data">要计算摘要的数据。</param>
    /// <returns>32 字节 SM3 摘要。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <c>null</c> 时抛出。</exception>
    public static byte[] Compute(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var digest = new SM3Digest();
        digest.BlockUpdate(data, 0, data.Length);
        var result = new byte[DigestSize];
        digest.DoFinal(result, 0);
        return result;
    }

    /// <summary>
    /// 使用 HMAC-SM3 计算数据的认证码。
    /// </summary>
    /// <param name="key">认证密钥，不能为空。</param>
    /// <param name="data">要认证的数据。</param>
    /// <returns>32 字节 HMAC-SM3 认证码。</returns>
    /// <exception cref="ArgumentNullException">任一参数为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为空时抛出。</exception>
    public static byte[] ComputeHmac(byte[] key, byte[] data)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (key.Length == 0)
            throw new ArgumentException("HMAC-SM3 密钥不能为空。", nameof(key));

        var hmac = new HMac(new SM3Digest());
        hmac.Init(new KeyParameter(key));
        hmac.BlockUpdate(data, 0, data.Length);
        var result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);
        return result;
    }
}