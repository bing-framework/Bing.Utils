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
    /// <param name="salt">由调用方保存的随机盐，长度介于 16 和 64 字节之间。</param>
    /// <param name="outputLength">派生密钥长度，介于 1 和 64 字节之间。</param>
    /// <param name="iterationCount">PBKDF2 迭代次数，介于 100000 和 1000000 之间。</param>
    /// <returns>派生密钥字节。</returns>
    public static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt, int outputLength, int iterationCount)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));
        if (salt.Length < 16 || salt.Length > Pbkdf2PasswordHasherOptions.MaximumSaltSize)
            throw new ArgumentException("PBKDF2 盐长度必须介于 16 和 64 字节之间。", nameof(salt));
        if (outputLength <= 0 || outputLength > Pbkdf2PasswordHasherOptions.MaximumHashSize)
            throw new ArgumentOutOfRangeException(nameof(outputLength), "派生密钥长度必须介于 1 和 64 字节之间。");
        if (iterationCount < 100000 || iterationCount > Pbkdf2PasswordHasherOptions.MaximumIterationCount)
            throw new ArgumentOutOfRangeException(nameof(iterationCount), "PBKDF2 迭代次数必须介于 100000 和 1000000 之间。");

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