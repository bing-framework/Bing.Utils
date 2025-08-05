using Bing.Date;

namespace Bing.IdUtils;

/// <summary>
/// 模型ID访问器，提供线程安全的索引生成和时间戳管理功能
/// </summary>
/// <remarks>
/// 此类主要用于生成唯一的模型标识符，包含以下功能：
/// <para>1. 线程安全的递增索引生成</para>
/// <para>2. 基于不重复时间戳的时间点管理</para>
/// <para>3. 支持手动刷新时间点以确保时间序列的单调性</para>
/// <para>常用场景：数据库实体ID生成、批次处理标识、时序数据标记等</para>
/// </remarks>
public sealed class ModelIdAccessor
{
    /// <summary>
    /// 索引操作的同步锁对象
    /// </summary>
    private readonly object _indexLock = new();

    /// <summary>
    /// 时间戳操作的同步锁对象
    /// </summary>
    private readonly object _timestampLock = new();

    /// <summary>
    /// 不重复时间戳工厂实例
    /// </summary>
    private readonly NoRepeatTimeStampFactory _timestampFactory = new();

    /// <summary>
    /// 当前索引值，用于生成递增的序号
    /// </summary>
    private int _currentIndex;

    /// <summary>
    /// 当前时间点，基于不重复时间戳生成
    /// </summary>
    private DateTime _currentTimeStamp;

    /// <summary>
    /// 初始化一个<see cref="ModelIdAccessor"/>类型的实例
    /// </summary>
    /// <remarks>
    /// 构造函数会立即从时间戳工厂获取初始时间点，确保实例创建时的时间一致性。
    /// </remarks>
    public ModelIdAccessor()
    {
        _currentTimeStamp = _timestampFactory.GetTimeStamp();
        _currentIndex = 0;
    }

    /// <summary>
    /// 初始化一个<see cref="ModelIdAccessor"/>类型的实例
    /// </summary>
    ///  <param name="initialIndex">初始索引值，必须大于等于0</param>
    /// <exception cref="ArgumentOutOfRangeException">当initialIndex小于0时抛出</exception>
    /// <remarks>
    /// 此构造函数允许指定起始索引，适用于需要从特定数值开始计数的场景。
    /// </remarks>
    /// <example>
    /// <code>
    /// // 从索引1000开始计数
    /// var accessor = new ModelIdAccessor(1000);
    /// int nextIndex = accessor.GetNextIndex(); // 返回1000
    /// </code>
    /// </example>
    public ModelIdAccessor(int initialIndex)
    {
        if (initialIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(initialIndex), initialIndex, "初始索引值不能为负数");
        _currentTimeStamp = _timestampFactory.GetTimeStamp();
        _currentIndex = initialIndex;
    }

    /// <summary>
    /// 获取下一个递增索引值
    /// </summary>
    /// <returns>递增的整数索引，从初始值开始依次递增</returns>
    /// <remarks>
    /// <para>此方法是线程安全的，多个线程同时调用时会返回不同的递增值。</para>
    /// <para>索引值会持续递增，不会重置，直到达到int.MaxValue后溢出。</para>
    /// <para>适用场景：批次编号、序列号生成、排序标识等。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var accessor = new ModelIdAccessor();
    /// int first = accessor.GetNextIndex();   // 返回0
    /// int second = accessor.GetNextIndex();  // 返回1
    /// int third = accessor.GetNextIndex();   // 返回2
    /// </code>
    /// </example>
    public int GetNextIndex()
    {
        lock (_indexLock)
            return _currentIndex++;
    }

    /// <summary>
    /// 获取当前时间点
    /// </summary>
    /// <returns>当前记录的时间戳</returns>
    /// <remarks>
    /// <para>返回的时间点基于不重复时间戳工厂生成，具有单调递增特性。</para>
    /// <para>此方法不会更新时间点，如需获取最新时间请调用RefreshTimeStamp()方法。</para>
    /// <para>适用于需要获取固定时间基准的场景，如批量数据的统一时间标记。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var accessor = new ModelIdAccessor();
    /// DateTime timePoint = accessor.GetCurrentTimeStamp();
    /// Console.WriteLine($"当前时间点: {timePoint:yyyy-MM-dd HH:mm:ss.fff}");
    /// </code>
    /// </example>
    public DateTime GetCurrentTimeStamp() => _currentTimeStamp;

    /// <summary>
    /// 刷新时间点为当前最新的不重复时间戳
    /// </summary>
    /// <returns>刷新后的新时间戳</returns>
    /// <remarks>
    /// <para>此方法会从时间戳工厂获取新的时间戳并更新内部状态。</para>
    /// <para>新时间戳保证不会早于之前的任何时间戳，具有严格的时间序列特性。</para>
    /// <para>适用场景：周期性更新时间基准、确保时间点的实时性等。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var accessor = new ModelIdAccessor();
    /// DateTime oldTime = accessor.GetCurrentTimeStamp();
    /// Thread.Sleep(100);
    /// DateTime newTime = accessor.RefreshTimeStamp();
    /// Console.WriteLine($"时间更新: {oldTime:HH:mm:ss.fff} -> {newTime:HH:mm:ss.fff}");
    /// </code>
    /// </example>
    public DateTime RefreshTimeStamp()
    {
        lock (_timestampLock)
        {
            _currentTimeStamp = _timestampFactory.GetTimeStamp();
            return _currentTimeStamp;
        }
    }

    /// <summary>
    /// 重置索引到指定值
    /// </summary>
    /// <param name="newIndex">新的索引值，必须大于等于0</param>
    /// <exception cref="ArgumentOutOfRangeException">当newIndex小于0时抛出</exception>
    /// <remarks>
    /// <para>此方法允许重置索引计数器到指定值，后续GetNextIndex()将从该值开始递增。</para>
    /// <para>操作是线程安全的，但建议在单线程环境下调用以避免逻辑混乱。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var accessor = new ModelIdAccessor();
    /// accessor.GetNextIndex(); // 返回0
    /// accessor.GetNextIndex(); // 返回1
    /// accessor.ResetIndex(100);
    /// accessor.GetNextIndex(); // 返回100
    /// </code>
    /// </example>
    public void ResetIndex(int newIndex)
    {
        if (newIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(newIndex), newIndex, "索引值不能为负数");
        lock (_indexLock) 
            _currentIndex = newIndex;
    }

    /// <summary>
    /// 获取当前索引值（不递增）
    /// </summary>
    /// <returns>当前索引值</returns>
    /// <remarks>
    /// 此方法返回当前索引值但不会使其递增，用于查看下一次GetNextIndex()将返回的值。
    /// </remarks>
    /// <example>
    /// <code>
    /// var accessor = new ModelIdAccessor();
    /// int current = accessor.GetCurrentIndex(); // 返回0
    /// int next = accessor.GetNextIndex();       // 返回0并递增
    /// int newCurrent = accessor.GetCurrentIndex(); // 返回1
    /// </code>
    /// </example>
    public int GetCurrentIndex()
    {
        lock (_indexLock)
            return _currentIndex;
    }

    /// <summary>
    /// 生成组合ID字符串，包含时间戳和索引信息
    /// </summary>
    /// <param name="prefix">可选的前缀字符串</param>
    /// <returns>格式为"前缀_时间戳_索引"的组合ID字符串</returns>
    /// <remarks>
    /// <para>此方法会自动获取下一个索引并使用当前时间戳生成组合ID。</para>
    /// <para>时间戳格式为yyyyMMddHHmmssfff，索引使用6位数字补零。</para>
    /// <para>适用于需要包含时间信息的唯一标识符生成。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var accessor = new ModelIdAccessor();
    /// string id1 = accessor.GenerateCompositeId("ORDER"); // "ORDER_20241201143059123_000000"
    /// string id2 = accessor.GenerateCompositeId("BATCH"); // "BATCH_20241201143059123_000001"
    /// string id3 = accessor.GenerateCompositeId();        // "20241201143059123_000002"
    /// </code>
    /// </example>
    public string GenerateCompositeId(string prefix = null)
    {
        var timestamp = _currentTimeStamp.ToString("yyyyMMddHHmmssfff");
        var index = GetNextIndex().ToString("D6");
        return string.IsNullOrEmpty(prefix)
            ? $"{timestamp}_{index}"
            : $"{prefix}_{timestamp}_{index}";
    }
}