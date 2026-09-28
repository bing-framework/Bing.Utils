using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Bing.Text.Pinyin;

/// <summary>
/// 测试外置拼音词库的读取与实例隔离。
/// </summary>
[Trait("TextUT", "PinyinCatalog")]
public class PinyinCatalogTests
{
    /// <summary>
    /// 验证外置词组读音优先于单字，并支持带调与无调转换。
    /// </summary>
    [Fact]
    public void Conversion_UsesExternalPhraseAndCharacterData()
    {
        using var characters = Compress("4E2D\tzhōng\n56FD\tguó\n91CD\tzhòng\n5E86\tqìng\n");
        using var phrases = Compress("重庆\tchóng qìng\n");
        var catalog = new PinyinCatalog(characters, phrases);

        catalog.GetPinyinWithTone("中国重庆", " ").ShouldBe("Zhōng Guó Chóng Qìng");
        catalog.GetContextualPinyin("中国重庆", " ").ShouldBe("Zhong Guo Chong Qing");
        catalog.GetPinyinWithTone("A😀\uD83D").ShouldBe("A😀\uD83D");
        catalog.GetPinyinWithTone(null).ShouldBeNull();
        catalog.GetContextualPinyin(string.Empty).ShouldBe(string.Empty);
        Should.Throw<ArgumentNullException>(() => catalog.GetPinyinWithTone("中国", null));
        characters.CanRead.ShouldBeTrue();
        phrases.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 验证不同外置目录互不影响，也不会修改静态内置入口。
    /// </summary>
    [Fact]
    public void Catalogs_AreIndependentOfEachOtherAndStaticEntry()
    {
        using var firstCharacters = Compress("4E2D\tzhōng\n");
        using var secondCharacters = Compress("4E2D\tzhòng\n");
        using var firstPhrases = Compress(string.Empty);
        using var secondPhrases = Compress(string.Empty);
        var first = new PinyinCatalog(firstCharacters, firstPhrases);
        var second = new PinyinCatalog(secondCharacters, secondPhrases);

        first.GetPinyinWithTone("中").ShouldBe("Zhōng");
        second.GetPinyinWithTone("中").ShouldBe("Zhòng");
        PinyinUtil.GetPinyinWithTone("中").ShouldBe("Zhōng");
        Parallel.For(0, 16, _ => first.GetContextualPinyin("中").ShouldBe("Zhong"));
    }

    /// <summary>
    /// 验证无效 UTF-8、重复记录与缺失字段会拒绝加载。
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvalidExternalData()
    {
        using var phrases = Compress(string.Empty);
        Should.Throw<ArgumentNullException>(() => new PinyinCatalog(null, phrases));
        using var invalidCharacters = Compress("4E2D\tzhōng\n4E2D\tzhòng\n");
        Should.Throw<InvalidDataException>(() => new PinyinCatalog(invalidCharacters, phrases));
        using var validCharacters = Compress("4E2D\tzhōng\n");
        using var invalidPhrases = Compress("中国\tzhōng\n");
        Should.Throw<InvalidDataException>(() => new PinyinCatalog(validCharacters, invalidPhrases));
        using var invalidUtf8 = CompressBytes(new byte[] { 0xFF });
        using var emptyPhrases = Compress(string.Empty);
        Should.Throw<InvalidDataException>(() => new PinyinCatalog(invalidUtf8, emptyPhrases));
    }

    /// <summary>
    /// 验证仓库独立资源与内置词库输出一致。
    /// </summary>
    [Fact]
    public void ExternalSnapshot_MatchesStaticConversion()
    {
        using var characters = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "pinyin-characters.gz"));
        using var phrases = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "pinyin-phrases.gz"));
        var catalog = new PinyinCatalog(characters, phrases);
        var texts = new[] { "重庆银行", "绿𠀀", "中国工具" };
        foreach (var text in texts)
        {
            catalog.GetPinyinWithTone(text, " ").ShouldBe(PinyinUtil.GetPinyinWithTone(text, " "));
            catalog.GetContextualPinyin(text, " ").ShouldBe(PinyinUtil.GetContextualPinyin(text, " "));
        }
    }

    /// <summary>
    /// 将 UTF-8 文本压缩到可读内存流。
    /// </summary>
    /// <param name="content">词库内容。</param>
    /// <returns>位置归零的压缩流。</returns>
    private static MemoryStream Compress(string content) => CompressBytes(Encoding.UTF8.GetBytes(content));

    /// <summary>
    /// 将原始字节压缩到可读内存流。
    /// </summary>
    /// <param name="content">原始字节。</param>
    /// <returns>位置归零的压缩流。</returns>
    private static MemoryStream CompressBytes(byte[] content)
    {
        var stream = new MemoryStream();
        using (var gzip = new GZipStream(stream, CompressionMode.Compress, true))
            gzip.Write(content, 0, content.Length);
        stream.Position = 0;
        return stream;
    }
}
