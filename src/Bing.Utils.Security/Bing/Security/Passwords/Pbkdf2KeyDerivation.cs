#if !NETSTANDARD2_0
using System.Security.Cryptography;
using System.Text;

namespace Bing.Security.Passwords;

/// <summary>
/// 提供从密码和调用方保存的随机盐派生加密密钥的操作。
/// </summary>
public static class Pbkdf2KeyDerivation
{
    /// <summary>
    /// 使用 PBKDF2-HMAC-SHA256 派生密钥。
    /// </summary>
    /// <param name="password">源密码；不应将同一密码和盐用于不同用途。</param>
    /// <param name="salt">由调用方保存的随机盐，至少 16 字节。</param>
    /// <param name="outputLength">派生密钥长度，必须大于零。</param>
    /// <param name="iterationCount">PBKDF2 迭代次数，必须不小于 100000。</param>
    /// <returns>派生密钥字节。</returns>
    public static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt, int outputLength, int iterationCount)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));
        if (salt.Length < 16)
            throw new ArgumentException("PBKDF2 盐必须至少为 16 字节。", nameof(salt));
        if (outputLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputLength), "派生密钥长度必须大于零。");
        if (iterationCount < 100000)
            throw new ArgumentOutOfRangeException(nameof(iterationCount), "PBKDF2 迭代次数必须不小于 100000。");

        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        var saltBytes = salt.ToArray();
        try
        {
            using var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, saltBytes, iterationCount, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(outputLength);
        }
        finally
        {
            CryptographicOperationsCompat.ZeroMemory(passwordBytes);
            CryptographicOperationsCompat.ZeroMemory(saltBytes);
        }
    }
}
#endif