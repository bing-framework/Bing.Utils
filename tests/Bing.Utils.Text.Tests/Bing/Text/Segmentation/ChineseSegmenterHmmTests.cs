using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bing.Text.Segmentation;
using Shouldly;
using Xunit;

namespace Bing.Text.Segmentation;

/// <summary>
/// 测试外置 HMM 模型的显式启用、边界、组合和并发契约。
/// </summary>
[Trait("TextUT", "ChineseSegmenter")]
public class ChineseSegmenterHmmTests
{
    /// <summary>
    /// 验证固定 jieba 模型可以识别经典的未登录词场景。
    /// </summary>
    [Fact]
    public void WithHmmModel_RecognizesFixedUnknownWordScenario()
    {
        var segmenter = CreateSegmenter(string.Empty);
        var hmmSegmenter = WithRealHmmModel(segmenter);

        hmmSegmenter.Cut("南京市长江大桥")
            .ShouldBe(new[] { "南京市", "长江大桥" });
    }

    /// <summary>
    /// 验证默认分词不启用 HMM，且已收录整词在启用 HMM 后保持完整。
    /// </summary>
    [Fact]
    public void DefaultSegmentationRemainsUnchanged_AndKnownWordStaysWhole()
    {
        var defaultSegmenter = CreateSegmenter(string.Empty);
        var knownSegmenter = CreateSegmenter("南京市长江大桥 100 n\n");
        var hmmSegmenter = WithRealHmmModel(knownSegmenter);

        defaultSegmenter.Cut("南京市长江大桥")
            .ShouldBe(new[] { "南", "京", "市", "长", "江", "大", "桥" });
        hmmSegmenter.Cut("南京市长江大桥")
            .ShouldBe(new[] { "南京市长江大桥" });
    }

    /// <summary>
    /// 验证补充平面汉字不进入 HMM，并且所有结果切片使用 UTF-16 位置。
    /// </summary>
    [Fact]
    public void WithHmmModel_PreservesSupplementaryBoundaryAndUtf16Slices()
    {
        var segmenter = WithRealHmmModel(CreateSegmenter(string.Empty));
        var text = "𠀀南京市长江大桥😀";

        var segments = segmenter.FindAll(text);

        segments[0].Value.ShouldBe("𠀀");
        segments[0].Index.ShouldBe(0);
        segments[0].Length.ShouldBe(2);
        segments.Last().Value.ShouldBe("😀");
        segments.Last().Index.ShouldBe(text.Length - 2);
        segments.Last().Length.ShouldBe(2);
        foreach (var segment in segments)
            text.Substring(segment.Index, segment.Length).ShouldBe(segment.Value);
        string.Concat(segments.Select(segment => segment.Value)).ShouldBe(text);
    }

    /// <summary>
    /// 验证先加载业务词典或先加载 HMM 都保留两种能力及相同结果。
    /// </summary>
    [Fact]
    public void WithDictionaryAndHmmModel_ComposeInEitherOrder()
    {
        var baseSegmenter = CreateSegmenter(string.Empty);
        var dictionaryThenHmm = WithRealHmmModel(
            baseSegmenter.WithDictionary(DictionaryStream("北京 100 n\n")));
        var hmmThenDictionary = WithRealHmmModel(baseSegmenter)
            .WithDictionary(DictionaryStream("北京 100 n\n"));

        dictionaryThenHmm.Cut("北京").ShouldBe(new[] { "北京" });
        hmmThenDictionary.Cut("北京").ShouldBe(new[] { "北京" });
        dictionaryThenHmm.Cut("南京市长江大桥")
            .ShouldBe(hmmThenDictionary.Cut("南京市长江大桥"));
    }

    /// <summary>
    /// 验证模型从流当前位置读取，成功加载后保持输入流开放。
    /// </summary>
    [Fact]
    public void WithHmmModel_ReadsFromCurrentPositionAndLeavesStreamOpen()
    {
        var modelBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "hmm-model.gz"));
        var prefix = Encoding.UTF8.GetBytes("prefix");
        var bytes = prefix.Concat(modelBytes).ToArray();
        using var stream = new TrackingStream(bytes);
        stream.Position = prefix.Length;

        var segmenter = CreateSegmenter(string.Empty).WithHmmModel(stream);

        stream.WasDisposed.ShouldBeFalse();
        stream.CanRead.ShouldBeTrue();
        segmenter.Cut("南京市长江大桥")
            .ShouldBe(new[] { "南京市", "长江大桥" });
    }

    /// <summary>
    /// 验证模型流的 null、不可读、无效 gzip 和截断输入抛出约定异常。
    /// </summary>
    [Fact]
    public void WithHmmModel_RejectsInvalidAndTruncatedModels()
    {
        var segmenter = CreateSegmenter(string.Empty);

        Should.Throw<ArgumentNullException>(() => segmenter.WithHmmModel(null));
        using (var unreadable = new UnreadableStream())
            Should.Throw<ArgumentException>(() => segmenter.WithHmmModel(unreadable));

        using (var invalidGzip = new MemoryStream(Encoding.UTF8.GetBytes("not gzip")))
            Should.Throw<InvalidDataException>(() => segmenter.WithHmmModel(invalidGzip));

        using var truncated = CreateGzip(new byte[]
        {
            (byte)'B', (byte)'H', (byte)'M', (byte)'1'
        });
        Should.Throw<InvalidDataException>(() => segmenter.WithHmmModel(truncated));
    }

    /// <summary>
    /// 验证同一 HMM 分词器可以被多个线程并发读取且结果稳定。
    /// </summary>
    [Fact]
    public void WithHmmModel_SupportsConcurrentReads()
    {
        var segmenter = WithRealHmmModel(CreateSegmenter(string.Empty));

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                segmenter.Cut("南京市长江大桥")
                    .ShouldBe(new[] { "南京市", "长江大桥" });
                var segments = segmenter.FindAll("南京市长江大桥");
                string.Concat(segments.Select(segment => segment.Value))
                    .ShouldBe("南京市长江大桥");
            }))
            .ToArray();

        Task.WhenAll(tasks).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 创建使用 UTF-8 词典的分词器。
    /// </summary>
    /// <param name="dictionary">Jieba 格式词典文本。</param>
    /// <returns>已加载的分词器。</returns>
    private static ChineseSegmenter CreateSegmenter(string dictionary)
    {
        using var stream = DictionaryStream(dictionary);
        return new ChineseSegmenter(stream);
    }

    /// <summary>
    /// 使用测试输出目录中的固定 HMM 模型创建分词器。
    /// </summary>
    /// <param name="segmenter">待启用 HMM 的分词器。</param>
    /// <returns>已启用 HMM 的分词器。</returns>
    private static ChineseSegmenter WithRealHmmModel(ChineseSegmenter segmenter)
    {
        using var model = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "hmm-model.gz"));
        return segmenter.WithHmmModel(model);
    }

    /// <summary>
    /// 创建 UTF-8 词典流。
    /// </summary>
    /// <param name="dictionary">词典文本。</param>
    /// <returns>包含 UTF-8 字节的内存流。</returns>
    private static MemoryStream DictionaryStream(string dictionary) =>
        new MemoryStream(Encoding.UTF8.GetBytes(dictionary));

    /// <summary>
    /// 创建指定字节的 gzip 模型流。
    /// </summary>
    /// <param name="bytes">待压缩模型字节。</param>
    /// <returns>包含 gzip 数据的内存流。</returns>
    private static MemoryStream CreateGzip(byte[] bytes)
    {
        var stream = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(
            stream, System.IO.Compression.CompressionMode.Compress, true))
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
