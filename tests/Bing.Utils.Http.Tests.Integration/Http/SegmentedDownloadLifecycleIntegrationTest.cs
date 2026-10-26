using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using Bing.Http;
using Bing.Http.Clients;
using Bing.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>通过实际 ASP.NET 请求链验证接口入口、检查点恢复与限速流组合。</summary>
public class SegmentedDownloadLifecycleIntegrationTest
{
    [Fact]
    public async Task InterfaceTaskResumesMissingSegmentsAndPreservesLimits()
    {
        var root = Path.Combine(Path.GetTempPath(), "bing-lifecycle-integration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); var target = Path.Combine(root, "file.bin");
        var body = Encoding.UTF8.GetBytes("ABCDEFGH"); var fail = true;
        var counts = new ConcurrentDictionary<string, int>();
        using var server = new TestServer(new WebHostBuilder().Configure(app => app.Run(async context =>
        {
            context.Response.Headers["Accept-Ranges"] = "bytes"; context.Response.Headers["ETag"] = "\"stable\"";
            var range = context.Request.Headers["Range"].ToString();
            if (string.IsNullOrEmpty(range))
            {
                context.Response.ContentLength = body.Length;
                if (context.Request.Method != "HEAD") await context.Response.Body.WriteAsync(body, 0, body.Length);
                return;
            }
            counts.AddOrUpdate(range, 1, (_, n) => n + 1);
            var parts = range.Substring(6).Split('-'); var start = int.Parse(parts[0]); var end = int.Parse(parts[1]);
            if (start == 4 && fail) { context.Response.StatusCode = 503; return; }
            context.Response.StatusCode = 206; context.Response.ContentLength = end - start + 1;
            context.Response.Headers["Content-Range"] = $"bytes {start}-{end}/{body.Length}";
            await context.Response.Body.WriteAsync(body, start, end - start + 1);
        })));
        using var client = server.CreateClient();
        try
        {
            IHttpRequest<string> request = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file");
            var config = new HttpSegmentedDownloadOptions { SegmentSize = 4, MaxConcurrency = 1, RateLimiter = new TransferRateLimiter(1000000, 2) };
            await using var task = request.CreateSegmentedDownloadTask(target, config);
            var first = await task.StartAsync();
            Assert.True(first.Segments[0].Completed); Assert.False(first.Segments[1].Completed);
            Assert.Equal(HttpSegmentedDownloadTaskStatus.Incomplete, task.Status); Assert.False(File.Exists(target));
            Assert.NotNull(task.InspectCheckpoint().State);
            fail = false; config.SegmentSize = 1;
            await task.ResumeAsync();
            Assert.Equal(HttpSegmentedDownloadTaskStatus.Completed, task.Status);
            Assert.Equal(body, File.ReadAllBytes(target)); Assert.Equal(1, counts["bytes=0-3"]); Assert.Equal(2, counts["bytes=4-7"]);
            Assert.Null(task.InspectCheckpoint().State);
            IHttpRequest<string> limited = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file").MaxDownloadBytes(3);
            await Assert.ThrowsAsync<HttpDownloadSizeLimitException>(() => limited.DownloadFileSegmentedAsync(Path.Combine(root, "over.bin")));
            Assert.False(File.Exists(Path.Combine(root, "over.bin")));
        }
        finally { Directory.Delete(root, true); }
    }
}
