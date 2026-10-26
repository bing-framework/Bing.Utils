using System.Data;
using System.Data.Common;

namespace Bing.Utils.Json;

/// <summary>
/// 数据库提供程序声明的列元数据，未提供的可选值保持 null。
/// </summary>
public sealed class DbFieldMetadata
{
    /// <summary>
    /// 获取列序号。
    /// </summary>
    public int Ordinal { get; internal set; }
    /// <summary>
    /// 获取列名。
    /// </summary>
    public string Name { get; internal set; }
    /// <summary>
    /// 获取 CLR 类型。
    /// </summary>
    public Type DataType { get; internal set; }
    /// <summary>
    /// 获取是否允许数据库空值；null 表示未声明。
    /// </summary>
    public bool? AllowDBNull { get; internal set; }
    /// <summary>
    /// 获取提供程序声明的列长度；单位由该提供程序定义。
    /// </summary>
    public long? ColumnSize { get; internal set; }
    /// <summary>
    /// 获取是否属于键；null 表示未声明。
    /// </summary>
    public bool? IsKey { get; internal set; }
    /// <summary>
    /// 获取是否只读；null 表示未声明。
    /// </summary>
    public bool? IsReadOnly { get; internal set; }
}

public static partial class TabularDataReader
{
    /// <summary>
    /// 读取当前结果集的结构元数据；GetSchemaTable 不受支持时退回名称和 CLR 类型。
    /// </summary>
    /// <returns>独立列元数据，不推进读取器。</returns>
    public static IReadOnlyList<DbFieldMetadata> ReadSchema(DbDataReader reader)
    {
        ValidateReader(reader);
        DataTable schema = null;
        try { schema = reader.GetSchemaTable(); }
        catch (NotSupportedException) { }
        try
        {
            var columns = new List<DbFieldMetadata>();
            for (var index = 0; index < reader.FieldCount; index++)
            {
                DataRow row = null;
                if (schema != null && schema.Columns.Contains("ColumnOrdinal"))
                    row = schema.Rows.Cast<DataRow>().FirstOrDefault(r => r["ColumnOrdinal"] is int ordinal && ordinal == index);
                columns.Add(new DbFieldMetadata { Ordinal = index, Name = reader.GetName(index), DataType = reader.GetFieldType(index),
                    AllowDBNull = SchemaBoolean(row, "AllowDBNull"), IsKey = SchemaBoolean(row, "IsKey"),
                    IsReadOnly = SchemaBoolean(row, "IsReadOnly"), ColumnSize = SchemaLength(row) });
            }
            return columns.AsReadOnly();
        }
        finally { schema?.Dispose(); }
    }

    /// <summary>
    /// 创建包含提供程序空值约束和字符串长度的单表结构。
    /// </summary>
    /// <returns>可交给表格 JSON 写入器的独立结构。</returns>
    public static TabularSchema CreateDetailedSchema(DbDataReader reader, string tableName = "Result")
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("表名不能为空。", nameof(tableName));
        var fields = ReadSchema(reader);
        using var table = new DataTable(tableName);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field.Name) || !names.Add(field.Name)) throw new ArgumentException("列名不能为空或重复。", nameof(reader));
            var column = table.Columns.Add(field.Name, field.DataType);
            if (field.AllowDBNull.HasValue) column.AllowDBNull = field.AllowDBNull.Value;
            if (field.IsReadOnly.HasValue) column.ReadOnly = field.IsReadOnly.Value;
            if (field.DataType == typeof(string) && field.ColumnSize >= 0 && field.ColumnSize <= int.MaxValue)
                column.MaxLength = (int)field.ColumnSize.Value;
        }
        return TabularSchema.FromTable(table);
    }

    /// <summary>
    /// 依次调用每个结果集的处理器，处理器完成后才推进 NextResultAsync。
    /// </summary>
    /// <remarks>包含零列/零行结果集，索引从零开始。处理器可逐行消费或逐块复制字段；返回时未读取的行由提供程序跳过。
    /// 不得在处理器返回后继续使用当前结果集，也不得由处理器调用 NextResult。</remarks>
    public static async Task ForEachResultAsync(DbDataReader reader, Func<int, DbDataReader, CancellationToken, Task> handler,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        ValidateReader(reader); if (handler == null) throw new ArgumentNullException(nameof(handler));
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var index = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                await handler(index++, reader, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            cancellationToken.ThrowIfCancellationRequested();
        }
        finally { if (!leaveOpen) reader.Dispose(); }
    }

    /// <summary>
    /// 以固定缓冲复制当前行二进制字段，空字段和 DBNull 返回零；不调用 GetValue。
    /// </summary>
    /// <remarks>源保持打开，目标从当前位置写入；超限允许部分输出但不写入超额字节。
    /// 提供程序须支持 GetBytes，真正数据库流式读取通常还要求调用方以 SequentialAccess 执行命令并顺序访问列。
    /// GetBytes 是同步提供程序操作，取消在每次提供程序调用前后及异步写入时观察。</remarks>
    /// <returns>实际写出的字节数。</returns>
    public static async Task<long> CopyBinaryFieldAsync(DbDataReader reader, int ordinal, Stream destination,
        long maxBytes = long.MaxValue, CancellationToken cancellationToken = default)
    {
        ValidateReader(reader);
        if (ordinal < 0 || ordinal >= reader.FieldCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标不可写。", nameof(destination));
        if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        cancellationToken.ThrowIfCancellationRequested();
        var isNull = await reader.IsDBNullAsync(ordinal, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested(); if (isNull) return 0;
        var buffer = new byte[81920]; long total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = maxBytes - total;
            var count = remaining >= buffer.Length ? buffer.Length : (int)remaining + 1;
            var read = checked((int)reader.GetBytes(ordinal, total, buffer, 0, count));
            cancellationToken.ThrowIfCancellationRequested();
            if (read == 0) return total;
            if (read > remaining) throw new InvalidDataException("二进制字段超过字节限额。");
            await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false); total += read;
        }
    }

    /// <summary>
    /// 以固定字符缓冲复制当前行文本字段，不通过 GetValue 或 GetString 加载整个字段。
    /// </summary>
    /// <remarks>不关闭读取器或目标；maxChars 计算 UTF-16 代码单元。GetChars/旧 TextWriter 不支持可取消调用，取消在每块边界观察。
    /// 超限允许部分输出但不写入超额字符；调用方选择目标编码及刷新策略。</remarks>
    /// <returns>实际写出的 UTF-16 字符数。</returns>
    public static async Task<long> CopyTextFieldAsync(DbDataReader reader, int ordinal, TextWriter destination,
        long maxChars = long.MaxValue, CancellationToken cancellationToken = default)
    {
        ValidateReader(reader);
        if (ordinal < 0 || ordinal >= reader.FieldCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (maxChars < 0) throw new ArgumentOutOfRangeException(nameof(maxChars));
        cancellationToken.ThrowIfCancellationRequested();
        var isNull = await reader.IsDBNullAsync(ordinal, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested(); if (isNull) return 0;
        var buffer = new char[4096]; long total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested(); var remaining = maxChars - total;
            var count = remaining >= buffer.Length ? buffer.Length : (int)remaining + 1;
            var read = checked((int)reader.GetChars(ordinal, total, buffer, 0, count));
            cancellationToken.ThrowIfCancellationRequested(); if (read == 0) return total;
            if (read > remaining) throw new InvalidDataException("文本字段超过字符限额。");
            await destination.WriteAsync(buffer, 0, read).ConfigureAwait(false); total += read;
        }
    }
    /// <summary>
    /// 保留未声明布尔值的未知状态。
    /// </summary>
    private static bool? SchemaBoolean(DataRow row, string name) => row?.Table.Columns.Contains(name) == true && row[name] is bool value ? value : (bool?)null;
    /// <summary>
    /// 读取提供程序支持的长度类型。
    /// </summary>
    private static long? SchemaLength(DataRow row)
    {
        if (row?.Table.Columns.Contains("ColumnSize") != true || row.IsNull("ColumnSize")) return null;
        var value = row["ColumnSize"]; return value is long number ? number : value is int size ? size : (long?)null;
    }
}
