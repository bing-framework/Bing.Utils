using Bing.Date;
using Bing.IdUtils.CombImplements.Providers;
using Bing.IdUtils.CombImplements.Strategies;

namespace Bing.IdUtils.CombImplements;

/// <summary>
/// 内部基于COMB GUID 实现的代理
/// </summary>
internal static class InternalCombImplementProxy
{
    /// <summary>
    /// 普通 COMB 生成方式
    /// </summary>
    public static ICombProvider Legacy = new MsSqlCombProvider(new SqlDateTimeStrategy());

    /// <summary>
    /// 普通 COMB 生成方式【不重复】
    /// </summary>
    public static ICombProvider LegacyWithNoRepeat = new MsSqlCombProvider(new SqlDateTimeStrategy(), new NoRepeatTimeStampFactory().GetUtcTimeStamp);

    /// <summary>
    /// 存储于 MSSQL 中的 COMB 生成方式
    /// </summary>
    public static ICombProvider MsSql = new MsSqlCombProvider(new UnixDateTimeStrategy());

    /// <summary>
    /// 存储于 MSSQL 中的 COMB 生成方式【不重复】
    /// </summary>
    public static ICombProvider MsSqlWithNoRepeat = new MsSqlCombProvider(new UnixDateTimeStrategy(), new NoRepeatTimeStampFactory().GetUtcTimeStamp);

    /// <summary>
    /// 存储于 PostgreSql 中的 COMB 生成方式
    /// </summary>
    public static ICombProvider PostgreSql = new PostgreSqlCombProvider(new UnixDateTimeStrategy());

    /// <summary>
    /// 存储于 PostgreSql 中的 COMB 生成方式【不重复】
    /// </summary>
    public static ICombProvider PostgreSqlWithNoRepeat = new PostgreSqlCombProvider(new UnixDateTimeStrategy(), new NoRepeatTimeStampFactory().GetUtcTimeStamp);
}