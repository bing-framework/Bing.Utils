using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bing.Extra.Emoji;
using Shouldly;
using Xunit;

namespace Bing.Utils.Extra.Tests.Bing.Extra.Emoji;

/// <summary>
/// 验证离线本地化的目录隔离与输入契约。
/// </summary>
public class EmojiCatalogLocalizationTests
{
    /// <summary>
    /// 提供有效的笑脸翻译记录。
    /// </summary>
    private const string Entry = "{\"unicode\":\"😀\",\"name\":\"业务笑脸\",\"keywords\":[\"业务词\",\"业务词\",\"Happy\",\"happy\"]}";

    /// <summary>
    /// 验证覆盖后所有目录入口使用一致元数据且不影响原目录。
    /// </summary>
    [Fact]
    public void Import_ReplacesLocalizationWithoutChangingOriginal()
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        original.TryGetByUnicode("😀", out var before).ShouldBeTrue();
        original.TryGetLocalization("😀", "zh-Hant", out var traditional).ShouldBeTrue();
        using var stream = Json(Document("ZH", Entry));
        var result = original.WithLocalizations(stream);

        result.ShouldNotBeSameAs(original);
        stream.CanRead.ShouldBeTrue();
        result.Items.Count.ShouldBe(original.Items.Count);
        result.GetLocales().ShouldBe(new[] { "zh", "zh-Hant" });
        result.TryGetLocalization("😀", "zh", out var localized).ShouldBeTrue();
        localized.Name.ShouldBe("业务笑脸");
        localized.Keywords.ShouldBe(new[] { "业务词", "Happy", "happy" });
        result.TryGetLocalization("😀", "zh-Hant", out var preserved).ShouldBeTrue();
        preserved.ShouldBeSameAs(traditional);
        result.TryGetByUnicode("😀", out var after).ShouldBeTrue();
        after.ShouldNotBeSameAs(before);
        after.Name.ShouldBe(before.Name);
        after.Group.ShouldBe(before.Group);
        after.Subgroup.ShouldBe(before.Subgroup);
        after.Version.ShouldBe(before.Version);
        after.Aliases.ShouldBe(before.Aliases);
        after.Tags.ShouldBe(before.Tags);
        after.Variants.ShouldBe(before.Variants);
        result.Search("业务词", "ZH").Single().ShouldBeSameAs(after);
        result.Search("业务笑脸").Single().ShouldBeSameAs(after);
        result.FindAll("A😀").Single().Emoji.ShouldBeSameAs(after);
        result.TryGetByAlias("grinning", out var byAlias).ShouldBeTrue();
        byAlias.ShouldBeSameAs(after);
        result.Replace("A😀", match => match.Emoji.Localizations.Single(l => l.Locale == "zh").Name).ShouldBe("A业务笑脸");
        original.Search("业务笑脸").ShouldBeEmpty();
        EmojiUtil.Search("业务笑脸").ShouldBeEmpty();
        result.Search("grinning face").ShouldContain(item => item.Unicode == "😀");
        result.Count("👍🏽👨‍👩‍👧‍👦🇨🇳").ShouldBe(3);
        Should.Throw<NotSupportedException>(() => ((IList<string>)localized.Keywords).Add("修改"));
        Should.Throw<NotSupportedException>(() => ((IList<EmojiLocalization>)after.Localizations).Clear());
        Should.Throw<NotSupportedException>(() => ((IList<string>)result.GetLocales()).Clear());
    }

    /// <summary>
    /// 验证兼容形式与多次导入保留语言规范标识。
    /// </summary>
    [Fact]
    public void Import_NormalizesVariantsAndSupportsSequentialImports()
    {
        EmojiUtil.TryGetByUnicode("❤️", out var heart).ShouldBeTrue();
        var original = EmojiUtil.CreateCatalog(new[] { heart });
        using var firstStream = Json(Document("X-BUSINESS", Entry.Replace("😀", "❤")));
        var first = original.WithLocalizations(firstStream);
        first.TryGetLocalization("❤", "x-business", out var added).ShouldBeTrue();
        added.Locale.ShouldBe("x-business");
        first.GetLocales().ShouldBe(new[] { "zh", "zh-Hant", "x-business" });
        using var secondStream = Json(Document("ZH-hANT", Entry.Replace("😀", "❤️").Replace("[\"业务词\",\"业务词\",\"Happy\",\"happy\"]", "[]")));
        var second = first.WithLocalizations(secondStream);
        second.TryGetLocalization("❤", "zh-Hant", out var replaced).ShouldBeTrue();
        replaced.Locale.ShouldBe("zh-Hant");
        replaced.Name.ShouldBe("业务笑脸");
        replaced.Keywords.ShouldBeEmpty();
        second.TryGetLocalization("❤️", "X-BUSINESS", out var retained).ShouldBeTrue();
        retained.ShouldBeSameAs(added);
        first.Search("业务笑脸", "zh-Hant").ShouldBeEmpty();
        original.GetLocales().ShouldBe(new[] { "zh", "zh-Hant" });
        second.IsEmoji("😀").ShouldBeFalse();
        second.IsEmoji("❤").ShouldBeTrue();
        second.Search("业务笑脸", "zh-TW").ShouldBeEmpty();
    }

    /// <summary>
    /// 验证导入后的肤色操作使用目录元数据和白名单范围。
    /// </summary>
    [Fact]
    public void ImportedCatalog_SkinToneOperationsUseImportedDirectory()
    {
        EmojiUtil.TryGetByUnicode("👍", out var thumb).ShouldBeTrue();
        EmojiUtil.TryGetByUnicode("👍🏽", out var medium).ShouldBeTrue();
        EmojiUtil.TryGetByUnicode("👍🏿", out var dark).ShouldBeTrue();
        var original = EmojiUtil.CreateCatalog(new[] { thumb, medium, dark });
        using var stream = Json(Document("zh", Entry.Replace("😀", "👍🏽")));
        var result = original.WithLocalizations(stream);

        result.TryGetSkinTone("👍🏽", out var info).ShouldBeTrue();
        result.TryGetByUnicode("👍🏽", out var localized).ShouldBeTrue();
        info.Emoji.ShouldBeSameAs(localized);
        info.BaseEmoji.ShouldBeSameAs(thumb);
        info.Variants.Select(item => item.Unicode).ShouldBe(new[] { "👍", "👍🏽", "👍🏿" });
        result.GetSkinToneVariants("👍🏽").ShouldContain(item => item == localized);
        result.GetBySkinTone(EmojiSkinTone.Medium).Single().ShouldBeSameAs(localized);
        result.ApplySkinTone("A👍🏽B", EmojiSkinTone.Dark).ShouldBe("A👍🏿B");
        result.ApplySkinTone("👍🏽", EmojiSkinTone.Light).ShouldBe("👍🏽");
        result.ApplySkinTones("👍🏽", new[] { EmojiSkinTone.Dark }).ShouldBe("👍🏿");
        result.RemoveSkinTones("A👍🏽B").ShouldBe("A👍B");
        result.TryGetSkinTone(null, out var missing).ShouldBeFalse();
        missing.ShouldBeNull();
        result.GetSkinToneVariants(null).ShouldBeEmpty();
        Should.Throw<ArgumentOutOfRangeException>(() => result.GetBySkinTone((EmojiSkinTone)0));
        Should.Throw<ArgumentNullException>(() => result.ApplySkinTones("👍", null));

        original.TryGetByUnicode("👍🏽", out var unchanged).ShouldBeTrue();
        unchanged.ShouldBeSameAs(medium);
        EmojiUtil.TryGetSkinTone("👍🏽", out var global).ShouldBeTrue();
        global.Emoji.ShouldBeSameAs(medium);
        EmojiUtil.GetSkinToneVariants("👍").Count.ShouldBe(6);
    }

    /// <summary>
    /// 验证导入后多肤色组合仍按修饰符顺序匹配目录变体。
    /// </summary>
    [Fact]
    public void ImportedCatalog_MultiSkinToneVariantsKeepTheirOrder()
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        using var stream = Json(Document("zh", Entry.Replace("😀", "🫱🏽‍🫲🏼")));
        var result = original.WithLocalizations(stream);

        result.TryGetSkinTone("🫱🏽‍🫲🏼", out var info).ShouldBeTrue();
        result.TryGetLocalization("🫱🏽‍🫲🏼", "zh", out var localization).ShouldBeTrue();
        info.Emoji.Localizations.ShouldContain(localization);
        info.Tones.ShouldBe(new[] { EmojiSkinTone.Medium, EmojiSkinTone.MediumLight });
        result.ApplySkinTones("🫱🏽‍🫲🏼", new[] { EmojiSkinTone.Dark, EmojiSkinTone.Medium })
            .ShouldBe("🫱🏿‍🫲🏽");
        result.ApplySkinTone("🫱🏽‍🫲🏼", EmojiSkinTone.Dark).ShouldBe("🫱🏽‍🫲🏼");
        result.RemoveSkinTones("🫱🏽‍🫲🏼").ShouldBe("🫱🏽‍🫲🏼");
    }

    /// <summary>
    /// 验证空记录创建独立目录且不添加空语言。
    /// </summary>
    [Fact]
    public void EmptyEntries_ReturnEquivalentNewCatalog()
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        using var stream = Json(Document("x-empty", ""));
        var result = original.WithLocalizations(stream);
        result.ShouldNotBeSameAs(original);
        result.Items.ShouldBe(original.Items);
        result.GetLocales().ShouldBe(original.GetLocales());
        var empty = EmojiUtil.CreateCatalog(Array.Empty<EmojiInfo>());
        using var emptyStream = Json(Document("x-empty", ""));
        empty.WithLocalizations(emptyStream).Items.ShouldBeEmpty();
    }

    /// <summary>
    /// 验证本地化查询失败时返回空输出。
    /// </summary>
    /// <param name="unicode">待查询的完整表情序列。</param>
    /// <param name="locale">待查询的语言标识。</param>
    [Theory]
    [InlineData(null, "zh")]
    [InlineData("", "zh")]
    [InlineData("😀", null)]
    [InlineData("😀", "")]
    [InlineData("😀", "en")]
    [InlineData("😀", "zh-CN")]
    [InlineData("not-emoji", "zh")]
    public void Query_MissingDataReturnsFalse(string unicode, string locale)
    {
        var catalog = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        catalog.TryGetLocalization(unicode, locale, out var value).ShouldBeFalse();
        value.ShouldBeNull();
    }

    /// <summary>
    /// 验证非法文档不会改变原目录。
    /// </summary>
    /// <param name="json">待验证的 JSON 文本。</param>
    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"formatVersion\":1")]
    [InlineData("{\"locale\":\"zh\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":2,\"locale\":\"zh\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":9999999999999999999,\"locale\":\"zh\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":\"1\",\"locale\":\"zh\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":1,\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":null,\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh CN\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\\tCN\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\"}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\",\"entries\":null}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\",\"entries\":{}}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\",\"locale\":\"zh\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"formatVersion\":1,\"locale\":\"zh\",\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\",\"entries\":[],\"entries\":[]}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\",\"entries\":[]}{}")]
    [InlineData("{\"formatVersion\":1,\"locale\":\"zh\",\"entries\":[]} garbage")]
    public void InvalidDocument_ThrowsInvalidData(string json)
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        using var stream = Json(json);
        Should.Throw<InvalidDataException>(() => original.WithLocalizations(stream));
        original.GetLocales().ShouldBe(new[] { "zh", "zh-Hant" });
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证条目缺失、类型错误、未知表情与关键词错误。
    /// </summary>
    /// <param name="entry">待验证的 JSON 条目。</param>
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"unicode\":\"😀\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\"}")]
    [InlineData("{\"name\":\"test\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":null,\"name\":\"test\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"text\",\"name\":\"test\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀😀\",\"name\":\"test\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":1,\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":null,\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"　\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":null}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":{}}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":[null]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":[1]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":[\"\"]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":[\"　\"]}")]
    [InlineData("{\"unicode\":\"😀\",\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"name\":\"test\",\"keywords\":[]}")]
    [InlineData("{\"unicode\":\"😀\",\"name\":\"test\",\"keywords\":[],\"keywords\":[]}")]
    public void InvalidEntry_ThrowsInvalidData(string entry)
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        using var stream = Json(Document("zh", entry));
        Should.Throw<InvalidDataException>(() => original.WithLocalizations(stream));
        original.Search("业务笑脸").ShouldBeEmpty();
    }

    /// <summary>
    /// 验证重复规范表情和部分有效输入均不会部分导入。
    /// </summary>
    [Fact]
    public void DuplicateAndUnknownEntries_FailAtomically()
    {
        EmojiUtil.TryGetByUnicode("❤️", out var heart).ShouldBeTrue();
        var original = EmojiUtil.CreateCatalog(new[] { heart });
        var entry = Entry.Replace("😀", "❤");
        foreach (var entries in new[] { entry + "," + entry, entry + "," + entry.Replace("❤", "❤️"), entry + "," + Entry })
        {
            using var stream = Json(Document("zh", entries));
            Should.Throw<InvalidDataException>(() => original.WithLocalizations(stream));
            original.Search("业务笑脸").ShouldBeEmpty();
        }
    }

    /// <summary>
    /// 验证合法转义、BOM 和尾部空白可正常导入。
    /// </summary>
    [Fact]
    public void EscapesAndUtf8Bom_AreSupported()
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        var json = Document("zh", Entry.Replace("业务笑脸", "括号{}[]\\\"引号\\\\路径")) + " \r\n\t";
        using var stream = Json("\uFEFF" + json);
        var result = original.WithLocalizations(stream);
        result.TryGetLocalization("😀", "zh", out var value).ShouldBeTrue();
        value.Name.ShouldBe("括号{}[]\"引号\\路径");
    }

    /// <summary>
    /// 验证非法 UTF-8 字节作为格式错误报告。
    /// </summary>
    [Fact]
    public void InvalidUtf8_ThrowsInvalidData()
    {
        var bytes = Encoding.UTF8.GetBytes(Document("zh", Entry));
        bytes[Array.IndexOf(bytes, (byte)'z')] = 0xFF;
        using var stream = new MemoryStream(bytes);
        Should.Throw<InvalidDataException>(() => EmojiUtil.CreateCatalog(EmojiUtil.GetAll()).WithLocalizations(stream));
    }

    /// <summary>
    /// 验证从当前位置读取且不关闭不可定位的输入流。
    /// </summary>
    [Fact]
    public void Import_ReadsCurrentPositionWithoutSeekingOrDisposing()
    {
        using var memory = Json("prefix" + Document("zh", Entry));
        memory.Position = 6;
        using var stream = new ReadOnlyStream(memory);
        var result = EmojiUtil.CreateCatalog(EmojiUtil.GetAll()).WithLocalizations(stream);
        result.Search("业务笑脸", "zh").Count.ShouldBe(1);
        stream.Disposed.ShouldBeFalse();
        memory.CanRead.ShouldBeTrue();
        memory.Position.ShouldBe(memory.Length);
    }

    /// <summary>
    /// 验证无效流参数和底层读取异常。
    /// </summary>
    [Fact]
    public void Import_RejectsUnreadableStreamsAndPreservesReadException()
    {
        var original = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
        Should.Throw<ArgumentNullException>(() => original.WithLocalizations(null));
        var closed = new MemoryStream();
        closed.Dispose();
        Should.Throw<ArgumentException>(() => original.WithLocalizations(closed));
        using var memory = new MemoryStream();
        var failure = new IOException("读取失败");
        using var stream = new ReadOnlyStream(memory, failure);
        Should.Throw<IOException>(() => original.WithLocalizations(stream)).ShouldBeSameAs(failure);
        stream.Disposed.ShouldBeFalse();
    }

    /// <summary>
    /// 验证导入目录支持并发读取。
    /// </summary>
    [Fact]
    public void ImportedCatalog_SupportsConcurrentReads()
    {
        using var stream = Json(Document("zh", Entry));
        var catalog = EmojiUtil.CreateCatalog(EmojiUtil.GetAll()).WithLocalizations(stream);
        Parallel.For(0, 32, _ =>
        {
            catalog.TryGetLocalization("😀", "ZH", out var value).ShouldBeTrue();
            value.Name.ShouldBe("业务笑脸");
            catalog.Search("业务词", "zh").Count.ShouldBe(1);
            catalog.FindAll("😀👍🏽").Count.ShouldBe(2);
        });
    }

    /// <summary>
    /// 构造测试用单语言 JSON 文档。
    /// </summary>
    /// <param name="locale">语言标识。</param>
    /// <param name="entries">JSON 条目片段。</param>
    /// <returns>完整 JSON 文档。</returns>
    private static string Document(string locale, string entries) =>
        "{\"formatVersion\":1,\"locale\":\"" + locale + "\",\"entries\":[" + entries + "]}";

    /// <summary>
    /// 创建 UTF-8 输入流。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    /// <returns>包含 UTF-8 字节的内存流。</returns>
    private static MemoryStream Json(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    /// <summary>
    /// 模拟不可定位的只读输入及读取异常。
    /// </summary>
    private sealed class ReadOnlyStream : Stream
    {
        /// <summary>
        /// 保存提供输入的底层流。
        /// </summary>
        private readonly Stream _inner;
        /// <summary>
        /// 保存读取时需要传播的异常。
        /// </summary>
        private readonly Exception _failure;
        /// <summary>
        /// 获取流是否已释放。
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// 初始化一个 <see cref="ReadOnlyStream" /> 类型的实例。
        /// </summary>
        /// <param name="inner">底层输入流。</param>
        /// <param name="failure">可选读取异常。</param>
        public ReadOnlyStream(Stream inner, Exception failure = null)
        {
            _inner = inner;
            _failure = failure;
        }
        /// <inheritdoc />
        public override bool CanRead => !Disposed;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => _failure != null ? throw _failure : _inner.Read(buffer, offset, count);
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
