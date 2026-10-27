namespace Bing.Diagnostics;

/// <summary>
/// 表示工作表中的单元格位置，为 Excel 等表格格式提供扩展点。
/// </summary>
public sealed class WorksheetCellLocation : DataLocation
{
    /// <summary>
    /// 初始化工作表单元格位置。
    /// </summary>
    /// <param name="sheetName">工作表名称。</param>
    /// <param name="rowNumber">从 1 开始的行号。</param>
    /// <param name="columnNumber">从 1 开始的列号。</param>
    /// <param name="columnName">可选的列名。</param>
    public WorksheetCellLocation(string sheetName, long rowNumber, int columnNumber, string columnName = null)
    {
        if (string.IsNullOrWhiteSpace(sheetName)) throw new ArgumentException("工作表名称不能为空。", nameof(sheetName));
        if (rowNumber <= 0) throw new ArgumentOutOfRangeException(nameof(rowNumber));
        if (columnNumber <= 0) throw new ArgumentOutOfRangeException(nameof(columnNumber));

        SheetName = sheetName;
        RowNumber = rowNumber;
        ColumnNumber = columnNumber;
        ColumnName = columnName;
    }

    /// <inheritdoc />
    public override string Kind => "worksheet-cell";

    /// <summary>
    /// 获取工作表名称。
    /// </summary>
    public string SheetName { get; }

    /// <summary>
    /// 获取从 1 开始的行号。
    /// </summary>
    public long RowNumber { get; }

    /// <summary>
    /// 获取从 1 开始的列号。
    /// </summary>
    public int ColumnNumber { get; }

    /// <summary>
    /// 获取列名；未知时为 null。
    /// </summary>
    public string ColumnName { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var column = ColumnName == null ? $"第 {ColumnNumber} 列" : $"第 {ColumnNumber} 列“{ColumnName}”";
        return $"工作表“{SheetName}”{column}第 {RowNumber} 行";
    }
}
