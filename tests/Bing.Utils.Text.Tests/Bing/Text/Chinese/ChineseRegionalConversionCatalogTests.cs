using System;
using System.Collections.Generic;
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
/// 测试地区简繁转换目录的两阶段规则、外置资源、隔离和并发契约。
/// </summary>
[Trait("TextUT", "ChineseConverter")]
public class ChineseRegionalConversionCatalogTests
{
    /// <summary>
    /// 验证基础目录和地区目录均按最长词组执行两阶段转换。
    /// </summary>
    [Fact]
    public void RegionalCatalog_AppliesBaseThenRegionalRulesWithLongestMatch()
    {
        var baseCatalog = CreateBaseCatalog();
        var regional = baseCatalog.WithRegionalRules(
            CreateGzip("標\t地\n標準\t地區\n"),
            CreateGzip("地\t標\n地區\t標準\n"));

        regional.ToTraditional("甲乙甲").ShouldBe("地區地");
        regional.ToSimplified("地區地").ShouldBe("甲乙甲");
    }

    /// <summary>
    /// 验证地区目录保留 null、空文本、未收录文本和 Emoji。
    /// </summary>
    [Fact]
    public void RegionalCatalog_PreservesBoundaryValues()
    {
        var regional = CreateBaseCatalog().WithRegionalRules(
            CreateGzip("標\t地\n"),
            CreateGzip("地\t標\n"));

        regional.ToTraditional(null).ShouldBeNull();
        regional.ToSimplified(null).ShouldBeNull();
        regional.ToTraditional(string.Empty).ShouldBe(string.Empty);
        regional.ToSimplified(string.Empty).ShouldBe(string.Empty);
        regional.ToTraditional("未收录😀A").ShouldBe("未收录😀A");
        regional.ToSimplified("未收錄😀A").ShouldBe("未收錄😀A");
    }

    /// <summary>
    /// 验证地区目录与基础目录及其他地区目录相互隔离。
    /// </summary>
    [Fact]
    public void RegionalCatalog_IsolatedFromBaseAndSiblingCatalogs()
    {
        var baseCatalog = CreateBaseCatalog();
        var first = baseCatalog.WithRegionalRules(
            CreateGzip("標準\t地區\n"),
            CreateGzip("地區\t標準\n"));
        var second = baseCatalog.WithRegionalRules(
            CreateGzip("標準\t另一\n"),
            CreateGzip("另一\t標準\n"));

        baseCatalog.ToTraditional("甲乙").ShouldBe("標準");
        first.ToTraditional("甲乙").ShouldBe("地區");
        second.ToTraditional("甲乙").ShouldBe("另一");
        first.ToSimplified("地區").ShouldBe("甲乙");
        second.ToSimplified("另一").ShouldBe("甲乙");
    }

    /// <summary>
    /// 验证固定台湾资源对常见软件和鼠标词组执行地区转换。
    /// </summary>
    [Fact]
    public void TaiwanSnapshot_UsesFixedRegionalResource()
    {
        var baseCatalog = CreateExternalBaseCatalog();
        using var forward = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "opencc-tw-forward.gz"));
        using var reverse = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "opencc-tw-reverse.gz"));
        var taiwan = baseCatalog.WithRegionalRules(forward, reverse);

        taiwan.ToTraditional("鼠标和软件").ShouldBe("滑鼠和軟體");
        taiwan.ToSimplified("滑鼠和軟體").ShouldBe("鼠标和软件");
    }

    /// <summary>
    /// 验证固定香港资源对基础繁体字形执行地区转换。
    /// </summary>
    [Fact]
    public void HongKongSnapshot_UsesFixedRegionalResource()
    {
        var baseCatalog = CreateExternalBaseCatalog();
        using var forward = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "opencc-hk-forward.gz"));
        using var reverse = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "opencc-hk-reverse.gz"));
        var hongKong = baseCatalog.WithRegionalRules(forward, reverse);

        baseCatalog.ToTraditional("户").ShouldBe("戶");
        hongKong.ToTraditional("户").ShouldBe("户");
        hongKong.ToSimplified("户").ShouldBe("户");
    }

    /// <summary>
    /// 验证地区规则流从当前位置读取并在构造后保持开放。
    /// </summary>
    [Fact]
    public void RegionalCatalog_ReadsRulesAndLeavesStreamsOpen()
    {
        using var forward = CreatePrefixedGzip("prefix", "標準\t地區\n");
        using var reverse = CreatePrefixedGzip("prefix", "地區\t標準\n");
        var regional = CreateBaseCatalog().WithRegionalRules(forward, reverse);

        regional.ToTraditional("甲乙").ShouldBe("地區");
        regional.ToSimplified("地區").ShouldBe("甲乙");
        forward.WasDisposed.ShouldBeFalse();
        reverse.WasDisposed.ShouldBeFalse();
        forward.CanRead.ShouldBeTrue();
        reverse.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证正反地区词表的 null、不可读、格式错误和非法 UTF-8 输入抛出约定异常。
    /// </summary>
    [Fact]
    public void RegionalCatalog_RejectsInvalidForwardAndReverseRules()
    {
        var baseCatalog = CreateBaseCatalog();

        using (var reverse = CreateGzip("地\t標\n"))
            Should.Throw<ArgumentNullException>(() => baseCatalog.WithRegionalRules(null, reverse));
        using (var forward = CreateGzip("標\t地\n"))
            Should.Throw<ArgumentNullException>(() => baseCatalog.WithRegionalRules(forward, null));

        using (var unreadable = new UnreadableStream())
        using (var reverse = CreateGzip("地\t標\n"))
            Should.Throw<ArgumentException>(() => baseCatalog.WithRegionalRules(unreadable, reverse));
        using (var forward = CreateGzip("標\t地\n"))
        using (var unreadable = new UnreadableStream())
            Should.Throw<ArgumentException>(() => baseCatalog.WithRegionalRules(forward, unreadable));

        using (var malformed = new MemoryStream(Encoding.UTF8.GetBytes("not gzip")))
        using (var reverse = CreateGzip("地\t標\n"))
            Should.Throw<InvalidDataException>(() => baseCatalog.WithRegionalRules(malformed, reverse));
        using (var forward = CreateGzip("標\t地\n"))
        using (var malformed = new MemoryStream(Encoding.UTF8.GetBytes("not gzip")))
            Should.Throw<InvalidDataException>(() => baseCatalog.WithRegionalRules(forward, malformed));

        using (var invalidUtf8 = CreateGzip(new byte[] { (byte)'a', 0xFF, (byte)'\t', (byte)'b' }))
        using (var reverse = CreateGzip("地\t標\n"))
            Should.Throw<InvalidDataException>(() => baseCatalog.WithRegionalRules(invalidUtf8, reverse));
        using (var forward = CreateGzip("標\t地\n"))
        using (var invalidUtf8 = CreateGzip(new byte[] { (byte)'a', 0xFF, (byte)'\t', (byte)'b' }))
            Should.Throw<InvalidDataException>(() => baseCatalog.WithRegionalRules(forward, invalidUtf8));
    }

    /// <summary>
    /// 验证同一地区目录可以被多个线程并发读取且结果稳定。
    /// </summary>
    [Fact]
    public void RegionalCatalog_SupportsConcurrentReads()
    {
        var regional = CreateBaseCatalog().WithRegionalRules(
            CreateGzip("標準\t地區\n"),
            CreateGzip("地區\t標準\n"));

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                regional.ToTraditional("甲乙").ShouldBe("地區");
                regional.ToSimplified("地區").ShouldBe("甲乙");
                regional.ToTraditional("A😀").ShouldBe("A😀");
            }))
            .ToArray();

        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 创建用于两阶段验证的手写基础目录。
    /// </summary>
    /// <returns>包含手写简繁词表的目录。</returns>
    private static ChineseConversionCatalog CreateBaseCatalog()
    {
        using var simplifiedToTraditional = CreateGzip("甲\t標\n甲乙\t標準\n");
        using var traditionalToSimplified = CreateGzip("標\t甲\n標準\t甲乙\n");
        return new ChineseConversionCatalog(simplifiedToTraditional, traditionalToSimplified);
    }

    /// <summary>
    /// 创建使用测试输出目录固定资源的基础目录。
    /// </summary>
    /// <returns>包含标准简繁资源的目录。</returns>
    private static ChineseConversionCatalog CreateExternalBaseCatalog()
    {
        using var simplifiedToTraditional = File.OpenRead(
            Path.Combine(AppContext.BaseDirectory, "opencc-s2t.gz"));
        using var traditionalToSimplified = File.OpenRead(
            Path.Combine(AppContext.BaseDirectory, "opencc-t2s.gz"));
        return new ChineseConversionCatalog(simplifiedToTraditional, traditionalToSimplified);
    }

    /// <summary>
    /// 创建从指定位置开始读取的 gzip 词表流。
    /// </summary>
    /// <param name="prefix">压缩数据前的前缀。</param>
    /// <param name="dictionary">词表文本。</param>
    /// <returns>位置已设置到压缩数据起点的跟踪流。</returns>
    private static TrackingStream CreatePrefixedGzip(string prefix, string dictionary)
    {
        using var compressed = CreateGzip(dictionary);
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);
        var bytes = prefixBytes.Concat(compressed.ToArray()).ToArray();
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
    /// 创建指定字节的 gzip 词表流。
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
