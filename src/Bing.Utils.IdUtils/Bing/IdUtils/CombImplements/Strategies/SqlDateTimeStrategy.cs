using Bing.Numeric;

namespace Bing.IdUtils.CombImplements.Strategies;

/// <summary>
/// 基于 SQL 的 COMB 日期时间策略
/// </summary>
internal class SqlDateTimeStrategy : ICombDateTimeStrategy
{
    /// <summary>
    /// 每天对应的 SQL 日期时间单位数（每秒 300 个单位）。
    /// </summary>
    private const double TICKS_PER_DAY = 86_400d * 300d;

    /// <summary>
    /// 每毫秒对应的 SQL 日期时间单位数。
    /// </summary>
    private const double TICKS_PER_MILLISECOND = 3d / 10d;

    /// <inheritdoc />
    public int NumDateBytes => 6;

    /// <inheritdoc />
    public DateTime MinDateTimeValue { get; } = new(1900, 1, 1);

    /// <inheritdoc />
    public DateTime MaxDateTimeValue => MinDateTimeValue.AddDays(NumberConst.UShortMax);

    /// <inheritdoc />
    public byte[] DateTimeToBytes(DateTime timestamp)
    {
        var ticks = (int)(timestamp.TimeOfDay.TotalMilliseconds * TICKS_PER_MILLISECOND);
        var days = (ushort)(timestamp - MinDateTimeValue).TotalDays;
        var tickBytes = BitConverter.GetBytes(ticks);
        var dayBytes = BitConverter.GetBytes(days);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(dayBytes);
            Array.Reverse(tickBytes);
        }

        var ret = new byte[NumDateBytes];
        Array.Copy(dayBytes, 0, ret, 0, 2);
        Array.Copy(tickBytes, 0, ret, 2, 4);
        return ret;
    }

    /// <inheritdoc />
    public DateTime BytesToDateTime(byte[] value)
    {
        var dayBytes = new byte[2];
        var tickBytes = new byte[4];
        Array.Copy(value, 0, dayBytes, 0, 2);
        Array.Copy(value, 0, tickBytes, 2, 4);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(dayBytes);
            Array.Reverse(tickBytes);
        }

        var days = BitConverter.ToUInt16(dayBytes, 0);
        var ticks = BitConverter.ToInt32(tickBytes, 0);

        if (ticks < 0f)
            throw new ArgumentException("Not a COMB, time component is negative.");
        if (ticks > TICKS_PER_DAY)
            throw new ArgumentException("Not a COMB, time component exceeds 24 hours.");

        return MinDateTimeValue.AddDays(days).AddMilliseconds((double)ticks / TICKS_PER_MILLISECOND);
    }
}
