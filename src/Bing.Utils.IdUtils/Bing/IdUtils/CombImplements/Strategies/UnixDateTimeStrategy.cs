namespace Bing.IdUtils.CombImplements.Strategies;

/// <summary>
/// 基于 Unix 的 COMB 日期时间策略
/// </summary>
internal class UnixDateTimeStrategy : ICombDateTimeStrategy
{
    /// <summary>
    /// 每毫秒对应的 .NET 时间刻度数。
    /// </summary>
    private const long TICKS_PER_MILLISECOND = 10_000;

    /// <inheritdoc />
    public int NumDateBytes => 6;

    /// <inheritdoc />
    public DateTime MinDateTimeValue { get; } = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public DateTime MaxDateTimeValue => MinDateTimeValue.AddMilliseconds(2 ^ (8 * NumDateBytes));

    /// <inheritdoc />
    public byte[] DateTimeToBytes(DateTime timestamp)
    {
        var ms = ToUnixTimeMilliseconds(timestamp);
        var msBytes = BitConverter.GetBytes(ms);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(msBytes);

        var ret = new byte[NumDateBytes];
        var index = msBytes.GetUpperBound(0) + 1 - NumDateBytes;

        Array.Copy(msBytes, index, ret, 0, NumDateBytes);

        return ret;
    }

    /// <inheritdoc />
    public DateTime BytesToDateTime(byte[] value)
    {
        var msBytes = new byte[8];
        var index = 8 - NumDateBytes;

        Array.Copy(value, 0, msBytes, index, NumDateBytes);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(msBytes);

        var ms = BitConverter.ToInt64(msBytes, 0);

        return FromUnixTimeMilliseconds(ms);
    }

    /// <summary>
    /// 将日期时间转换为相对于 Unix 纪元的毫秒数。
    /// </summary>
    /// <param name="timestamp">要转换的日期时间。</param>
    /// <returns>相对于 Unix 纪元的毫秒数。</returns>
    private long ToUnixTimeMilliseconds(DateTime timestamp) => (timestamp.Ticks - MinDateTimeValue.Ticks) / TICKS_PER_MILLISECOND;

    /// <summary>
    /// 将相对于 Unix 纪元的毫秒数转换为日期时间。
    /// </summary>
    /// <param name="ms">相对于 Unix 纪元的毫秒数。</param>
    /// <returns>对应的日期时间。</returns>
    private DateTime FromUnixTimeMilliseconds(long ms) => MinDateTimeValue.AddTicks(ms * TICKS_PER_MILLISECOND);
}
