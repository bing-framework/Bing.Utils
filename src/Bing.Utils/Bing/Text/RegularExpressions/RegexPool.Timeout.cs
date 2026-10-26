using System.Text.RegularExpressions;

namespace Bing.Text.RegularExpressions;

public static partial class RegexPool
{
    /// <summary>获取具有显式匹配超时的正则；模式、选项、文化和超时相同才复用缓存。</summary>
    /// <param name="pattern">正则模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配的超时；须大于零且不超过 2147483646 毫秒，或使用 Regex.InfiniteMatchTimeout。</param>
    /// <returns>缓存的正则实例。</returns>
    /// <exception cref="ArgumentOutOfRangeException">超时无效。</exception>
    /// <remarks>超时不包含正则构造时间。旧重载仍使用运行时默认超时，通常为无限等待。</remarks>
    public static Regex GetOrCreate(string pattern, RegexOptions options, TimeSpan matchTimeout)
    {
        ValidateMatchTimeout(matchTimeout);
        return GetOrCreateCore(pattern, options, matchTimeout);
    }

    /// <summary>在显式超时内判断是否匹配；超时抛出 RegexMatchTimeoutException。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时，取值同 GetOrCreate 的显式超时重载。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>是否匹配。</returns>
    public static bool IsMatch(string input, string pattern, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
        => useCache ? GetOrCreate(pattern, options, matchTimeout).IsMatch(input) : Regex.IsMatch(input, pattern, options, matchTimeout);

    /// <summary>在显式超时内查找首个匹配；超时抛出 RegexMatchTimeoutException。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>首个匹配结果。</returns>
    public static Match Match(string input, string pattern, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
        => useCache ? GetOrCreate(pattern, options, matchTimeout).Match(input) : Regex.Match(input, pattern, options, matchTimeout);

    /// <summary>查找所有匹配；每次匹配受超时约束，惰性枚举时也可能抛出 RegexMatchTimeoutException。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时，不是整个集合枚举的总时间预算。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>匹配集合。</returns>
    public static MatchCollection Matches(string input, string pattern, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
        => useCache ? GetOrCreate(pattern, options, matchTimeout).Matches(input) : Regex.Matches(input, pattern, options, matchTimeout);

    /// <summary>使用显式匹配超时替换文本；空白输入仍返回空字符串。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="replacement">替换模板。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时；超时抛出 RegexMatchTimeoutException。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>替换后的文本。</returns>
    public static string Replace(string input, string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
    {
        ValidateMatchTimeout(matchTimeout);
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return useCache ? GetOrCreate(pattern, options, matchTimeout).Replace(input, replacement)
            : Regex.Replace(input, pattern, replacement, options, matchTimeout);
    }

    /// <summary>使用显式匹配超时分割文本；空白输入仍返回空数组。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">分隔模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时；超时抛出 RegexMatchTimeoutException。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>分割结果。</returns>
    public static string[] Split(string input, string pattern, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
    {
        ValidateMatchTimeout(matchTimeout);
        if (string.IsNullOrWhiteSpace(input)) return [];
        return useCache ? GetOrCreate(pattern, options, matchTimeout).Split(input) : Regex.Split(input, pattern, options, matchTimeout);
    }

    /// <summary>使用显式匹配超时获取匹配值；空白输入或无匹配时返回空字符串。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="resultPattern">结果模板，空字符串表示整个匹配。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时；超时抛出 RegexMatchTimeoutException。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>匹配值。</returns>
    public static string GetValue(string input, string pattern, string resultPattern, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
    {
        ValidateMatchTimeout(matchTimeout);
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var match = Match(input, pattern, options, matchTimeout, useCache);
        if (!match.Success) return string.Empty;
        if (string.IsNullOrWhiteSpace(resultPattern)) return match.Value;
        try
        {
            var result = match.Result(resultPattern);
            return IsInvalidGroupReference(result, resultPattern) ? string.Empty : result;
        }
        catch (ArgumentException) { return string.Empty; }
    }

    /// <summary>使用显式匹配超时获取多个结果模板的值。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="pattern">正则模式。</param>
    /// <param name="resultPatterns">结果模板列表。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">单次匹配超时；超时抛出 RegexMatchTimeoutException。</param>
    /// <param name="useCache">是否使用缓存。</param>
    /// <returns>模板到匹配结果的映射。</returns>
    public static Dictionary<string, string> GetValues(string input, string pattern, string[] resultPatterns, RegexOptions options, TimeSpan matchTimeout, bool useCache = true)
    {
        ValidateMatchTimeout(matchTimeout);
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input)) return result;
        var match = Match(input, pattern, options, matchTimeout, useCache);
        if (match.Success) AddResults(result, match, resultPatterns);
        return result;
    }

    /// <summary>移除当前文化和指定超时对应的缓存，不影响其他超时或旧重载的缓存。</summary>
    /// <param name="pattern">正则模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">缓存项的显式超时。</param>
    /// <returns>是否移除成功。</returns>
    public static bool Remove(string pattern, RegexOptions options, TimeSpan matchTimeout)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        ValidateMatchTimeout(matchTimeout);
        return Cache.TryRemove(GenerateCacheKey(pattern, options, matchTimeout), out _);
    }

    /// <summary>检查当前文化和指定超时对应的缓存是否存在。</summary>
    /// <param name="pattern">正则模式。</param>
    /// <param name="options">匹配选项。</param>
    /// <param name="matchTimeout">缓存项的显式超时。</param>
    /// <returns>是否存在。</returns>
    public static bool Contains(string pattern, RegexOptions options, TimeSpan matchTimeout)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        ValidateMatchTimeout(matchTimeout);
        return Cache.ContainsKey(GenerateCacheKey(pattern, options, matchTimeout));
    }

    /// <summary>与 Regex 构造函数保持相同的超时取值范围，避免将参数错误包装成模式错误。</summary>
    private static void ValidateMatchTimeout(TimeSpan matchTimeout)
    {
        if (matchTimeout != Regex.InfiniteMatchTimeout &&
            (matchTimeout <= TimeSpan.Zero || matchTimeout > TimeSpan.FromMilliseconds(int.MaxValue - 1)))
            throw new ArgumentOutOfRangeException(nameof(matchTimeout));
    }
}
