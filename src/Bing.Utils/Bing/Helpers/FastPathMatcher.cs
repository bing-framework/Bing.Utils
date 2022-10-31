namespace Bing.Helpers;

/// <summary>
/// 快速路径匹配器
/// </summary>
public class FastPathMatcher
{
    /// <summary>
    /// 是否匹配路径
    /// </summary>
    /// <param name="pattern">模式</param>
    /// <param name="path">路径</param>
    public static bool Match(string pattern, string path) => path != null && NormalMatch(pattern, 0, path, 0);

    /// <summary>
    /// 正常匹配
    /// </summary>
    /// <param name="pattern">模式</param>
    /// <param name="p">模式索引</param>
    /// <param name="str">字符串</param>
    /// <param name="s">字符串索引</param>
    private static bool NormalMatch(string pattern, int p, string str, int s)
    {
        while (p<pattern.Length)
        {
            var pc = pattern[p];
            var sc = SafeCharAt(str, s);
            if (pc == '*')
            {
                p++;
                if (SafeCharAt(pattern, p) == '*')
                {
                    p++;
                    return MultiWildcardMatch(pattern, p, str, s);
                }

                return WildcardMatch(pattern, p, str, s);
            }

            if ((pc == '?' && sc != 0 && sc != '/') || pc == sc)
            {
                s++;
                p++;
                continue;
            }

            return false;
        }

        return s == str.Length;
    }

    /// <summary>
    /// 通配符匹配
    /// </summary>
    /// <param name="pat">模式</param>
    /// <param name="p">模式索引</param>
    /// <param name="str">字符串</param>
    /// <param name="s">字符串索引</param>
    private static bool WildcardMatch(string pat, int p, string str, int s)
    {
        var pc = SafeCharAt(pat, p);
        while (true)
        {
            var sc = SafeCharAt(str, s);
            if (sc == '/')
                return pc == sc && NormalMatch(pat, p + 1, str, s + 1);

            if (!NormalMatch(pat, p, str, s))
            {
                if (s >= str.Length)
                    return false;
                s++;
                continue;
            }

            return true;
        }
    }

    /// <summary>
    /// 多通配符匹配
    /// </summary>
    /// <param name="pat">模式</param>
    /// <param name="p">模式索引</param>
    /// <param name="str">字符串</param>
    /// <param name="s">字符串索引</param>
    private static bool MultiWildcardMatch(string pat, int p, string str, int s)
    {
        if (p >= pat.Length && s < str.Length)
            return str[str.Length - 1] != '/';

        while (true)
        {
            if (!NormalMatch(pat, p, str, s))
            {
                if (s >= str.Length)
                    return false;
                s++;
                continue;
            }
            return true;
        }
    }

    /// <summary>
    /// 安全地根据索引获取字符
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="index">索引</param>
    private static char SafeCharAt(string value, int index)
    {
        if (index >= value.Length)
            return (char)0;
        return value[index];
    }
}