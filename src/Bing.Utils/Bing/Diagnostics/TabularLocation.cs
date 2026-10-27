namespace Bing.Diagnostics;

/// <summary>
/// 表示通用表格数据中的逻辑行和源字段位置。
/// </summary>
public class TabularLocation : DataLocation
{
    /// <summary>
    /// 初始化表格位置。
    /// </summary>
    /// <param name="rowNumber">逻辑行号；由数据源决定是否从零或从一开始。</param>
    /// <param name="ordinal">从零开始的源字段序号；负数表示没有特定字段。</param>
    /// <param name="columnName">源字段名；未知时为 null。</param>
    public TabularLocation(long rowNumber, int ordinal = -1, string columnName = null)
    {
        if (rowNumber < 0) throw new ArgumentOutOfRangeException(nameof(rowNumber));
        if (ordinal < -1) throw new ArgumentOutOfRangeException(nameof(ordinal));

        RowNumber = rowNumber;
        Ordinal = ordinal;
        ColumnName = columnName;
    }

    /// <inheritdoc />
    public override string Kind => "tabular";

    /// <summary>
    /// 获取逻辑行号；具体起始值由数据源约定。
    /// </summary>
    public long RowNumber { get; }

    /// <summary>
    /// 获取从零开始的源字段序号；负一表示没有特定字段。
    /// </summary>
    public int Ordinal { get; }

    /// <summary>
    /// 获取源字段名；未知时为 null。
    /// </summary>
    public string ColumnName { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Ordinal < 0)
            return $"表格第 {RowNumber} 行";
        return ColumnName == null
            ? $"表格第 {RowNumber} 行第 {Ordinal} 列"
            : $"表格第 {RowNumber} 行第 {Ordinal} 列“{ColumnName}”";
    }
}
