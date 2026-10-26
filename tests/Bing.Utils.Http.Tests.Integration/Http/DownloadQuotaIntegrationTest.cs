using System.Net.Http;
using System.Text;
using Bing.Http;
using Bing.Http.Clients;
using Bing.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Bing.Utils.Http.Tests.Integration.Http;

public class DownloadQuotaIntegrationTest
{
    [Fact]
    public async Task IncompleteTaskKeepsPeakReservationAndResumeReleasesIt()
    {
        var root = Path.Combine(Path.GetTempPath(), "bing-quota-integration-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var fail = true; var bytes = Encoding.UTF8.GetBytes("abcdefgh"); var ranges = 0;
        using var server = new TestServer(new WebHostBuilder().Configure(app => app.Run(async context =>
        {
            context.Response.Headers["Accept-Ranges"] = "bytes"; context.Response.Headers["ETag"] = "\"stable\"";
            var range = context.Request.Headers["Range"].ToString();
            if (range.Length == 0) { context.Response.ContentLength = bytes.Length; await context.Response.Body.WriteAsync(bytes, 0, bytes.Length); return; }
            ranges++; var parts = range.Substring(6).Split('-'); var first = int.Parse(parts[0]); var last = int.Parse(parts[1]);
            if (fail && first == 4) { context.Response.StatusCode = 503; return; }
            context.Response.StatusCode = 206; context.Response.ContentLength = last - first + 1;
            context.Response.Headers["Content-Range"] = $"bytes {first}-{last}/{bytes.Length}";
            await context.Response.Body.WriteAsync(bytes, first, last - first + 1);
        })));
        using var client = server.CreateClient();
        try
        {
            var store = new TemporaryFileStore(Path.Combine(root, "store"), 16); var metrics = new HttpTransferMetrics();
            var options = new HttpSegmentedDownloadOptions { SegmentSize = 4, MaxConcurrency = 1, TemporaryStore = store, Metrics = metrics,
                RateLimiter = new TransferRateLimiter(1000000, 2) };
            IHttpRequest<string> request = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file");
            var path = Path.Combine(root, "file.bin");
            var incomplete = await request.DownloadFileSegmentedResultAsync(path, options);
            Assert.False(incomplete.IsComplete); Assert.Single(incomplete.FailedSegments); Assert.Single(incomplete.Failures);
            Assert.Equal(16, store.GetStatistics().ReservedBytes); Assert.Equal(1, store.CleanupExpired(DateTime.UtcNow.AddDays(1), false).SkippedFiles);
            await Assert.ThrowsAsync<IOException>(() => request.DownloadFileSegmentedResultAsync(Path.Combine(root, "other.bin"), options));
            Assert.Equal(2, ranges);
            fail = false; var complete = await request.DownloadFileSegmentedResultAsync(path, options);
            Assert.True(complete.IsComplete); Assert.Null(complete.RecoveryPath); Assert.Equal(bytes, File.ReadAllBytes(path));
            Assert.Equal(0, store.GetStatistics().ReservedBytes); Assert.Equal(8, metrics.BytesRead); Assert.Equal(3, ranges);
            Assert.True(metrics.CheckpointTime > TimeSpan.Zero); Assert.True(metrics.MergeTime > TimeSpan.Zero); Assert.True(metrics.ThrottleWaitTime > TimeSpan.Zero);
        }
        finally { Directory.Delete(root, true); }
    }
}
