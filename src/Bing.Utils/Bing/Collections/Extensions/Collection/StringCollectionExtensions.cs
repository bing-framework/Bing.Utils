using System.Text;
using Bing.Text.Joiners;

// ReSharper disable once CheckNamespace
namespace Bing.Collections;

/// <summary>
/// 字符串扩展
/// </summary>
public static class StringCollectionExtensions
{
    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <param name="list">列表</param>
    public static string JoinToString(this IEnumerable<string> list) =>
        JoinToString<string>(list, ",", _ => true, s => s);

    /// <summary>
    /// 将集合合并为字符串，并使用指定的分隔符分割。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    public static string JoinToString(this IEnumerable<string> list, string delimiter) =>
        JoinToString<string>(list, delimiter, _ => true, s => s);

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="predicate">条件</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString(this IEnumerable<string> list, Func<string, bool> predicate, Func<string, string> replaceFunc = null) =>
        JoinToString<string>(list, ",", predicate, s => s, replaceFunc);

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="predicate">条件</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString(this IEnumerable<string> list, Func<string, int, bool> predicate, Func<string, int, string> replaceFunc = null) =>
        JoinToString<string>(list, ",", predicate, (s, _) => s, replaceFunc);

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString(this IEnumerable<string> list, string delimiter, Func<string, bool> predicate, Func<string, string> replaceFunc = null) =>
        JoinToString<string>(list, delimiter, predicate, s => s, replaceFunc);

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString(this IEnumerable<string> list, string delimiter, Func<string, int, bool> predicate, Func<string, int, string> replaceFunc = null) =>
        JoinToString<string>(list, delimiter, predicate, (s, _) => s, replaceFunc);

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    public static string JoinToString<T>(this IEnumerable<T> list) =>
        JoinToString<T>(list, ",", _ => true, t => $"{t}");

    /// <summary>
    /// 将集合合并为字符串，并使用指定的分隔符分割。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    public static string JoinToString<T>(this IEnumerable<T> list, string delimiter) =>
        JoinToString<T>(list, delimiter, _ => true, t => $"{t}");

    /// <summary>
    /// 将集合合并为字符串，并使用指定的分隔符分割。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString<T>(this IEnumerable<T> list, string delimiter, Func<T, bool> predicate, Func<T, T> replaceFunc = null) =>
        JoinToString<T>(list, delimiter, predicate, s => $"{s}", replaceFunc);

    /// <summary>
    /// 将集合合并为字符串，并使用指定的分隔符分割。
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="to">转换函数</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString<T>(this IEnumerable<T> list, string delimiter, Func<T, string> to, Func<T, T> replaceFunc = null) =>
        JoinToString<T>(list, delimiter, _ => true, to, replaceFunc);

    /// <summary>
    /// 将集合合并为字符串。默认：换行符。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    public static string JoinOnePerLine<T>(this IEnumerable<T> list) where T : IFormattable =>
        JoinToString(list, Environment.NewLine, _ => true, t => $"{t}") + Environment.NewLine;

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    public static string JoinToStringFormat<T>(this IEnumerable<T> list) where T : IFormattable =>
        JoinToStringFormat(list, ",");

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    public static string JoinToStringFormat<T>(this IEnumerable<T> list, string delimiter) where T : IFormattable =>
        JoinToStringFormat(list, delimiter, null);

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="provider">格式化提供程序</param>
    public static string JoinToStringFormat<T>(this IEnumerable<T> list, string delimiter, IFormatProvider provider) where T : IFormattable =>
        JoinToString(list, delimiter, _ => true, t => t.ToString(null, provider));

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="to">转换函数</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString<T>(this IEnumerable<T> list, string delimiter, Func<T, bool> predicate, Func<T, string> to, Func<T, T> replaceFunc = null)
    {
        if (list is null)
            return string.Empty;
        var sb = new StringBuilder();
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s), list, delimiter, predicate, to, replaceFunc);
        return sb.ToString();
    }

    /// <summary>
    /// 将集合合并为字符串。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="delimiter">分隔符</param>
    /// <param name="predicate">条件</param>
    /// <param name="to">转换函数</param>
    /// <param name="replaceFunc">替换函数</param>
    public static string JoinToString<T>(this IEnumerable<T> list, string delimiter, Func<T, int, bool> predicate, Func<T, int, string> to, Func<T, int, T> replaceFunc = null)
    {
        if (list is null)
            return string.Empty;
        var sb = new StringBuilder();
        CommonJoinUtils.JoinToString(sb, (c, s) => c.Append(s), list, delimiter, predicate, to, replaceFunc);
        return sb.ToString();
    }
}