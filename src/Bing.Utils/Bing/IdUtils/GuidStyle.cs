namespace Bing.IdUtils;

/// <summary>
/// GUID 风格
/// </summary>
public enum GuidStyle
{
    /// <summary>
    /// 基础
    /// </summary>
    BasicStyle,

    /// <summary>
    /// 时间戳
    /// </summary>
    TimeStampStyle,

    /// <summary>
    /// UNIX 时间戳
    /// </summary>
    UnixTimeStampStyle,

    /// <summary>
    /// SQL 时间戳
    /// </summary>
    SqlTimestampStyle,

    /// <summary>
    /// 合法 SQL 时间戳
    /// </summary>
    LegacySqlTimeStampStyle,

    /// <summary>
    /// PostgreSQL 时间戳
    /// </summary>
    PostgreSqlTimeStampStyle,

    /// <summary>
    /// COMB 风格
    /// </summary>
    CombStyle,

    /// <summary>
    /// 字符串序列
    /// </summary>
    SequentialAsStringStyle,

    /// <summary>
    /// 二进制序列
    /// </summary>
    SequentialAsBinaryStyle,

    /// <summary>
    /// 顺序
    /// </summary>
    SequentialAtEndStyle,

    /// <summary>
    /// Equifax 风格
    /// </summary>
    EquifaxStyle,
}