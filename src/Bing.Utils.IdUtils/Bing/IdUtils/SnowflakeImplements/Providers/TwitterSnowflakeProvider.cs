namespace Bing.IdUtils.SnowflakeImplements.Providers;

/// <summary>
/// 基于 Twitter 算法的雪花ID 提供程序
/// </summary>
internal class TwitterSnowflakeProvider : BaseSnowflakeProvider, ISnowflakeProvider
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
    const int WORKER_ID_BITS = 5;

    /// <summary>
    /// 数据中心标志位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int DATA_CENTER_ID_BITS = 5;

    /// <summary>
    /// 序列号标识位数
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int SEQUENCE_BITS = 12;

    /// <summary>
    /// 机器ID最大值，结果是31。
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long MAX_WORKER_ID = -1L ^ (-1L << WORKER_ID_BITS);

    /// <summary>
    /// 数据中心标志ID最大值，结果是31。
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long MAX_DATA_CENTER_ID = -1L ^ (-1L << DATA_CENTER_ID_BITS);

    /// <summary>
    /// 生成序列的掩码，这里为4095
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const long SEQUENCE_MASK = -1L ^ (-1L << SEQUENCE_BITS);

    /// <summary>
    /// 机器ID的偏移量(12)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo 
    const int WORKER_ID_SHIFT = SEQUENCE_BITS;

    /// <summary>
    /// 数据中心ID的偏移量(12+5)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int DATA_CENTER_ID_SHIFT = SEQUENCE_BITS + WORKER_ID_BITS;

    /// <summary>
    /// 时间戳的偏移量(5+5+12)
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    const int TIMESTAMP_LEFT_SHIFT = SEQUENCE_BITS + WORKER_ID_BITS + DATA_CENTER_ID_BITS;

    #endregion

    #region 字段

    /// <summary>
    /// 毫秒内序列(0 ~ 4095)
    /// </summary>
    private long _sequence = 0L;

    /// <summary>
    /// 上次生成ID的时间截
    /// </summary>
    private long _lastTimestamp = -1L;

    /// <summary>
    /// 同步锁
    /// </summary>
    private readonly object _lock = new();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="TwitterSnowflakeProvider"/>类型的实例
    /// </summary>
    /// <param name="workerId">机器ID，取值范围: 0 ~ 31</param>
    /// <param name="datacenterId">数据中心ID，取值范围: 0 ~ 31</param>
    /// <param name="sequence">毫秒内序列，取值范围: 0 ~ 4095</param>
    /// <exception cref="ArgumentException"></exception>
    public TwitterSnowflakeProvider(long workerId, long datacenterId, long sequence = 0L)
    {
        // 如果超出范围就抛出异常
        if (workerId > MAX_WORKER_ID || workerId < 0)
            throw new ArgumentException($"worker Id 必须大于0，且不能大于 MaxWorkerId：{MAX_WORKER_ID}");
        if (datacenterId > MAX_DATA_CENTER_ID || datacenterId < 0)
            throw new ArgumentException($"datacenter Id 必须大于0，且不能大于 MaxDatacenterId：{MAX_DATA_CENTER_ID}");

        // 先校验再赋值
        WorkerId = workerId;
        DatacenterId = datacenterId;
        _sequence = sequence;
    }

    #endregion

    #region 属性

    /// <summary>
    /// 工作机器ID(0 ~ 31)
    /// </summary>
    public long WorkerId { get; protected set; }

    /// <summary>
    /// 数据中心ID(0 ~ 31)
    /// </summary>
    public long DatacenterId { get; protected set; }

    /// <summary>
    /// 毫秒内序列(0 ~ 4095)
    /// </summary>
    public long Sequence
    {
        get => _sequence;
        internal set => _sequence = value;
    }

    #endregion

    /// <inheritdoc />
    public override long NextId()
    {
        lock (_lock) // 同步锁保证线程安全
        {
            var timestamp = TimeGen();

            // 系统时钟不允许回退
            // 说明：这种回拨机制判断仅限于运行时检查，如果关闭以后回拨系统时间，则生成的ID就同样存在风险。
            if (timestamp < _lastTimestamp)
                throw new ApplicationException($"时间戳必须大于上一次生成ID的时间戳，拒绝为{_lastTimestamp - timestamp}毫秒生成id");

            // 如果是同一时间生成的，则进行毫秒内序列生成
            if (_lastTimestamp == timestamp)
            {
                // sequence自增，和sequenceMask相与一下，去掉高位
                _sequence = (_sequence + 1) & SEQUENCE_MASK;
                // 判断是否溢出,也就是每毫秒内超过1024，当为1024时，与sequenceMask相与，sequence就等于0
                if (_sequence == 0)
                {
                    // 阻塞到下一个毫秒，获得新的时间戳
                    timestamp = TilNextMillis(_lastTimestamp);
                }
            }
            else
            {
                // 如果和上次生成时间不同,重置sequence，就是下一毫秒开始，sequence计数重新从0开始累加,
                // 为了保证尾数随机性更大一些,最后一位可以设置一个随机数
                _sequence = 0;//new Random().Next(10);
            }

            // 上次生成ID的时间截
            _lastTimestamp = timestamp;

            // 移位并通过或运算拼到一起组成64位的ID
            return ((timestamp - TWEPOCH) << TIMESTAMP_LEFT_SHIFT)  // 时间戳部分
                   | (DatacenterId << DATA_CENTER_ID_SHIFT)         // 数据中心部分
                   | (WorkerId << WORKER_ID_SHIFT)                  // 机器标识部分
                   | _sequence;                                     // 序列号部分
        }
    }

    /// <summary>
    /// 获取增量时间戳，防止产生的时间比之前的时间还要小（由于NTP回拨等问题），保持增量的趋势
    /// </summary>
    /// <param name="lastTimestamp">最后一个时间戳</param>
    protected virtual long TilNextMillis(long lastTimestamp)
    {
        var timestamp = TimeGen();
        while (timestamp <= lastTimestamp)
            timestamp = TimeGen();
        return timestamp;
    }
}