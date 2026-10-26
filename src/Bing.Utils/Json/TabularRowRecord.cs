using System.Collections.ObjectModel;
using System.Data;

namespace Bing.Utils.Json;

/// <summary>
/// 表示一行独立的值版本、状态与错误信息。
/// </summary>
/// <remarks>缺少的版本用 null 表示，数据库空值使用 DBNull.Value；版本数组和二进制单元格在构造时复制。</remarks>
public sealed class TabularRowRecord
{
    /// <summary>
    /// 获取所属表在结构中的序号。
    /// </summary>
    public int TableIndex { get; }
    /// <summary>
    /// 获取行状态，不支持 Detached。
    /// </summary>
    public DataRowState State { get; }
    /// <summary>
    /// 获取原始版本；Added 行为 null。
    /// </summary>
    public IReadOnlyList<object> Original { get; }
    /// <summary>
    /// 获取当前版本；Deleted 行为 null。
    /// </summary>
    public IReadOnlyList<object> Current { get; }
    /// <summary>
    /// 获取尚未提交的编辑版本；未编辑时为 null。
    /// </summary>
    public IReadOnlyList<object> Proposed { get; }
    /// <summary>
    /// 获取行错误文本。
    /// </summary>
    public string RowError { get; }
    /// <summary>
    /// 获取以列序号为键的错误文本。
    /// </summary>
    public IReadOnlyDictionary<int, string> ColumnErrors { get; }

    /// <summary>
    /// 创建独立行记录；版本与列类型在读写时按结构验证。
    /// </summary>
    /// <param name="tableIndex">非负表序号。</param>
    /// <param name="state">表内行状态。</param>
    /// <param name="original">原始值版本。</param>
    /// <param name="current">当前值版本。</param>
    /// <param name="proposed">可选编辑版本。</param>
    /// <param name="rowError">可选行错误。</param>
    /// <param name="columnErrors">可选列错误。</param>
    public TabularRowRecord(int tableIndex, DataRowState state, IEnumerable<object> original, IEnumerable<object> current,
        IEnumerable<object> proposed = null, string rowError = null, IReadOnlyDictionary<int, string> columnErrors = null)
    {
        if (tableIndex < 0) throw new ArgumentOutOfRangeException(nameof(tableIndex));
        if (state != DataRowState.Added && state != DataRowState.Unchanged && state != DataRowState.Modified && state != DataRowState.Deleted)
            throw new ArgumentOutOfRangeException(nameof(state));
        TableIndex = tableIndex; State = state;
        Original = Copy(original); Current = Copy(current); Proposed = Copy(proposed); RowError = rowError ?? "";
        ColumnErrors = new ReadOnlyDictionary<int, string>(columnErrors == null ? new Dictionary<int, string>()
            : columnErrors.ToDictionary(p => p.Key, p => p.Value ?? ""));
    }

    /// <summary>
    /// 捕获表内行的所有现有版本，不调用 EndEdit 或 AcceptChanges。
    /// </summary>
    /// <param name="row">要捕获的行。</param>
    /// <param name="tableIndex">结构中的表序号。</param>
    /// <returns>独立记录。</returns>
    public static TabularRowRecord FromRow(DataRow row, int tableIndex = 0)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        object[] Values(DataRowVersion version) => row.HasVersion(version)
            ? row.Table.Columns.Cast<DataColumn>().Select(c => row[c, version]).ToArray() : null;
        return new TabularRowRecord(tableIndex, row.RowState, Values(DataRowVersion.Original), Values(DataRowVersion.Current),
            Values(DataRowVersion.Proposed), row.RowError, row.GetColumnsInError().ToDictionary(c => c.Ordinal, row.GetColumnError));
    }

    /// <summary>
    /// 复制值数组及可变二进制单元格。
    /// </summary>
    private static IReadOnlyList<object> Copy(IEnumerable<object> values) => values == null ? null
        : System.Array.AsReadOnly(values.Select(v => v is byte[] bytes ? bytes.Clone() : v ?? DBNull.Value).ToArray());
}
