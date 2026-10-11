using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Bing.Http;
using Bing.Http.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Bing.Utils.Http.Tests.Integration.Http;

/// <summary>

/// 验证断点续传的片段恢复、完整覆盖和 416 提交语义。

/// </summary>
public class HttpResumableDownloadIntegrationTest
{
    /// <summary>
    /// 验证续传部分响应并提交文件。
    /// </summary>
    [Fact]
    public async Task ResumableDownload_ResumesPartialResponseAndCommitsFile()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "download.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.OK, body, 4),
            _ =>
            {
                var response = CreateResponse(HttpStatusCode.PartialContent, body.Skip(4).ToArray());
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, body.Length - 1, body.Length);
                return response;
            });

        try
        {
            using var client = new HttpClient(handler);
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .Header("X-Correlation-Id", "resume-42")
                    .DownloadFileResumableWithResultAsync(target));

            File.Exists(target + ".binghttp.part").ShouldBeTrue();
            File.Exists(target + ".binghttp.resume.json").ShouldBeTrue();
            var stateText = await File.ReadAllTextAsync(target + ".binghttp.resume.json");
            stateText.ShouldContain("\"SchemaVersion\":1");
            stateText.ShouldContain("\"BytesWritten\":4");

            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .Header("X-Correlation-Id", "resume-42")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            handler.Requests.Count.ShouldBe(2);
            handler.Requests.Select(request => request.Headers.GetValues("X-Correlation-Id").Single())
                .ShouldBe(new[] { "resume-42", "resume-42" });
            handler.Requests[1].Headers.Range.Ranges.Single().From.ShouldBe(4);
            handler.Requests[1].Headers.IfRange.ToString().ShouldBe("\"v1\"");
            handler.Requests[1].Headers.AcceptEncoding.Single().Value.ShouldBe("identity");
            File.Exists(target + ".binghttp.part").ShouldBeFalse();
            File.Exists(target + ".binghttp.resume.json").ShouldBeFalse();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>
    /// 验证 SHA-256 校验成功后才提交目标文件并删除续传状态。
    /// </summary>
    [Fact]
    public async Task ResumableDownload_ValidatesSha256BeforeCommit()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "digest.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK, body));

        try
        {
            using var client = new HttpClient(handler);
            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/digest.bin")
                .ValidateResumableDownloadSha256("84d89877-f0d4-041e-fb6b-f91a16f0248f2fd573e6af05c19f96bedb9f882f7882")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            File.Exists(target + ".binghttp.part").ShouldBeFalse();
            File.Exists(target + ".binghttp.resume.json").ShouldBeFalse();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证续传内容校验器收到请求取消令牌。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_PassesCancellationTokenToValidator()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "validator-cancellation.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK, body));
        using var cancellation = new CancellationTokenSource();
        try
        {
            using var client = new HttpClient(handler);
            var request = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/validator.bin");
            request.SetResumableDownloadValidator((stream, token) =>
            {
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

            await Should.ThrowAsync<OperationCanceledException>(() =>
                request.DownloadFileResumableWithResultAsync(target, cancellation.Token));
            File.Exists(target).ShouldBeFalse();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>
    /// 验证 SHA-256 校验失败时不替换目标文件并保留可续传状态。
    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsSha256MismatchAndKeepsResumeState()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "digest-mismatch.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK, body));

        try
        {
            using var client = new HttpClient(handler);
            var error = await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/digest-mismatch.bin")
                    .ValidateResumableDownloadSha256(new string('0', 64))
                    .DownloadFileResumableWithResultAsync(target));

            error.Message.ShouldContain("SHA-256");
            File.Exists(target).ShouldBeFalse();
            File.Exists(target + ".binghttp.part").ShouldBeTrue();
            File.Exists(target + ".binghttp.resume.json").ShouldBeTrue();
            // 摘要校验发生在正文写完之后，状态记录完整临时文件长度，下一次可按完整片段处理。
            (await File.ReadAllTextAsync(target + ".binghttp.resume.json"))
                .ShouldContain("\"BytesWritten\":10");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证拒绝未知状态架构并开始新的。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsUnknownStateSchemaAndStartsFresh()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "unknown-schema.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        await File.WriteAllBytesAsync(target + ".binghttp.part", body.Take(4).ToArray());
        await File.WriteAllTextAsync(
            target + ".binghttp.resume.json",
            "{\"SchemaVersion\":999,\"Identity\":\"GET|http://localhost/file.bin\",\"BytesWritten\":4,\"ETag\":\"\\\"v1\\\"\",\"TotalLength\":10}",
            Encoding.UTF8);
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK, body));

        try
        {
            using var client = new HttpClient(handler);
            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            handler.Requests.Single().Headers.Contains("Range").ShouldBeFalse();
            File.Exists(target + ".binghttp.part").ShouldBeFalse();
            File.Exists(target + ".binghttp.resume.json").ShouldBeFalse();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证重新开始时服务端返回完整响应。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_RestartsWhenServerReturnsFullResponse()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "download.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.OK, body, 4),
            _ => CreateResponse(HttpStatusCode.OK, body));

        try
        {
            using var client = new HttpClient(handler);
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            handler.Requests[1].Headers.Range.Ranges.Single().From.ShouldBe(4);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证完成完整的部分文件文件从范围不可满足。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_CompletesFullPartialFileFromRangeNotSatisfiable()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "download.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.OK, body, body.Length),
            _ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    Content = new ByteArrayContent(Array.Empty<byte>())
                };
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(body.Length);
                response.Headers.ETag = new EntityTagHeaderValue("\"v1\"");
                return response;
            });

        try
        {
            using var client = new HttpClient(handler);
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            result.StatusCode.ShouldBe(HttpStatusCode.RequestedRangeNotSatisfiable);
            result.IsSuccessStatusCode.ShouldBeFalse();
            result.BytesWritten.ShouldBe(body.Length);
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            handler.Requests.Count.ShouldBe(2);
        }
        finally
        {
            DeleteRoot(root);
        }
    }


    /// <summary>


    /// 验证代理中断后可从真实服务端恢复下载。


    /// </summary>
    [Fact]
    public async Task ResumableDownload_RecoversThroughProxyAfterRealServerInterruption()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "test-server-proxy.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var originRanges = new List<string>();
        var originSawIdentityEncoding = false;
        var originRequestCount = 0;

        using var origin = new TestServer(new WebHostBuilder().Configure(app => app.Run(async context =>
        {
            var requestNumber = Interlocked.Increment(ref originRequestCount);
            var range = context.Request.Headers["Range"].ToString();
            originRanges.Add(range);
            originSawIdentityEncoding |=
                string.Equals(context.Request.Headers["Accept-Encoding"].ToString(), "identity", StringComparison.OrdinalIgnoreCase);
            context.Response.Headers["ETag"] = "\"v1\"";

            if (requestNumber == 1)
            {
                context.Response.ContentLength = body.Length;
                await context.Response.Body.WriteAsync(body, 0, 4);
                await context.Response.Body.FlushAsync();
                return;
            }

            range.ShouldBe("bytes=4-");
            context.Response.StatusCode = StatusCodes.Status206PartialContent;
            context.Response.Headers["Content-Range"] = "bytes 4-9/10";
            context.Response.ContentLength = body.Length - 4;
            await context.Response.Body.WriteAsync(body, 4, body.Length - 4);
        })));
        using var originClient = origin.CreateClient();
        using var proxy = new TestServer(new WebHostBuilder().Configure(app => app.Run(async context =>
        {
            var path = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            using var forwarded = new HttpRequestMessage(new HttpMethod(context.Request.Method), path);
            foreach (var header in context.Request.Headers)
            {
                if (string.Equals(header.Key, "Range", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(header.Key, "Accept-Encoding", StringComparison.OrdinalIgnoreCase))
                    forwarded.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            using var response = await originClient.SendAsync(
                forwarded, HttpCompletionOption.ResponseHeadersRead);
            context.Response.StatusCode = (int)response.StatusCode;
            if (response.Headers.TryGetValues("ETag", out var etags))
                context.Response.Headers["ETag"] = etags.ToArray();
            if (response.Content.Headers.TryGetValues("Content-Range", out var ranges))
                context.Response.Headers["Content-Range"] = ranges.ToArray();
            context.Response.ContentLength = response.Content.Headers.ContentLength;
            await response.Content.CopyToAsync(context.Response.Body);
        })));

        try
        {
            using var client = proxy.CreateClient();
            await Should.ThrowAsync<HttpRequestException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            originSawIdentityEncoding.ShouldBeTrue();
            originRanges.ShouldBe(new[] { string.Empty, "bytes=4-" });
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证拒绝无效部分正文范围。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsInvalidPartialContentRange()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "invalid-range.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var partialPath = target + ".binghttp.part";
        var statePath = target + ".binghttp.resume.json";
        await File.WriteAllBytesAsync(partialPath, body.Take(4).ToArray());
        await File.WriteAllTextAsync(
            statePath,
            "{\"Identity\":\"GET|http://localhost/file.bin\",\"BytesWritten\":4,\"ETag\":\"\\\"v1\\\"\",\"TotalLength\":10}");
        var handler = new SequenceHandler(_ =>
        {
            var response = CreateResponse(HttpStatusCode.PartialContent, body.Skip(4).ToArray());
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, 4, body.Length);
            return response;
        });

        try
        {
            using var client = new HttpClient(handler);
            var error = await Should.ThrowAsync<HttpRequestException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            error.Message.ShouldContain("Content-Range");
            File.Exists(target).ShouldBeFalse();
            File.Exists(partialPath).ShouldBeTrue();
            File.Exists(statePath).ShouldBeTrue();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证格式错误的完整范围响应不会提交文件。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_DoesNotCommitMalformedCompletedRangeResponse()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "invalid-completed-range.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var partialPath = target + ".binghttp.part";
        var statePath = target + ".binghttp.resume.json";
        await File.WriteAllBytesAsync(partialPath, body);
        await File.WriteAllTextAsync(
            statePath,
            "{\"Identity\":\"GET|http://localhost/file.bin\",\"BytesWritten\":10,\"ETag\":\"\\\"v1\\\"\",\"TotalLength\":10}");
        var handler = new SequenceHandler(_ =>
        {
            var response = CreateResponse(
                HttpStatusCode.RequestedRangeNotSatisfiable, Array.Empty<byte>());
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, 9, body.Length);
            return response;
        });

        try
        {
            using var client = new HttpClient(handler);
            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeFalse();
            result.BytesWritten.ShouldBe(body.Length);
            File.Exists(target).ShouldBeFalse();
            File.Exists(partialPath).ShouldBeTrue();
            File.Exists(statePath).ShouldBeTrue();
        }
        finally
        {
            DeleteRoot(root);
        }
    }
    /// <summary>
    /// 验证重新开始时检测请求表示的响应头变化。
    /// </summary>
    [Fact]
    public async Task ResumableDownload_RestartsWhenRequestRepresentationHeadersChange()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "request-fingerprint.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.OK, body, 4),
            _ => CreateResponse(HttpStatusCode.OK, body));

        try
        {
            using var client = new HttpClient(handler);
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .Header("Accept-Language", "en-US")
                    .DownloadFileResumableWithResultAsync(target));

            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .Header("Accept-Language", "fr-FR")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            handler.Requests[1].Headers.Contains("Range").ShouldBeFalse();
            File.Exists(target + ".binghttp.part").ShouldBeFalse();
            File.Exists(target + ".binghttp.resume.json").ShouldBeFalse();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证拒绝响应正文编码变化。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsResponseContentEncodingChange()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "content-encoding.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(
            _ =>
            {
                var response = CreateResponse(HttpStatusCode.OK, body, 4);
                response.Content.Headers.ContentEncoding.Add("gzip");
                return response;
            },
            _ =>
            {
                var response = CreateResponse(HttpStatusCode.PartialContent, body.Skip(4).ToArray());
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, body.Length - 1, body.Length);
                response.Content.Headers.ContentEncoding.Add("br");
                return response;
            });

        try
        {
            using var client = new HttpClient(handler);
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            var error = await Should.ThrowAsync<HttpRequestException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            error.Message.ShouldContain("encoding");
            File.Exists(target).ShouldBeFalse();
            File.Exists(target + ".binghttp.part").ShouldBeTrue();
            File.Exists(target + ".binghttp.resume.json").ShouldBeTrue();
        }
        finally
        {
            DeleteRoot(root);
        }
    }
    /// <summary>
    /// 验证使用客户端基址解析相对地址。
    /// </summary>
    [Fact]
    public async Task ResumableDownload_ResolvesRelativeUrlAgainstClientBaseAddress()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "relative.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.OK, body, 4),
            _ =>
            {
                var response = CreateResponse(HttpStatusCode.PartialContent, body.Skip(4).ToArray());
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, body.Length - 1, body.Length);
                return response;
            });

        try
        {
            using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            await Should.ThrowAsync<IOException>(() =>
                new HttpRequest<string>(null, client, HttpMethod.Get, "file.bin")
                    .DownloadFileResumableWithResultAsync(target));

            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
            handler.Requests[1].Headers.Range.Ranges.Single().From.ShouldBe(4);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证使用最后修改时间时是否接受弱 ETag。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_UsesLastModifiedWhenEtagIsWeak()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "last-modified.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var lastModified = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        var partialPath = target + ".binghttp.part";
        var statePath = target + ".binghttp.resume.json";
        await File.WriteAllBytesAsync(partialPath, body.Take(4).ToArray());
        await File.WriteAllTextAsync(
            statePath,
            "{\"Identity\":\"GET|http://localhost/file.bin\",\"BytesWritten\":4,\"ETag\":\"W/\\\"v1\\\"\",\"LastModified\":\"Tue, 06 Oct 2026 00:00:00 GMT\",\"TotalLength\":10}");

        var handler = new SequenceHandler(
            _ =>
            {
                var response = CreateLastModifiedResponse(
                    HttpStatusCode.PartialContent, body.Skip(4).ToArray(), lastModified);
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(4, body.Length - 1, body.Length);
                return response;
            });

        try
        {
            using var client = new HttpClient(handler);
            var result = await new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);

            result.IsFileSaved.ShouldBeTrue();
            handler.Requests[0].Headers.IfRange.Date.ShouldBe(lastModified);
            (await File.ReadAllBytesAsync(target)).ShouldBe(body);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证目标地址变化时拒绝提交已完成的范围响应。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsUriMutationBeforeCompletedRangeCommit()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "uri-mutation.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var lastModified = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        var partialPath = target + ".binghttp.part";
        var statePath = target + ".binghttp.resume.json";
        await File.WriteAllBytesAsync(partialPath, body);
        await File.WriteAllTextAsync(statePath,
            "{\"Identity\":\"GET|http://localhost/file.bin\",\"BytesWritten\":10,\"ETag\":\"W/\\\"v1\\\"\",\"LastModified\":\"Tue, 06 Oct 2026 00:00:00 GMT\",\"TotalLength\":10}");
        var handler = new SequenceHandler(_ =>
        {
            var response = CreateLastModifiedResponse(HttpStatusCode.RequestedRangeNotSatisfiable, Array.Empty<byte>(), lastModified);
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(body.Length);
            return response;
        });
        try
        {
            using var client = new HttpClient(handler);
            var request = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin");
            request.OnSendBefore(message =>
            {
                message.RequestUri = new Uri("http://localhost/other.bin");
                return true;
            });
            var exception = await Should.ThrowAsync<HttpRequestException>(() =>
                request.DownloadFileResumableWithResultAsync(target));
            exception.Message.ShouldContain("URI");
            File.Exists(target).ShouldBeFalse();
            File.Exists(partialPath).ShouldBeTrue();
            File.Exists(statePath).ShouldBeTrue();
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 验证拒绝并发目标下载。

    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsConcurrentTargetDownload()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "concurrent.bin");
        var body = Encoding.UTF8.GetBytes("0123456789");
        var handler = new BlockingHandler(body);
        Task<HttpDownloadResult> firstTask = null;

        try
        {
            using var client = new HttpClient(handler);
            firstTask = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);

            var started = await Task.WhenAny(handler.RequestStarted.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            started.ShouldBe(handler.RequestStarted.Task);
            File.Exists(target + ".binghttp.lock").ShouldBeTrue();

            var secondTask = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin")
                .DownloadFileResumableWithResultAsync(target);
            var secondCompleted = await Task.WhenAny(secondTask, Task.Delay(TimeSpan.FromSeconds(5)));
            secondCompleted.ShouldBe(secondTask);
            await Should.ThrowAsync<IOException>(() => secondTask);
            handler.RequestCount.ShouldBe(1);

            handler.Release.TrySetResult(true);
            var result = await firstTask;
            result.IsFileSaved.ShouldBeTrue();
        }
        finally
        {
            handler.Release.TrySetResult(true);
            if (firstTask != null)
            {
                try { await firstTask; } catch { }
            }
            DeleteRoot(root);
        }
    }


    /// <summary>


    /// 验证拒绝共享可续传锁由其他进程持有独立进程。


    /// </summary>
    [Fact]
    public async Task SegmentedDownload_RejectsSharedResumableLockHeldBySeparateProcess()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "segmented-cross-process.bin");
        var lockPath = target + ".binghttp.lock";
        var readyPath = Path.Combine(root, "helper.ready");
        var releasePath = Path.Combine(root, "helper.release");
        var stagingDirectory = target + ".binghttp.segments";
        var stagedSegment = Path.Combine(stagingDirectory, "segment-0-3.part");
        Process helper = null;
        try
        {
            await File.WriteAllTextAsync(target, "original-target");
            Directory.CreateDirectory(stagingDirectory);
            await File.WriteAllTextAsync(stagedSegment, "saved-segment");
            helper = StartLockHelper(lockPath, readyPath, releasePath);
            await WaitForFileAsync(readyPath);

            var handler = new SequenceHandler(_ => CreateResponse(
                HttpStatusCode.OK, Encoding.UTF8.GetBytes("payload")));
            using var client = new HttpClient(handler);
            var request = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin");
            var state = new HttpDownloadSegmentsState
            {
                SchemaVersion = HttpDownloadSegmentStateStore.CurrentSchemaVersion,
                TotalLength = 4,
                Segments = HttpDownloadRangePlanner.Create(4, 4).Select(range =>
                    new HttpDownloadSegmentState { Start = range.Start, End = range.End }).ToList()
            };

            await Should.ThrowAsync<IOException>(() =>
                request.DownloadFileSegmentedFromRequestAsync(target, state, 4, 1));

            handler.Requests.Count.ShouldBe(0);
            helper.HasExited.ShouldBeFalse();
            (await File.ReadAllTextAsync(target)).ShouldBe("original-target");
            (await File.ReadAllTextAsync(stagedSegment)).ShouldBe("saved-segment");
            File.Exists(lockPath).ShouldBeTrue();
            File.Exists(target + ".binghttp.segments.json").ShouldBeFalse();
        }
        finally
        {
            File.WriteAllText(releasePath, "release", Encoding.UTF8);
            if (helper != null)
            {
                helper.WaitForExit(5000);
                if (!helper.HasExited) helper.Kill();
                helper.Dispose();
            }
            DeleteRoot(root);
        }
    }
    /// <summary>
    /// 验证拒绝锁由其他进程持有独立进程。
    /// </summary>
    [Fact]
    public async Task ResumableDownload_RejectsLockHeldBySeparateProcess()
    {
        var root = CreateRoot();
        var target = Path.Combine(root, "cross-process.bin");
        var lockPath = target + ".binghttp.lock";
        var readyPath = Path.Combine(root, "helper.ready");
        var releasePath = Path.Combine(root, "helper.release");
        Process helper = null;
        try
        {
            helper = StartLockHelper(lockPath, readyPath, releasePath);
            await WaitForFileAsync(readyPath);
            using var client = new HttpClient(new SequenceHandler(_ => CreateResponse(
                HttpStatusCode.OK, Encoding.UTF8.GetBytes("payload"))));
            var request = new HttpRequest<string>(null, client, HttpMethod.Get, "http://localhost/file.bin");

            await Should.ThrowAsync<IOException>(() => request.DownloadFileResumableWithResultAsync(target));
            helper.HasExited.ShouldBeFalse();
            File.Exists(lockPath).ShouldBeTrue();
        }
        finally
        {
            File.WriteAllText(releasePath, "release", Encoding.UTF8);
            if (helper != null)
            {
                helper.WaitForExit(5000);
                if (!helper.HasExited) helper.Kill();
                helper.Dispose();
            }
            DeleteRoot(root);
        }
    }

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static HttpResponseMessage CreateLastModifiedResponse(
        HttpStatusCode status, byte[] body, DateTimeOffset lastModified, int? failAfter = null)
    {
        var response = CreateResponse(status, body, failAfter);
        response.Headers.ETag = new EntityTagHeaderValue("\"v1\"", true);
        response.Content.Headers.LastModified = lastModified;
        return response;
    }

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static HttpResponseMessage CreateResponse(HttpStatusCode status, byte[] body, int? failAfter = null)
    {
        HttpContent content = failAfter.HasValue
            ? new StreamContent(new FailingReadStream(body, failAfter.Value))
            : new ByteArrayContent(body);
        content.Headers.ContentLength = body.Length;
        var response = new HttpResponseMessage(status) { Content = content };
        response.Headers.ETag = new EntityTagHeaderValue("\"v1\"");
        return response;
    }

    /// <summary>

    /// 创建测试所需的辅助对象或数据。

    /// </summary>
    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "Bing.Utils.Http.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>

    /// 清理测试生成的临时资源。

    /// </summary>
    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }


    /// <summary>


    /// 处理测试场景中的 StartLockHelper。


    /// </summary>
    private static Process StartLockHelper(string lockPath, string readyPath, string releasePath)
    {
        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "pwsh";
        const string script = "$lock=[IO.File]::Open($env:BING_RESUME_LOCK,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); [IO.File]::WriteAllText($env:BING_RESUME_READY,'ready'); while(-not [IO.File]::Exists($env:BING_RESUME_RELEASE)){ Start-Sleep -Milliseconds 10 }; $lock.Dispose()";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "-NoProfile -NonInteractive -EncodedCommand " + encoded,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        startInfo.EnvironmentVariables["BING_RESUME_LOCK"] = lockPath;
        startInfo.EnvironmentVariables["BING_RESUME_READY"] = readyPath;
        startInfo.EnvironmentVariables["BING_RESUME_RELEASE"] = releasePath;
        try
        {
            var process = Process.Start(startInfo);
            if (process == null) throw new InvalidOperationException("The lock helper process could not be started.");
            return process;
        }
        catch (Win32Exception error)
        {
            throw Xunit.Sdk.SkipException.ForSkip("PowerShell is unavailable; cross-process resumable lock test is skipped: " + error.Message);
        }
    }

    /// <summary>

    /// 等待测试条件满足。

    /// </summary>
    private static async Task WaitForFileAsync(string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!File.Exists(path) && DateTime.UtcNow < deadline)
            await Task.Delay(10);
        File.Exists(path).ShouldBeTrue("The lock helper process did not signal readiness.");
    }

    /// <summary>

    /// 按请求顺序返回预设响应的消息处理器。

    /// </summary>
    private sealed class SequenceHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试处理流程使用的委托。
        /// </summary>
        private readonly Func<HttpRequestMessage, HttpResponseMessage>[] _responses;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private int _index;

        /// <summary>

        /// 初始化一个<see cref="SequenceHandler"/>类型的实例。

        /// </summary>
        /// <param name="responses">按请求顺序返回的响应工厂。</param>
        public SequenceHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
            => _responses = responses;

        /// <summary>

        /// 记录处理程序收到的请求。

        /// </summary>
        public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responses[Math.Min(_index++, _responses.Length - 1)](request));
        }
    }

    /// <summary>

    /// 收到请求后等待测试释放再返回正文的消息处理器。

    /// </summary>
    private sealed class BlockingHandler : HttpMessageHandler
    {
        /// <summary>
        /// 测试请求或响应使用的字节数据。
        /// </summary>
        private readonly byte[] _body;
        /// <summary>
        /// 测试期间记录的调用状态。
        /// </summary>
        private int _requestCount;

        /// <summary>

        /// 初始化一个<see cref="BlockingHandler"/>类型的实例。

        /// </summary>
        /// <param name="body">响应正文数据。</param>
        public BlockingHandler(byte[] body) => _body = body;

        /// <summary>

        /// 通知测试处理程序已收到请求。

        /// </summary>
        public TaskCompletionSource<bool> RequestStarted { get; } = new TaskCompletionSource<bool>();
        /// <summary>
        /// 控制测试请求的继续时机。
        /// </summary>
        public TaskCompletionSource<bool> Release { get; } = new TaskCompletionSource<bool>();
        /// <summary>
        /// 记录收到的请求数量。
        /// </summary>
        public int RequestCount => _requestCount;

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            RequestStarted.TrySetResult(true);
            await Release.Task.ConfigureAwait(false);
            return CreateResponse(HttpStatusCode.OK, _body);
        }
    }

    /// <summary>

    /// 超过可读字节数后抛出异常的内存流。

    /// </summary>
    private sealed class FailingReadStream : MemoryStream
    {
        /// <summary>
        /// 记录触发读取异常前的字节数。
        /// </summary>
        private readonly int _failAfter;

        /// <summary>

        /// 初始化一个<see cref="FailingReadStream"/>类型的实例。

        /// </summary>
        /// <param name="buffer">流读取的字节数据。</param>
        /// <param name="failAfter">触发读取异常前可读取的字节数。</param>
        public FailingReadStream(byte[] buffer, int failAfter) : base(buffer, writable: false)
            => _failAfter = failAfter;

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            if (Position >= _failAfter)
                throw new IOException("Simulated interrupted download.");
            count = Math.Min(count, _failAfter - (int)Position);
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}
