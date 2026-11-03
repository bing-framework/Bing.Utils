namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 基于 Seata 算法的雪花ID 提供程序
/// </summary>
/// <remarks>参考链接: <see href="https://seata.apache.org/zh-cn/blog/seata-analysis-UUID-generator/"/></remarks>
internal class SeataSnowflakeProvider : BaseSnowflakeProvider, ISnowflakeProvider
{
    #region 常量

    /// <summary>
    /// 基准时间
    /// </summary>
    /// <remarks>开始时间截(毫秒) 2010-11-04 09:42:54</remarks>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public const long TWEPOCH = 1288834974657L;

    /// <summary>
    /// 机器标识位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int WORKER_ID_BITS = 10;

    /// <summary>
    /// 时间戳标识位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int TIMESTAMP_BITS = 41;

    /// <summary>
    /// 序列号标识位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int SEQUENCE_BITS = 12;

    /// <summary>
    /// 机器ID最大值，结果是1023。
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int MAX_WORKER_ID = ~(-1 << WORKER_ID_BITS);

    /// <summary>
    /// 生成时间戳和序列的掩码，这里为4095
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long TIMESTAMP_AND_SEQUENCE_MASK = ~(-1L << (TIMESTAMP_BITS + SEQUENCE_BITS));

    /// <summary>
    /// 41 位逻辑时间字段的最大值。
    /// </summary>
    const long MAX_TIMESTAMP = ~(-1L << TIMESTAMP_BITS);

    #endregion

    #region 字段

    /// <summary>
    /// 时间戳和序列号混合在一个长整型变量中 <br />
    /// 最高的11位：未使用 <br />
    /// 中间的41位：时间戳 <br />
    /// 最低的12位：序列号
    /// </summary>
    private long _timestampAndSequence;

    /// <summary>
    /// 同步锁
    /// </summary>
    private readonly object _lock = new();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个 <see cref="SeataSnowflakeProvider"/> 类型的实例。
    /// </summary>
    /// <param name="workerId">机器ID，取值范围: 0 ~ 1023</param>
    /// <exception cref="ArgumentException"></exception>
    public SeataSnowflakeProvider(long workerId)
    {
        Initialize(workerId);
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前节点使用的工作机器 ID。
    /// </summary>
    /// <remarks>内存布局为最高 1 位固定为 0、中间 10 位保存机器 ID、最低 53 位为 0。</remarks>
    public long WorkerId { get; protected set; }

    #endregion

    /// <inheritdoc />
    public override long NextId()
    {
        lock (_lock)
        {
            WaitIfNecessary();
            var timestampWithSequence = _timestampAndSequence & TIMESTAMP_AND_SEQUENCE_MASK;
            return WorkerId | timestampWithSequence;
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="workerId">机器ID</param>
    /// <exception cref="ArgumentException"></exception>
    private void Initialize(long workerId)
    {
        // 如果超出范围就抛出异常
        if (workerId > MAX_WORKER_ID || workerId < 0)
            throw new ArgumentException($"worker Id 必须大于等于0，且不能大于 MaxWorkerId：{MAX_WORKER_ID}");

        InitTimestampAndSequence();
        WorkerId = workerId << (TIMESTAMP_BITS + SEQUENCE_BITS);
    }

    /// <summary>
    /// 初始化时间戳和序列号
    /// </summary>
    private void InitTimestampAndSequence()
    {
        var timestamp = TimeGen();
        if (timestamp < 0 || timestamp > MAX_TIMESTAMP)
            throw new InvalidOperationException("当前时间超出 Seata 雪花算法的 41 位时间字段范围。");
        var timestampWithSequence = timestamp << SEQUENCE_BITS;
        _timestampAndSequence = timestampWithSequence;
    }

    /// <summary>
    /// 如果获取UUID的QPS过高导致当前序列空间耗尽，则阻塞当前线程
    /// </summary>
    private void WaitIfNecessary()
    {
        if ((_timestampAndSequence & TIMESTAMP_AND_SEQUENCE_MASK) == TIMESTAMP_AND_SEQUENCE_MASK)
            throw new InvalidOperationException("Seata 雪花算法的逻辑时间和序列空间已耗尽。");

        var currentWithSequence = ++_timestampAndSequence;
        var current = currentWithSequence >> SEQUENCE_BITS;
        if (current > MAX_TIMESTAMP)
            throw new InvalidOperationException("Seata 雪花算法的逻辑时间字段已耗尽。");
        var newest = TimeGen();
        if (current > newest)
            System.Threading.Thread.Sleep(5);
    }

    /// <summary>
    /// 为测试设置逻辑时间和序列状态，不属于生产调用契约。
    /// </summary>
    internal void SetTimestampAndSequenceForTests(long timestamp, long sequence)
    {
        if (timestamp < 0 || timestamp > MAX_TIMESTAMP)
            throw new ArgumentOutOfRangeException(nameof(timestamp));
        if (sequence < 0 || sequence > TIMESTAMP_AND_SEQUENCE_MASK >> TIMESTAMP_BITS)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        lock (_lock)
            _timestampAndSequence = (timestamp << SEQUENCE_BITS) | sequence;
    }

    /// <inheritdoc />
    /// <returns>经时钟偏移调整后的当前毫秒时间戳。</returns>
    protected override long TimeGen() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - TWEPOCH;
}
