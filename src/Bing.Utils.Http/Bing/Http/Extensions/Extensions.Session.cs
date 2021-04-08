using Bing.Utils.Json;
using Microsoft.AspNetCore.Http;

namespace Bing.Http.Extensions
{
    /// <summary>
    /// 会话(<see cref="ISession"/>) 扩展
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// 设置会话
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="session">会话</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void Set<T>(this ISession session, string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            session.Set(key, JsonHelper.ToJson(value));
        }

        /// <summary>
        /// 获取指定键名的值
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="session">会话</param>
        /// <param name="key">键名</param>
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
}
