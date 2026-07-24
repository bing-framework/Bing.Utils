using System.Security.Cryptography;

namespace Bing.Security;

/// <summary>
/// 提供用于验证敏感数据的固定时间比较操作。
/// </summary>
public static class SecureComparison
{
    /// <summary>
    /// 使用固定时间比较两个字节序列。
    /// </summary>
    /// <param name="left">第一个字节序列。</param>
    /// <param name="right">第二个字节序列。</param>
    /// <returns>两个序列长度和内容均相同时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
#if NETSTANDARD2_0
        if (left.Length != right.Length)
            return false;

        var difference = 0;
        for (var index = 0; index < left.Length; index++)
            difference |= left[index] ^ right[index];
        return difference == 0;
#else
        return CryptographicOperations.FixedTimeEquals(left, right);
#endif
    }
}