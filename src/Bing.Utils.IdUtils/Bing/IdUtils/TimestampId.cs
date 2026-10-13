using Bing.Extensions;

namespace Bing.IdUtils;

/// <summary>
/// 时间戳ID，借鉴雪花算法，生成唯一时间戳ID
/// 参考文章：http://www.cnblogs.com/rjf1979/p/6282855.html
/// </summary>
public class TimestampId
{
    /// <summary>
    /// 上一次生成 ID 时采用的时间戳，用于保证后续时间戳递增。
    /// </summary>
    private long _lastTimestamp;
    /// <summary>
    /// 当前时间戳内已分配的序号。
    /// </summary>
    private long _sequence;//计数从零开始
    /// <summary>
    /// 生成时间戳时使用的可选起始时间。
    /// </summary>
    private readonly DateTime? _initialDateTime;
    /// <summary>
    /// 全局共享的时间戳 ID 生成器实例。
    /// </summary>
    private static TimestampId _timestampId;
    /// <summary>
    /// 序号填充宽度对应的最大值。
    /// </summary>
    private const int MAX_END_NUMBER = 9999;

    /// <summary>
    /// 初始化一个 <see cref="TimestampId"/> 类型的实例。
    /// </summary>
    /// <param name="initialDateTime">计算时间戳差值时使用的可选起始时间。</param>
    private TimestampId(DateTime? initialDateTime)
    {
        _initialDateTime = initialDateTime;
    }

    /// <summary>
    /// 获取单个实例对象。
    /// </summary>
    /// <param name="initialDateTime">计算时间戳差值时使用的可选起始时间。</param>
    /// <returns>使用指定起始时间初始化的共享实例。</returns>
    /// <returns></returns>
    public static TimestampId GetInstance(DateTime? initialDateTime = null)
    {
        if (initialDateTime.IsNull())
            Interlocked.CompareExchange(ref _timestampId, new TimestampId(initialDateTime), null);
        return _timestampId;
    }

    /// <summary>
    /// 获取用于计算时间戳差值的起始时间。
    /// </summary>
    protected DateTime InitialDateTime
    {
        get
        {
            if (_initialDateTime == null || _initialDateTime.Value == DateTime.MinValue)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return _initialDateTime.Value;
        }
    }

    /// <summary>
    /// 获取唯一时间戳 ID。
    /// </summary>
    /// <returns></returns>
    public string GetId()
    {
        long temp;
        var timestamp = GetUniqueTimeStamp(_lastTimestamp, out temp);
        return $"{timestamp}{Fill(temp)}";
    }

    /// <summary>
    /// 将序号补齐到固定宽度。
    /// </summary>
    /// <param name="temp">数字</param>
    /// <returns></returns>
    private string Fill(long temp)
    {
        var num = temp.ToString();
        IList<char> chars = new List<char>();
        for (int i = 0; i < MAX_END_NUMBER.ToString().Length - num.Length; i++)
        {
            chars.Add('0');
        }
        return new string(chars.ToArray()) + num;
    }

    /// <summary>
    /// 获取唯一时间戳
    /// </summary>
    /// <param name="lastTimeStamp">最后时间戳</param>
    /// <param name="temp">临时时间戳</param>
    /// <returns></returns>
    public long GetUniqueTimeStamp(long lastTimeStamp, out long temp)
    {
        lock (this)
        {
            temp = 1;
            var timeStamp = GetTimeStamp();
            if (timeStamp == _lastTimestamp)
            {
                _sequence = _sequence + 1;
                temp = _sequence;
                if (temp >= MAX_END_NUMBER)
                {
                    timeStamp = GetTimeStamp();
                    _lastTimestamp = timeStamp;
                    temp = _sequence = 1;
                }
            }
            else
            {
                _sequence = 1;
                _lastTimestamp = timeStamp;
            }
            return timeStamp;
        }
    }

    /// <summary>
    /// 获取时间戳
    /// </summary>
    /// <returns></returns>
    private long GetTimeStamp()
    {
        if (InitialDateTime >= DateTime.Now)
            throw new Exception("初始化时间比当前时间还大，不合理");
        var ts = DateTime.UtcNow - InitialDateTime;
        return (long)ts.TotalMilliseconds;
    }
}
