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
/// 测试中文分词器词典覆盖、搜索子词、生命周期和并发契约。
/// </summary>
[Trait("TextUT", "ChineseSegmenter")]
public class ChineseSegmenterAdvancedTests
{
    /// <summary>
    /// 验证词典覆盖会替换同名词频，并且连续覆盖保留此前新增词。
    /// </summary>
    [Fact]
    public void WithDictionary_OverridesExistingFrequencyAndAccumulates()
    {
        var original = CreateSegmenter(
            "甲 100 n\n乙 100 n\n丙 100 n\n甲乙 1 n\n");

        var first = original.WithDictionary(DictionaryStream("甲乙 10000 n\n"));
        var second = first.WithDictionary(DictionaryStream("乙丙 10000 n\n"));

        original.Cut("甲乙").ShouldBe(new[] { "甲", "乙" });
        first.Cut("甲乙").ShouldBe(new[] { "甲乙" });
        first.Cut("乙丙").ShouldBe(new[] { "乙", "丙" });
        second.Cut("甲乙").ShouldBe(new[] { "甲乙" });
        second.Cut("乙丙").ShouldBe(new[] { "乙丙" });
        first.ShouldNotBeSameAs(original);
        second.ShouldNotBeSameAs(first);
    }

    /// <summary>
    /// 验证同一业务词典中的重复词使用最后一条词频。
    /// </summary>
    [Fact]
    public void WithDictionary_UsesLastDuplicateInBusinessDictionary()
    {
        var original = CreateSegmenter("甲 10 n\n乙 10 n\n");
        var customized = original.WithDictionary(DictionaryStream(
            "甲乙 1 n\n甲乙 1000 n\n"));

        customized.Cut("甲乙").ShouldBe(new[] { "甲乙" });
    }

    /// <summary>
    /// 验证连续覆盖同一词时仅新实例使用最新词频，既有实例保持不变。
    /// </summary>
    [Fact]
    public void WithDictionary_ReplacesSameWordAcrossSnapshots()
    {
        var original = CreateSegmenter("甲 10 n\n乙 10 n\n");
        var first = original.WithDictionary(DictionaryStream("甲乙 1000 n\n"));
        var second = first.WithDictionary(DictionaryStream("甲乙 1 n\n"));

        original.Cut("甲乙").ShouldBe(new[] { "甲", "乙" });
        first.Cut("甲乙").ShouldBe(new[] { "甲乙" });
        second.Cut("甲乙").ShouldBe(new[] { "甲", "乙" });
    }

    /// <summary>
    /// 验证空覆盖返回内容等价的新实例且不改变原分词器。
    /// </summary>
    [Fact]
    public void WithDictionary_EmptyDictionaryReturnsIndependentEquivalentSnapshot()
    {
        var original = CreateSegmenter("中国 100 ns\n");
        using var stream = DictionaryStream(string.Empty);

        var copy = original.WithDictionary(stream);

        copy.ShouldNotBeSameAs(original);
        copy.Cut("中国").ShouldBe(new[] { "中国" });
        original.Cut("中国").ShouldBe(new[] { "中国" });
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证搜索结果包含原分词和命中的二字、三字重叠子词，并按位置和长度排序。
    /// </summary>
    [Fact]
    public void FindForSearch_ReturnsOverlappingSubwordsInStableOrder()
    {
        var segmenter = CreateSegmenter(
            "中华人民共和国 100 n\n" +
            "中华 1 n\n" +
            "中华人 1 n\n" +
            "人民 1 n\n" +
            "共和国 1 n\n");
        var text = "A😀中华人民共和国!";

        var segments = segmenter.FindForSearch(text);

        segments.Select(segment => segment.Value).ShouldBe(new[]
        {
            "A", "😀", "中华", "中华人", "中华人民共和国", "人民", "共和国", "!"
        });
        segments.Select(segment => segment.Index).ShouldBe(new[] { 0, 1, 3, 3, 3, 5, 7, 10 });
        segments.Select(segment => segment.Length).ShouldBe(new[] { 1, 2, 2, 3, 7, 2, 3, 1 });
        segmenter.CutForSearch(text).ShouldBe(segments.Select(segment => segment.Value));
        AssertReadOnly(segments);
        AssertReadOnly(segmenter.CutForSearch(text));
    }

    /// <summary>
    /// 验证搜索子词按 Unicode 标量计数，并将补充平面汉字的位置换算为 UTF-16 长度。
    /// </summary>
    [Fact]
    public void FindForSearch_HandlesSupplementaryHanziWithUtf16Positions()
    {
        var segmenter = CreateSegmenter(
            "𠀀甲乙丙 10000 n\n" +
            "𠀀甲 1 n\n" +
            "甲乙 1 n\n" +
            "乙丙 1 n\n");

        var segments = segmenter.FindForSearch("𠀀甲乙丙");

        segments.Select(segment => segment.Value).ShouldBe(new[]
        {
            "𠀀甲", "𠀀甲乙丙", "甲乙", "乙丙"
        });
        segments.Select(segment => segment.Index).ShouldBe(new[] { 0, 0, 2, 3 });
        segments.Select(segment => segment.Length).ShouldBe(new[] { 3, 5, 2, 2 });
        AssertReadOnly(segments);
    }

    /// <summary>
    /// 验证无词典时搜索仍保留非中文原分词，不生成虚假的搜索子词。
    /// </summary>
    [Fact]
    public void FindForSearch_EmptyDictionaryPreservesNonChineseSegments()
    {
        var segmenter = CreateSegmenter(string.Empty);
        var text = "abc123 \t😀!甲";

        var segments = segmenter.FindForSearch(text);

        segments.Select(segment => segment.Value).ShouldBe(
            new[] { "abc123", " \t", "😀", "!", "甲" });
        string.Concat(segments.Select(segment => segment.Value)).ShouldBe(text);
        segmenter.CutForSearch(text).ShouldBe(segments.Select(segment => segment.Value));
    }

    /// <summary>
    /// 验证搜索分词接口对 null 和空文本返回空的只读列表。
    /// </summary>
    [Fact]
    public void FindForSearch_NullAndEmptyTextReturnReadOnlyEmptyResults()
    {
        var segmenter = CreateSegmenter("中国 10 ns\n");

        var nullSegments = segmenter.FindForSearch(null);
        var emptySegments = segmenter.FindForSearch(string.Empty);
        var nullWords = segmenter.CutForSearch(null);
        var emptyWords = segmenter.CutForSearch(string.Empty);

        nullSegments.ShouldBeEmpty();
        emptySegments.ShouldBeEmpty();
        nullWords.ShouldBeEmpty();
        emptyWords.ShouldBeEmpty();
        AssertReadOnly(nullSegments);
        AssertReadOnly(emptySegments);
        AssertReadOnly(nullWords);
        AssertReadOnly(emptyWords);
    }

    /// <summary>
    /// 验证三字原词同时作为搜索子词时只保留一个相同位置的结果。
    /// </summary>
    [Fact]
    public void FindForSearch_RemovesDuplicateOriginalThreeCharacterWord()
    {
        var segmenter = CreateSegmenter("甲乙丙 100 n\n");

        var segments = segmenter.FindForSearch("甲乙丙");

        segments.Select(segment => segment.Value).ShouldBe(new[] { "甲乙丙" });
        segments.Select(segment => segment.Index).ShouldBe(new[] { 0 });
        segments.Select(segment => segment.Length).ShouldBe(new[] { 3 });
        segmenter.CutForSearch("甲乙丙").ShouldBe(new[] { "甲乙丙" });
    }

    /// <summary>
    /// 验证词典覆盖读取当前位置、保持输入流开放，并拒绝非法参数和 UTF-8。
    /// </summary>
    [Fact]
    public void WithDictionary_ValidatesStreamAndLeavesItOpen()
    {
        var segmenter = CreateSegmenter("甲 1 n\n");
        using var stream = new TrackingStream(Encoding.UTF8.GetBytes("prefix甲乙 10 n\n"));
        stream.Position = Encoding.UTF8.GetByteCount("prefix");

        var result = segmenter.WithDictionary(stream);

        result.Cut("甲乙").ShouldBe(new[] { "甲乙" });
        stream.WasDisposed.ShouldBeFalse();
        stream.CanRead.ShouldBeTrue();
        stream.Position.ShouldBe(stream.Length);

        Should.Throw<ArgumentNullException>(() => segmenter.WithDictionary(null));
        using (var unreadable = new UnreadableStream())
            Should.Throw<ArgumentException>(() => segmenter.WithDictionary(unreadable));
        using (var invalidUtf8 = new MemoryStream(new byte[] { 0xE4, 0xB8, 0xFF }))
            Should.Throw<InvalidDataException>(() => segmenter.WithDictionary(invalidUtf8));
    }

    /// <summary>
    /// 验证导入失败不会改变原实例或此前成功生成的派生实例。
    /// </summary>
    [Fact]
    public void WithDictionary_FailedImportLeavesExistingSnapshotsUnchanged()
    {
        var original = CreateSegmenter("甲 10 n\n乙 10 n\n");
        var customized = original.WithDictionary(DictionaryStream("甲乙 1000 n\n"));
        using var invalid = DictionaryStream("甲乙 1 n\n坏行\n");

        Should.Throw<InvalidDataException>(() => customized.WithDictionary(invalid));

        original.Cut("甲乙").ShouldBe(new[] { "甲", "乙" });
        customized.Cut("甲乙").ShouldBe(new[] { "甲乙" });
    }

    /// <summary>
    /// 验证可读但不可定位的词典流能够导入并在读取后保持打开。
    /// </summary>
    [Fact]
    public void Dictionary_ReadsNonSeekableStreamAndLeavesItOpen()
    {
        using var baseStream = new NonSeekableReadableStream(
            Encoding.UTF8.GetBytes("甲乙 100 n\n"));
        var segmenter = new ChineseSegmenter(baseStream);

        baseStream.WasDisposed.ShouldBeFalse();
        segmenter.Cut("甲乙").ShouldBe(new[] { "甲乙" });

        using var overlayStream = new NonSeekableReadableStream(
            Encoding.UTF8.GetBytes("乙丙 100 n\n"));
        var customized = segmenter.WithDictionary(overlayStream);

        overlayStream.WasDisposed.ShouldBeFalse();
        customized.Cut("乙丙").ShouldBe(new[] { "乙丙" });
    }

    /// <summary>
    /// 验证覆盖后的分词器和搜索器可以被多个线程并发读取。
    /// </summary>
    [Fact]
    public void WithDictionary_ConcurrentReadsRemainStable()
    {
        var segmenter = CreateSegmenter(
            "中华 1 n\n" +
            "中华人 1 n\n" +
            "人民 1 n\n" +
            "共和国 1 n\n");
        var customized = segmenter.WithDictionary(DictionaryStream("中华人民 5000 n\n"));

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() =>
            {
                customized.Cut("中华人民共和国").ShouldBe(new[] { "中华人民", "共和国" });
                customized.FindForSearch("中华人民共和国")
                    .ShouldContain(segment => segment.Value == "中华");
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
    /// 创建 UTF-8 词典流。
    /// </summary>
    /// <param name="dictionary">词典文本。</param>
    /// <returns>包含 UTF-8 字节的内存流。</returns>
    private static MemoryStream DictionaryStream(string dictionary) =>
        new MemoryStream(Encoding.UTF8.GetBytes(dictionary));

    /// <summary>
    /// 验证只读列表不支持修改。
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
    /// 提供可读但不可定位的测试流。
    /// </summary>
    private sealed class NonSeekableReadableStream : Stream
    {
        /// <summary>
        /// 流的初始字节。
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        /// 当前读取位置。
        /// </summary>
        private int _position;

        /// <summary>
        /// 初始化一个 <see cref="NonSeekableReadableStream"/> 类型的实例。
        /// </summary>
        /// <param name="buffer">流的初始字节。</param>
        public NonSeekableReadableStream(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        /// <summary>
        /// 获取流是否已释放。
        /// </summary>
        public bool WasDisposed { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => !WasDisposed;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (WasDisposed)
                throw new ObjectDisposedException(nameof(NonSeekableReadableStream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset > buffer.Length - count)
                throw new ArgumentOutOfRangeException();

            var length = Math.Min(count, _buffer.Length - _position);
            if (length > 0)
            {
                Buffer.BlockCopy(_buffer, _position, buffer, offset, length);
                _position += length;
            }
            return length;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

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
