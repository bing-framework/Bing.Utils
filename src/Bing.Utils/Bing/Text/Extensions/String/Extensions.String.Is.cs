
// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 判断是否为大写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUpper(this string text) => Strings.IsUpper(text);

    /// <summary>
    /// 判断是否为小写。
    /// </summary>
    /// <param name="text">字符串</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLower(this string text) => Strings.IsLower(text);

    #region IsLike(通配符比较)

    /// <summary>
    /// 检查字符串是否匹配任一通配符模式
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="patterns">通配符模式集合，支持'*'作为通配符</param>
    /// <returns>
    /// 如果字符串与任一模式匹配，则返回 true；否则返回 false。
    /// 如果 patterns 为空或 null，则返回 false。
    /// </returns>
    /// <remarks>
    /// 通配符比较支持简单的'*'匹配，'*'可以匹配任意数量的字符（包括零个）。
    /// </remarks>
    /// <example>
    /// <code>
    /// "hello".IsLikeAny("h*", "world") => true
    /// "hello".IsLikeAny("a*", "b*") => false
    /// </code>
    /// </example>
    public static bool IsLikeAny(this string value, params string[] patterns)
    {
        if (patterns == null || patterns.Length == 0)
            return false;
        return patterns.Any(value.IsLike);
    }

    /// <summary>
    /// 检查字符串是否匹配指定的通配符模式
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="pattern">通配符模式，支持'*'作为通配符</param>
    /// <returns>
    /// 如果字符串与模式匹配，则返回 true；否则返回 false。
    /// </returns>
    /// <remarks>
    /// 通配符比较支持'*'作为通配符，可以匹配任意数量的字符（包括零个）。
    /// 目前仅支持'*'通配符，不支持'?'等其他通配符。
    /// 
    /// 匹配规则：
    /// 1. 如果字符串与模式完全相同，返回 true
    /// 2. 如果模式以'*'开头且模式长度大于1，则从字符串的每个位置尝试匹配剩余模式
    /// 3. 如果模式仅为'*'，则匹配任何字符串，返回 true
    /// 4. 如果字符串首字符与模式首字符相同，则递归匹配剩余部分
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 或 <paramref name="pattern"/> 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// "hello".IsLike("h*") => true
    /// "hello".IsLike("*llo") => true
    /// "hello".IsLike("h*o") => true
    /// "hello".IsLike("ha*") => false
    /// "hello".IsLike("*") => true
    /// "".IsLike("*") => true
    /// "hello".IsLike("hello") => true
    /// </code>
    /// </example>
    public static bool IsLike(this string value, string pattern)
    {
        // 参数验证
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        // 如果pattern为空串，只有value也为空串时才匹配
        if (pattern.Length == 0)
            return value.Length == 0;

        // 如果字符串与模式完全相同，返回true
        if (value == pattern)
            return true;

        // 如果模式以'*'开头
        if (pattern[0] == '*')
        {
            // 如果模式仅为'*'，则匹配任何字符串
            if (pattern.Length == 1)
                return true;

            // 从字符串的每个位置尝试匹配剩余模式
            return value.Where((_, index) => value.Substring(index).IsLike(pattern.Substring(1))).Any();
        }

        // 如果字符串为空但模式不是以'*'开头，不匹配
        if (value.Length == 0)
            return false;

        // 如果字符串首字符与模式首字符相同，递归匹配剩余部分
        if (pattern[0] == value[0])
            return value.Substring(1).IsLike(pattern.Substring(1));

        // 不匹配
        return false;
    }

    #endregion
}