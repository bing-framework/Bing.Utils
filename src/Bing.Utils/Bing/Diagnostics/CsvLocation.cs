namespace Bing.Diagnostics;

/// <summary>
/// 表示 CSV 逻辑记录中的行列位置。
/// </summary>
public sealed class CsvLocation : DataLocation
{
    /// <summary>
    /// 初始化 CSV 位置。
    /// </summary>
    /// <param name="recordNumber">从 1 开始的逻辑记录号。</param>
    /// <param name="columnNumber">从 1 开始的列号；零表示没有特定列。</param>
    /// <param name="columnName">可用的列名。</param>
    public CsvLocation(long recordNumber, int columnNumber = 0, string columnName = null)
    {
        if (recordNumber <= 0) throw new ArgumentOutOfRangeException(nameof(recordNumber));
        if (columnNumber < 0) throw new ArgumentOutOfRangeException(nameof(columnNumber));

        RecordNumber = recordNumber;
        ColumnNumber = columnNumber;
        ColumnName = columnName;
    }

    /// <inheritdoc />
    public override string Kind => "csv";

    /// <summary>
    /// 获取从 1 开始的逻辑记录号。
    /// </summary>
    public long RecordNumber { get; }

    /// <summary>
    /// 获取从 1 开始的列号；零表示没有特定列。
    /// </summary>
    public int ColumnNumber { get; }

    /// <summary>
    /// 获取列名；未知时为 null。
    /// </summary>
    public string ColumnName { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var column = ColumnNumber == 0
            ? "记录"
            : ColumnName == null ? $"第 {ColumnNumber} 列" : $"第 {ColumnNumber} 列“{ColumnName}”";
        return $"CSV 第 {RecordNumber} 条{column}";
    }
}
