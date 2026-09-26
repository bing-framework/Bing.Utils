using Bing.Utils.Json;
using Microsoft.AspNetCore.Http;

namespace Bing.Http.Extensions;

/// <summary>
/// 会话(<see cref="ISession"/>) 扩展
/// </summary>
public static class SessionExtensions
{
    /// <summary>
    /// 将对象序列化后写入会话。
    /// </summary>
    /// <param name="session">当前会话。</param>
    /// <param name="key">会话键；为空白时不写入。</param>
    /// <param name="value">要保存的对象。</param>
    /// <typeparam name="T">会话对象类型。</typeparam>
    public static void Set<T>(this ISession session, string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        session.SetString(key, JsonHelper.ToJson(value));
    }

    /// <summary>
    /// 读取并反序列化会话值。
    /// </summary>
    /// <param name="session">当前会话。</param>
    /// <param name="key">会话键。</param>
    /// <returns>反序列化后的对象；会话值为空白时返回 T 的默认值。</returns>
    /// <remarks>
    /// NETSTANDARD2_1 编译分支直接返回 T 的默认值。
    /// </remarks>
    /// <typeparam name="T">会话对象类型。</typeparam>
    public static T Get<T>(this ISession session, string key)
    {
#if NETSTANDARD2_1
            var value = string.Empty;
#else
        var value = session.GetString(key);
#endif
        return string.IsNullOrWhiteSpace(value) ? default : JsonHelper.ToObject<T>(value);
    }
}
