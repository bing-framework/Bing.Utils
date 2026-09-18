using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bing.Security.Encoding;
using Org.BouncyCastle.Crypto.Digests;

namespace Bing.Security.Gm;

/// <summary>提供 SM3 摘要计算。</summary>
public static class Sm3
{
    /// <summary>SM3 摘要长度，单位为字节。</summary>
    public const int DigestSize = 32;

    /// <summary>计算字节数组的 SM3 摘要。</summary>
    public static byte[] Compute(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        var digest = new SM3Digest();
        digest.BlockUpdate(data, 0, data.Length);
        return Finish(digest);
    }

    /// <summary>计算流的 SM3 摘要，不关闭输入流。</summary>
    public static byte[] Compute(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        var digest = new SM3Digest();
        var buffer = new byte[81920];
        try
        {
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                digest.BlockUpdate(buffer, 0, count);
            return Finish(digest);
        }
        finally { GmCryptographicOperationsCompat.ZeroMemory(buffer); }
    }

    /// <summary>异步计算流的 SM3 摘要，不关闭输入流。</summary>
    public static async Task<byte[]> ComputeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        var digest = new SM3Digest();
        var buffer = new byte[81920];
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0)
                    return Finish(digest);
                digest.BlockUpdate(buffer, 0, count);
            }
        }
        finally { GmCryptographicOperationsCompat.ZeroMemory(buffer); }
    }

    /// <summary>计算小写十六进制 SM3 摘要。</summary>
    public static string ComputeHex(byte[] data) => HexEncoding.Encode(Compute(data));

    /// <summary>计算 Base64 SM3 摘要。</summary>
    public static string ComputeBase64(byte[] data) => Convert.ToBase64String(Compute(data));

    /// <summary>异步计算文件的小写十六进制 SM3 摘要。</summary>
    public static async Task<string> ComputeFileHexAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("文件路径不能为空。", nameof(path));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return HexEncoding.Encode(await ComputeAsync(stream, cancellationToken).ConfigureAwait(false));
    }

    private static byte[] Finish(SM3Digest digest)
    {
        var result = new byte[DigestSize];
        digest.DoFinal(result, 0);
        return result;
    }
}