using Bing.Text;

namespace BingUtilsUT.StringUT.Extensions;

/// <summary>
/// 字符串移除扩展方法测试
/// </summary>
[Trait("StringUT.Extensions", "String.Remove")]
public class StringRemoveExtensionsTest
{
    /// <summary>
    /// 测试 - 移除字符串中指定的子字符串之后的内容（忽略大小写）
    /// </summary>
    [Fact]
    public void Test_RemoveFromIgnoreCase_Extension()
    {
        // 基本功能测试
        "HelloWorld".RemoveFromIgnoreCase("o").ShouldBe("Hell");
        "HelloWorld".RemoveFromIgnoreCase("O").ShouldBe("Hell");
        "HelloWorld".RemoveFromIgnoreCase("x").ShouldBe("HelloWorld"); // 不包含的子字符串

        // 边界情况测试
        "".RemoveFromIgnoreCase("o").ShouldBe("");
        "HelloWorld".RemoveFromIgnoreCase("").ShouldBe("HelloWorld");
        ((string)null).RemoveFromIgnoreCase("o").ShouldBe(null);
    }

    /// <summary>
    /// 测试 - 移除字符串中的重复空格
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateSpaces_Extension()
    {
        // 基本功能测试
        "Hello  World".RemoveDuplicateSpaces().ShouldBe("Hello World");
        "Hello   World  Test".RemoveDuplicateSpaces().ShouldBe("Hello World Test");
        "No  Multiple   Spaces".RemoveDuplicateSpaces().ShouldBe("No Multiple Spaces");

        // 边界情况测试
        "".RemoveDuplicateSpaces().ShouldBe("");
        ((string)null).RemoveDuplicateSpaces().ShouldBeNull();
        " ".RemoveDuplicateSpaces().ShouldBe(" "); // 单空格不变
        "   ".RemoveDuplicateSpaces().ShouldBe(" "); // 多空格变单空格
    }

    /// <summary>
    /// 测试 - RemoveDuplicateSpaces 与 RemoveDuplicateWhiteSpaces 功能一致性
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateSpaces_Consistency_Extension()
    {
        string[] testCases = {
            "Hello  World",
            "Hello   World  Test",
            "",
            null,
            " ",
            "   ",
            "NoSpaces"
        };

        foreach (var testCase in testCases)
        {
            var result1 = testCase.RemoveDuplicateSpaces();
            var result2 = testCase.RemoveDuplicateWhiteSpaces();
            Assert.Equal(result2, result1);
        }
    }

    /// <summary>
    /// 测试 - 移除音调字符
    /// </summary>
    [Fact]
    public void Test_RemoveAccentsIgnoreCase_Extension()
    {
        // 基本功能测试
        "café".RemoveAccentsIgnoreCase().ShouldBe("cafe");
        "Hélló Wórld".RemoveAccentsIgnoreCase().ShouldBe("Hello World");

        // 特殊字符保留测试
        "Áaa Ééé Ííí Óóó Úúú".RemoveAccentsIgnoreCase().ShouldBe("Aaa Eee Iii Ooo Uuu");
        "üÜ".RemoveAccentsIgnoreCase().ShouldBe("uU");

        // 边界情况测试
        "".RemoveAccentsIgnoreCase().ShouldBe("");
        ((string)null).RemoveAccentsIgnoreCase().ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 移除音调字符，包括 'Ñ'/'ñ' 字符
    /// </summary>
    [Fact]
    public void Test_RemoveAccentsIgnoreCaseAndN_Extension()
    {
        // 基本功能测试
        "café".RemoveAccentsIgnoreCaseAndN().ShouldBe("cafe");
        "El niño".RemoveAccentsIgnoreCaseAndN().ShouldBe("El nino");

        // 特殊字符测试
        "España".RemoveAccentsIgnoreCaseAndN().ShouldBe("Espana");
        "SEÑOR".RemoveAccentsIgnoreCaseAndN().ShouldBe("SENOR");

        // 边界情况测试
        "".RemoveAccentsIgnoreCaseAndN().ShouldBe("");
        ((string)null).RemoveAccentsIgnoreCaseAndN().ShouldBeNull();
    }

    /// <summary>
    /// 测试 - 移除子字符串扩展方法
    /// </summary>
    [Fact]
    public void Test_Remove_Extension()
    {
        // 基本功能测试
        "Hello World".Remove("World").ShouldBe("Hello ");
        "Hello World".Remove("world", IgnoreCase.True).ShouldBe("Hello ");

        // 验证与静态方法的一致性
        var text = "Hello World";
        var removeText = "World";
        text.Remove(removeText).ShouldBe(Strings.Remove(text, removeText));
        text.Remove(removeText, IgnoreCase.True).ShouldBe(Strings.Remove(text, removeText, IgnoreCase.True));
    }

    /// <summary>
    /// 测试 - 移除字符扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveChars_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveChars(' ').ShouldBe("HelloWorld");
        "Hello World".RemoveChars('e', 'o').ShouldBe("Hll Wrld");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveChars('e', 'o').ShouldBe(Strings.RemoveChars(text, 'e', 'o'));

        // 边界情况测试
        "".RemoveChars('a').ShouldBe("");
        ((string)null).RemoveChars('a').ShouldBeNull();
        "abc".RemoveChars().ShouldBe("abc"); // 不移除任何字符
    }

    /// <summary>
    /// 测试 - 移除空格扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveWhiteSpace_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveWhiteSpace().ShouldBe("HelloWorld");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveWhiteSpace().ShouldBe(Strings.RemoveWhiteSpace(text));
    }

    /// <summary>
    /// 测试 - 移除重复空白字符扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateWhiteSpaces_Extension()
    {
        // 基本功能测试
        "Hello  World".RemoveDuplicateWhiteSpaces().ShouldBe("Hello World");

        // 验证与静态方法的一致性
        var text = "Hello  World";
        text.RemoveDuplicateWhiteSpaces().ShouldBe(Strings.RemoveDuplicateWhiteSpaces(text));
    }

    /// <summary>
    /// 测试 - 移除重复字符扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveDuplicateChar_Extension()
    {
        // 基本测试
        "Hello".RemoveDuplicateChar('l').ShouldBe("Helo");
        "aabbc".RemoveDuplicateChar('a').ShouldBe("abbc");
        "aaa".RemoveDuplicateChar('a').ShouldBe("a");

        // 区分大小写测试
        "aAabBb".RemoveDuplicateChar('a', IgnoreCase.False).ShouldBe("aAabBb");
        "aAabBb".RemoveDuplicateChar('A', IgnoreCase.False).ShouldBe("aAabBb");

        // 忽略大小写测试
        "aAabBb".RemoveDuplicateChar('a', IgnoreCase.True).ShouldBe("abBb");
        "aAabBb".RemoveDuplicateChar('A', IgnoreCase.True).ShouldBe("abBb");

        // 边界情况测试
        "".RemoveDuplicateChar('a').ShouldBe("");
        ((string)null).RemoveDuplicateChar('a').ShouldBeNull();
        "abcdef".RemoveDuplicateChar('z').ShouldBe("abcdef");

        // 特殊字符测试
        "a  b  c".RemoveDuplicateChar(' ').ShouldBe("a b c");
        "a\t\t\tb".RemoveDuplicateChar('\t').ShouldBe("a\tb");

        // 复杂情况测试
        "Mississippi".RemoveDuplicateChar('i').ShouldBe("Mississippi");
        "Mississippi".RemoveDuplicateChar('s').ShouldBe("Misisippi");

        // 验证与静态方法的一致性
        var text = "Hello";
        text.RemoveDuplicateChar('l').ShouldBe(Strings.RemoveDuplicateChar(text, 'l'));
        text.RemoveDuplicateChar('l', IgnoreCase.True).ShouldBe(Strings.RemoveDuplicateChar(text, 'l', IgnoreCase.True));
    }

    /// <summary>
    /// 测试 - 从索引位置移除字符扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveSince_Index_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveSince(5).ShouldBe("Hello");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveSince(5).ShouldBe(Strings.RemoveSince(text, 5));
    }

    /// <summary>
    /// 测试 - 从子字符串位置移除扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveSince_String_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveSince("World").ShouldBe("Hello ");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveSince("World").ShouldBe(Strings.RemoveSince(text, "World"));
        text.RemoveSince("world", IgnoreCase.True).ShouldBe(Strings.RemoveSince(text, "world", IgnoreCase.True));
    }

    /// <summary>
    /// 测试 - 从子字符串位置移除（忽略大小写）扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveSinceIgnoreCase_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveSinceIgnoreCase("world").ShouldBe("Hello ");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveSinceIgnoreCase("world").ShouldBe(Strings.RemoveSinceIgnoreCase(text, "world"));
    }

    /// <summary>
    /// 测试 - 移除开头字符串扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveStart_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveStart("Hello").ShouldBe(" World");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveStart("Hello").ShouldBe(Strings.RemoveStart(text, "Hello"));
    }

    /// <summary>
    /// 测试 - 移除末尾字符串扩展方法
    /// </summary>
    [Fact]
    public void Test_RemoveEnd_Extension()
    {
        // 基本功能测试
        "Hello World".RemoveEnd("World").ShouldBe("Hello ");

        // 验证与静态方法的一致性
        var text = "Hello World";
        text.RemoveEnd("World").ShouldBe(Strings.RemoveEnd(text, "World"));
    }
}