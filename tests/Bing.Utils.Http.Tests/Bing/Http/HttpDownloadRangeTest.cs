using System.Net.Http;
using System.Net.Http.Headers;
using Bing.Http;

namespace Bing.Utils.Http.Tests.Bing.Http;



/// <summary>
/// HTTP 分片范围规划和响应边界验证测试。
/// </summary>
public class HttpDownloadRangeTest
{
    /// <summary>
    /// 验证规划的分片无重叠地覆盖完整正文且可生成范围头。
    /// </summary>
    [Fact]
    public void Create_CoversBodyWithoutOverlap()
    {
        var ranges = HttpDownloadRangePlanner.Create(10, 4);

        ranges.ShouldBe(new[]
        {
            new HttpDownloadRange(0, 3),
            new HttpDownloadRange(4, 7),
            new HttpDownloadRange(8, 9)
        });
        ranges.Sum(item => item.Length).ShouldBe(10);
        ranges[0].ToHeaderValue().ShouldBe("bytes=0-3");
    }



    /// <summary>
    /// 验证空正文生成空计划，并拒绝负长度及非正分片大小。
    /// </summary>
    [Fact]
    public void Create_ValidatesArgumentsAndHandlesEmptyBody()
    {
        HttpDownloadRangePlanner.Create(0, 4).ShouldBeEmpty();
        Should.Throw<ArgumentOutOfRangeException>(() => HttpDownloadRangePlanner.Create(-1, 4));
        Should.Throw<ArgumentOutOfRangeException>(() => HttpDownloadRangePlanner.Create(1, 0));
    }



    /// <summary>
    /// 验证接受匹配的范围响应，并拒绝范围、总长度不匹配或缺失范围头。
    /// </summary>
    [Fact]
    public void ValidateResponse_RejectsMismatchedRangeOrLength()
    {
        var expected = new HttpDownloadRange(4, 7);
        HttpDownloadRangePlanner.ValidateResponse(expected,
            new ContentRangeHeaderValue(4, 7, 10), 10);

        Should.Throw<HttpRequestException>(() => HttpDownloadRangePlanner.ValidateResponse(
            expected, new ContentRangeHeaderValue(0, 3, 10), 10));
        Should.Throw<HttpRequestException>(() => HttpDownloadRangePlanner.ValidateResponse(
            expected, new ContentRangeHeaderValue(4, 7, 11), 10));
        Should.Throw<HttpRequestException>(() => HttpDownloadRangePlanner.ValidateResponse(
            expected, null, 10));
    }
}
