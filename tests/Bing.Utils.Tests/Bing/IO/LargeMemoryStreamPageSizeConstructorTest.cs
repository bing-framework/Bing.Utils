namespace Bing.IO;

/// <summary>
/// 验证大型内存流的小页公开构造方式。
/// </summary>
[Trait("Bing.IO", "LargeMemoryStream.PageSizeConstructor")]
public class LargeMemoryStreamPageSizeConstructorTest
{
    /// <summary>
    /// 验证公开构造函数使用指定页大小时可跨页读写。
    /// </summary>
    [Fact]
    public void LargeMemoryStream_WithSmallPageSize_ReadsAndWritesAcrossPages()
    {
        byte[] expected = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        using var stream = new LargeMemoryStream(4);

        stream.Write(expected, 0, expected.Length);
        stream.Length.ShouldBe(expected.Length);
        stream.Position = 0;
        var actual = new byte[expected.Length];
        stream.Read(actual, 0, actual.Length).ShouldBe(actual.Length);

        actual.ShouldBe(expected);
    }

    /// <summary>
    /// 验证非正页大小被拒绝。
    /// </summary>
    /// <param name="pageSize">无效页大小。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LargeMemoryStream_WithNonPositivePageSize_Throws(int pageSize)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new LargeMemoryStream(pageSize));
    }
}
