using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bing.Collections;
using Bing.CommandLine;
using Bing.Http;
using Bing.IdUtils;
using Bing.IO;
using Bing.Net.FTP;
using Bing.Serialization.Csv;
using Bing.Serialization.SystemTextJson;
using Bing.Utils.Json;
using Microsoft.AspNetCore.Http;

/// <summary>
/// 流处理能力的跨框架消费者：固定命名参数、null/default 与可选接口入口。
/// </summary>
public static class StreamPlatformContracts
{
    public static async Task Consume(Stream source, Stream output, TextWriter text, DbDataReader reader,
        HttpClient client, IHttpRequest<string> request, IFtpAsyncClient ftp, HttpResponse response,
        IAsyncEnumerable<int> numbers, IAsyncEnumerable<ServerSentEvent> events, string directory,
        CancellationToken token = default)
    {
        var store = new TemporaryFileStore(directory, maxReservedBytes: 10000);
        var entries = store.ListFiles(cancellationToken: token);
        using (var lease = store.CreateFile(100))
        {
            lease.Resize(maxBytes: 200); lease.SetRetention(reason: null);
            lease.Promote(destinationPath: directory + ".final", overwrite: false, cancellationToken: default);
        }
        foreach (var lease in store.CreateFiles(new long[] { 10, 20 }, deleteOnDispose: true, cancellationToken: token)) lease.Dispose();
        await store.CleanupExpiredAsync(DateTime.UtcNow, dryRun: true, cancellationToken: default);
        await using var spill = new SpilloverStream(store, memoryThresholdBytes: 10, maxLength: 1000);
        await using var watcher = new FileWatcher(new[] { directory }, new FileWatcherOptions());
        await watcher.StopAsync();
        _ = JsonStream.ReadArrayAsync<int>(source, readOptions: null, jsonOptions: default, leaveOpen: true, cancellationToken: token);
        _ = CsvStream.ReadAsync(source, options: null, leaveOpen: true, cancellationToken: default);
        _ = CsvStream.ReadAsync(source, options: new CsvOptions { MaxRows = 100, MaxInputBytes = 1024 });
        _ = new CsvRow(new[] { "1" }, new[] { "id" }).Get<int>("id");
        _ = numbers.PrefetchAsync(capacity: 8, cancellationToken: default).BatchAsync(maxCount: 100, maxDelay: null, cancellationToken: token);
        _ = ServerSentEvents.ReadAsync(source, options: null, leaveOpen: true, cancellationToken: default);
        await ServerSentEvents.WriteAsync(output, new ServerSentEvent { Data = "value" }, flush: false, cancellationToken: token);
        _ = client.GetServerSentEventsAsync(new Uri("https://example.test/events"), options: null, cancellationToken: default);
        _ = client.GetServerSentEventsAsync(new Uri("https://example.test/events"), new HttpSseOptions { IdleTimeout = TimeSpan.FromSeconds(30) });
        await response.WriteServerSentEventsAsync(events, cancellationToken: token);
        await using var manager = new HttpDownloadManager(maxConcurrentDownloads: 2, maxDownloadsPerHost: 1, queueCapacity: 8);
        var options = new HttpSegmentedDownloadOptions { TemporaryStore = store, Metrics = new HttpTransferMetrics() };
        await manager.EnqueueAsync(new Uri("https://example.test/file"), request, "target", options: options, cancellationToken: token);
        await request.DownloadFileSegmentedResultAsync("target", options: null, cancellationToken: default);
        await HttpSegmentedDownloadFiles.DiscardAsync("target", store, cancellationToken: token);
        _ = TabularDataReader.ReadSchema(reader);
        _ = TabularDataReader.CreateDetailedSchema(reader, tableName: "Items");
        await TabularDataReader.ForEachResultAsync(reader, (index, current, cancellation) => Task.CompletedTask, leaveOpen: true, cancellationToken: token);
        await TabularDataReader.CopyBinaryFieldAsync(reader, 0, output, maxBytes: 1000, cancellationToken: default);
        await TabularDataReader.CopyTextFieldAsync(reader, 1, text, maxChars: 1000, cancellationToken: token);
        _ = new CommandOptions { StandardInput = (writer, cancellation) => writer.WriteAsync("input"),
            OnStandardOutput = (chunk, cancellation) => Task.CompletedTask, OnStandardError = null };
        _ = Uuid7.TryGetTimestamp(Uuid7.Generate(timestamp: DateTimeOffset.UtcNow), out var milliseconds);
        await ftp.GetDirectoryEntriesAsync("/files", cancellationToken: token);
        await ftp.GetFileChecksumAsync("/files/item", cancellationToken: default);
        await ftp.UploadDirectoryAsync(directory, "/files", conflictPolicy: FtpConflictPolicy.Error, progress: null, cancellationToken: token);
        await ftp.DownloadDirectoryAsync(directory, "/files", conflictPolicy: FtpConflictPolicy.Resume, progress: default, cancellationToken: token);
        await ftp.MoveDirectoryAsync("/files", "/archive", conflictPolicy: FtpConflictPolicy.Skip, cancellationToken: default);
        await ftp.DeleteDirectoryAsync("/files", cancellationToken: token);
        var limits = new FtpDirectoryTransferLimits { MaxFiles = 100, MaxDepth = 3, MaxTotalFileBytes = 1024 };
        await ftp.UploadDirectoryWithLimitsAsync(directory, "/files", limits: limits, progress: null, cancellationToken: default);
        await ftp.DownloadDirectoryWithLimitsAsync(directory, "/files", limits: limits, conflictPolicy: FtpConflictPolicy.Skip, cancellationToken: token);
    }
}
