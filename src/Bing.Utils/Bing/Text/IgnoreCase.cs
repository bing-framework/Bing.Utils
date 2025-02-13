namespace Bing.Text;

/// <summary>
/// 忽略大小写开关
/// </summary>
public enum IgnoreCase
{
    /// <summary>
    /// 忽略大小写
    /// </summary>
    True,

    /// <summary>
    /// 不忽略大小写
    /// </summary>
    False
}

/// <summary>
/// 忽略大小写开关 扩展
/// </summary>
internal static class IgnoreCaseExtensions
{
    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="ignoreCase">忽略大小写开关</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool X(this IgnoreCase ignoreCase)
    {
        return ignoreCase switch
        {
            IgnoreCase.True => true,
            IgnoreCase.False => false,
            _ => false
        };
    }

    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="b">布尔值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IgnoreCase X(this bool b)
    {
        return b switch
        {
            true => IgnoreCase.True,
            false => IgnoreCase.False
        };
    }
}

/// <summary>
/// 忽略大小写开关帮助类
/// </summary>
internal static class IgnoreCaseHelper
{
    /// <summary>
    /// 填充字符
    /// </summary>
    /// <param name="chars">字符数组</param>
    public static char[] FillChars(char[] chars)
    {
        var t = new char[chars.Length * 2];
        for (var i = 0; i < chars.Length; i++)
        {
            t[i * 2] = char.ToLowerInvariant(chars[i]);
            t[i * 2 + 1] = char.ToUpperInvariant(chars[i]);
        }
        return t;
    }
}