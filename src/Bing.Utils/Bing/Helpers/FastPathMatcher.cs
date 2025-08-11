namespace Bing.Helpers;

/// <summary>
/// 快速路径匹配器，用于高效匹配路径模式
/// </summary>
/// <remarks>
/// 支持的通配符模式：<br />
/// - '*' : 匹配除路径分隔符 '/' 以外的任意字符序列<br />
/// - '**' : 匹配任意字符序列，包括路径分隔符 '/'<br />
/// - '?' : 匹配除路径分隔符 '/' 以外的单个字符<br /><br />
/// 
/// 使用场景：<br />
/// - URL 路径匹配<br />
/// - 文件路径通配符匹配<br />
/// - API 路径过滤<br />
/// - 静态资源路径匹配
/// </remarks>
/// <example>
/// <code>
/// // 精确匹配
/// bool result = FastPathMatcher.Match("/api/users", "/api/users"); // true
/// 
/// // 单级通配符
/// bool result = FastPathMatcher.Match("/api/*", "/api/users"); // true
/// bool result = FastPathMatcher.Match("/api/*", "/api/users/123"); // false
/// 
/// // 多级通配符
/// bool result = FastPathMatcher.Match("/api/**", "/api/users/123/profile"); // true
/// 
/// // 单字符通配符
/// bool result = FastPathMatcher.Match("/api/user?", "/api/users"); // true
/// 
/// // 组合使用
/// bool result = FastPathMatcher.Match("http://*/swagger/**", "http://localhost/swagger/index.html"); // true
/// </code>
/// </example>
public static class FastPathMatcher
{
    /// <summary>
    /// 路径分隔符
    /// </summary>
    private const char PathSeparator = '/';

    /// <summary>
    /// 单级通配符
    /// </summary>
    private const char SingleWildcard = '*';

    /// <summary>
    /// 单字符通配符
    /// </summary>
    private const char QuestionWildcard = '?';

    /// <summary>
    /// 空字符（用于安全访问）
    /// </summary>
    private const char NullChar = '\0';

    /// <summary>
    /// 判断路径是否匹配指定的模式
    /// </summary>
    /// <param name="pattern">匹配模式，支持 *、**、? 通配符</param>
    /// <param name="path">待匹配的路径</param>
    /// <returns>如果路径匹配模式返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 pattern 为 null 时抛出</exception>
    /// <remarks>
    /// 匹配规则：<br />
    /// - null 路径被视为不匹配<br />
    /// - 空字符串路径只能匹配空模式或纯通配符模式<br />
    /// - 匹配过程区分大小写<br />
    /// - '/' 被视为路径分隔符，具有特殊含义
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基本用法
    /// bool isMatch = FastPathMatcher.Match("/api/users/*", "/api/users/123");
    /// 
    /// // 处理 null 和空值
    /// bool isMatch1 = FastPathMatcher.Match("/api/*", null);        // false
    /// bool isMatch2 = FastPathMatcher.Match("", "");               // true
    /// bool isMatch3 = FastPathMatcher.Match("*", "");              // true
    /// </code>
    /// </example>
    public static bool Match(string pattern, string path)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern), "匹配模式不能为 null");
        if (path == null)
            return false;
        return DoMatch(pattern, 0, path, 0);
    }

    /// <summary>
    /// 批量匹配路径，检查路径是否匹配任一模式
    /// </summary>
    /// <param name="patterns">匹配模式数组</param>
    /// <param name="path">待匹配的路径</param>
    /// <returns>如果路径匹配任一模式返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 patterns 为 null 时抛出</exception>
    /// <remarks>
    /// 此方法会依次检查每个模式，找到第一个匹配项就立即返回 true。
    /// 如果所有模式都不匹配，则返回 false。
    /// </remarks>
    /// <example>
    /// <code>
    /// string[] patterns = { "/api/users/*", "/api/orders/*", "/swagger/**" };
    /// bool isMatch = FastPathMatcher.MatchAny(patterns, "/swagger/index.html"); // true
    /// </code>
    /// </example>
    public static bool MatchAny(string[] patterns, string path)
    {
        if (patterns == null)
            throw new ArgumentNullException(nameof(patterns), "匹配模式数组不能为 null");
        if (path == null)
            return false;
        foreach (var pattern in patterns)
        {
            if (pattern != null && Match(pattern, path))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 批量匹配路径，检查路径是否匹配所有模式
    /// </summary>
    /// <param name="patterns">匹配模式数组</param>
    /// <param name="path">待匹配的路径</param>
    /// <returns>如果路径匹配所有模式返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 patterns 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// string[] patterns = { "http://**", "http://localhost/swagger/**" };
    /// bool isMatch = FastPathMatcher.MatchAll(patterns, "http://localhost/swagger/index.html"); // true
    /// </code>
    /// </example>
    public static bool MatchAll(string[] patterns, string path)
    {
        if (patterns == null)
            throw new ArgumentNullException(nameof(patterns), "匹配模式数组不能为 null");
        if (path == null)
            return false;
        foreach (var pattern in patterns)
        {
            if (pattern == null || !Match(pattern, path))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 获取匹配的模式
    /// </summary>
    /// <param name="patterns">匹配模式数组</param>
    /// <param name="path">待匹配的路径</param>
    /// <returns>第一个匹配的模式，如果没有匹配则返回 null</returns>
    /// <exception cref="ArgumentNullException">当 patterns 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// string[] patterns = { "/api/users/*", "/api/orders/*", "/swagger/**" };
    /// string matched = FastPathMatcher.GetMatchedPattern(patterns, "/swagger/index.html"); // "/swagger/**"
    /// </code>
    /// </example>
    public static string GetMatchedPattern(string[] patterns, string path)
    {
        if (patterns == null)
            throw new ArgumentNullException(nameof(patterns), "匹配模式数组不能为 null");
        if (path == null)
            return null;
        foreach (var pattern in patterns)
        {
            if (pattern != null && Match(pattern, path))
                return pattern;
        }
        return null;
    }

    /// <summary>
    /// 验证模式格式是否正确
    /// </summary>
    /// <param name="pattern">要验证的模式</param>
    /// <returns>如果模式格式正确返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则：
    /// - null 模式被视为无效
    /// - 检查是否包含无效的通配符组合（如 ***）
    /// - 检查是否包含无效字符
    /// </remarks>
    /// <example>
    /// <code>
    /// bool isValid1 = FastPathMatcher.IsValidPattern("/api/**");     // true
    /// bool isValid2 = FastPathMatcher.IsValidPattern("/api/***");    // false
    /// </code>
    /// </example>
    public static bool IsValidPattern(string pattern)
    {
        if (pattern == null)
            return false;
        var consecutiveStars = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            if (ch == SingleWildcard)
            {
                consecutiveStars++;
                if (consecutiveStars > 2) // 超过两个连续的 * 被认为是无效的
                    return false;
            }
            else
            {
                consecutiveStars = 0;
            }
        }

        return true;
    }

    /// <summary>
    /// 执行实际的匹配操作
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="patternIndex">模式当前索引</param>
    /// <param name="path">路径字符串</param>
    /// <param name="pathIndex">路径当前索引</param>
    /// <returns>匹配结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DoMatch(string pattern, int patternIndex, string path, int pathIndex)
    {
        while (patternIndex < pattern.Length)
        {
            var patternChar = pattern[patternIndex];
            var pathChar = GetSafeChar(path, pathIndex);
            if (patternChar == SingleWildcard)
            {
                patternIndex++;

                // 检查是否是双通配符 **
                if (GetSafeChar(pattern, patternIndex) == SingleWildcard)
                {
                    patternIndex++;
                    return MatchMultipleSegments(pattern, patternIndex, path, pathIndex);
                }

                return MatchSingleSegment(pattern, patternIndex, path, pathIndex);
            }

            // 单字符通配符或精确匹配
            if ((patternChar == QuestionWildcard && pathChar != NullChar && pathChar != PathSeparator) 
                || patternChar == pathChar)
            {
                pathIndex++;
                patternIndex++;
                continue;
            }

            return false;
        }

        // 模式已匹配完，检查路径是否也匹配完
        return pathIndex == path.Length;
    }

    /// <summary>
    /// 匹配单个路径段（* 通配符）
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="patternIndex">模式当前索引</param>
    /// <param name="path">路径字符串</param>
    /// <param name="pathIndex">路径当前索引</param>
    /// <returns>匹配结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchSingleSegment(string pattern, int patternIndex, string path, int pathIndex)
    {
        var nextPatternChar = GetSafeChar(pattern, patternIndex);
        while (pathIndex <= path.Length)
        {
            var pathChar = GetSafeChar(path, pathIndex);
            // 遇到路径分隔符，检查模式的下一个字符
            if (pathChar == PathSeparator)
                return nextPatternChar == PathSeparator && DoMatch(pattern, patternIndex + 1, path, pathIndex + 1);
            // 尝试匹配剩余的模式
            if (DoMatch(pattern, patternIndex, path, pathIndex))
                return true;
            // 如果已到达路径末尾，无法继续匹配
            if (pathIndex >= path.Length)
                return false;
            pathIndex++;
        }

        return false;
    }

    /// <summary>
    /// 匹配多个路径段（** 通配符）
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="patternIndex">模式当前索引</param>
    /// <param name="path">路径字符串</param>
    /// <param name="pathIndex">路径当前索引</param>
    /// <returns>匹配结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchMultipleSegments(string pattern, int patternIndex, string path, int pathIndex)
    {
        // 如果模式已结束，检查路径是否以 / 结尾（特殊处理）
        if (patternIndex >= pattern.Length && pathIndex < path.Length)
            return path[path.Length - 1] != PathSeparator;
        while (pathIndex <= path.Length)
        {
            // 尝试匹配剩余的模式
            if (DoMatch(pattern, patternIndex, path, pathIndex))
                return true;
            // 如果已到达路径末尾，无法继续匹配
            if (pathIndex >= path.Length)
                return false;
            pathIndex++;
        }
        return false;
    }

    /// <summary>
    /// 安全地获取字符串指定位置的字符
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="index">索引位置</param>
    /// <returns>指定位置的字符，如果越界则返回空字符</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char GetSafeChar(string str, int index) => index >= str.Length ? NullChar : str[index];
}