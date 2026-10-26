using System.Text.Json;
using System.Threading;
using Bing.Serialization.SystemTextJson;

namespace Bing.Tests.Serialization;

public class JsonArrayReaderTest
{
    [Fact]
    public async Task ShortReadsNestedStringsNullAndNonzeroPosition()
    {
        using var source = new ShortStream("skip[ {\"s\":\"a]\\\"b\",\"v\":[1,2]},null,3] \r\n", 4);
        var values = await Collect(JsonStream.ReadArrayAsync<JsonElement>(source));
        Assert.Equal(3, values.Count); Assert.Equal("a]\"b", values[0].GetProperty("s").GetString());
        Assert.Equal(JsonValueKind.Null, values[1].ValueKind); Assert.Equal(3, values[2].GetInt32());
        Assert.True(source.CanRead);
    }
    [Theory]
    [InlineData("[]", 0, 2, true)]
    [InlineData("[123]", 3, 5, true)]
    [InlineData("[123]", 2, 5, false)]
    [InlineData("[123]", 3, 4, false)]
    public async Task InclusiveItemAndTotalBudgets(string text, long item, long total, bool success)
    {
        using var source = new ShortStream(text);
        var operation = Collect(JsonStream.ReadArrayAsync<int>(source, new JsonArrayReadOptions { MaxItemBytes = item, MaxInputBytes = total }));
        if (success) await operation; else await Assert.ThrowsAsync<InvalidDataException>(() => operation);
    }
    [Theory]
    [InlineData("[1,]")][InlineData("[1]x")][InlineData("[1 2]")][InlineData("[\"bad")][InlineData("[1,{]}")]
    public async Task InvalidInputHasItemAndByteContext(string text)
    {
        using var source = new ShortStream(text);
        var error = await Assert.ThrowsAsync<JsonException>(() => Collect(JsonStream.ReadArrayAsync<JsonElement>(source)));
        Assert.Contains("字节", error.Message); Assert.Contains("项", error.Message);
    }
    [Fact]
    public async Task BomTrailingCommaCancellationAndEarlyDisposal()
    {
        var source = new ShortStream("\ufeff[1,]");
        Assert.Single(await Collect(JsonStream.ReadArrayAsync<int>(source, jsonOptions: new JsonSerializerOptions { AllowTrailingCommas = true }, leaveOpen: false)));
        Assert.False(source.CanRead);
        source = new ShortStream("[1,2]");
        await using (var enumerator = JsonStream.ReadArrayAsync<int>(source, leaveOpen: false).GetAsyncEnumerator())
            Assert.True(await enumerator.MoveNextAsync());
        Assert.False(source.CanRead);
        using var cts = new CancellationTokenSource(); cts.Cancel(); source = new ShortStream("[]");
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Collect(JsonStream.ReadArrayAsync<int>(source, leaveOpen: false, cancellationToken: cts.Token)));
        Assert.Equal(cts.Token, ex.CancellationToken); Assert.True(source.CanRead); source.Dispose();
    }
    private static async Task<List<T>> Collect<T>(IAsyncEnumerable<T> source) { var values = new List<T>(); await foreach (var value in source) values.Add(value); return values; }
    private sealed class ShortStream : MemoryStream
    {
        public ShortStream(string text, int start = 0) : base(Encoding.UTF8.GetBytes(text)) { Position = start; }
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => base.ReadAsync(buffer, offset, Math.Min(2, count), token);
    }
}
