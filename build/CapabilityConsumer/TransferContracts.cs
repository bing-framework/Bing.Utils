using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bing.Http;
using Bing.IO;
using Bing.Utils.Json;

/// <summary>本批新增能力在四个目标框架上的消费者编译契约。</summary>
public static class TransferContracts
{
    public static async Task Consume(IHttpRequest<string> request, DbDataReader input, Stream destination,
        IAsyncEnumerable<TabularRowRecord> rows, string directory, CancellationToken token = default)
    {
        var limiter = new TransferRateLimiter(bytesPerSecond: 1024, burstBytes: 512);
        limiter.BytesPerSecond = 2048;
        using var limited = new RateLimitedStream(destination, limiter, leaveOpen: true);
        await limiter.WaitAsync(byteCount: 0, cancellationToken: default);
        var options = new HttpSegmentedDownloadOptions { RateLimiter = limiter };
        await request.DownloadFileSegmentedAsync("target", options: null, cancellationToken: default);
        await using var task = request.CreateSegmentedDownloadTask("target", options);
        await task.StartAsync(cancellationToken: token);
        await task.PauseAsync();
        _ = task.InspectCheckpoint();
        var store = new TemporaryFileStore(directory, maxReservedBytes: 1024);
        using var lease = store.CreateFile(maxBytes: 512, deleteOnDispose: true);
        _ = store.GetStatistics();
        _ = store.CleanupExpired(System.DateTime.UtcNow, dryRun: true);
        var schema = TabularDataReader.CreateSchema(input, tableName: "Result");
        _ = TabularDataReader.ReadRowsAsync(input, leaveOpen: true, cancellationToken: default);
        using var reader = TabularDataReader.CreateReader(schema, rows, cancellationToken: token);
        await TabularDataReader.WriteTabularAsync(destination, input, options: null, cancellationToken: default);
        await TabularDataReader.WriteJsonArrayAsync(destination, input, options: default, leaveReaderOpen: true, leaveStreamOpen: true, cancellationToken: token);
        await TabularDataReader.WriteNdjsonAsync(destination, input, options: null, cancellationToken: default);
    }
}
