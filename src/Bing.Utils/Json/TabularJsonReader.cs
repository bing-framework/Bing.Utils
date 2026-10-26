using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

/// <summary>
/// 顺序读取结构先行的 v2 表格文档。
/// </summary>
/// <remarks>只允许一次枚举，不支持并发读取；提前结束会释放读取器。完整性仅在读到文档末尾后成立。</remarks>
public sealed class TabularJsonReader : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 保存底层流及所有权策略。
    /// </summary>
    private readonly Stream _source;
    /// <summary>
    /// 指示释放时保留底层流。
    /// </summary>
    private readonly bool _leaveOpen;
    /// <summary>
    /// 保存原始调用取消令牌。
    /// </summary>
    private readonly CancellationToken _token;
    /// <summary>
    /// 保存固定配置。
    /// </summary>
    private readonly TabularJsonOptions _options;
    /// <summary>
    /// 保存有界字节记录读取器。
    /// </summary>
    private readonly TabularJsonFrameReader _frames;
    /// <summary>
    /// 指示枚举已启动。
    /// </summary>
    private int _started;
    /// <summary>
    /// 指示资源已释放。
    /// </summary>
    private bool _disposed;
    /// <summary>
    /// 获取读取到的结构快照。
    /// </summary>
    public TabularSchema Schema { get; private set; }
    /// <summary>
    /// 获取是否已经验证全部行、计数和文档结尾；不代表跨行数据库约束已验证。
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// 创建已通过参数校验的读取器。
    /// </summary>
    internal TabularJsonReader(Stream source, TabularJsonOptions options, bool leaveOpen, CancellationToken token)
    {
        _source = source; _options = options; _leaveOpen = leaveOpen; _token = token;
        _frames = new TabularJsonFrameReader(source, options);
    }

    /// <summary>
    /// 读取固定格式头和完整结构，定位到行数组。
    /// </summary>
    internal async Task InitializeAsync()
    {
        await _frames.SkipBomAsync(_token).ConfigureAwait(false);
        if (await _frames.NextAsync(_token).ConfigureAwait(false) == 'n')
        {
            var value = await _frames.ValueAsync(4, _token).ConfigureAwait(false);
            if (value.Type != JTokenType.Null || await _frames.NextAsync(_token).ConfigureAwait(false) != -1)
                throw new JsonSerializationException("表格 null 文档无效。");
            IsComplete = true; return;
        }
        await _frames.ExpectAsync('{', _token).ConfigureAwait(false);
        await _frames.PropertyAsync("$format", _token).ConfigureAwait(false);
        var format = await _frames.ValueAsync(64, _token).ConfigureAwait(false);
        if (format.Type != JTokenType.String || (string)format != "bing.tabular") throw new JsonSerializationException("表格格式无效。");
        await _frames.ExpectAsync(',', _token).ConfigureAwait(false);
        await _frames.PropertyAsync("$version", _token).ConfigureAwait(false);
        var version = await _frames.ValueAsync(16, _token).ConfigureAwait(false);
        if (version.Type != JTokenType.Integer || (int)version != 2) throw new JsonSerializationException("逐行读取只支持 v2。");
        await _frames.ExpectAsync(',', _token).ConfigureAwait(false);
        await _frames.PropertyAsync("schema", _token).ConfigureAwait(false);
        var model = await _frames.ValueAsync(_options.MaxSchemaBytes, _token).ConfigureAwait(false);
        Schema = TabularJson.ParseSchema(model as JObject ?? throw new JsonSerializationException("缺少结构对象。"), _options);
        await _frames.ExpectAsync(',', _token).ConfigureAwait(false);
        await _frames.PropertyAsync("rows", _token).ConfigureAwait(false);
        await _frames.ExpectAsync('[', _token).ConfigureAwait(false);
    }

    /// <summary>
    /// 按需读取独立行记录；所有行消费完成后检查总数和尾部。
    /// </summary>
    /// <param name="cancellationToken">与创建读取器时的令牌共同生效。</param>
    /// <returns>单次枚举的行序列；失败不回滚已交付行。</returns>
    public async IAsyncEnumerable<TabularRowRecord> ReadRowsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TabularJsonReader));
        if (Interlocked.Exchange(ref _started, 1) != 0) throw new InvalidOperationException("表格读取器只能枚举一次。");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        var token = linked.Token;
        var failed = false;
        await using var rows = ReadRowsCoreAsync(token).GetAsyncEnumerator(token);
        try
        {
            while (true)
            {
                bool hasNext;
                try { hasNext = await rows.MoveNextAsync().ConfigureAwait(false); }
                catch { failed = true; throw; }
                if (!hasNext) break;
                yield return rows.Current;
            }
        }
        finally { TabularJson.DisposeResource(this, failed); }
    }

    /// <summary>
    /// 解析行及结束标记；资源释放由外层枚举器负责，以保留主异常。
    /// </summary>
    private async IAsyncEnumerable<TabularRowRecord> ReadRowsCoreAsync([EnumeratorCancellation] CancellationToken token)
    {
        var counts = new long[Schema?.TableCount ?? 0];
        var first = true;
        token.ThrowIfCancellationRequested();
        if (Schema == null) yield break;
        while (await _frames.NextAsync(token).ConfigureAwait(false) != ']')
        {
            if (!first) await _frames.ExpectAsync(',', token).ConfigureAwait(false);
            var model = await _frames.ValueAsync(_options.MaxRowBytes, token).ConfigureAwait(false);
            var row = TabularJson.DecodeRecord(Schema, model as JObject ?? throw new JsonSerializationException("行记录应为对象。"), _options);
            if (_options.MaxRowsPerTable.HasValue && ++counts[row.TableIndex] > _options.MaxRowsPerTable.Value)
                throw new JsonSerializationException("行数超过限额。");
            if (!_options.MaxRowsPerTable.HasValue) counts[row.TableIndex]++;
            first = false;
            yield return row;
        }
        await _frames.ExpectAsync(']', token).ConfigureAwait(false);
        await _frames.ExpectAsync(',', token).ConfigureAwait(false);
        await _frames.PropertyAsync("counts", token).ConfigureAwait(false);
        var footer = await _frames.ValueAsync(_options.MaxSchemaBytes, token).ConfigureAwait(false) as JArray;
        if (footer == null || footer.Count != counts.Length || footer.Where((v, i) => v.Type != JTokenType.Integer || (long)v != counts[i]).Any())
            throw new JsonSerializationException("表格行计数不一致。");
        await _frames.ExpectAsync('}', token).ConfigureAwait(false);
        if (await _frames.NextAsync(token).ConfigureAwait(false) != -1) throw new JsonSerializationException("表格 JSON 包含额外根内容。");
        IsComplete = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_leaveOpen) _source.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() { Dispose(); return default; }
}
