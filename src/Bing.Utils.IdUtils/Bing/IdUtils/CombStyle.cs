namespace Bing.IdUtils;

/// <summary>
/// COMB 风格
/// </summary>
public enum CombStyle
{
    /// <summary>
    /// 普通风格，不特定于任何操作系统或数据库，适用于通用场景。
    /// </summary>
    NormalStyle,

    /// <summary>
    /// Unix 风格，适用于Unix或Linux环境，遵循Unix操作系统的命名和路径规范。
    /// </summary>
    UnixStyle,

    /// <summary>
    /// SQL 风格，专为SQL查询和命令设计，遵循SQL语法和命名规范。
    /// </summary>
    SqlStyle,

    /// <summary>
    /// 旧版合法的 SQL 风格，用于与早期版本的SQL代码或系统兼容。
    /// </summary>
    LegacySqlStyle,

    /// <summary>
    /// PostgreSQL 风格，专门为PostgreSQL数据库设计，遵循其特定的命名和编码规范。
    /// </summary>
    PostgreSqlStyle,
}