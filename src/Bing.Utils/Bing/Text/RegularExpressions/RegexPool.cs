using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bing.Text.RegularExpressions;

/// <summary>
/// 正则表达式池 - 管理和缓存已编译的正则表达式对象
/// </summary>
public static partial class RegexPool
{
    #region 私有字段

    /// <summary>
    /// 正则表达式缓存
    /// </summary>
    private static readonly ConcurrentDictionary<CacheKey, CacheItem> Cache = new();

    /// <summary>
    /// 缓存最大容量，超过此值将触发清理机制
    /// </summary>
    private const int MaxCacheSize = 1000;

    /// <summary>协调冷插入、淘汰和清空；热命中不获取此锁。</summary>
    private static readonly object CacheGate = new();

    /// <summary>每次 Clear 创建独立统计代际，旧请求收尾不污染新统计。</summary>
    private static CacheStatistics _statistics = new();

    #endregion

    #region 核心池方法

    /// <summary>
    /// 从池中获取正则表达式对象，如果不存在则创建并缓存
    /// </summary>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>缓存的或新创建的正则表达式对象</returns>
    /// <exception cref="ArgumentNullException">当模式字符串为null时抛出</exception>
    /// <exception cref="ArgumentException">当正则表达式模式无效时抛出</exception>
    public static Regex GetOrCreate(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        => GetOrCreateCore(pattern, options, null);

    /// <summary>复用统一缓存实现；null 保留运行时为旧入口配置的默认超时。</summary>
    private static Regex GetOrCreateCore(string pattern, RegexOptions options, TimeSpan? matchTimeout, Func<Regex> factory = null)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern), "正则表达式模式不能为空");
        var key = GenerateCacheKey(pattern, options, matchTimeout);
        var created = false;
        if (!Cache.TryGetValue(key, out var item))
        {
            lock (CacheGate)
            {
                if (!Cache.TryGetValue(key, out item))
                {
                    if (Cache.Count >= MaxCacheSize) CleanupLRU();
                    item = CreateCacheItem(pattern, options, matchTimeout, factory, _statistics);
                    Cache[key] = item;
                    created = true;
                }
            }
        }
        try
        {
            var regex = item.Value.Value;
            Interlocked.Exchange(ref item.LastAccessTime, Interlocked.Increment(ref item.Statistics.Timestamp));
            if (created) Interlocked.Increment(ref item.Statistics.Misses);
            else Interlocked.Increment(ref item.Statistics.Hits);
            return regex;
        }
        catch
        {
            // 构造期间可能发生 Remove/Clear 及同键重建，只删除本次失败的项。
            ((ICollection<KeyValuePair<CacheKey, CacheItem>>)Cache).Remove(new KeyValuePair<CacheKey, CacheItem>(key, item));
            throw;
        }
    }

    /// <summary>仅冷路径创建闭包，避免热命中为工厂捕获参数分配对象。</summary>
    private static CacheItem CreateCacheItem(string pattern, RegexOptions options, TimeSpan? timeout,
        Func<Regex> factory, CacheStatistics statistics)
        => new(factory ?? (() => CreateRegex(pattern, options, timeout)), statistics);

    /// <summary>在缓存项的共享初始化中构造正则，保留既有异常包装。</summary>
    private static Regex CreateRegex(string pattern, RegexOptions options, TimeSpan? matchTimeout)
    {
        try
        {
            return matchTimeout.HasValue
                ? new Regex(pattern, options | RegexOptions.Compiled, matchTimeout.Value)
                : new Regex(pattern, options | RegexOptions.Compiled);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"无效的正则表达式模式: {pattern}", nameof(pattern), ex);
        }
    }

    /// <summary>实例级工厂接缝，用于验证单次初始化、失败重试和移除竞态，不替换全局构造器。</summary>
    internal static Regex GetOrCreateForTesting(string pattern, Func<Regex> factory)
        => GetOrCreateCore(pattern, RegexOptions.None, null, factory);

    /// <summary>
    /// 生成缓存键，统一缓存键的生成逻辑
    /// </summary>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <param name="matchTimeout">显式超时；null 表示旧入口的运行时默认值。</param>
    /// <returns>生成的缓存键</returns>
    private static CacheKey GenerateCacheKey(string pattern, RegexOptions options, TimeSpan? matchTimeout = null)
    {
        var cultureName = (options & RegexOptions.CultureInvariant) != 0
            ? CultureInfo.InvariantCulture.Name
            : CultureInfo.CurrentCulture.Name;
        return new CacheKey(pattern, options, cultureName, matchTimeout);
    }

    /// <summary>
    /// 使用LRU策略清理缓存，移除最久未使用的项目
    /// </summary>
    private static void CleanupLRU()
    {
        try
        {
            // 清理最旧的25%项目
            var itemsToRemove = Cache.Count / 4;
            if (itemsToRemove <= 0) return;

            var oldestItems = Cache
                .OrderBy(kvp => Interlocked.Read(ref kvp.Value.LastAccessTime))
                .Take(itemsToRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestItems)
                Cache.TryRemove(key, out _);
        }
        catch
        {
            // 如果LRU清理失败，退回到清空所有缓存
            Cache.Clear();
        }
    }

    #endregion

    #region 基础正则操作方法

    /// <summary>
    /// 判断字符串是否匹配指定正则表达式模式
    /// </summary>
    /// <param name="input">待匹配的输入字符串</param>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>如果输入字符串匹配模式则返回true，否则返回false</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为null时抛出</exception>
    public static bool IsMatch(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        if (useCache)
        {
            var regex = GetOrCreate(pattern, options);
            return regex.IsMatch(input);
        }

        return Regex.IsMatch(input, pattern, options);
    }

    /// <summary>
    /// 在输入字符串中搜索第一个匹配项
    /// </summary>
    /// <param name="input">待搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>第一个匹配项的Match对象，如无匹配则返回失败的Match对象</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为null时抛出</exception>
    public static Match Match(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        if (useCache)
        {
            var regex = GetOrCreate(pattern, options);
            return regex.Match(input);
        }

        return Regex.Match(input, pattern, options);
    }

    /// <summary>
    /// 在输入字符串中搜索所有匹配项
    /// </summary>
    /// <param name="input">待搜索的输入字符串</param>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>包含所有匹配项的MatchCollection集合</returns>
    /// <exception cref="ArgumentNullException">当输入字符串或模式为null时抛出</exception>
    public static MatchCollection Matches(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        if (useCache)
        {
            var regex = GetOrCreate(pattern, options);
            return regex.Matches(input);
        }

        return Regex.Matches(input, pattern, options);
    }

    /// <summary>
    /// 在输入字符串中替换所有匹配指定模式的子字符串
    /// </summary>
    /// <param name="input">待替换的输入字符串</param>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="replacement">替换字符串，可包含捕获组引用如$1、$2等</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>替换后的字符串</returns>
    /// <exception cref="ArgumentNullException">当模式或替换字符串为null时抛出</exception>
    public static string Replace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));

        if (useCache)
        {
            var regex = GetOrCreate(pattern, options);
            return regex.Replace(input, replacement);
        }

        return Regex.Replace(input, pattern, replacement, options);
    }

    /// <summary>
    /// 使用正则表达式模式分割输入字符串
    /// </summary>
    /// <param name="input">待分割的输入字符串</param>
    /// <param name="pattern">用作分隔符的正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>分割后的字符串数组</returns>
    /// <exception cref="ArgumentNullException">当模式为null时抛出</exception>
    public static string[] Split(string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        if (useCache)
        {
            var regex = GetOrCreate(pattern, options);
            return regex.Split(input);
        }

        return Regex.Split(input, pattern, options);
    }

    #endregion

    #region 高级功能方法

    /// <summary>
    /// 获取匹配值，支持结果模式转换
    /// </summary>
    /// <param name="input">待匹配的输入字符串</param>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="resultPattern">结果模式字符串，如"$1"用于获取第一个捕获组的值</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>匹配的值，如果未匹配或输入为空则返回空字符串</returns>
    public static string GetValue(string input, string pattern, string resultPattern = "", RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var match = Match(input, pattern, options, useCache);
        if (!match.Success)
            return string.Empty;
        if (string.IsNullOrWhiteSpace(resultPattern))
            return match.Value;

        try
        {
            var result = match.Result(resultPattern);

            // 检查是否是无效的分组引用
            if (IsInvalidGroupReference(result, resultPattern))
                return string.Empty;

            return result;
        }
        catch (ArgumentException)
        {
            // 如果结果模式无效，返回空字符串
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取多个匹配值，将结果组织为字典形式
    /// </summary>
    /// <param name="input">待匹配的输入字符串</param>
    /// <param name="pattern">正则表达式模式字符串</param>
    /// <param name="resultPatterns">结果模式字符串数组，如new[]{"$1","$2"}用于获取多个捕获组</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <param name="useCache">是否使用缓存，默认为true以提高性能</param>
    /// <returns>包含匹配值的字典，键为结果模式，值为对应的匹配结果</returns>
    public static Dictionary<string, string> GetValues(string input, string pattern, string[] resultPatterns, RegexOptions options = RegexOptions.IgnoreCase, bool useCache = true)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input))
            return result;

        var match = Match(input, pattern, options, useCache);
        if (!match.Success)
            return result;

        AddResults(result, match, resultPatterns);
        return result;
    }

    /// <summary>
    /// 检查是否是无效的分组引用
    /// </summary>
    /// <param name="result">Match.Result的返回值</param>
    /// <param name="resultPattern">原始的结果模式</param>
    /// <returns>如果是无效引用返回true，否则返回false</returns>
    private static bool IsInvalidGroupReference(string result, string resultPattern)
    {
        // 如果结果和原始模式相同，且包含$符号，说明没有被替换（可能是无效引用）
        if (result == resultPattern && result.Contains('$'))
        {
            // 进一步检查是否是明显的分组引用格式
            return IsGroupReferencePattern(resultPattern);
        }
        return false;
    }

    /// <summary>
    /// 检查字符串是否是分组引用模式
    /// </summary>
    /// <param name="pattern">要检查的模式</param>
    /// <returns>如果是分组引用模式返回true，否则返回false</returns>
    private static bool IsGroupReferencePattern(string pattern)
    {
        // 检查 $数字 格式 (如 $1, $99)
        if (Regex.IsMatch(pattern, @"^\$\d+$"))
            return true;

        // 检查 ${名称} 格式 (如 ${name}, ${invalid})
        if (Regex.IsMatch(pattern, @"^\$\{[^}]*\}$"))
            return true;

        // 检查 $名称 格式 (如 $name, $abc)
        if (Regex.IsMatch(pattern, @"^\$[a-zA-Z_]\w*$"))
            return true;

        return false;
    }

    /// <summary>
    /// 添加匹配结果到字典
    /// </summary>
    /// <param name="result">结果字典</param>
    /// <param name="match">匹配结果</param>
    /// <param name="resultPatterns">结果模式数组，范例：new[]{"$1","$2"}</param>
    private static void AddResults(Dictionary<string, string> result, Match match, string[] resultPatterns)
    {
        if (resultPatterns == null || resultPatterns.Length == 0)
        {
            result[string.Empty] = match.Value;
            return;
        }
        foreach (var resultPattern in resultPatterns)
        {
            if (string.IsNullOrEmpty(resultPattern))
                continue;

            // 避免重复键，如果键已存在则添加序号后缀
            var key = resultPattern;
            var counter = 1;
            while (result.ContainsKey(key))
                key = $"{resultPattern}_{counter++}";

            try
            {
                var matchResult = match.Result(resultPattern);

                // 检查是否是无效的分组引用
                result[key] = IsInvalidGroupReference(matchResult, resultPattern) ? string.Empty : matchResult;
            }
            catch (ArgumentException)
            {
                // 如果结果模式无效，使用空字符串
                result[key] = string.Empty;
            }
        }
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 清空所有正则表达式缓存并重置统计信息
    /// </summary>
    public static void Clear()
    {
        lock (CacheGate)
        {
            Cache.Clear();
            _statistics = new CacheStatistics();
        }
    }

    /// <summary>
    /// 移除指定模式的缓存项
    /// </summary>
    /// <param name="pattern">要移除的正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>如果成功移除返回true，否则返回false</returns>
    /// <exception cref="ArgumentNullException">当模式为null时抛出</exception>
    public static bool Remove(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        var cacheKey = GenerateCacheKey(pattern, options);
        return Cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// 检查指定模式是否已在缓存中
    /// </summary>
    /// <param name="pattern">要检查的正则表达式模式</param>
    /// <param name="options">正则表达式编译选项</param>
    /// <returns>如果模式已缓存返回true，否则返回false</returns>
    /// <exception cref="ArgumentNullException">当模式为null时抛出</exception>
    public static bool Contains(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        var cacheKey = GenerateCacheKey(pattern, options);
        return Cache.ContainsKey(cacheKey);
    }

    #endregion

    #region 统计信息

    /// <summary>
    /// 获取当前缓存中的正则表达式数量
    /// </summary>
    public static int Count => Cache.Count;

    /// <summary>
    /// 获取成功共享缓存项的总次数，包含等待同一项初始化的请求
    /// </summary>
    public static long HitCount => Interlocked.Read(ref _statistics.Hits);

    /// <summary>
    /// 获取成功创建缓存项的请求总次数，构造失败不计入
    /// </summary>
    public static long MissCount => Interlocked.Read(ref _statistics.Misses);

    /// <summary>
    /// 获取缓存命中率，范围为0.0到1.0
    /// </summary>
    public static double HitRate
    {
        get
        {
            var hits = HitCount;
            var total = hits + MissCount;
            return total == 0 ? 0 : (double)hits / total;
        }
    }

    /// <summary>
    /// 获取完整的缓存统计信息
    /// </summary>
    /// <returns>包含详细统计数据的PoolStatistics结构体</returns>
    public static PoolStatistics GetStatistics()
    {
        return new PoolStatistics
        {
            Count = Count,
            HitCount = HitCount,
            MissCount = MissCount,
            HitRate = HitRate,
            MaxSize = MaxCacheSize
        };
    }

    #endregion

    #region 常用正则表达式预定义

    /// <summary>
    /// 常用正则表达式模式常量定义
    /// </summary>
    public static class CommonPatterns
    {
        /// <summary>
        /// 邮箱地址验证模式
        /// </summary>
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        /// <summary>
        /// 中国大陆手机号码验证模式
        /// </summary>
        public const string MobilePhone = @"^1[3-9]\d{9}$";

        /// <summary>
        /// IPv4地址验证模式
        /// </summary>
        public const string IPv4 = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

        /// <summary>
        /// 中文字符匹配模式
        /// </summary>
        public const string ChineseCharacters = @"[\u4e00-\u9fa5]";

        /// <summary>
        /// 纯数字匹配模式
        /// </summary>
        public const string DigitsOnly = @"^\d+$";

        /// <summary>
        /// 纯字母匹配模式
        /// </summary>
        public const string LettersOnly = @"^[a-zA-Z]+$";

        /// <summary>
        /// 字母数字组合匹配模式
        /// </summary>
        public const string Alphanumeric = @"^[a-zA-Z0-9]+$";

        /// <summary>
        /// 中国大陆身份证号码验证模式
        /// </summary>
        public const string IdCard = @"^\d{17}[\dXx]$";

        /// <summary>
        /// 中国邮政编码验证模式
        /// </summary>
        public const string PostalCode = @"^\d{6}$";
    }

    /// <summary>
    /// 预编译所有常用正则表达式模式到缓存中，提高后续使用性能
    /// </summary>
    public static void PrecompileCommonPatterns()
    {
        try
        {
            var patterns = typeof(CommonPatterns)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null))
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            // 使用并行处理提高预编译速度
            Parallel.ForEach(patterns, pattern =>
            {
                try
                {
                    GetOrCreate(pattern);
                }
                catch (ArgumentException)
                {
                    // 忽略无效的正则表达式模式
                }
            });
        }
        catch
        {
            // 如果预编译失败，不影响主要功能
        }
    }

    #endregion

    #region 内部类型

    /// <summary>
    /// 缓存键包含文化及超时；旧入口与显式超时入口分别缓存。
    /// </summary>
    private readonly record struct CacheKey(string Pattern, RegexOptions Options, string CultureName, TimeSpan? MatchTimeout);

    /// <summary>
    /// 缓存项，包含正则表达式和访问时间戳
    /// </summary>
    private sealed class CacheItem
    {
        internal readonly Lazy<Regex> Value;
        internal readonly CacheStatistics Statistics;
        internal long LastAccessTime;

        internal CacheItem(Func<Regex> factory, CacheStatistics statistics)
        {
            Value = new Lazy<Regex>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
            Statistics = statistics;
            LastAccessTime = Interlocked.Increment(ref statistics.Timestamp);
        }
    }

    /// <summary>缓存代际统计，允许已清空代际的请求独立收尾。</summary>
    private sealed class CacheStatistics
    {
        internal long Hits;
        internal long Misses;
        internal long Timestamp;
    }

    #endregion
}
