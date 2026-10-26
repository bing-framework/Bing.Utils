using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bing.Collections;
using Bing.Http;
using Bing.IO;
using Bing.Serialization.SystemTextJson;
using Bing.Utils.Json;

/// <summary>
/// 四个目标框架上的命名参数、null/default 和可选能力消费者编译合同。
/// </summary>
public static class Contracts
{
    public static async Task Consume(Stream source, Stream destination, IAsyncEnumerable<int> values,
        IHttpRequest<string> legacyCompatibleRequest, CancellationToken token = default)
    {
        await StreamTransfer.CopyAsync(source, destination, options: null, leaveOpen: true, cancellationToken: default);
        await JsonStream.WriteArrayAsync(destination, values, options: null, flushEveryItems: 1, cancellationToken: token);
        await JsonStream.WriteNdjsonAsync(destination, values, recordOptions: null, jsonOptions: null, cancellationToken: default);
        await values.ForEachAwaitAsync((item, cancellation) => Task.CompletedTask, maxDegreeOfParallelism: 2, cancellationToken: token);
        var converted = values.SelectConcurrentAsync((item, cancellation) => Task.FromResult(item), 2, preserveOrder: false, cancellationToken: default);
        var json = TabularJson.SerializeTable(default(DataTable), options: null);
        using var table = TabularJson.DeserializeTable(json, options: default);
        using var rowReader = await TabularJson.OpenReaderAsync(source, options: null, leaveOpen: true, cancellationToken: token);
        if (rowReader.Schema != null)
            await TabularJson.WriteRowsAsync(destination, rowReader.Schema, rowReader.ReadRowsAsync(cancellationToken: default), options: null, cancellationToken: token);
        using var smallPages = new LargeMemoryStream();
        // 新能力保持在可选接口及扩展层；旧接口实现无需新添成员。
        if (legacyCompatibleRequest is IHttpRequestDownloadLimitPolicy<string>)
            legacyCompatibleRequest.MaxDownloadBytes(100).WithoutMaxDownloadLimit();
        await legacyCompatibleRequest.GetBytesAsync(cancellationToken: default);
        var clock = new Bing.Date.Clock.FixedClock(DateTimeOffset.UtcNow);
        var calendar = new Bing.Date.Calendars.BusinessCalendar(holidays: null, workingDayOverrides: null);
        _ = calendar.AddBusinessDays(clock.UtcNow.DateTime, 0);
        _ = Bing.IdUtils.SecureRandomId.Create(16, Bing.IdUtils.SecureRandomId.AlphaNumericAlphabet);
    }
}
