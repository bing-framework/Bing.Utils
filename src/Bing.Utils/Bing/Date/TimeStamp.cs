using System.Globalization;

namespace Bing.Date;

/// <summary>
/// 基于 .NET Ticks 的时间戳类，提供时间与时间戳之间的双向转换功能
/// </summary>
/// <remarks>
/// <para>此类使用 .NET DateTime.Ticks 作为时间戳的基础单位，具有以下特点：</para>
/// <para>- 精度：100纳秒（.NET Ticks 的最小单位）</para>
/// <para>- 范围：0001年1月1日至9999年12月31日</para>
/// <para>- 用途：适用于需要高精度时间记录的场景</para>
/// <para>- 与Unix时间戳的区别：本类使用Ticks，Unix时间戳使用秒/毫秒</para>
/// <para>常用场景：日志记录、数据审计、时间序列分析、性能监控等</para>
/// </remarks>
public class TimeStamp : IEquatable<TimeStamp>, IComparable<TimeStamp>
{
    /// <summary>
    /// 内部时间戳值（以.NET Ticks为单位）
    /// </summary>
    // ReSharper disable once InconsistentNaming
    protected long m_timestamp;

    /// <summary>
    /// 内部时间值
    /// </summary>
    // ReSharper disable once InconsistentNaming
    protected DateTime m_datetime;

    #region 构造函数

    /// <summary>
    /// 初始化一个<see cref="TimeStamp"/>类型的实例
    /// </summary>
    /// <remarks>
    /// 此构造函数创建一个表示当前时间的时间戳对象。
    /// </remarks>
    /// <example>
    /// <code>
    /// var timeStamp = new TimeStamp();
    /// Console.WriteLine($"当前时间戳: {timeStamp.ToTimestamp()}");
    /// </code>
    /// </example>
    public TimeStamp() : this(DateTime.Now) { }

    /// <summary>
    /// 初始化一个<see cref="TimeStamp"/>类型的实例
    /// </summary>
    /// <param name="timestamp">时间戳值（.NET Ticks）</param>
    /// <exception cref="ArgumentOutOfRangeException">当timestamp不在有效范围内时抛出</exception>
    /// <remarks>
    /// 将时间戳（Ticks）转换为 DateTime 对象并创建时间戳实例。
    /// 有效范围：0 到 DateTime.MaxValue.Ticks
    /// </remarks>
    /// <example>
    /// <code>
    /// long ticks = DateTime.Now.Ticks;
    /// var timeStamp = new TimeStamp(ticks);
    /// Console.WriteLine($"时间: {timeStamp.ToDateTime():yyyy-MM-dd HH:mm:ss}");
    /// </code>
    /// </example>
    public TimeStamp(long timestamp) : this(FromTimestampFunc(timestamp), timestamp) { }

    /// <summary>
    /// 初始化一个<see cref="TimeStamp"/>类型的实例
    /// </summary>
    /// <param name="dt">时间对象</param>
    /// <remarks>
    /// 将 DateTime 对象转换为时间戳并创建时间戳实例。
    /// </remarks>
    /// <example>
    /// <code>
    /// var specific = new DateTime(2024, 1, 1, 12, 0, 0);
    /// var timeStamp = new TimeStamp(specific);
    /// Console.WriteLine($"时间戳: {timeStamp.ToTimestamp()}");
    /// </code>
    /// </example>
    public TimeStamp(DateTime dt) : this(dt, ToTimestampFunc(dt)) { }

    /// <summary>
    /// 初始化一个<see cref="TimeStamp"/>类型的实例
    /// </summary>
    /// <param name="dt">时间对象</param>
    /// <param name="timestamp">时间戳值</param>
    /// <remarks>
    /// 此构造函数允许派生类直接设置时间和时间戳值，用于优化性能和保持数据一致性。
    /// </remarks>
    protected TimeStamp(DateTime dt, long timestamp)
    {
        m_datetime = dt;
        m_timestamp = timestamp;
    }

    #endregion

    #region 核心转换方法

    /// <summary>
    /// 转换为时间对象
    /// </summary>
    /// <returns>对应的 DateTime 对象</returns>
    /// <remarks>
    /// 返回当前时间戳对应的 DateTime 对象，保持原有的时区信息。
    /// </remarks>
    /// <example>
    /// <code>
    /// var timeStamp = new TimeStamp();
    /// DateTime dateTime = timeStamp.ToDateTime();
    /// Console.WriteLine($"转换后的时间: {dateTime:yyyy-MM-dd HH:mm:ss.fff}");
    /// </code>
    /// </example>
    public virtual DateTime ToDateTime() => m_datetime;

    /// <summary>
    /// 转换为时间戳值
    /// </summary>
    /// <returns>时间戳值（.NET Ticks）</returns>
    /// <remarks>
    /// 返回当前时间对应的时间戳值，以 .NET Ticks 为单位。
    /// 1 Tick = 100 纳秒
    /// </remarks>
    /// <example>
    /// <code>
    /// var timeStamp = new TimeStamp();
    /// long ticks = timeStamp.ToTimestamp();
    /// Console.WriteLine($"时间戳 Ticks: {ticks}");
    /// </code>
    /// </example>
    public virtual long ToTimestamp() => m_timestamp;

    #endregion

    #region 静态转换方法

    /// <summary>
    /// 将时间对象转换为时间戳
    /// </summary>
    /// <returns>时间戳值（.NET Ticks）</returns>
    private static readonly Func<DateTime, long> ToTimestampFunc = time => time.Ticks;

    /// <summary>
    /// 将时间戳转换为时间对象
    /// </summary>
    /// <returns>对应的 DateTime 对象</returns>
    /// <exception cref="ArgumentOutOfRangeException">当timestamp超出有效范围时抛出</exception>
    private static readonly Func<long, DateTime> FromTimestampFunc = timestamp =>
    {
        if (timestamp < 0 || timestamp > DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(nameof(timestamp), $"时间戳值超出有效范围。有效范围：0 到 {DateTime.MaxValue.Ticks}");
        return new DateTime(timestamp);
    };

    #endregion

    #region 静态属性和方法

    /// <summary>
    /// 获取当前时间的时间戳
    /// </summary>
    /// <returns>当前本地时间的时间戳值</returns>
    /// <remarks>
    /// 提供快速获取当前时间戳的静态方法，使用本地时间。
    /// </remarks>
    /// <example>
    /// <code>
    /// long currentTimestamp = TimeStamp.Now();
    /// Console.WriteLine($"当前时间戳: {currentTimestamp}");
    /// </code>
    /// </example>
    public static long Now() => ToTimestampFunc(DateTime.Now);

    /// <summary>
    /// 获取当前UTC时间的时间戳
    /// </summary>
    /// <returns>当前UTC时间的时间戳值</returns>
    /// <remarks>
    /// 提供快速获取当前UTC时间戳的静态方法，不受时区影响。
    /// </remarks>
    /// <example>
    /// <code>
    /// long utcTimestamp = TimeStamp.UtcNow();
    /// Console.WriteLine($"当前UTC时间戳: {utcTimestamp}");
    /// </code>
    /// </example>
    public static long UtcNow() => ToTimestampFunc(DateTime.UtcNow);

    /// <summary>
    /// 创建表示当前时间的时间戳对象
    /// </summary>
    /// <returns>表示当前时间的时间戳实例</returns>
    public static TimeStamp CreateNow() => new(DateTime.Now);

    /// <summary>
    /// 创建表示当前UTC时间的时间戳对象
    /// </summary>
    /// <returns>表示当前UTC时间的时间戳实例</returns>
    public static TimeStamp CreateUtcNow() => new(DateTime.UtcNow);

    #endregion

    #region 实用工具方法

    /// <summary>
    /// 获取时间戳与指定时间戳之间的差值
    /// </summary>
    /// <param name="other">要比较的时间戳</param>
    /// <returns>时间差（当前时间戳 - 指定时间戳）</returns>
    /// <exception cref="ArgumentNullException">当other为null时抛出</exception>
    /// <remarks>
    /// 返回值为正数表示当前时间戳较晚，为负数表示当前时间戳较早。
    /// </remarks>
    /// <example>
    /// <code>
    /// var ts1 = new TimeStamp(DateTime.Now);
    /// Thread.Sleep(1000);
    /// var ts2 = new TimeStamp(DateTime.Now);
    /// long diff = ts2.GetDifference(ts1);
    /// Console.WriteLine($"时间差: {diff} Ticks");
    /// </code>
    /// </example>
    public long GetDifference(TimeStamp other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        return m_timestamp - other.m_timestamp;
    }

    /// <summary>
    /// 获取时间戳与指定时间戳之间的时间间隔
    /// </summary>
    /// <param name="other">要比较的时间戳</param>
    /// <returns>时间间隔对象</returns>
    /// <exception cref="ArgumentNullException">当other为null时抛出</exception>
    public TimeSpan GetTimeSpan(TimeStamp other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        return TimeSpan.FromTicks(Math.Abs(GetDifference(other)));
    }

    /// <summary>
    /// 添加指定的时间间隔
    /// </summary>
    /// <param name="timeSpan">要添加的时间间隔</param>
    /// <returns>新的时间戳对象</returns>
    /// <example>
    /// <code>
    /// var timeStamp = new TimeStamp();
    /// var futureStamp = timeStamp.Add(TimeSpan.FromHours(1));
    /// Console.WriteLine($"一小时后: {futureStamp.ToDateTime():HH:mm:ss}");
    /// </code>
    /// </example>
    public virtual TimeStamp Add(TimeSpan timeSpan) => new(m_datetime.Add(timeSpan));

    /// <summary>
    /// 减去指定的时间间隔
    /// </summary>
    /// <param name="timeSpan">要减去的时间间隔</param>
    /// <returns>新的时间戳对象</returns>
    public virtual TimeStamp Subtract(TimeSpan timeSpan) => new(m_datetime.Subtract(timeSpan));

    #endregion

    #region 格式化方法

    /// <summary>
    /// 将时间戳格式化为字符串
    /// </summary>
    /// <param name="format">时间格式字符串，默认为"yyyy-MM-dd HH:mm:ss.fff"</param>
    /// <returns>格式化后的时间字符串</returns>
    /// <example>
    /// <code>
    /// var timeStamp = new TimeStamp();
    /// string formatted = timeStamp.ToString("yyyy-MM-dd HH:mm:ss");
    /// Console.WriteLine($"格式化时间: {formatted}");
    /// </code>
    /// </example>
    public string ToString(string format) => m_datetime.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// 将时间戳转换为字符串表示
    /// </summary>
    /// <returns>默认格式的时间字符串</returns>
    public override string ToString() => ToString("yyyy-MM-dd HH:mm:ss.fff");

    #endregion

    #region 相等性和比较实现

    /// <summary>
    /// 比较当前时间戳与另一个时间戳的大小关系
    /// </summary>
    /// <param name="other">要比较的时间戳</param>
    /// <returns>
    /// 小于0：当前时间戳早于指定时间戳
    /// 等于0：时间戳相等
    /// 大于0：当前时间戳晚于指定时间戳
    /// </returns>
    public int CompareTo(TimeStamp other)
    {
        if (other == null) 
            return 1;
        return m_timestamp.CompareTo(other.m_timestamp);
    }

    /// <summary>
    /// 检查当前时间戳是否与另一个时间戳相等
    /// </summary>
    /// <param name="other">要比较的时间戳</param>
    /// <returns>如果时间戳相等则返回true，否则返回false</returns>
    public bool Equals(TimeStamp other)
    {
        if (other == null) 
            return false;
        return m_timestamp == other.m_timestamp;
    }

    /// <summary>
    /// 检查当前时间戳是否与指定对象相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果对象是相等的时间戳则返回true，否则返回false</returns>
    public override bool Equals(object obj) => Equals(obj as TimeStamp);

    /// <summary>
    /// 获取当前时间戳的哈希码
    /// </summary>
    /// <returns>哈希码值</returns>
    public override int GetHashCode() => m_timestamp.GetHashCode();

    #endregion

    #region 运算符重载

    /// <summary>
    /// 时间戳相等运算符
    /// </summary>
    public static bool operator ==(TimeStamp left, TimeStamp right)
    {
        if (ReferenceEquals(left, right)) 
            return true;
        if (left is null || right is null) 
            return false;
        return left.Equals(right);
    }

    /// <summary>
    /// 时间戳不等运算符
    /// </summary>
    public static bool operator !=(TimeStamp left, TimeStamp right) => !(left == right);

    /// <summary>
    /// 时间戳小于运算符
    /// </summary>
    public static bool operator <(TimeStamp left, TimeStamp right)
    {
        if (left is null) 
            return right is not null;
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// 时间戳小于等于运算符
    /// </summary>
    public static bool operator <=(TimeStamp left, TimeStamp right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// 时间戳大于运算符
    /// </summary>
    public static bool operator >(TimeStamp left, TimeStamp right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    /// <summary>
    /// 时间戳大于等于运算符
    /// </summary>
    public static bool operator >=(TimeStamp left, TimeStamp right)
    {
        if (left is null) 
            return right is null;
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// 时间戳加法运算符
    /// </summary>
    public static TimeStamp operator +(TimeStamp timeStamp, TimeSpan timeSpan)
    {
        if (timeStamp == null) 
            throw new ArgumentNullException(nameof(timeStamp));
        return timeStamp.Add(timeSpan);
    }

    /// <summary>
    /// 时间戳减法运算符
    /// </summary>
    public static TimeStamp operator -(TimeStamp timeStamp, TimeSpan timeSpan)
    {
        if (timeStamp == null) 
            throw new ArgumentNullException(nameof(timeStamp));
        return timeStamp.Subtract(timeSpan);
    }

    /// <summary>
    /// 时间戳差值运算符
    /// </summary>
    public static TimeSpan operator -(TimeStamp left, TimeStamp right)
    {
        if (left == null) 
            throw new ArgumentNullException(nameof(left));
        if (right == null) 
            throw new ArgumentNullException(nameof(right));
        return TimeSpan.FromTicks(left.GetDifference(right));
    }

    #endregion

    /// <summary>
    /// 当前时间戳委托（兼容性）
    /// </summary>
    /// <remarks>此属性已过时，建议使用Now()静态方法</remarks>
    [Obsolete("请使用 TimeStamp.Now() 静态方法替代此属性", false)]
    public static Func<long> NowTimeStamp = () => ToTimestampFunc(DateTime.Now);

    /// <summary>
    /// 当前UTC时间戳委托（兼容性）
    /// </summary>
    /// <remarks>此属性已过时，建议使用UtcNow()静态方法</remarks>
    [Obsolete("请使用 TimeStamp.UtcNow() 静态方法替代此属性", false)]
    public static Func<long> UtcNowTimeStamp = () => ToTimestampFunc(DateTime.UtcNow);
}