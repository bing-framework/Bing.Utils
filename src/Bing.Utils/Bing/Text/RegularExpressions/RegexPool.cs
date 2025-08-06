using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Bing.Text.RegularExpressions;

/// <summary>
/// 正则表达式池 - 管理和缓存已编译的正则表达式对象
/// </summary>
public static class RegexPool
{
    #region 私有字段

    /// <summary>
    /// 正则表达式缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, CacheItem> Cache = new();

    /// <summary>
    /// 缓存最大容量，超过此值将触发清理机制
    /// </summary>
    private const int MaxCacheSize = 1000;

    /// <summary>
    /// 缓存命中统计计数器
    /// </summary>
    private static long _hitCount;

    /// <summary>
    /// 缓存未命中统计计数器
    /// </summary>
    private static long _missCount;

    /// <summary>
    /// 用于LRU清理的访问时间戳
    /// </summary>
    private static long _currentTimestamp;

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
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern), "正则表达式模式不能为空");

        var cacheKey = GenerateCacheKey(pattern, options);
        var currentTime = Interlocked.Increment(ref _currentTimestamp);

        // 首先尝试获取已存在的缓存项
        if (Cache.TryGetValue(cacheKey, out var cachedItem))
        {
            Interlocked.Increment(ref _hitCount);
            Cache.TryUpdate(cacheKey, cachedItem with { LastAccessTime = currentTime }, cachedItem);
            return cachedItem.Regex;
        }

        // 如果不存在，尝试创建并添加
        // 使用一个标志来确保统计的准确性
        var wasAdded = false;
        var newItem = Cache.GetOrAdd(cacheKey, _ =>
        {
            wasAdded = true;

            // 如果缓存达到最大容量，使用LRU策略清理
            if (Cache.Count >= MaxCacheSize)
                CleanupLRU();

            try
            {
                var regex = new Regex(pattern, options | RegexOptions.Compiled);
                return new CacheItem(regex, currentTime);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"无效的正则表达式模式: {pattern}", nameof(pattern), ex);
            }
        });

        // 只有真正添加了新项时才增加miss计数
        if (wasAdded)
        {
            Interlocked.Increment(ref _missCount);
        }
        else
        {
            // 如果没有添加（说明其他线程已经添加了），这是一次hit
            Interlocked.Increment(ref _hitCount);
        }

        return newItem.Regex;
    }

    /// <summary>
    /// 生成缓存键，统一缓存键的生成逻辑
    /// </summary>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="options">正则表达式选项</param>
    /// <returns>生成的缓存键</returns>
    private static string GenerateCacheKey(string pattern, RegexOptions options) => $"{pattern}:{(int)options}";

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
                .OrderBy(kvp => kvp.Value.LastAccessTime)
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
        Cache.Clear();
        Interlocked.Exchange(ref _hitCount, 0);
        Interlocked.Exchange(ref _missCount, 0);
        Interlocked.Exchange(ref _currentTimestamp, 0);
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
    /// 获取缓存命中总次数
    /// </summary>
    public static long HitCount => _hitCount;

    /// <summary>
    /// 获取缓存未命中总次数
    /// </summary>
    public static long MissCount => _missCount;

    /// <summary>
    /// 获取缓存命中率，范围为0.0到1.0
    /// </summary>
    public static double HitRate
    {
        get
        {
            var total = _hitCount + _missCount;
            return total == 0 ? 0 : (double)_hitCount / total;
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
        /// <summary>邮箱地址验证模式</summary>
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        /// <summary>中国大陆手机号码验证模式</summary>
        public const string MobilePhone = @"^1[3-9]\d{9}$";

        /// <summary>IPv4地址验证模式</summary>
        public const string IPv4 = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

        /// <summary>中文字符匹配模式</summary>
        public const string ChineseCharacters = @"[\u4e00-\u9fa5]";

        /// <summary>纯数字匹配模式</summary>
        public const string DigitsOnly = @"^\d+$";

        /// <summary>纯字母匹配模式</summary>
        public const string LettersOnly = @"^[a-zA-Z]+$";

        /// <summary>字母数字组合匹配模式</summary>
        public const string Alphanumeric = @"^[a-zA-Z0-9]+$";

        /// <summary>中国大陆身份证号码验证模式</summary>
        public const string IdCard = @"^\d{17}[\dXx]$";

        /// <summary>中国邮政编码验证模式</summary>
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
    /// 缓存项，包含正则表达式和访问时间戳
    /// </summary>
    private readonly record struct CacheItem
    {
        /// <summary>
        /// 缓存的正则表达式对象
        /// </summary>
        public Regex Regex { get; init; }

        /// <summary>
        /// 最后访问时间戳，用于LRU清理
        /// </summary>
        public long LastAccessTime { get; init; }

        /// <summary>
        /// 初始化缓存项
        /// </summary>
        /// <param name="regex">正则表达式对象</param>
        /// <param name="lastAccessTime">最后访问时间戳</param>
        public CacheItem(Regex regex, long lastAccessTime)
        {
            Regex = regex;
            LastAccessTime = lastAccessTime;
        }
    }

    #endregion
}