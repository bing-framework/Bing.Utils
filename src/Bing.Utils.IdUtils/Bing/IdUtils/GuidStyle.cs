namespace Bing.IdUtils;

/// <summary>
/// GUID 风格
/// </summary>
public enum GuidStyle
{
    /// <summary>
    /// 基本风格，生成标准的GUID。
    /// </summary>
    BasicStyle,

    /// <summary>
    ///  时间戳风格，GUID中包含时间戳信息。
    /// </summary>
    TimeStampStyle,

    /// <summary>
    /// Unix 时间戳风格，GUID中包含Unix格式的时间戳。
    /// </summary>
    UnixTimeStampStyle,

    /// <summary>
    /// SQL 时间戳风格，为SQL数据库设计的时间戳格式。
    /// </summary>
    SqlTimeStampStyle,

    /// <summary>
    /// 旧版合法的 SQL 时间戳风格，用于兼容旧版SQL系统的时间戳格式。
    /// </summary>
    LegacySqlTimeStampStyle,

    /// <summary>
    /// PostgreSQL 时间戳风格，专为PostgreSQL数据库设计的时间戳格式。
    /// </summary>
    PostgreSqlTimeStampStyle,

    /// <summary>
    /// COMB 风格，将时间戳与随机或顺序元素相结合，以生成唯一的GUID。
    /// </summary>
    CombStyle,

    /// <summary>
    /// 顺序字符串风格，生成的GUID按照字符串顺序排列。
    /// </summary>
    /// <remarks>数据库：MySql、PostgreSql</remarks>
    SequentialAsStringStyle,

    /// <summary>
    /// 顺序二进制风格，生成的GUID按照二进制顺序排列。
    /// </summary>
    /// <remarks>数据库：Oracle</remarks>
    SequentialAsBinaryStyle,

    /// <summary>
    /// 顺序末尾风格，GUID的顺序部分位于末尾。
    /// </summary>
    /// <remarks>数据库：SqlServer</remarks>
    SequentialAsEndStyle,

    /// <summary>
    /// Equifax 风格，特定于Equifax使用的GUID格式。
    /// </summary>
    EquifaxStyle,
}