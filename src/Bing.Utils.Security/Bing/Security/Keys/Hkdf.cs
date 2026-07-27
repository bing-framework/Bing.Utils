using System.Security.Cryptography;

namespace Bing.Security.Keys;

/// <summary>
/// 提供 RFC 5869 定义的 HKDF-SHA256 密钥派生操作。
/// </summary>
public static class Hkdf
{
    /// <summary>
    /// SHA-256 摘要长度，单位为字节。
    /// </summary>
    private const int HashLength = 32;

    /// <summary>
    /// 使用 HKDF-SHA256 从输入密钥材料、盐和用途上下文派生密钥。
    /// </summary>
    /// <param name="inputKeyMaterial">输入密钥材料，不能为空。</param>
    /// <param name="salt">盐；空值使用 RFC 5869 定义的全零盐。</param>
    /// <param name="info">绑定派生用途的上下文字节。</param>
    /// <param name="outputLength">要派生的密钥长度，必须介于 1 和 8160 字节之间。</param>
    /// <returns>派生的密钥字节。</returns>
    /// <exception cref="ArgumentException"><paramref name="inputKeyMaterial"/> 为空或 <paramref name="outputLength"/> 超出范围时抛出。</exception>
    public static byte[] DeriveKeySha256(ReadOnlySpan<byte> inputKeyMaterial, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info, int outputLength)
    {
        if (inputKeyMaterial.IsEmpty)
            throw new ArgumentException("HKDF 输入密钥材料不能为空。", nameof(inputKeyMaterial));
        if (outputLength <= 0 || outputLength > 255 * HashLength)
            throw new ArgumentOutOfRangeException(nameof(outputLength), "HKDF 输出长度必须介于 1 和 8160 字节之间。 ");

        var extractSalt = salt.IsEmpty ? new byte[HashLength] : salt.ToArray();
        var input = inputKeyMaterial.ToArray();
        var context = info.ToArray();
        byte[] pseudorandomKey = null;
        byte[] previous = Array.Empty<byte>();
        try
        {
            using (var extract = new HMACSHA256(extractSalt))
                pseudorandomKey = extract.ComputeHash(input);

            var output = new byte[outputLength];
            var offset = 0;
            for (var counter = 1; offset < output.Length; counter++)
            {
                var blockInput = new byte[previous.Length + context.Length + 1];
                try
                {
                    if (previous.Length > 0)
                        previous.CopyTo(blockInput, 0);
                    if (context.Length > 0)
                        context.CopyTo(blockInput, previous.Length);
                    blockInput[^1] = (byte)counter;

                    using var expand = new HMACSHA256(pseudorandomKey);
                    var current = expand.ComputeHash(blockInput);
                    CryptographicOperationsCompat.ZeroMemory(previous);
                    previous = current;
                    var copyLength = Math.Min(previous.Length, output.Length - offset);
                    previous.AsSpan(0, copyLength).CopyTo(output.AsSpan(offset));
                    offset += copyLength;
                }
                finally
                {
                    CryptographicOperationsCompat.ZeroMemory(blockInput);
                }
            }

            return output;
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(extractSalt);
            CryptographicOperationsCompat.ZeroMemory(input);
            CryptographicOperationsCompat.ZeroMemory(context);
            if (pseudorandomKey != null)
                CryptographicOperationsCompat.ZeroMemory(pseudorandomKey);
            CryptographicOperationsCompat.ZeroMemory(previous);
        }
    }
}