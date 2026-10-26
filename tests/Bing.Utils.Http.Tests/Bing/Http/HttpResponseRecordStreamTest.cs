using System.Text;
using System.Threading;
using Bing.Http;
using Microsoft.AspNetCore.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;

/// <summary>
/// 验证 HTTP 响应记录流适配器。
/// </summary>
public class HttpResponseRecordStreamTest
{
    /// <summary>
    /// 数组和 NDJSON 响应适配器保留正文并响应请求中止。
    /// </summary>
    [Fact]
    public async Task ResponseAdaptersWriteRecordsAndHonorAbort()
    {
        async IAsyncEnumerable<int> Source() { yield return 1; await Task.Yield(); yield return 2; }
        var context = new DefaultHttpContext(); using var body = new MemoryStream(); context.Response.Body = body;
        await context.Response.WriteJsonArrayStreamAsync(Source(), flushEveryItems: 1);
        Assert.Equal("[1,2]", Encoding.UTF8.GetString(body.ToArray())); Assert.True(body.CanWrite);
        body.SetLength(0);
        await context.Response.WriteNdjsonStreamAsync(Source());
        Assert.Equal("1\n2\n", Encoding.UTF8.GetString(body.ToArray())); Assert.Contains("ndjson", context.Response.ContentType);
        using var abort = new CancellationTokenSource(); abort.Cancel(); context.RequestAborted = abort.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Response.WriteJsonArrayStreamAsync(Source()));
    }
}
