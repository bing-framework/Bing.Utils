namespace Bing.Helpers;

/// <summary>
/// GZip压缩操作测试
/// </summary>
public class GZipTest : TestBase
{
    /// <summary>
    /// 初始化一个 <see cref="GZipTest"/> 类型的实例。
    /// </summary>
    /// <param name="output">xUnit 测试输出助手。</param>
    public GZipTest(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// 测试压缩以及解压缩
    /// </summary>
    [Fact]
    public void Test_Compress_Decompress()
    {
        var content = "测试一下压缩情况";
        var compressResult = GZip.Compress(content);
        var decompressResulut = GZip.Decompress(compressResult);
        Assert.Equal(content, decompressResulut);
    }
}
