using System.Text.RegularExpressions;
using Bing.Extensions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// 测试 <see cref="StringExtensions"/> 中的 Safe 扩展
/// (ToHtmlSafe / EncryptToBytes+DecryptFromBytes / FilterHtml)
/// </summary>
public class StringExtensionsSafeTests
{
    // ──────────────────────────────────────────
    //  ToHtmlSafe
    // ──────────────────────────────────────────

    [Fact]
    public void ToHtmlSafe_EmptyString_ReturnsEmpty()
    {
        "".ToHtmlSafe().ShouldBe(string.Empty);
    }

    [Fact]
    public void ToHtmlSafe_AngleBrackets_EncodesSpecialChars()
    {
        // '<' (60) and '>' (62) are in the entities list
        var result = "<b>".ToHtmlSafe();
        result.ShouldContain("&#60;");
        result.ShouldContain("&#62;");
    }

    [Fact]
    public void ToHtmlSafe_AllFalse_SafeCharsPassThrough()
    {
        // 'H','e','l','o','!' — '!' is 33, not in entities list when all=false
        // normal letters pass through
        var result = "Hello".ToHtmlSafe(false, false);
        result.ShouldBe("Hello");
    }

    [Fact]
    public void ToHtmlSafe_AllTrue_EncodesEveryChar()
    {
        var result = "AB".ToHtmlSafe(all: true, replace: false);
        // 'A'=65 → &#65;, 'B'=66 → &#66;
        result.ShouldBe("&#65;&#66;");
    }

    // ──────────────────────────────────────────
    //  EncryptToBytes / DecryptFromBytes (round-trip)
    // ──────────────────────────────────────────

    [Fact]
    public void EncryptAndDecrypt_RoundTrip_ReturnsOriginal()
    {
        var original = "HelloWorld";
        var pwd = "mySecret";
        var encrypted = original.EncryptToBytes(pwd);
        encrypted.ShouldNotBeNull();
        encrypted.Length.ShouldBeGreaterThan(0);

        var decrypted = encrypted.DecryptFromBytes(pwd);
        // The decrypted result should start with the original (padding may add nulls)
        decrypted.TrimEnd('\0').ShouldBe(original);
    }

    // ──────────────────────────────────────────
    //  FilterHtml
    // ──────────────────────────────────────────

    [Fact]
    public void FilterHtml_RemovesHtmlTags()
    {
        var result = "<p>Hello <b>World</b></p>".FilterHtml();
        result.ShouldBe("Hello World");
    }

    [Fact]
    public void FilterHtml_RemovesScriptTag()
    {
        var result = "<script>alert('xss')</script>Text".FilterHtml();
        result.ShouldNotContain("<script>");
        result.ShouldContain("Text");
    }

    [Fact]
    public void FilterHtml_EmptyOrWhitespace_ReturnsEmpty()
    {
        "".FilterHtml().ShouldBe(string.Empty);
        "   ".FilterHtml().ShouldBe(string.Empty);
    }

    [Fact]
    public void FilterHtml_NbspReplacedWithSpace()
    {
        var result = "Hello&nbsp;World".FilterHtml();
        result.ShouldBe("Hello World");
    }
}

/// <summary>
/// 测试 <see cref="StringExtensions"/> 中的 Regex 扩展
/// (RegexSplit / GetWords / GetWordByIndex / SpaceOnUpper / ReplaceWith)
/// </summary>
public class StringExtensionsRegexTests
{
    // ──────────────────────────────────────────
    //  RegexSplit
    // ──────────────────────────────────────────

    [Fact]
    public void RegexSplit_DigitPattern_SplitsOnDigits()
    {
        var parts = "a1b2c3".RegexSplit(@"\d", RegexOptions.None);
        parts.ShouldBe(new[] { "a", "b", "c", "" });
    }

    // ──────────────────────────────────────────
    //  GetWords / GetWordByIndex
    // ──────────────────────────────────────────

    [Fact]
    public void GetWords_SpaceSeparated_ReturnsWords()
    {
        var words = "hello world".GetWords();
        words.ShouldContain("hello");
        words.ShouldContain("world");
    }

    [Fact]
    public void GetWordByIndex_ValidIndex_ReturnsWord()
    {
        "hello world".GetWordByIndex(0).ShouldBe("hello");
        "hello world".GetWordByIndex(1).ShouldBe("world");
    }

    [Fact]
    public void GetWordByIndex_InvalidIndex_ThrowsIndexOutOfRangeException()
    {
        Should.Throw<IndexOutOfRangeException>(() => "hello".GetWordByIndex(5));
    }

    // ──────────────────────────────────────────
    //  SpaceOnUpper
    // ──────────────────────────────────────────

    [Fact]
    public void SpaceOnUpper_CamelCase_InsertsSpaces()
    {
        var result = "HelloWorld".SpaceOnUpper();
        result.ShouldBe("Hello World");
    }

    [Fact]
    public void SpaceOnUpper_AllLower_NoChange()
    {
        "hello".SpaceOnUpper().ShouldBe("hello");
    }

    // ──────────────────────────────────────────
    //  ReplaceWith
    // ──────────────────────────────────────────

    [Fact]
    public void ReplaceWith_StringReplacement_ReplacesPattern()
    {
        "abc123".ReplaceWith(@"\d", "X").ShouldBe("abcXXX");
    }

    [Fact]
    public void ReplaceWith_MatchEvaluator_TransformsMatch()
    {
        var result = "abc".ReplaceWith(@"[a-c]", m => m.Value.ToUpper());
        result.ShouldBe("ABC");
    }

    [Fact]
    public void ReplaceWith_WithOptions_IgnoresCase()
    {
        var result = "Hello".ReplaceWith("hello", "Hi", RegexOptions.IgnoreCase);
        result.ShouldBe("Hi");
    }

    [Fact]
    public void ReplaceWith_OptionsAndEvaluator_Works()
    {
        var result = "Hello".ReplaceWith("hello", RegexOptions.IgnoreCase, m => "[" + m.Value + "]");
        result.ShouldBe("[Hello]");
    }
}
