using Bing.Diagnostics;

namespace Bing.Tabular;

/// <summary>
/// 表示一个可供映射器读取的表格数据行。
/// </summary>
public interface ITabularRow
{
    /// <summary>
    /// 获取逻辑行号。行号由数据源定义，通常从一开始。
    /// </summary>
    long RowNumber { get; }

    /// <summary>
    /// 获取字段数量。
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取指定序号的字段名称；数据源没有名称时可以返回 null。
    /// </summary>
    /// <param name="ordinal">从零开始的字段序号。</param>
    string GetName(int ordinal);

    /// <summary>
    /// 获取指定序号的原始字段值。
    /// </summary>
    /// <param name="ordinal">从零开始的字段序号。</param>
    object GetValue(int ordinal);

    /// <summary>
    /// 获取该行在原始数据中的位置；数据源没有更细定位时可以返回 null。
    /// </summary>
    DataLocation Location { get; }
}
