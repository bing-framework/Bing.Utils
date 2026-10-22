using System.Security.Cryptography;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

#if !NETSTANDARD

// 类型工厂 - 工具类
internal static partial class TypeFactory
{
    /// <summary>
    /// 根据输入文本生成类型签名。
    /// </summary>
    /// <param name="input">用于生成签名的输入文本。</param>
    /// <returns>输入文本的 MD5 十六进制签名。</returns>
    private static string SignType(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash);
    }
}

#endif
