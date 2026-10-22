using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bing.Text.Chinese;
using Shouldly;
using Xunit;

namespace Bing.Text.Chinese;

/// <summary>
/// 测试调用方词表目录的简繁转换、流所有权和并发隔离契约。
/// </summary>
[Trait("TextUT", "ChineseConverter")]
public class ChineseConverterExternalTests
{
    /// <summary>
    /// 验证目录优先匹配最长词组，并支持两个方向的自定义转换。
    /// </summary>
    [Fact]
    public void Catalog_UsesLongestPhraseInBothDirections()
    {
        var catalog = CreateCatalog(
            "甲\t乙\n甲乙\t丙\n测试\t測試\n测试用例\t測試案例\n",
            "乙\t甲\n丙\t甲乙\n測試\t测试\n測試案例\t测试用例\n");

        catalog.ToTraditional("甲乙测试用例").ShouldBe("丙測試案例");
        catalog.ToSimplified("丙測試案例").ShouldBe("甲乙测试用例");
        catalog.ToTraditional("未收录").ShouldBe("未收录");
        catalog.ToSimplified("未收錄").ShouldBe("未收錄");
    }

    /// <summary>
    /// 验证 null 和空文本保持原值。
    /// </summary>
    [Fact]
    public void Catalog_PreservesNullAndEmptyText()
    {
        var catalog = CreateCatalog("甲\t乙\n", "乙\t甲\n");

        catalog.ToTraditional(null).ShouldBeNull();
        catalog.ToSimplified(null).ShouldBeNull();
        catalog.ToTraditional(string.Empty).ShouldBe(string.Empty);
        catalog.ToSimplified(string.Empty).ShouldBe(string.Empty);
    }

    /// <summary>
    /// 验证词表从流当前位置读取且构造后不关闭两个输入流。
    /// </summary>
    [Fact]
    public void Catalog_ReadsFromCurrentPosition_AndLeavesStreamsOpen()
    {
        using var simplifiedToTraditional = CreatePrefixedGzip("prefix", "甲\t乙\n");
        using var traditionalToSimplified = CreatePrefixedGzip("prefix", "乙\t甲\n");

        var catalog = new ChineseConversionCatalog(simplifiedToTraditional, traditionalToSimplified);

        catalog.ToTraditional("甲").ShouldBe("乙");
        catalog.ToSimplified("乙").ShouldBe("甲");
        simplifiedToTraditional.WasDisposed.ShouldBeFalse();
        traditionalToSimplified.WasDisposed.ShouldBeFalse();
        simplifiedToTraditional.CanRead.ShouldBeTrue();
        traditionalToSimplified.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证自定义目录之间及其与静态目录之间相互隔离。
    /// </summary>
    [Fact]
    public void Catalog_IsolatedFromOtherCatalogsAndStaticConverter()
    {
        var first = CreateCatalog("甲\t第一\n", "乙\t第一简\n");
        var second = CreateCatalog("甲\t第二\n", "乙\t第二简\n");

        first.ToTraditional("甲").ShouldBe("第一");
        second.ToTraditional("甲").ShouldBe("第二");
        first.ToSimplified("乙").ShouldBe("第一简");
        second.ToSimplified("乙").ShouldBe("第二简");
        ChineseConverter.ToTraditional("汉").ShouldBe("漢");
        ChineseConverter.ToSimplified("漢").ShouldBe("汉");
    }

    /// <summary>
    /// 验证同一目录可以被多个线程并发读取且结果稳定。
    /// </summary>
    [Fact]
    public void Catalog_SupportsConcurrentReads()
    {
        var catalog = CreateCatalog(
            "甲\t乙\n甲乙\t丙\n测试\t測試\n",
            "乙\t甲\n丙\t甲乙\n測試\t测试\n");

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                catalog.ToTraditional("甲乙测试").ShouldBe("丙測試");
                catalog.ToSimplified("丙測試").ShouldBe("甲乙测试");
            }))
            .ToArray();

        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 验证仓库独立数据文件与内置静态入口使用相同的转换结果。
    /// </summary>
    [Fact]
    public void ExternalSnapshot_MatchesStaticConverter()
    {
        using var simplified = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "opencc-s2t.gz"));
        using var traditional = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "opencc-t2s.gz"));
        var catalog = new ChineseConversionCatalog(simplified, traditional);
        foreach (var text in new[] { "发展和头发", "干净的房间", "A😀\uD83D字" })
        {
            catalog.ToTraditional(text).ShouldBe(ChineseConverter.ToTraditional(text));
            catalog.ToSimplified(text).ShouldBe(ChineseConverter.ToSimplified(text));
        }
    }

    /// <summary>
    /// 验证 null、不可读、格式错误和非法 UTF-8 输入抛出约定异常。
    /// </summary>
    [Fact]
    public void Catalog_RejectsNullUnreadableMalformedAndInvalidUtf8Inputs()
    {
        using var simplified = CreateGzip("甲\t乙\n");
        using var traditional = CreateGzip("乙\t甲\n");
        Should.Throw<ArgumentNullException>(() => new ChineseConversionCatalog(null, traditional));
        Should.Throw<ArgumentNullException>(() => new ChineseConversionCatalog(simplified, null));

        using (var unreadable = new UnreadableStream())
        using (var validTraditional = CreateGzip("乙\t甲\n"))
            Should.Throw<ArgumentException>(() => new ChineseConversionCatalog(unreadable, validTraditional));

        using (var malformed = new MemoryStream(Encoding.UTF8.GetBytes("not gzip")))
        using (var validTraditional = CreateGzip("乙\t甲\n"))
            Should.Throw<InvalidDataException>(() => new ChineseConversionCatalog(malformed, validTraditional));

        using (var invalidFormat = CreateGzip("甲\n"))
        using (var validTraditional = CreateGzip("乙\t甲\n"))
            Should.Throw<InvalidDataException>(() => new ChineseConversionCatalog(invalidFormat, validTraditional));

        using (var invalidUtf8 = CreateGzip(new byte[] { (byte)'a', 0xFF, (byte)'\t', (byte)'b' }))
        using (var validTraditional = CreateGzip("乙\t甲\n"))
            Should.Throw<InvalidDataException>(() => new ChineseConversionCatalog(invalidUtf8, validTraditional));
    }

    /// <summary>
    /// 创建使用指定 UTF-8 gzip 词表的目录。
    /// </summary>
    /// <param name="simplifiedToTraditional">简体到繁体词表文本。</param>
    /// <param name="traditionalToSimplified">繁体到简体词表文本。</param>
    /// <returns>已加载的转换目录。</returns>
    private static ChineseConversionCatalog CreateCatalog(
        string simplifiedToTraditional, string traditionalToSimplified)
    {
        using var simplifiedStream = CreateGzip(simplifiedToTraditional);
        using var traditionalStream = CreateGzip(traditionalToSimplified);
        return new ChineseConversionCatalog(simplifiedStream, traditionalStream);
    }

    /// <summary>
    /// 创建从指定位置开始的 gzip 词表流。
    /// </summary>
    /// <param name="prefix">压缩数据前的前缀。</param>
    /// <param name="dictionary">词表文本。</param>
    /// <returns>位置已设置到压缩数据起点的跟踪流。</returns>
    private static TrackingStream CreatePrefixedGzip(string prefix, string dictionary)
    {
        using var compressed = CreateGzip(dictionary);
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var compressedBytes = compressed.ToArray();
        var bytes = prefixBytes.Concat(compressedBytes).ToArray();
        var stream = new TrackingStream(bytes);
        stream.Position = prefixBytes.Length;
        return stream;
    }

    /// <summary>
    /// 创建 UTF-8 gzip 词表流。
    /// </summary>
    /// <param name="dictionary">词表文本。</param>
    /// <returns>包含 gzip 数据的内存流。</returns>
    private static MemoryStream CreateGzip(string dictionary) =>
        CreateGzip(Encoding.UTF8.GetBytes(dictionary));

    /// <summary>
    /// 创建指定字节的 gzip 流。
    /// </summary>
    /// <param name="bytes">待压缩字节。</param>
    /// <returns>包含 gzip 数据的内存流。</returns>
    private static MemoryStream CreateGzip(byte[] bytes)
    {
        var stream = new MemoryStream();
        using (var gzip = new GZipStream(stream, CompressionMode.Compress, true))
            gzip.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// 记录释放状态的内存流。
    /// </summary>
    private sealed class TrackingStream : MemoryStream
    {
        /// <summary>
        /// 初始化一个 <see cref="TrackingStream"/> 类型的实例。
        /// </summary>
        /// <param name="buffer">流的初始内容。</param>
        public TrackingStream(byte[] buffer)
            : base(buffer, writable: false)
        {
        }

        /// <summary>
        /// 获取流是否已释放。
        /// </summary>
        public bool WasDisposed { get; private set; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 不可读的测试流。
    /// </summary>
    private sealed class UnreadableStream : Stream
    {
        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => 0;

        /// <inheritdoc />
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
