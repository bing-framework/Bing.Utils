using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bing.Security;
using Bing.Security.Encoding;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace Bing.Security.Gm;

/// <summary>
/// 提供 HMAC-SM3 消息认证码计算与验证。
/// </summary>
public static class HmacSm3
{
    /// <summary>
    /// 计算 HMAC-SM3。
    /// </summary>
    public static byte[] Compute(byte[] key, byte[] data)
    {
        Validate(key, data);
        var hmac = Create(key);
        hmac.BlockUpdate(data, 0, data.Length);
        return Finish(hmac);
    }

    /// <summary>
    /// 计算流的 HMAC-SM3，不关闭输入流。
    /// </summary>
    public static byte[] Compute(byte[] key, Stream stream)
    {
        ValidateKey(key);
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        var hmac = Create(key);
        var buffer = new byte[81920];
        try
        {
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                hmac.BlockUpdate(buffer, 0, count);
            return Finish(hmac);
        }
        finally { GmCryptographicOperationsCompat.ZeroMemory(buffer); }
    }

    /// <summary>
    /// 异步计算流的 HMAC-SM3，不关闭输入流。
    /// </summary>
    public static async Task<byte[]> ComputeAsync(byte[] key, Stream stream, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        var hmac = Create(key);
        var buffer = new byte[81920];
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0)
                    return Finish(hmac);
                hmac.BlockUpdate(buffer, 0, count);
            }
        }
        finally { GmCryptographicOperationsCompat.ZeroMemory(buffer); }
    }

    /// <summary>
    /// 使用固定时间比较验证 HMAC-SM3。
    /// </summary>
    public static bool Verify(byte[] key, byte[] data, byte[] expectedMac)
    {
        if (expectedMac == null)
            throw new ArgumentNullException(nameof(expectedMac));
        var actual = Compute(key, data);
        try { return SecureComparison.FixedTimeEquals(actual, expectedMac); }
        finally { GmCryptographicOperationsCompat.ZeroMemory(actual); }
    }

    /// <summary>
    /// 计算小写十六进制 HMAC-SM3。
    /// </summary>
    public static string ComputeHex(byte[] key, byte[] data) => Encode(Compute(key, data), false);

    /// <summary>
    /// 计算 Base64 HMAC-SM3。
    /// </summary>
    public static string ComputeBase64(byte[] key, byte[] data) => Encode(Compute(key, data), true);

    /// <summary>
    /// 异步计算文件的小写十六进制 HMAC-SM3。
    /// </summary>
    public static async Task<string> ComputeFileHexAsync(string path, byte[] key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("文件路径不能为空。", nameof(path));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Encode(await ComputeAsync(key, stream, cancellationToken).ConfigureAwait(false), false);
    }

    /// <summary>
    /// 按指定格式编码 HMAC-SM3 摘要，并清零摘要缓冲区。
    /// </summary>
    /// <param name="mac">待编码的摘要字节。</param>
    /// <param name="base64">为 true 时使用 Base64，否则使用十六进制。</param>
    /// <returns>编码后的摘要文本。</returns>
    private static string Encode(byte[] mac, bool base64)
    {
        try { return base64 ? Convert.ToBase64String(mac) : HexEncoding.Encode(mac); }
        finally { GmCryptographicOperationsCompat.ZeroMemory(mac); }
    }
    /// <summary>
    /// 创建并初始化 SM3 HMAC 计算器。
    /// </summary>
    /// <param name="key">用于初始化计算器的密钥字节。</param>
    /// <returns>使用指定密钥初始化的 HMAC-SM3 计算器。</returns>
    private static HMac Create(byte[] key)
    {
        var hmac = new HMac(new SM3Digest());
        hmac.Init(new KeyParameter(key));
        return hmac;
    }

    /// <summary>
    /// 完成 HMAC-SM3 计算并取得摘要字节。
    /// </summary>
    /// <param name="hmac">待完成的 HMAC 计算器。</param>
    /// <returns>计算得到的摘要字节。</returns>
    private static byte[] Finish(HMac hmac)
    {
        var result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);
        return result;
    }

    /// <summary>
    /// 验证 HMAC-SM3 运算所需的密钥和数据。
    /// </summary>
    /// <param name="key">用于计算摘要的密钥。</param>
    /// <param name="data">参与摘要计算的数据。</param>
    private static void Validate(byte[] key, byte[] data)
    {
        ValidateKey(key);
        if (data == null)
            throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// 验证 HMAC-SM3 密钥非空且至少包含一个字节。
    /// </summary>
    /// <param name="key">待验证的密钥。</param>
    private static void ValidateKey(byte[] key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (key.Length == 0)
            throw new ArgumentException("HMAC-SM3 密钥不能为空。", nameof(key));
    }
}
