using System.Data.Common;
using Bing.Diagnostics;

namespace Bing.Tabular;

/// <summary>将数据库读取器当前行快照适配为表格行，不推进或释放读取器。</summary>
/// <remarks>复制列名、值数组及 byte[] 字段；其他引用类型按引用保留。</remarks>
public sealed class DbDataReaderRow : ITabularRow
{
    private readonly string[] _names;
    private readonly object[] _values;

    /// <summary>从已定位到有效数据行的读取器创建快照。</summary>
    /// <param name="reader">已执行 Read 或 ReadAsync 的读取器。</param>
    /// <param name="rowNumber">调用方维护的逻辑行号，从一开始。</param>
    public DbDataReaderRow(DbDataReader reader, long rowNumber)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (rowNumber < 1) throw new ArgumentOutOfRangeException(nameof(rowNumber));
        if (reader.IsClosed) throw new ArgumentException("读取器已关闭。", nameof(reader));
        RowNumber = rowNumber;
        Location = new TabularLocation(rowNumber);
        _names = new string[reader.FieldCount];
        _values = new object[reader.FieldCount];
        for (var ordinal = 0; ordinal < Count; ordinal++)
        {
            _names[ordinal] = reader.GetName(ordinal);
            var value = reader.GetValue(ordinal);
            _values[ordinal] = value is byte[] bytes ? bytes.Clone() : value;
        }
    }

    /// <inheritdoc />
    public long RowNumber { get; }
    /// <inheritdoc />
    public int Count => _values.Length;
    /// <inheritdoc />
    public DataLocation Location { get; }
    /// <inheritdoc />
    public string GetName(int ordinal) => _names[ordinal];
    /// <inheritdoc />
    public object GetValue(int ordinal) => _values[ordinal];
}
