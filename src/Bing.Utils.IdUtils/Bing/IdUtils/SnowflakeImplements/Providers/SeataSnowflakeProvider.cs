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
    /// 初始化一个<see cref="SeataSnowflakeProvider"/>类型的实例
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
    /// 机器ID（0 ~ 1023）<br />
    /// 在内存中的实际布局：<br />
    /// 最高1位：0 <br />
    /// 中间10位：机器ID <br />
    /// 最低53位：全部为0
    /// </summary>
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
        InitTimestampAndSequence();
        // 如果超出范围就抛出异常
        if (workerId > MAX_WORKER_ID || workerId < 0)
            throw new ArgumentException($"worker Id 必须大于0，且不能大于 MaxWorkerId：{MAX_WORKER_ID}");
        WorkerId = workerId << (TIMESTAMP_BITS + SEQUENCE_BITS);
    }

    /// <summary>
    /// 初始化时间戳和序列号
    /// </summary>
    private void InitTimestampAndSequence()
    {
        var timestamp = TimeGen();
        var timestampWithSequence = timestamp << SEQUENCE_BITS;
        _timestampAndSequence = timestampWithSequence;
    }

    /// <summary>
    /// 如果获取UUID的QPS过高导致当前序列空间耗尽，则阻塞当前线程
    /// </summary>
    private void WaitIfNecessary()
    {
        var currentWithSequence = ++_timestampAndSequence;
        var current = currentWithSequence >> SEQUENCE_BITS;
        var newest = TimeGen();
        if (current > newest)
            System.Threading.Thread.Sleep(5);
    }

    /// <inheritdoc />
    protected override long TimeGen() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - TWEPOCH;
}