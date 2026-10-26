using System.Data;
using System.Data.Common;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Bing.Serialization.SystemTextJson;

namespace Bing.Utils.Json;

/// <summary>
/// 在数据库读取器与逐行表格格式之间流式转换数据。
/// </summary>
/// <remarks>
/// 所有方法只处理当前结果集；除非关闭参数另有说明，不会推进到下一个结果集。
/// </remarks>
public static partial class TabularDataReader
{
    /// <summary>
    /// 从当前结果集的字段元数据创建单表结构快照。
    /// </summary>
    /// <param name="reader">已打开的数据库读取器；读取器位置不会前进。</param>
    /// <param name="tableName">快照中的表名，默认值为 Result。</param>
    /// <returns>只包含列定义的独立结构；不要求先加载完整 DataTable。</returns>
    /// <exception cref="ArgumentNullException">读取器或表名为空。</exception>
    /// <exception cref="ArgumentException">字段名为空或重复。</exception>
    /// <exception cref="Newtonsoft.Json.JsonSerializationException">字段类型不在表格格式白名单中。</exception>
    public static TabularSchema CreateSchema(DbDataReader reader, string tableName = "Result")
    {
        ValidateReader(reader);
        if (tableName == null) throw new ArgumentNullException(nameof(tableName));
        if (tableName.Length == 0) throw new ArgumentException("表名不能为空。", nameof(tableName));

        using var table = new DataTable(tableName);
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var name = reader.GetName(ordinal);
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"第 {ordinal} 个字段的名称为空。", nameof(reader));
            if (!names.Add(name))
                throw new ArgumentException($"字段名重复：{name}。", nameof(reader));

            var type = reader.GetFieldType(ordinal);
            if (type == null)
                throw new NotSupportedException($"字段 {name} 未声明 CLR 类型。");
            table.Columns.Add(name, type);
        }

        return TabularSchema.FromTable(table);
    }

    /// <summary>
    /// 从数据库读取器的后续行开始单次异步枚举行记录。
    /// </summary>
    /// <param name="reader">当前结果集读取器。</param>
    /// <param name="leaveOpen">是否在枚举结束、提前释放或失败后保留读取器。</param>
    /// <param name="cancellationToken">取消后续读取与枚举。</param>
    /// <returns>表索引为零、状态为 Added 的独立行记录流。</returns>
    /// <exception cref="ArgumentNullException">读取器为空。</exception>
    /// <exception cref="ArgumentException">读取器已关闭。</exception>
    /// <exception cref="OperationCanceledException">调用时取消令牌已取消，或读取期间被取消。</exception>
    public static IAsyncEnumerable<TabularRowRecord> ReadRowsAsync(DbDataReader reader,
        bool leaveOpen = true, CancellationToken cancellationToken = default)
    {
        ValidateReader(reader);
        cancellationToken.ThrowIfCancellationRequested();
        return new ReaderRowsEnumerable(reader, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// 将当前结果集逐行写成现有 v2 表格文档。
    /// </summary>
    /// <param name="destination">可写目标；从当前位置写入，不复位或截断。</param>
    /// <param name="reader">当前结果集读取器。</param>
    /// <param name="options">v2 表格结构、行数与字节预算。</param>
    /// <param name="leaveReaderOpen">是否在处理结束后保留读取器。</param>
    /// <param name="leaveStreamOpen">是否在处理结束后保留目标流。</param>
    /// <param name="cancellationToken">取消读取、枚举或写入。</param>
    /// <returns>表示流式写入操作的异步任务。</returns>
    /// <remarks>只写入当前结果集；失败时目标可能包含部分 v2 文档。参数错误或开始前取消不会接管资源。</remarks>
    public static Task WriteTabularAsync(Stream destination, DbDataReader reader,
        TabularJsonOptions options = null, bool leaveReaderOpen = true, bool leaveStreamOpen = true,
        CancellationToken cancellationToken = default)
    {
        ValidateWriteArguments(destination, reader);
        var config = (options ?? new TabularJsonOptions()).Snapshot();
        cancellationToken.ThrowIfCancellationRequested();
        return WriteWithOwnershipAsync(destination, reader, leaveReaderOpen, leaveStreamOpen, async () =>
        {
            var schema = CreateSchema(reader);
            var rows = ReadRowsAsync(reader, leaveOpen: true, cancellationToken: cancellationToken);
            await TabularJson.WriteRowsAsync(destination, schema, rows, config, leaveOpen: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 将当前结果集逐行写为 JSON 对象数组。
    /// </summary>
    /// <param name="destination">可写目标；从当前位置写入，不复位或截断。</param>
    /// <param name="reader">当前结果集读取器。</param>
    /// <param name="options">System.Text.Json 序列化选项，空值使用默认值。</param>
    /// <param name="leaveReaderOpen">是否在处理结束后保留读取器。</param>
    /// <param name="leaveStreamOpen">是否在处理结束后保留目标流。</param>
    /// <param name="cancellationToken">取消读取、枚举或写入。</param>
    /// <returns>表示流式写入操作的异步任务。</returns>
    /// <remarks>数据库空值写为 JSON null；失败时目标可能包含部分数组。参数错误或开始前取消不会接管资源。</remarks>
    public static Task WriteJsonArrayAsync(Stream destination, DbDataReader reader,
        JsonSerializerOptions options = null, bool leaveReaderOpen = true, bool leaveStreamOpen = true,
        CancellationToken cancellationToken = default)
    {
        ValidateWriteArguments(destination, reader);
        var jsonOptions = options;
        cancellationToken.ThrowIfCancellationRequested();
        return WriteWithOwnershipAsync(destination, reader, leaveReaderOpen, leaveStreamOpen, async () =>
        {
            var names = GetColumnNames(reader);
            await JsonStream.WriteArrayAsync(destination, ReadJsonRows(reader, names, cancellationToken),
                jsonOptions, leaveOpen: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 将当前结果集逐行写为 NDJSON 对象记录。
    /// </summary>
    /// <param name="destination">可写目标；从当前位置写入，不复位或截断。</param>
    /// <param name="reader">当前结果集读取器。</param>
    /// <param name="options">System.Text.Json 序列化选项，空值使用默认值。</param>
    /// <param name="leaveReaderOpen">是否在处理结束后保留读取器。</param>
    /// <param name="leaveStreamOpen">是否在处理结束后保留目标流。</param>
    /// <param name="cancellationToken">取消读取、枚举或写入。</param>
    /// <returns>表示流式写入操作的异步任务。</returns>
    /// <remarks>每行单独序列化且数据库空值写为 JSON null；失败时目标可能包含部分行。参数错误或开始前取消不会接管资源。</remarks>
    public static Task WriteNdjsonAsync(Stream destination, DbDataReader reader,
        JsonSerializerOptions options = null, bool leaveReaderOpen = true, bool leaveStreamOpen = true,
        CancellationToken cancellationToken = default)
    {
        ValidateWriteArguments(destination, reader);
        if (options?.WriteIndented == true)
            throw new ArgumentException("NDJSON 不支持多行缩进。", nameof(options));
        var jsonOptions = options;
        cancellationToken.ThrowIfCancellationRequested();
        return WriteWithOwnershipAsync(destination, reader, leaveReaderOpen, leaveStreamOpen, async () =>
        {
            var names = GetColumnNames(reader);
            await JsonStream.WriteNdjsonAsync(destination, ReadJsonRows(reader, names, cancellationToken),
                jsonOptions: jsonOptions, leaveOpen: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 基于单表结构和异步行记录创建只读数据库读取器。
    /// </summary>
    /// <param name="schema">单表结构快照。</param>
    /// <param name="rows">逐行提供当前版本的异步记录源。</param>
    /// <param name="cancellationToken">取消后续异步读取。</param>
    /// <returns>调用方负责关闭的只读 <see cref="DbDataReader"/>。</returns>
    /// <exception cref="ArgumentNullException">结构或行源为空。</exception>
    /// <exception cref="NotSupportedException">结构包含多个表或表示 DataSet。</exception>
    /// <exception cref="OperationCanceledException">调用时取消令牌已取消。</exception>
    /// <remarks>只公开 Current 版本并拒绝 Deleted 行。同步 Read 和 HasRows 会等待异步源；在依赖调用方同步上下文的环境中应改用 ReadAsync。</remarks>
    public static DbDataReader CreateReader(TabularSchema schema, IAsyncEnumerable<TabularRowRecord> rows,
        CancellationToken cancellationToken = default)
    {
        if (schema == null) throw new ArgumentNullException(nameof(schema));
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (schema.IsDataSet || schema.TableCount != 1)
            throw new NotSupportedException("数据库读取器只支持单表结构。");
        cancellationToken.ThrowIfCancellationRequested();
        return new TabularDataDbDataReader(schema.GetTableSchema(0), rows, cancellationToken);
    }

    /// <summary>
    /// 校验数据库读取器引用及打开状态。
    /// </summary>
    private static void ValidateReader(DbDataReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (reader.IsClosed) throw new ArgumentException("数据库读取器已关闭。", nameof(reader));
    }

    /// <summary>
    /// 校验写入目标及读取器引用。
    /// </summary>
    private static void ValidateWriteArguments(Stream destination, DbDataReader reader)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标流不可写。", nameof(destination));
        ValidateReader(reader);
    }

    /// <summary>
    /// 捕获并验证当前结果集的唯一列名。
    /// </summary>
    private static string[] GetColumnNames(DbDataReader reader)
    {
        var names = new string[reader.FieldCount];
        var unique = new HashSet<string>(StringComparer.Ordinal);
        for (var ordinal = 0; ordinal < names.Length; ordinal++)
        {
            var name = reader.GetName(ordinal);
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"第 {ordinal} 个字段的名称为空。", nameof(reader));
            if (!unique.Add(name))
                throw new ArgumentException($"字段名重复：{name}。", nameof(reader));
            names[ordinal] = name;
        }
        return names;
    }

    /// <summary>
    /// 从当前结果集按需生成普通 JSON 对象。
    /// </summary>
    private static async IAsyncEnumerable<Dictionary<string, object>> ReadJsonRows(DbDataReader reader,
        string[] names, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasNext = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!hasNext) yield break;
            var values = new Dictionary<string, object>(names.Length, StringComparer.Ordinal);
            for (var ordinal = 0; ordinal < names.Length; ordinal++)
            {
                var value = reader.GetValue(ordinal);
                values.Add(names[ordinal], value == DBNull.Value ? null : value);
            }
            yield return values;
        }
    }

    /// <summary>
    /// 执行流式写入并按所有权参数释放输入读取器和输出流。
    /// </summary>
    private static async Task WriteWithOwnershipAsync(Stream destination, DbDataReader reader,
        bool leaveReaderOpen, bool leaveStreamOpen, Func<Task> write)
    {
        Exception operationError = null;
        try { await write().ConfigureAwait(false); }
        catch (Exception error) { operationError = error; throw; }
        finally
        {
            var cleanupError = await DisposeOwnedResourcesAsync(reader, destination,
                leaveReaderOpen, leaveStreamOpen, operationError != null).ConfigureAwait(false);
            if (operationError == null && cleanupError != null)
                ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }
    }

    /// <summary>
    /// 尝试释放两个调用方资源，并保留首个清理失败。
    /// </summary>
    private static async Task<Exception> DisposeOwnedResourcesAsync(DbDataReader reader, Stream destination,
        bool leaveReaderOpen, bool leaveStreamOpen, bool suppressErrors)
    {
        Exception cleanupError = null;
        if (!leaveReaderOpen)
        {
            try { await DisposeReaderAsync(reader).ConfigureAwait(false); }
            catch (Exception error) when (suppressErrors) { }
            catch (Exception error) { cleanupError = error; }
        }
        if (!leaveStreamOpen)
        {
            try { destination.Dispose(); }
            catch (Exception error) when (suppressErrors || cleanupError != null) { }
            catch (Exception error) { cleanupError = error; }
        }
        return cleanupError;
    }

    /// <summary>
    /// 在支持异步释放的目标框架上异步释放读取器。
    /// </summary>
    private static async Task DisposeReaderAsync(DbDataReader reader)
    {
#if NET6_0_OR_GREATER
        await reader.DisposeAsync().ConfigureAwait(false);
#else
        reader.Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// 提供只允许枚举一次的数据库行异步序列。
    /// </summary>
    private sealed class ReaderRowsEnumerable : IAsyncEnumerable<TabularRowRecord>
    {
        /// <summary>
        /// 保存待读取的数据库结果集。
        /// </summary>
        private readonly DbDataReader _reader;
        /// <summary>
        /// 保存枚举结束时的读取器所有权策略。
        /// </summary>
        private readonly bool _leaveOpen;
        /// <summary>
        /// 保存创建序列时提供的取消令牌。
        /// </summary>
        private readonly CancellationToken _cancellationToken;
        /// <summary>
        /// 标记读取器是否已经交给一个枚举器。
        /// </summary>
        private int _enumerated;

        /// <summary>
        /// 创建绑定到单个数据库读取器的异步行序列。
        /// </summary>
        internal ReaderRowsEnumerable(DbDataReader reader, bool leaveOpen, CancellationToken cancellationToken)
        {
            _reader = reader;
            _leaveOpen = leaveOpen;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// 创建唯一的行枚举器并合并枚举阶段的取消令牌。
        /// </summary>
        public IAsyncEnumerator<TabularRowRecord> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _enumerated, 1) != 0)
                throw new InvalidOperationException("数据库读取器行序列只能枚举一次。");
            return new ReaderRowsEnumerator(_reader, _leaveOpen, _cancellationToken, cancellationToken);
        }
    }

    /// <summary>
    /// 按需读取单表数据库行并在释放时处理读取器所有权。
    /// </summary>
    private sealed class ReaderRowsEnumerator : IAsyncEnumerator<TabularRowRecord>
    {
        /// <summary>
        /// 保存正在读取的数据库结果集。
        /// </summary>
        private readonly DbDataReader _reader;
        /// <summary>
        /// 指示释放枚举器时是否保留读取器。
        /// </summary>
        private readonly bool _leaveOpen;
        /// <summary>
        /// 保存合并后的读取取消令牌。
        /// </summary>
        private readonly CancellationToken _token;
        /// <summary>
        /// 保存合并令牌的可选来源。
        /// </summary>
        private readonly CancellationTokenSource _linkedCancellation;
        /// <summary>
        /// 标记枚举器是否已经释放。
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 创建读取器行枚举器并合并取消令牌。
        /// </summary>
        internal ReaderRowsEnumerator(DbDataReader reader, bool leaveOpen, CancellationToken methodToken,
            CancellationToken enumerationToken)
        {
            _reader = reader;
            _leaveOpen = leaveOpen;
            if (methodToken.CanBeCanceled && enumerationToken.CanBeCanceled)
            {
                _linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(methodToken, enumerationToken);
                _token = _linkedCancellation.Token;
            }
            else
                _token = methodToken.CanBeCanceled ? methodToken : enumerationToken;
        }

        /// <summary>
        /// 获取最近一次成功读取的独立行记录。
        /// </summary>
        public TabularRowRecord Current { get; private set; }

        /// <summary>
        /// 异步读取下一行并复制二进制单元格。
        /// </summary>
        /// <returns>存在下一行时返回 true。</returns>
        public async ValueTask<bool> MoveNextAsync()
        {
            if (_disposed) return false;
            try
            {
                _token.ThrowIfCancellationRequested();
                var hasNext = await _reader.ReadAsync(_token).ConfigureAwait(false);
                _token.ThrowIfCancellationRequested();
                if (!hasNext)
                {
                    await DisposeAsync().ConfigureAwait(false);
                    return false;
                }
                _token.ThrowIfCancellationRequested();
                var values = new object[_reader.FieldCount];
                for (var ordinal = 0; ordinal < values.Length; ordinal++)
                    values[ordinal] = _reader.GetValue(ordinal);
                Current = new TabularRowRecord(0, DataRowState.Added, null, values);
                return true;
            }
            catch
            {
                await DisposeAfterFailureAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// 按读取器所有权释放枚举器和可选底层读取器。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            Current = null;
            _linkedCancellation?.Dispose();
            if (!_leaveOpen) await DisposeReaderAsync(_reader).ConfigureAwait(false);
        }

        /// <summary>
        /// 释放行读取期间失败的资源，同时保留原始异常。
        /// </summary>
        private async ValueTask DisposeAfterFailureAsync()
        {
            try { await DisposeAsync().ConfigureAwait(false); }
            catch { }
        }
    }
}
