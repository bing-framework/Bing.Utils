using Bing.IdUtils.CombImplements.Strategies;

namespace Bing.IdUtils.CombImplements.Providers;

/// <summary>
/// PostgreSQL 类型的 COMB 提供程序
/// </summary>
internal class PostgreSqlCombProvider : BaseCombProvider
{
    /// <summary>
    /// 初始化一个 <see cref="PostgreSqlCombProvider"/> 类型的实例。
    /// </summary>
    /// <param name="strategy">日期时间策略</param>
    /// <param name="customTimeStampProvider">自定义时间戳提供程序</param>
    /// <param name="customGuidProvider">自定义Guid提供程序</param>
    public PostgreSqlCombProvider(ICombDateTimeStrategy strategy, InternalTimeStampProvider customTimeStampProvider = null, InternalGuidProvider customGuidProvider = null)
        : base(strategy, customTimeStampProvider, customGuidProvider)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// PostgreSQL 布局将日期时间字节写入 GUID 的起始部分，并按字符串顺序调整字节。
    /// </remarks>
    public override Guid Create(Guid value, DateTime timestamp)
    {
        var gBytes = value.ToByteArray();
        var dBytes = _dateTimeStrategy.DateTimeToBytes(timestamp);
        Array.Copy(dBytes, 0, gBytes, 0, _dateTimeStrategy.NumDateBytes);
        SwapByteOrderForStringOrder(gBytes);
        return new Guid(gBytes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 按字符串顺序调整 GUID 字节后，从起始部分读取 PostgreSQL 布局的日期时间数据。
    /// </remarks>
    public override DateTime GetTimeStamp(Guid value)
    {
        var gBytes = value.ToByteArray();
        var dBytes = new byte[_dateTimeStrategy.NumDateBytes];
        SwapByteOrderForStringOrder(gBytes);
        Array.Copy(gBytes, 0, dBytes, 0, _dateTimeStrategy.NumDateBytes);
        return _dateTimeStrategy.BytesToDateTime(dBytes);
    }

    /// <summary>
    /// 交换字节排序
    /// </summary>
    /// <param name="input">输入值</param>
    private void SwapByteOrderForStringOrder(byte[] input)
    {
        Array.Reverse(input, 4, 4);
        if (input.Length == 4)
            return;
        Array.Reverse(input, 4, 2);
    }
}
