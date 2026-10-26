using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Bing.Serialization.SystemTextJson;

namespace Bing.Utils.Tests.Serialization;

/// <summary>
/// 验证数组和 NDJSON 的增量、记录限额及资源所有权。
/// </summary>
public class JsonStreamCapabilityTest
{
    /// <summary>
    /// 首项刷新发生在继续枚举前，字符串保持 JSON 值语义。
    /// </summary>
    [Fact]
    public async Task ArrayWritesIncrementally()
    {
        using var output = new MemoryStream();
        async IAsyncEnumerable<string> Source()
        {
            yield return "a"; await Task.Yield();
            Assert.Equal("[\"a\"", Encoding.UTF8.GetString(output.ToArray())); yield return "b";
        }
        await JsonStream.WriteArrayAsync(output, Source(), flushEveryItems: 1);
        Assert.Equal("[\"a\",\"b\"]", Encoding.UTF8.GetString(output.ToArray())); Assert.True(output.CanWrite);
    }

    /// <summary>
    /// BOM、CRLF、短读及无末尾换行可跨读取边界，超限和无效记录报告位置。
    /// </summary>
    [Fact]
    public async Task NdjsonHandlesUtf8AndRecordBudgets()
    {
        using var source = new ShortStream(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("\"中\"\r\n\"文\"" )).ToArray());
        var values = new List<string>();
        await foreach (var value in JsonStream.ReadNdjsonAsync<string>(source, new NdjsonOptions { MaxRecordBytes = 5 })) values.Add(value);
        Assert.Equal(new[] { "中", "文" }, values);
        using var invalid = new MemoryStream(Encoding.UTF8.GetBytes("1\nnot json\n"));
        var ex = await Assert.ThrowsAsync<JsonException>(async () => { await foreach (var value in JsonStream.ReadNdjsonAsync<int>(invalid)) { } });
        Assert.Contains("2", ex.Message);
        using var tooLarge = new ShortStream(Encoding.UTF8.GetBytes("\"中\"\n"));
        await Assert.ThrowsAsync<InvalidDataException>(async () => { await foreach (var value in JsonStream.ReadNdjsonAsync<string>(tooLarge, new NdjsonOptions { MaxRecordBytes = 4 })) { } });
    }

    /// <summary>
    /// 写超限记录不提交该记录，空行策略、提前释放和预取消均遵守契约。
    /// </summary>
    [Fact]
    public async Task NdjsonOwnershipAndAtomicRecordBudget()
    {
        async IAsyncEnumerable<int> Source([EnumeratorCancellation] CancellationToken token = default)
        { yield return 1; await Task.Yield(); token.ThrowIfCancellationRequested(); yield return 123; }
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => JsonStream.WriteNdjsonAsync(output, Source(), new NdjsonOptions { MaxRecordBytes = 1 }));
        Assert.Equal("1\n", Encoding.UTF8.GetString(output.ToArray())); Assert.True(output.CanWrite);
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("\n1\n2\n"));
        await foreach (var value in JsonStream.ReadNdjsonAsync<int>(source, new NdjsonOptions { IgnoreEmptyLines = true }, leaveOpen: false)) { Assert.Equal(1, value); break; }
        Assert.False(source.CanRead);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => JsonStream.WriteArrayAsync(output, Source(), leaveOpen: false, cancellationToken: canceled.Token));
        Assert.True(output.CanWrite);
    }

    /// <summary>
    /// 不支持定位、每次只读一个字节的源。
    /// </summary>
    private sealed class ShortStream : MemoryStream
    {
        public ShortStream(byte[] data) : base(data) { }
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);
    }
}
