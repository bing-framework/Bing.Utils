using System.Text;
using System.Threading;
using Bing.Http.Clients;
using Bing.Utils.Http.Tests.Integration.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>
/// 通过真实 multipart 解析和文件读写验证上传文件名与请求隔离。
/// </summary>
[Trait("Category", "Regression")]
[Trait("Bing.Http", "HttpUpload.Reliability")]
public class HttpUploadReliabilityTest
{
    /// <summary>
    /// 只有文件名、没有目录的上传文件应完整落盘，不丢失二进制内容。
    /// </summary>
    [Fact]
    public async Task FileContent_FileNameWithoutDirectory_PreservesNameAndBytes()
    {
        using var server = CreateServer();
        using var httpClient = server.CreateClient();
        var client = new HttpClientService().SetHttpClient(httpClient);
        var bytes = new byte[] { 0, 1, 127, 128, 255 };
        using var stream = new MemoryStream(bytes);

        var result = await client.Post<List<Test6Controller.UploadReceipt>>("/api/test6/inspect")
            .FileContent(stream, "upload.bin", "file")
            .GetResultAsync();

        result.Count.ShouldBe(1);
        AssertReceipt(result[0], "file", "upload.bin", bytes);
    }

    /// <summary>
    /// 同一请求中不同字段下的同名文件应保留各自内容，不按文件名去重或覆盖。
    /// </summary>
    /// <remarks>FileContent 按字段名替换已有参数，因此同名文件使用不同字段名。</remarks>
    [Fact]
    public async Task FileContent_DuplicateFileNames_PreservesEveryPart()
    {
        using var server = CreateServer();
        using var httpClient = server.CreateClient();
        var client = new HttpClientService().SetHttpClient(httpClient);
        var firstBytes = Encoding.UTF8.GetBytes("第一份较长内容");
        var secondBytes = Encoding.UTF8.GetBytes("短");
        using var first = new MemoryStream(firstBytes);
        using var second = new MemoryStream(secondBytes);

        var result = await client.Post<List<Test6Controller.UploadReceipt>>("/api/test6/inspect")
            .FileContent(first, "same.txt", "first")
            .FileContent(second, "same.txt", "second")
            .GetResultAsync();

        result.Count.ShouldBe(2);
        AssertReceipt(result[0], "first", "same.txt", firstBytes);
        AssertReceipt(result[1], "second", "same.txt", secondBytes);
    }

    /// <summary>
    /// 共享客户端的两个独立上传请求同时到达服务端后，同名文件内容仍各自独立。
    /// </summary>
    [Fact]
    public async Task FileContent_ConcurrentRequestsWithSameFileName_KeepIndependentContents()
    {
        var arrivals = 0;
        var bothArrived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var server = CreateServer(async (context, next) =>
        {
            if (Interlocked.Increment(ref arrivals) == 2)
                bothArrived.TrySetResult(true);

            // 第一条请求必须等到第二条到达，避免串行完成也被误计为并发覆盖。
            var timeout = Task.Delay(Timeout.Infinite, cancellation.Token);
            if (await Task.WhenAny(bothArrived.Task, timeout) != bothArrived.Task)
                throw new TimeoutException("并发上传请求未同时到达服务端。");
            await next();
        });
        using var httpClient = server.CreateClient();
        var client = new HttpClientService().SetHttpClient(httpClient);
        var firstBytes = Encoding.UTF8.GetBytes("并发请求甲的内容");
        var secondBytes = Encoding.UTF8.GetBytes("乙");
        using var first = new MemoryStream(firstBytes);
        using var second = new MemoryStream(secondBytes);

        var requests = new[]
        {
            client.Post<List<Test6Controller.UploadReceipt>>("/api/test6/inspect")
                .FileContent(first, "same.txt", "file").GetResultAsync(cancellation.Token),
            client.Post<List<Test6Controller.UploadReceipt>>("/api/test6/inspect")
                .FileContent(second, "same.txt", "file").GetResultAsync(cancellation.Token)
        };
        var results = await Task.WhenAll(requests);

        arrivals.ShouldBe(2);
        results[0].Count.ShouldBe(1);
        results[1].Count.ShouldBe(1);
        AssertReceipt(results[0][0], "file", "same.txt", firstBytes);
        AssertReceipt(results[1][0], "file", "same.txt", secondBytes);
    }

    /// <summary>
    /// 复用现有上传控制器建立内存 HTTP 主机，可插入受控并发屏障。
    /// </summary>
    private static TestServer CreateServer(Func<HttpContext, Func<Task>, Task> middleware = null) =>
        new(new WebHostBuilder()
            .ConfigureServices(services => services.AddControllers().AddApplicationPart(typeof(Test6Controller).Assembly))
            .Configure(app =>
            {
                app.UseRouting();
                if (middleware != null)
                    app.Use(middleware);
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));

    /// <summary>
    /// 同时断言字段、文件名及实际保存字节，避免仅检查成功状态掩盖数据串值。
    /// </summary>
    private static void AssertReceipt(Test6Controller.UploadReceipt receipt, string fieldName,
        string fileName, byte[] bytes)
    {
        receipt.Name.ShouldBe(fieldName);
        receipt.FileName.ShouldBe(fileName);
        Convert.FromBase64String(receipt.Content).ShouldBe(bytes);
    }
}
