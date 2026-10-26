using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Bing.Serialization.Csv;

namespace Bing.Utils.Tests.Serialization;

/// <summary>
/// 验证 CSV/TSV 流式解析、预算、转换和资源所有权。
/// </summary>
public class CsvStreamTest
{
    /// <summary>
    /// 短读不可定位流支持 BOM、制表分隔配置、转义、多行和空字段。
    /// </summary>
    [Fact]
    public async Task ReadsShortNonSeekableCsvWithHeaderAndCulture()
    {
        var input = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(
            "name;amount;notes\r\n\"Zoë\";\"1,25\";\"first\r\nsecond\"\r\n;;\r\n")).ToArray();
        using var source = new ShortReadStream(input, 1);
        var options = new CsvOptions { Delimiter = ';' };
        var rows = new List<CsvRow>();
        await foreach (var row in CsvStream.ReadAsync(source, options)) rows.Add(row);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Zoë", rows[0].Get<string>("NAME"));
        Assert.Equal(1.25m, rows[0].Get<decimal>("amount", CultureInfo.GetCultureInfo("fr-FR")));
        Assert.Equal("first\r\nsecond", rows[0].Get<string>("notes"));
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal(string.Empty, rows[1][0]);
        Assert.Equal(string.Empty, rows[1][2]);
    }

    /// <summary>
    /// 严格拒绝错误引号、超限字段、超限记录和超限字段数量，并报告位置。
    /// </summary>
    [Fact]
    public async Task RejectsMalformedRowsAndBudgetOverrunsWithLocation()
    {
        using var malformed = new MemoryStream(Encoding.UTF8.GetBytes("a,b\n\"x\"q,y\n"));
        var malformedException = await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(malformed)) { }
        });
        Assert.Equal(2, malformedException.RowNumber);
        Assert.Equal(1, malformedException.ColumnNumber);

        using var fieldLimit = new MemoryStream(Encoding.UTF8.GetBytes("h\n\"xy\"\n"));
        var fieldException = await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(fieldLimit, new CsvOptions { MaxFieldBytes = 3 })) { }
        });
        Assert.Equal(2, fieldException.RowNumber);
        Assert.Equal(1, fieldException.ColumnNumber);

        using var rowLimit = new MemoryStream(Encoding.UTF8.GetBytes("a,b\n"));
        var rowException = await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(rowLimit,
                               new CsvOptions { HasHeader = false, MaxRowBytes = 2 })) { }
        });
        Assert.Equal(1, rowException.RowNumber);
        Assert.Equal(2, rowException.ColumnNumber);

        using var fieldCount = new MemoryStream(Encoding.UTF8.GetBytes("a,b,c\n"));
        var countException = await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(fieldCount,
                               new CsvOptions { HasHeader = false, MaxFieldsPerRow = 2 })) { }
        });
        Assert.Equal(3, countException.ColumnNumber);
    }

    /// <summary>
    /// 输入字节预算精确统计 BOM、表头、数据和 CRLF，并拒绝零预算下的非空输入。
    /// </summary>
    [Fact]
    public async Task InputByteBudgetCountsBomHeaderAndLineEndings()
    {
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("h\r\n1\r\n2\r\n")).ToArray();
        using var exact = new ShortReadStream(content, 1);
        var rows = new List<CsvRow>();
        await foreach (var row in CsvStream.ReadAsync(exact, new CsvOptions { MaxInputBytes = content.Length }))
        {
            rows.Add(row);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal("1", rows[0][0]);
        Assert.Equal("2", rows[1][0]);

        using var overLimit = new ShortReadStream(content, 2);
        var delivered = new List<CsvRow>();
        var exception = await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(overLimit,
                               new CsvOptions { MaxInputBytes = content.Length - 1 }))
            {
                delivered.Add(row);
            }
        });

        Assert.Contains("输入字节数", exception.Message);
        Assert.Single(delivered);
        Assert.Equal("1", delivered[0][0]);
    }

    /// <summary>
    /// 零输入预算只接受空流，零行预算允许表头但不交付数据行。
    /// </summary>
    [Fact]
    public async Task ZeroBudgetsAcceptOnlyEmptyInputOrHeader()
    {
        using var empty = new MemoryStream();
        var emptyRows = new List<CsvRow>();
        await foreach (var row in CsvStream.ReadAsync(empty, new CsvOptions { MaxInputBytes = 0, MaxRows = 0 }))
        {
            emptyRows.Add(row);
        }
        Assert.Empty(emptyRows);

        using var nonEmpty = new MemoryStream(Encoding.UTF8.GetBytes("x"));
        await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(nonEmpty,
                               new CsvOptions { MaxInputBytes = 0, HasHeader = false })) { }
        });

        using var noHeaderData = new MemoryStream(Encoding.UTF8.GetBytes("1\n"));
        var noHeaderRows = new List<CsvRow>();
        await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(noHeaderData,
                               new CsvOptions { MaxRows = 0, HasHeader = false }))
            {
                noHeaderRows.Add(row);
            }
        });
        Assert.Empty(noHeaderRows);

        using var headerOnly = new MemoryStream(Encoding.UTF8.GetBytes("h\r\n"));
        var headerRows = new List<CsvRow>();
        await foreach (var row in CsvStream.ReadAsync(headerOnly,
                           new CsvOptions { MaxInputBytes = 3, MaxRows = 0 }))
        {
            headerRows.Add(row);
        }
        Assert.Empty(headerRows);
    }

    /// <summary>
    /// 读取预算不允许交付超限数据行，写入预算不提交超限行且不限制输入字节选项。
    /// </summary>
    [Fact]
    public async Task RowBudgetLimitsReadAndWriteDataRowsOnly()
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("id\n1\n2\n"));
        var delivered = new List<CsvRow>();
        await Assert.ThrowsAsync<CsvFormatException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(input, new CsvOptions { MaxRows = 1 }))
            {
                delivered.Add(row);
            }
        });
        Assert.Single(delivered);
        Assert.Equal("1", delivered[0][0]);

        async IAsyncEnumerable<CsvRow> TwoRows()
        {
            yield return new CsvRow(new[] { "1" }, new[] { "id" });
            await Task.Yield();
            yield return new CsvRow(new[] { "2" }, new[] { "id" });
        }

        using var headerOnlyOutput = new MemoryStream();
        await Assert.ThrowsAsync<CsvFormatException>(() => CsvStream.WriteAsync(headerOnlyOutput,
            TwoRows(), new CsvOptions { MaxRows = 0, NewLine = "\n" }));
        Assert.Equal("id\n", Encoding.UTF8.GetString(headerOnlyOutput.ToArray()));

        using var output = new MemoryStream();
        await Assert.ThrowsAsync<CsvFormatException>(() => CsvStream.WriteAsync(output, TwoRows(),
            new CsvOptions { MaxInputBytes = 0, MaxRows = 1, NewLine = "\n" }));
        Assert.Equal("id\n1\n", Encoding.UTF8.GetString(output.ToArray()));
    }

    /// <summary>
    /// 字节预算和行数预算拒绝负值。
    /// </summary>
    [Fact]
    public async Task NegativeBudgetsAreRejected()
    {
        using var input = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvStream.ReadAsync(input,
            new CsvOptions { MaxInputBytes = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CsvStream.ReadAsync(input,
            new CsvOptions { MaxRows = -1 }));

        using var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CsvStream.WriteAsync(output, Rows(),
            new CsvOptions { MaxRows = -1 }));
    }

    /// <summary>
    /// 写入转义逗号、双引号和换行，并在同一格式下逐条读回。
    /// </summary>
    [Fact]
    public async Task WritesQuotedRowsAndReadsThemBack()
    {
        using var output = new MemoryStream();
        await CsvStream.WriteAsync(output, Rows(), new CsvOptions { NewLine = "\n" });

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Equal("name,description\nAlice,\"a,\"\"b\"\"\nline\"\n", text);
        Assert.True(output.CanWrite);
        output.Position = 0;
        var readBack = new List<CsvRow>();
        await foreach (var row in CsvStream.ReadAsync(output)) readBack.Add(row);
        Assert.Equal("a,\"b\"\nline", readBack[0].Get<string>("description"));
    }

    /// <summary>
    /// 写入端在提交一条记录前校验字段与记录预算。
    /// </summary>
    [Fact]
    public async Task WriteBudgetFailureDoesNotCommitCurrentRecord()
    {
        using var fieldLimited = new MemoryStream();
        await Assert.ThrowsAsync<CsvFormatException>(() => CsvStream.WriteAsync(fieldLimited, Rows(),
            new CsvOptions { WriteHeader = false, MaxFieldBytes = 3 }));
        Assert.Empty(fieldLimited.ToArray());

        async IAsyncEnumerable<CsvRow> TwoFields()
        {
            yield return new CsvRow(new[] { "a", "b" });
            await Task.Yield();
        }

        using var rowLimited = new MemoryStream();
        await Assert.ThrowsAsync<CsvFormatException>(() => CsvStream.WriteAsync(rowLimited, TwoFields(),
            new CsvOptions { WriteHeader = false, MaxRowBytes = 2 }));
        Assert.Empty(rowLimited.ToArray());
    }

    /// <summary>
    /// 类型转换错误保留行列上下文，表头配置拒绝大小写重复列。
    /// </summary>
    [Fact]
    public async Task ConversionAndHeaderErrorsHaveContext()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("id,amount\n1,nope\n"));
        CsvRow row = null;
        await foreach (var value in CsvStream.ReadAsync(source)) row = value;
        var conversion = Assert.Throws<CsvFormatException>(() => row.Get<decimal>("amount"));
        Assert.Equal(2, conversion.RowNumber);
        Assert.Equal(2, conversion.ColumnNumber);

        using var empty = new MemoryStream();
        Assert.Throws<ArgumentException>(() => CsvStream.ReadAsync(empty,
            new CsvOptions { Headers = new[] { "id", "ID" } }));
    }

    /// <summary>
    /// 预取消不接管资源，执行中取消和提前结束在 leaveOpen=false 时关闭源。
    /// </summary>
    [Fact]
    public async Task CancellationAndEarlyStopHonorStreamOwnership()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var beforeStart = new ShortReadStream(Encoding.UTF8.GetBytes("a\n"), 1);
        Assert.ThrowsAny<OperationCanceledException>(() =>
            CsvStream.ReadAsync(beforeStart, leaveOpen: false, cancellationToken: canceled.Token));
        Assert.True(beforeStart.CanRead);

        using var duringReadCancellation = new CancellationTokenSource();
        var blocking = new BlockingStream();
        var enumerator = CsvStream.ReadAsync(blocking,
                new CsvOptions { MaxInputBytes = 1, MaxRows = 1 }, leaveOpen: false,
                cancellationToken: duringReadCancellation.Token)
            .GetAsyncEnumerator();
        var move = enumerator.MoveNextAsync().AsTask();
        await blocking.ReadStarted.Task;
        duringReadCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => move);
        Assert.False(blocking.CanRead);
        await enumerator.DisposeAsync();

        var earlyStop = new ShortReadStream(Encoding.UTF8.GetBytes("h\n1\n2\n"), 2);
        await foreach (var row in CsvStream.ReadAsync(earlyStop, leaveOpen: false)) break;
        Assert.False(earlyStop.CanRead);
    }

    /// <summary>
    /// 底层读取返回数据时触发取消，不交付尚未解析的记录并保留原令牌。
    /// </summary>
    [Fact]
    public async Task CancellationAfterBufferedReadPreventsRowDelivery()
    {
        using var cancellation = new CancellationTokenSource();
        var readCount = 0;
        using var source = new ShortReadStream(Encoding.UTF8.GetBytes("h\n1\n"), 1, () =>
        {
            if (Interlocked.Increment(ref readCount) == 4)
                cancellation.Cancel();
        });
        var rows = new List<CsvRow>();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in CsvStream.ReadAsync(source,
                               new CsvOptions { MaxInputBytes = 4, MaxRows = 1 },
                               cancellationToken: cancellation.Token))
            {
                rows.Add(row);
            }
        });

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Empty(rows);
    }

    private static async IAsyncEnumerable<CsvRow> Rows([EnumeratorCancellation] CancellationToken token = default)
    {
        yield return new CsvRow(new[] { "Alice", "a,\"b\"\nline" }, new[] { "name", "description" });
        await Task.Yield();
        token.ThrowIfCancellationRequested();
    }

    private sealed class ShortReadStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _maximumRead;
        private readonly Action _afterRead;
        private int _position;
        private bool _disposed;

        internal ShortReadStream(byte[] data, int maximumRead, Action afterRead = null)
        {
            _data = data;
            _maximumRead = maximumRead;
            _afterRead = afterRead;
        }

        public override bool CanRead => !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ShortReadStream));
            var read = Math.Min(Math.Min(count, _maximumRead), _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, read);
            _position += read;
            _afterRead?.Invoke();
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(buffer, offset, count));
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class BlockingStream : Stream
    {
        private bool _disposed;
        internal TaskCompletionSource<bool> ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadStarted.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
