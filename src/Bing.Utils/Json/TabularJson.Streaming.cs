using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

public static partial class TabularJson
{
    /// <summary>
    /// 打开 v2 逐行读取器并读取结构，不缓存全部数据行。
    /// </summary>
    /// <param name="source">可读流，从当前位置开始。</param>
    /// <param name="options">读取预算；格式由文档决定，仅接受 v2 或 null。</param>
    /// <param name="leaveOpen">是否保留源；参数错误或开始前取消不接管资源。</param>
    /// <param name="cancellationToken">创建和后续枚举共同观察的令牌。</param>
    /// <returns>调用方负责释放的读取器；JSON null 的 Schema 为 null。</returns>
    public static async Task<TabularJsonReader> OpenReaderAsync(Stream source, TabularJsonOptions options = null,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源不可读。", nameof(source));
        var config = (options ?? new TabularJsonOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        var reader = new TabularJsonReader(source, config, leaveOpen, cancellationToken);
        try { await reader.InitializeAsync().ConfigureAwait(false); return reader; }
        catch { DisposeResource(reader, true); throw; }
    }

    /// <summary>
    /// 将结构和异步行序列写成 v2 文档，不缓存全部行。
    /// </summary>
    /// <param name="destination">可写目标，不复位或截断。</param>
    /// <param name="schema">独立结构快照。</param>
    /// <param name="rows">按每张表内部行顺序提供记录；可以交错不同表。</param>
    /// <param name="options">结构、行、单元格和行数预算；Format 不改变此入口的 v2 格式。</param>
    /// <param name="leaveOpen">是否保留目标；参数失败或开始前取消不接管资源。</param>
    /// <param name="cancellationToken">传递给枚举和写入的取消令牌。</param>
    /// <remarks>失败可能留下部分文档。记录校验不验证跨行唯一性及外键约束，完整装载时才验证这些约束。</remarks>
    public static async Task WriteRowsAsync(Stream destination, TabularSchema schema, IAsyncEnumerable<TabularRowRecord> rows,
        TabularJsonOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标不可写。", nameof(destination));
        if (schema == null) throw new ArgumentNullException(nameof(schema));
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        var config = (options ?? new TabularJsonOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        var failed = false;
        try
        {
            Limit(schema.TableCount, config.MaxTables, "表数量");
            foreach (var table in schema.Tables) Limit(table.Columns.Count, config.MaxColumnsPerTable, "列数量");
            var counts = new long[schema.TableCount];
            await WriteTextAsync(destination, "{\"$format\":\"bing.tabular\",\"$version\":2,\"schema\":", cancellationToken).ConfigureAwait(false);
            await WriteRecordAsync(destination, schema.Model, config.MaxSchemaBytes, cancellationToken).ConfigureAwait(false);
            await WriteTextAsync(destination, ",\"rows\":[", cancellationToken).ConfigureAwait(false);
            var first = true;
            await foreach (var row in rows.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var model = EncodeRecord(schema, row, config);
                Limit(++counts[row.TableIndex], config.MaxRowsPerTable, "行数量");
                if (!first) await WriteTextAsync(destination, ",", cancellationToken).ConfigureAwait(false);
                await WriteRecordAsync(destination, model, config.MaxRowBytes, cancellationToken).ConfigureAwait(false);
                first = false;
            }
            await WriteTextAsync(destination, "],\"counts\":", cancellationToken).ConfigureAwait(false);
            await WriteRecordAsync(destination, new JArray(counts), config.MaxSchemaBytes, cancellationToken).ConfigureAwait(false);
            await WriteTextAsync(destination, "}", cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch { failed = true; throw; }
        finally { if (!leaveOpen) DisposeResource(destination, failed); }
    }

    /// <summary>
    /// 写出一个记录；缓冲仅随单条记录增长。
    /// </summary>
    private static async Task WriteRecordAsync(Stream target, JToken model, long? maximum, CancellationToken token)
    {
        var text = model.ToString(Formatting.None);
        Limit(Encoding.UTF8.GetByteCount(text), maximum, "表格记录字节数");
        await WriteTextAsync(target, text, token).ConfigureAwait(false);
    }

    /// <summary>
    /// 使用无 BOM UTF-8 写入当前片段。
    /// </summary>
    private static Task WriteTextAsync(Stream target, string text, CancellationToken token)
    {
        var bytes = new UTF8Encoding(false, true).GetBytes(text);
        return target.WriteAsync(bytes, 0, bytes.Length, token);
    }

    /// <summary>
    /// 惰性捕获表内行，不改变源对象的编辑状态。
    /// </summary>
    private static async IAsyncEnumerable<TabularRowRecord> Records(object value)
    {
        var tables = value is DataTable table ? new[] { table } : ((DataSet)value).Tables.Cast<DataTable>().ToArray();
        for (var index = 0; index < tables.Length; index++)
            foreach (DataRow row in tables[index].Rows) yield return TabularRowRecord.FromRow(row, index);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// 将 v2 流逐行装载到新对象；失败时释放内部对象。
    /// </summary>
    private static async Task<object> ReadVersionAsync(Stream source, bool isDataSet, TabularJsonOptions options, bool leaveOpen, CancellationToken token)
    {
        DataSet target = null;
        var failed = false;
        try
        {
            using var reader = await OpenReaderAsync(source, options, true, token).ConfigureAwait(false);
            if (reader.Schema == null) return null;
            if (reader.Schema.IsDataSet != isDataSet) throw new JsonSerializationException("表格对象种类不匹配。");
            target = CreateVersionTarget(reader.Schema, options);
            await foreach (var row in reader.ReadRowsAsync(token).ConfigureAwait(false)) RestoreRecord(target, row);
            token.ThrowIfCancellationRequested();
            return FinishVersionTarget(target, reader.Schema);
        }
        catch (Exception ex)
        {
            failed = true;
            DisposeResource(target, true);
            if (ex is DataException || ex is ArgumentException || ex is OverflowException || ex is FormatException || ex is InvalidOperationException)
                throw new JsonSerializationException($"表格结构或数据无效：{ex.Message}", ex);
            throw;
        }
        finally { if (!leaveOpen) DisposeResource(source, failed); }
    }

    /// <summary>
    /// 释放处理阶段接管的资源；已有主异常时不让清理异常将其覆盖。
    /// </summary>
    internal static void DisposeResource(IDisposable resource, bool operationFailed)
    {
        try { resource?.Dispose(); }
        catch when (operationFailed) { }
    }

    /// <summary>
    /// 为文本和 JsonConverter 入口创建 v2 模型；这些入口按其契约缓冲完整结果。
    /// </summary>
    internal static JToken VersionModel(object value, TabularJsonOptions options, CancellationToken token)
    {
        if (value == null) return JValue.CreateNull();
        var schema = CaptureSchema(value);
        var tables = value is DataTable table ? new[] { table } : ((DataSet)value).Tables.Cast<DataTable>().ToArray();
        var rows = new JArray();
        var counts = new long[tables.Length];
        for (var i = 0; i < tables.Length; i++)
            foreach (DataRow row in tables[i].Rows)
            {
                token.ThrowIfCancellationRequested();
                var record = EncodeRecord(schema, TabularRowRecord.FromRow(row, i), options);
                Limit(Encoding.UTF8.GetByteCount(record.ToString(Formatting.None)), options.MaxRowBytes, "行字节数");
                Limit(++counts[i], options.MaxRowsPerTable, "行数量"); rows.Add(record);
            }
        Limit(schema.TableCount, options.MaxTables, "表数量");
        foreach (var item in schema.Tables) Limit(item.Columns.Count, options.MaxColumnsPerTable, "列数量");
        Limit(Encoding.UTF8.GetByteCount(schema.Model.ToString(Formatting.None)), options.MaxSchemaBytes, "结构字节数");
        return new JObject { ["$format"] = "bing.tabular", ["$version"] = 2, ["schema"] = schema.Model.DeepClone(), ["rows"] = rows, ["counts"] = new JArray(counts) };
    }

    /// <summary>
    /// 恢复已缓冲的 v2 模型，与流路径使用相同的版本重放规则。
    /// </summary>
    private static object RestoreVersionModel(JObject root, bool isDataSet, TabularJsonOptions options, CancellationToken token)
    {
        var schemaModel = Object(root["schema"]);
        Limit(Encoding.UTF8.GetByteCount(schemaModel.ToString(Formatting.None)), options.MaxSchemaBytes, "结构字节数");
        var schema = ParseSchema(schemaModel, options);
        if (schema.IsDataSet != isDataSet) throw new JsonSerializationException("表格对象种类不匹配。");
        var target = CreateVersionTarget(schema, options);
        try
        {
            var counts = new long[schema.TableCount];
            foreach (var item in Array(root["rows"]))
            {
                token.ThrowIfCancellationRequested();
                Limit(Encoding.UTF8.GetByteCount(item.ToString(Formatting.None)), options.MaxRowBytes, "行字节数");
                var row = DecodeRecord(schema, Object(item), options);
                Limit(++counts[row.TableIndex], options.MaxRowsPerTable, "行数量");
                RestoreRecord(target, row);
            }
            var actual = Array(root["counts"]);
            if (actual.Count != counts.Length || actual.Where((v, i) => v.Type != JTokenType.Integer || (long)v != counts[i]).Any())
                throw new JsonSerializationException("表格行计数不一致。");
            return FinishVersionTarget(target, schema);
        }
        catch { target.Dispose(); throw; }
    }
}
