using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// HTTP 范围能力探测和响应适配测试。
/// </summary>
public class HttpDownloadRangeProbeTest
{
    /// <summary>
    /// 验证识别服务端声明的字节范围能力及有效的部分内容响应。
    /// </summary>
    [Fact]
    public void Probe_RecognizesAdvertisedAndValidPartialRanges()
    {
        HttpDownloadRangeProbe.Probe(new HttpDownloadRangeResponseMetadata
        {
            StatusCode = HttpStatusCode.OK,
            AcceptRanges = "bytes",
            ContentLength = 10
        }).Status.ShouldBe(HttpDownloadRangeSupportStatus.Supported);

        HttpDownloadRangeProbe.Probe(new HttpDownloadRangeResponseMetadata
        {
            StatusCode = HttpStatusCode.PartialContent,
            AcceptRanges = "bytes",
            ContentLength = 4,
            ContentRange = new ContentRangeHeaderValue(4, 7, 10)
        }, new HttpDownloadRange(4, 7)).Status.ShouldBe(HttpDownloadRangeSupportStatus.Supported);
    }



    /// <summary>
    /// 验证区分服务端忽略范围请求与范围元数据不一致的响应。
    /// </summary>
    [Fact]
    public void Probe_DistinguishesServerRefusalAndInvalidResponses()
    {
        HttpDownloadRangeProbe.Probe(new HttpDownloadRangeResponseMetadata
        {
            StatusCode = HttpStatusCode.OK,
            ContentLength = 10
        }, new HttpDownloadRange(0, 3)).Status.ShouldBe(HttpDownloadRangeSupportStatus.Unsupported);

        HttpDownloadRangeProbe.Probe(new HttpDownloadRangeResponseMetadata
        {
            StatusCode = HttpStatusCode.PartialContent,
            ContentRange = new ContentRangeHeaderValue(0, 3, 11),
            ContentLength = 4
        }, new HttpDownloadRange(0, 3), 10).Status.ShouldBe(HttpDownloadRangeSupportStatus.Invalid);

        var wrongUnit = new ContentRangeHeaderValue(0, 3, 10) { Unit = "items" };
        HttpDownloadRangeProbe.Probe(new HttpDownloadRangeResponseMetadata
        {
            StatusCode = HttpStatusCode.PartialContent,
            ContentRange = wrongUnit,
            ContentLength = 4
        }, new HttpDownloadRange(0, 3)).Status.ShouldBe(HttpDownloadRangeSupportStatus.Invalid);
    }



    /// <summary>
    /// 验证范围能力探测将取消令牌传递给异步探测操作。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_ForwardsCancellation()
    {
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => HttpDownloadRangeProbe.ProbeAsync(
                async token =>
                {
                    await Task.Delay(100, token);
                    return new HttpDownloadRangeResponseMetadata();
                }, cancellationToken: cancellation.Token));
        }
    }
}
