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
/// 测试基于 Jieba 词典的中文分词、位置和并发只读契约。
/// </summary>
[Trait("TextUT", "ChineseSegmenter")]
public class ChineseSegmenterTests
{
    /// <summary>
    /// 验证词频 DAG 选择全局最优路径，而不是只选择最长词。
    /// </summary>
    [Fact]
    public void FindAll_UsesFrequencyOptimalDagPath()
    {
        var segmenter = CreateSegmenter(
            "研究 100 n\n" +
            "研究 1 n\n" +
            "研究生 50 n\n" +
            "生命 100 n\n" +
            "命 1 n\n");

        var segments = segmenter.FindAll("研究生命");

        segments.Select(segment => segment.Value).ShouldBe(new[] { "研究", "生命" });
        segments.Select(segment => segment.Index).ShouldBe(new[] { 0, 2 });
        segments.Select(segment => segment.Length).ShouldBe(new[] { 2, 2 });
        segmenter.Cut("研究生命").ShouldBe(new[] { "研究", "生命" });
    }

    /// <summary>
    /// 验证词典中的纯空白行可以跳过，重复词保留首条频率。
    /// </summary>
    [Fact]
    public void Dictionary_SkipsBlankLinesAndKeepsFirstDuplicate()
    {
        var segmenter = CreateSegmenter(
            "\n" +
            "研究 100 n\n" +
            "   \n" +
            "研究 1 n\n" +
            "生命 100 n\n" +
            "命 1 n\n");

        segmenter.Cut("研究生命").ShouldBe(new[] { "研究", "生命" });
    }

    /// <summary>
    /// 验证非中文文本按连续字母数字、连续空白及单个标点或 Emoji 标量分段。
    /// </summary>
    [Fact]
    public void FindAll_PreservesNonChineseBoundariesAndUtf16Positions()
    {
        var segmenter = CreateSegmenter("中国 10 ns\n");
        var text = "abc123 \t中国!😀，";

        var segments = segmenter.FindAll(text);

        segments.Select(segment => segment.Value).ShouldBe(
            new[] { "abc123", " \t", "中国", "!", "😀", "，" });
        segments.Select(segment => segment.Index).ShouldBe(new[] { 0, 6, 8, 10, 11, 13 });
        segments.Select(segment => segment.Length).ShouldBe(new[] { 6, 2, 2, 1, 2, 1 });
        string.Concat(segments.Select(segment => segment.Value)).ShouldBe(text);
        segmenter.Cut(text).ShouldBe(segments.Select(segment => segment.Value));
    }

    /// <summary>
    /// 验证孤立代理项保持独立原文片段且拼接结果完全等于输入。
    /// </summary>
    [Fact]
    public void FindAll_PreservesIsolatedSurrogates()
    {
        var segmenter = CreateSegmenter(string.Empty);
        var text = "A\uD83D|\uDE00B";

        var segments = segmenter.FindAll(text);

        segments.Select(segment => segment.Value)
            .ShouldBe(new[] { "A", "\uD83D", "|", "\uDE00", "B" });
        segments.Select(segment => segment.Index).ShouldBe(new[] { 0, 1, 2, 3, 4 });
        segments.Select(segment => segment.Length).ShouldBe(new[] { 1, 1, 1, 1, 1 });
        string.Concat(segments.Select(segment => segment.Value)).ShouldBe(text);
    }

    /// <summary>
    /// 验证 null 文本和空文本返回空的只读结果。
    /// </summary>
    [Fact]
    public void NullAndEmptyText_ReturnEmptyReadOnlyResults()
    {
        var segmenter = CreateSegmenter("中国 10 ns\n");

        segmenter.FindAll(null).ShouldBeEmpty();
        segmenter.FindAll(string.Empty).ShouldBeEmpty();
        segmenter.Cut(null).ShouldBeEmpty();
        segmenter.Cut(string.Empty).ShouldBeEmpty();
        AssertReadOnly(segmenter.FindAll(null));
        AssertReadOnly(segmenter.Cut(string.Empty));
    }

    /// <summary>
    /// 验证 null 流、不可读流和错误词典格式抛出约定异常。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidStreamsAndDictionary()
    {
        Should.Throw<ArgumentNullException>(() => new ChineseSegmenter(null));

        using (var unreadable = new UnreadableStream())
            Should.Throw<ArgumentException>(() => new ChineseSegmenter(unreadable));

        foreach (var dictionary in new[]
        {
            "中国\n",
            "中国 frequency n\n",
            "中国 10 n\n坏行"
        })
        {
            using var stream = DictionaryStream(dictionary);
            Should.Throw<InvalidDataException>(() => new ChineseSegmenter(stream));
        }
    }

    /// <summary>
    /// 验证构造函数不关闭输入流，并允许构造后的分词器并发只读。
    /// </summary>
    [Fact]
    public void Constructor_LeavesStreamOpen_AndSupportsConcurrentReads()
    {
        using var stream = new TrackingStream(Encoding.UTF8.GetBytes(
            "中国 10 ns\n研究 100 n\n生命 100 n\n"));
        var segmenter = new ChineseSegmenter(stream);

        stream.WasDisposed.ShouldBeFalse();
        stream.CanRead.ShouldBeTrue();

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                segmenter.FindAll("中国研究生命")
                    .Select(segment => segment.Value)
                    .ShouldBe(new[] { "中国", "研究", "生命" });
                segmenter.Cut("中国研究生命")
                    .ShouldBe(new[] { "中国", "研究", "生命" });
            }))
            .ToArray();

        Task.WhenAll(tasks).GetAwaiter().GetResult();
        AssertReadOnly(segmenter.FindAll("中国"));
    }

    /// <summary>
    /// 验证仓库中的完整外置词典可以独立加载并执行常见中文分词。
    /// </summary>
    [Fact]
    public void ExternalDictionary_LoadsFullSnapshot()
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "segmentation-dict.txt"));
        var segmenter = new ChineseSegmenter(stream);

        segmenter.Cut("我爱中国").ShouldBe(new[] { "我", "爱", "中国" });
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证 UTF-8 标记可用，且错误编码不会被自动切换为其他格式。
    /// </summary>
    [Fact]
    public void Constructor_RequiresUtf8AndAcceptsUtf8Bom()
    {
        using (var marked = new MemoryStream(Encoding.UTF8.GetPreamble()
                   .Concat(Encoding.UTF8.GetBytes("中国 10 ns\n")).ToArray()))
            new ChineseSegmenter(marked).Cut("中国").ShouldBe(new[] { "中国" });

        using var invalid = new MemoryStream(new byte[] { 0xFF, 0xFE, 0x2D });
        Should.Throw<InvalidDataException>(() => new ChineseSegmenter(invalid));
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
    /// 创建 UTF-8 词典输入流。
    /// </summary>
    /// <param name="dictionary">词典文本。</param>
    /// <returns>包含 UTF-8 字节的内存流。</returns>
    private static MemoryStream DictionaryStream(string dictionary) =>
        new MemoryStream(Encoding.UTF8.GetBytes(dictionary));

    /// <summary>
    /// 验证结果列表不支持修改。
    /// </summary>
    /// <typeparam name="T">列表元素类型。</typeparam>
    /// <param name="values">待验证的只读列表。</param>
    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        var list = values as System.Collections.IList;
        list.ShouldNotBeNull();
        Should.Throw<NotSupportedException>(() => list.Add(default(T)));
    }

    /// <summary>
    /// 记录释放状态的内存流。
    /// </summary>
    private sealed class TrackingStream : MemoryStream
    {
        /// <summary>
        /// 初始化一个 <see cref="TrackingStream"/> 类型的实例。
        /// </summary>
        /// <param name="buffer">流的初始字节。</param>
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
