using Bing.Diagnostics;

namespace Bing.Serialization.Csv;

/// <summary>
/// 表示 CSV 格式、预算或字段转换错误，并保留行列位置。
/// </summary>
public sealed class CsvFormatException : FormatException
{
    /// <summary>
    /// 使用记录位置创建 CSV 格式异常。
    /// </summary>
    /// <param name="message">错误说明。</param>
    /// <param name="rowNumber">从一开始的逻辑记录号。</param>
    /// <param name="columnNumber">从一开始的字段号；零表示错误不属于特定字段。</param>
    /// <param name="columnName">可用的字段名。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public CsvFormatException(string message, long rowNumber, int columnNumber = 0, string columnName = null,
        Exception innerException = null)
        : base(FormatMessage(message, rowNumber, columnNumber, columnName), innerException)
    {
        RowNumber = rowNumber;
        ColumnNumber = columnNumber;
        ColumnName = columnName;
    }

    /// <summary>
    /// 获取从一开始的逻辑记录号。
    /// </summary>
    public long RowNumber { get; }

    /// <summary>
    /// 获取从一开始的字段号；零表示没有特定字段。
    /// </summary>
    public int ColumnNumber { get; }

    /// <summary>
    /// 获取可用的字段名。
    /// </summary>
    public string ColumnName { get; }

    /// <summary>获取统一的 CSV 位置；历史调用传入非法坐标时返回 null。</summary>
    public CsvLocation Location => RowNumber > 0 && ColumnNumber >= 0
        ? new CsvLocation(RowNumber, ColumnNumber, ColumnName) : null;

    private static string FormatMessage(string message, long rowNumber, int columnNumber, string columnName)
    {
        var location = columnNumber > 0
            ? columnName == null ? $"第 {columnNumber} 列" : $"第 {columnNumber} 列“{columnName}”"
            : "记录";
        return $"CSV 第 {rowNumber} 条{location}：{message}";
    }
}
