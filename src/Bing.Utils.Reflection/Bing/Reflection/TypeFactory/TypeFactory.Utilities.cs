using System.Security.Cryptography;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

#if !NETSTANDARD

// 类型工厂 - 工具类
internal static partial class TypeFactory
{
    /// <summary>
    /// 签名类型
    /// </summary>
    /// <param name="input">输入值</param>
    private static string SignType(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash);
    }
}

#endif
