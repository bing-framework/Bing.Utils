using Bing.Extensions;
using Bing.Text;

namespace BingUtilsUT.StringUT;

[Trait("StringUT", "Strings.Basic")]
public class StringsTest
{
    /// <summary>
    /// 测试 - 合并字符串 - 字符集
    /// </summary>
    [Fact]
    public void Test_Merge_For_AllChars()
    {
        var chars = new List<char> {'a', 'b', 'c'};
        Strings.Merge(chars).ShouldBe("abc");
    }

    /// <summary>
    /// 测试 - 合并字符串 - 字符串集合
    /// </summary>
    [Fact]
    public void Test_Merge_For_Strings()
    {
        var strings = new List<string> {"00", "11", "22"};
        Strings.Merge("AA", strings.ToArray()).ShouldBe("AA001122");
    }

    /// <summary>
    /// 测试 - 合并字符串 - 字符串+字符串数组
    /// </summary>
    [Fact]
    public void Test_Merge_For_StringAndStrings()
    {
        Strings.Merge("AA", "00", "11", "22").ShouldBe("AA001122");
    }

    /// <summary>
    /// 测试 - 合并字符串 - 字符串+字符数组
    /// </summary>
    [Fact]
    public void Test_Merge_For_StringAndChars()
    {
        Strings.Merge("AA", '0', '0', '1', '1', '2', '2').ShouldBe("AA001122");
    }

    /// <summary>
    /// 测试 - 返回是否包含字母
    /// </summary>
    [Fact]
    public void Test_HasLetters()
    {
        Strings.HasLetters("").ShouldBeFalse();
        Strings.HasLetters("1234567890").ShouldBeFalse();
        Strings.HasLetters("1234567890a").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 至少包含指定数量的字母
    /// </summary>
    [Fact]
    public void Test_HasLettersAtLeast()
    {
        Strings.HasLettersAtLeast("1234567890", 0).ShouldBeFalse();
        Strings.HasLettersAtLeast("1234567890", -1).ShouldBeFalse();
        Strings.HasLettersAtLeast("1234567890a", 0).ShouldBeTrue();
        Strings.HasLettersAtLeast("1234567890a", 1).ShouldBeTrue();
        Strings.HasLettersAtLeast("1234567890a", 2).ShouldBeFalse();
    }

    #region Equals

    /// <summary>
    /// 测试 - 确定字符串是否与指定集合中的任一字符串相等
    /// </summary>
    [Fact]
    public void Test_EqualsAnyOf()
    {
        // 区分大小写
        "Hello".EqualsAnyOf(StringComparison.Ordinal, "Hello", "World").ShouldBeTrue();
        "Hello".EqualsAnyOf(StringComparison.Ordinal, "hello", "World").ShouldBeFalse();

        // 忽略大小写
        "Hello".EqualsAnyOf(StringComparison.OrdinalIgnoreCase, "hello", "World").ShouldBeTrue();
        "Hello".EqualsAnyOf(StringComparison.OrdinalIgnoreCase, "HELLO", "WORLD").ShouldBeTrue();
        "Hello".EqualsAnyOf(StringComparison.OrdinalIgnoreCase, "hi", "world").ShouldBeFalse();

        // 空字符串比较
        "".EqualsAnyOf(StringComparison.Ordinal, "").ShouldBeTrue();
        "".EqualsAnyOf(StringComparison.Ordinal, "Empty").ShouldBeFalse();

        // null 值处理
        string nullString = null;
        nullString.EqualsAnyOf(StringComparison.Ordinal, null).ShouldBeFalse();
        "Hello".EqualsAnyOf(StringComparison.Ordinal, null).ShouldBeFalse();
        nullString.EqualsAnyOf(StringComparison.Ordinal, "").ShouldBeFalse();

        // 空参数数组
        "Hello".EqualsAnyOf(StringComparison.Ordinal).ShouldBeFalse();

        // 匹配集合中的不同元素
        "Hello".EqualsAnyOf(StringComparison.Ordinal, "Bye", "Hello", "Hi").ShouldBeTrue();
        "World".EqualsAnyOf(StringComparison.Ordinal, "Hello", "World", "Hi").ShouldBeTrue();
        "Hi".EqualsAnyOf(StringComparison.Ordinal, "Hello", "World", "Hi").ShouldBeTrue();

        // 不匹配任何元素
        "Hello".EqualsAnyOf(StringComparison.Ordinal, "Bye", "Good", "Hi").ShouldBeFalse();

        // 各种 StringComparison 选项
        "Hello".EqualsAnyOf(StringComparison.CurrentCulture, "Hello", "World").ShouldBeTrue();
        "Hello".EqualsAnyOf(StringComparison.InvariantCulture, "Hello", "World").ShouldBeTrue();
        "Hello".EqualsAnyOf(StringComparison.CurrentCultureIgnoreCase, "hello", "World").ShouldBeTrue();
        "Hello".EqualsAnyOf(StringComparison.InvariantCultureIgnoreCase, "hello", "World").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 确定字符串是否与指定集合中的任一字符串相等，忽略大小写
    /// </summary>
    [Fact]
    public void Test_EqualsAnyOfIgnoreCase()
    {
        // 基本测试，忽略大小写
        "AAA".EqualsAnyOfIgnoreCase("a", "aa", "aaa").ShouldBeTrue();
        "aaa".EqualsAnyOfIgnoreCase("b", "a", "bb", "AA", "AAA").ShouldBeTrue();
        "Hello".EqualsAnyOfIgnoreCase("hello", "HELLO", "HeLLo").ShouldBeTrue();
        "Hello".EqualsAnyOfIgnoreCase("hi", "world", "bye").ShouldBeFalse();

        // 空字符串
        "".EqualsAnyOfIgnoreCase("").ShouldBeTrue();
        "".EqualsAnyOfIgnoreCase("Empty").ShouldBeFalse();

        // null 值处理
        string nullString = null;
        nullString.EqualsAnyOfIgnoreCase(null).ShouldBeFalse();
        nullString.EqualsAnyOfIgnoreCase("").ShouldBeFalse();
        "Hello".EqualsAnyOfIgnoreCase(null).ShouldBeFalse();

        // 空参数数组
        "ZZZ".EqualsAnyOfIgnoreCase().ShouldBeFalse();

        // 文化相关的字符处理
        "café".EqualsAnyOfIgnoreCase("CAFÉ", "COFFEE").ShouldBeTrue();
        "istanbul".EqualsAnyOfIgnoreCase("ISTANBUL", "TURKEY").ShouldBeTrue();

        // 匹配集合中的不同元素
        "Hello".EqualsAnyOfIgnoreCase("bye", "hello", "hi").ShouldBeTrue();
        "World".EqualsAnyOfIgnoreCase("hello", "world", "hi").ShouldBeTrue();
        "Hi".EqualsAnyOfIgnoreCase("hello", "world", "hi").ShouldBeTrue();

        // 不匹配任何元素
        "Hello".EqualsAnyOfIgnoreCase("bye", "good", "hi").ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - EqualsAnyOf 和 EqualsAnyOfIgnoreCase 的特殊情况
    /// </summary>
    [Fact]
    public void Test_EqualsAnyOf_SpecialCases()
    {
        // 特殊字符串
        "Hello\r\nWorld".EqualsAnyOf(StringComparison.Ordinal, "Hello\r\nWorld").ShouldBeTrue();
        "Hello\r\nWorld".EqualsAnyOf(StringComparison.Ordinal, "Hello\nWorld").ShouldBeFalse();

        // 特殊字符
        "Hello@World".EqualsAnyOf(StringComparison.Ordinal, "Hello@World", "Test").ShouldBeTrue();
        "Hello🙂World".EqualsAnyOf(StringComparison.Ordinal, "Hello🙂World", "Test").ShouldBeTrue();

        // 不可见字符
        "Hello\u200BWorld".EqualsAnyOf(StringComparison.Ordinal, "Hello\u200BWorld").ShouldBeTrue(); // 零宽空格
        "Hello\u200BWorld".EqualsAnyOf(StringComparison.Ordinal, "HelloWorld").ShouldBeFalse();

        // 首尾空格处理 (这些方法不应修剪空格，区别于 EqualsInvariant)
        "  Hello  ".EqualsAnyOf(StringComparison.Ordinal, "  Hello  ").ShouldBeTrue();
        "  Hello  ".EqualsAnyOf(StringComparison.Ordinal, "Hello").ShouldBeFalse();

        // 同样测试 EqualsAnyOfIgnoreCase
        "Hello\r\nWorld".EqualsAnyOfIgnoreCase("HELLO\r\nWORLD").ShouldBeTrue();
        "Hello@World".EqualsAnyOfIgnoreCase("HELLO@WORLD", "TEST").ShouldBeTrue();
        "  Hello  ".EqualsAnyOfIgnoreCase("  HELLO  ").ShouldBeTrue();
        "  Hello  ".EqualsAnyOfIgnoreCase("HELLO").ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 比较不同方法实现的性能差异 (仅用于标记潜在优化点)
    /// </summary>
    [Fact]
    public void Test_EqualsAnyOf_Performance_Comparison()
    {
        string testValue = "HelloWorld";
        string[] testArray = new[] { "Test1", "Test2", "Test3", "Test4", "Test5", "HelloWorld" };

        // 对比优化前后的性能
        // 1. 直接使用 LINQ Any 方法
        bool result1 = testArray.Any(v => string.Equals(testValue, v, StringComparison.Ordinal));

        // 2. 使用优化后的 EqualsAnyOf 方法
        bool result2 = testValue.EqualsAnyOf(StringComparison.Ordinal, testArray);

        // 结果应该是相同的
        Assert.Equal(result1, result2);
    }

    /// <summary>
    /// 测试 - 相等判断，忽略大小写 - 2个字符串
    /// </summary>
    [Fact]
    public void Test_EqualsIgnoreCase()
    {
        "AAA".EqualsIgnoreCase("aaa").ShouldBeTrue();
        "aaa".EqualsIgnoreCase("AAA").ShouldBeTrue();
        "AaA".EqualsIgnoreCase("aAa").ShouldBeTrue();
        "".EqualsIgnoreCase("").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 相等判断，使用不变区域性比较并忽略大小写
    /// </summary>
    [Fact]
    public void Test_EqualsInvariant()
    {
        // 基本比较，大小写不同
        "AAA".EqualsInvariant("aaa").ShouldBeTrue();
        "aaa".EqualsInvariant("AAA").ShouldBeTrue();
        "AaA".EqualsInvariant("aAa").ShouldBeTrue();

        // 带空格的比较
        "  AAA  ".EqualsInvariant("aaa").ShouldBeTrue();
        "AAA".EqualsInvariant("  aaa  ").ShouldBeTrue();
        "  AaA  ".EqualsInvariant("  aAa  ").ShouldBeTrue();

        // 空字符串比较
        "".EqualsInvariant("").ShouldBeTrue();
        "  ".EqualsInvariant("").ShouldBeTrue();
        "".EqualsInvariant("  ").ShouldBeTrue();
        "  ".EqualsInvariant("  ").ShouldBeTrue();

        // null 值比较
        string nullString = null;
        string anotherNullString = null;
        nullString.EqualsInvariant(anotherNullString).ShouldBeTrue();
        nullString.EqualsInvariant("").ShouldBeFalse();
        "".EqualsInvariant(nullString).ShouldBeFalse();
        "AAA".EqualsInvariant(nullString).ShouldBeFalse();
        nullString.EqualsInvariant("AAA").ShouldBeFalse();

        // 不相等的情况
        "AAA".EqualsInvariant("BBB").ShouldBeFalse();
        "AAA".EqualsInvariant("AAAA").ShouldBeFalse();
        "AAAA".EqualsInvariant("AAA").ShouldBeFalse();

        // 特殊字符比较
        "Hello!".EqualsInvariant("hello!").ShouldBeTrue();
        "Привет".EqualsInvariant("привет").ShouldBeTrue();
        "你好".EqualsInvariant("你好").ShouldBeTrue();

        // 带重音符号字符的比较 (区域性相关)
        "café".EqualsInvariant("CAFÉ").ShouldBeTrue();
        "résumé".EqualsInvariant("RÉSUMÉ").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 特殊区域性情况下的相等判断
    /// </summary>
    [Fact]
    public void Test_EqualsInvariant_Culture()
    {
        // 土耳其区域性中 'i' 和 'I' 的大小写转换规则不同
        // 但使用 InvariantCultureIgnoreCase 可以保证一致性
        "istanbul".EqualsInvariant("ISTANBUL").ShouldBeTrue();
        "file".EqualsInvariant("FILE").ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 使用可定制选项比较两个字符串是否相等
    /// </summary>
    [Fact]
    public void Test_EqualsWithOptions()
    {
        // 基本比较 - 默认使用 InvariantCultureIgnoreCase
        "Hello".EqualsWithOptions("hello").ShouldBeTrue();
        "Hello".EqualsWithOptions("HELLO").ShouldBeTrue();
        "Hello".EqualsWithOptions("world").ShouldBeFalse();

        // 使用不同的比较类型
        "Hello".EqualsWithOptions("hello", StringComparison.Ordinal).ShouldBeFalse();
        "Hello".EqualsWithOptions("Hello", StringComparison.Ordinal).ShouldBeTrue();
        "café".EqualsWithOptions("café", StringComparison.Ordinal).ShouldBeTrue();
        "cafe".EqualsWithOptions("café", StringComparison.InvariantCulture).ShouldBeFalse();

        // 带 trim 参数
        "  Hello  ".EqualsWithOptions("hello", trim: true).ShouldBeTrue();
        "  Hello  ".EqualsWithOptions("hello", trim: false).ShouldBeFalse();
        "  ".EqualsWithOptions("", trim: true).ShouldBeTrue();
        "  ".EqualsWithOptions("", trim: false).ShouldBeFalse();

        // 处理 null 值
        string nullString = null;
        string anotherNullString = null;
        nullString.EqualsWithOptions(anotherNullString, handleNull: true).ShouldBeTrue();
        nullString.EqualsWithOptions("", handleNull: true).ShouldBeFalse();
        "".EqualsWithOptions(nullString, handleNull: true).ShouldBeFalse();

        // 不特殊处理null值时遵循普通string.Equals行为
        nullString.EqualsWithOptions(anotherNullString).ShouldBeTrue();
        nullString.EqualsWithOptions("").ShouldBeFalse();
        "".EqualsWithOptions(nullString).ShouldBeFalse();

        // 组合使用选项
        "  Hello  ".EqualsWithOptions("hello", StringComparison.OrdinalIgnoreCase, trim: true, handleNull: true).ShouldBeTrue();
        "  Hello  ".EqualsWithOptions("hello", StringComparison.Ordinal, trim: true, handleNull: true).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - EqualsTo 方法（兼容性方法）
    /// </summary>
    [Fact]
    public void Test_EqualsTo()
    {
        // 基本比较 - 默认使用 InvariantCultureIgnoreCase
        "Hello".EqualsTo("hello").ShouldBeTrue();
        "Hello".EqualsTo("HELLO").ShouldBeTrue();
        "Hello".EqualsTo("world").ShouldBeFalse();

        // 使用不同的比较类型
        "Hello".EqualsTo("hello", StringComparison.Ordinal).ShouldBeFalse();
        "Hello".EqualsTo("Hello", StringComparison.Ordinal).ShouldBeTrue();

        // 不应该处理空格（这是与 EqualsWithOptions 的区别）
        "  Hello  ".EqualsTo("hello").ShouldBeFalse();

        // 验证 null 值处理行为与 EqualsWithOptions(..., handleNull: false) 一致
        string nullString = null;
        string anotherNullString = null;
        nullString.EqualsTo(anotherNullString).ShouldBeTrue();
        nullString.EqualsTo("").ShouldBeFalse();
        "".EqualsTo(nullString).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 字符串比较方法的一致性
    /// </summary>
    [Fact]
    public void Test_StringComparisonConsistency()
    {
        // 测试三个相关方法的行为是否一致

        // 1. 简单大小写忽略比较
        "Hello".EqualsIgnoreCase("hello").ShouldBeTrue();
        "Hello".EqualsWithOptions("hello", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();

        // 2. 带空格的比较
        "  Hello  ".EqualsInvariant("hello").ShouldBeTrue();
        "  Hello  ".EqualsWithOptions("hello", trim: true).ShouldBeTrue();

        // 3. null值比较
        string nullString = null;
        string anotherNull = null;
        nullString.EqualsInvariant(anotherNull).ShouldBeTrue();
        nullString.EqualsWithOptions(anotherNull, handleNull: true).ShouldBeTrue();

        // 4. 确认不同方法在相同参数下行为一致
        // a. EqualsInvariant 相当于 EqualsWithOptions 使用特定参数
        string test1 = "  Hello  ";
        string test2 = "hello";
        test1.EqualsInvariant(test2).ShouldBeEquivalentTo(
            test1.EqualsWithOptions(test2, StringComparison.InvariantCultureIgnoreCase, trim: true, handleNull: true));

        // b. EqualsIgnoreCase 相当于 EqualsWithOptions 使用特定参数
        string test3 = "Hello";
        string test4 = "HELLO";
        test3.EqualsIgnoreCase(test4).ShouldBeEquivalentTo(
            test3.EqualsWithOptions(test4, StringComparison.OrdinalIgnoreCase));

        // c. EqualsTo 相当于 EqualsWithOptions 不使用高级选项
        string test5 = "Hello";
        string test6 = "hello";
        test5.EqualsTo(test6).ShouldBeEquivalentTo(
            test5.EqualsWithOptions(test6, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// 测试 - 字符串比较方法在区域性敏感场景下的行为
    /// </summary>
    [Fact]
    public void Test_StringComparison_CultureSensitive()
    {
        // 1. 土耳其语区域性中，'i' 大写是 'İ' (带点的I)，而不是 'I'
        // 使用InvariantCulture可以避免这种区域性差异

        // 在土耳其语环境下，这个比较会失败
        "file".EqualsWithOptions("FILE", StringComparison.CurrentCultureIgnoreCase).ShouldBeTrue();

        // 使用InvariantCultureIgnoreCase保证在所有区域都一致
        "file".EqualsWithOptions("FILE", StringComparison.InvariantCultureIgnoreCase).ShouldBeTrue();
        "file".EqualsInvariant("FILE").ShouldBeTrue();

        // 2. 测试带重音符号的字符串比较
        "café".EqualsWithOptions("CAFÉ", StringComparison.InvariantCultureIgnoreCase).ShouldBeTrue();
        "café".EqualsInvariant("CAFÉ").ShouldBeTrue();

        // 3. 测试Unicode标准化形式的差异
        // 如："é" 可以是单个字符，也可以是 'e' + 组合重音符
        string normalizedE1 = "é"; // 单个字符
        string normalizedE2 = "e\u0301"; // 'e' + 组合重音符

        // 使用Ordinal比较时会认为不同
        normalizedE1.EqualsWithOptions(normalizedE2, StringComparison.Ordinal).ShouldBeFalse();

        // 对于用户界面显示的字符串，使用InvariantCultureIgnoreCase更合适
        normalizedE1.EqualsInvariant(normalizedE2).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 字符串比较方法的特殊场景
    /// </summary>
    [Fact]
    public void Test_StringComparison_SpecialCases()
    {
        // 1. 空字符串与null比较
        string emptyString = "";
        string nullString = null;
        string whitespaceString = "   ";

        // null值处理
        nullString.EqualsWithOptions(nullString, handleNull: true).ShouldBeTrue();
        nullString.EqualsWithOptions(emptyString, handleNull: true).ShouldBeFalse();

        // 空格处理
        whitespaceString.EqualsWithOptions(emptyString, trim: true).ShouldBeTrue();
        whitespaceString.EqualsWithOptions(emptyString, trim: false).ShouldBeFalse();

        // 2. 非打印字符比较
        string withNonPrintable1 = "Hello\u200BWorld"; // 零宽空格
        string withNonPrintable2 = "Hello\u200BWorld";
        string withoutNonPrintable = "HelloWorld";

        withNonPrintable1.EqualsWithOptions(withNonPrintable2).ShouldBeTrue();
        withNonPrintable1.EqualsWithOptions(withoutNonPrintable, StringComparison.Ordinal).ShouldBeFalse();

        // 3. 组合使用选项处理复杂情况
        string complexString1 = "  Hello\u200BWorld  ";
        string complexString2 = "hello\u200Bworld";

        // 忽略大小写 + 修剪空格 + 继续保留非打印字符
        complexString1.EqualsWithOptions(complexString2,
            StringComparison.OrdinalIgnoreCase, trim: true).ShouldBeTrue();

        // 4. 特殊场景：验证方法的鲁棒性
        // 当一个字符串为null，另一个不为null时
        nullString.EqualsWithOptions("anything").ShouldBeFalse();
        "anything".EqualsWithOptions(nullString).ShouldBeFalse();

        // 极端空格场景
        string extremeWhitespace = "                  ";
        extremeWhitespace.EqualsWithOptions("", trim: true).ShouldBeTrue();
        extremeWhitespace.EqualsWithOptions(" ", trim: true).ShouldBeTrue();
    }

    #endregion

    #region Filter

    /// <summary>
    /// 测试 - 过滤字符 - 正常情况
    /// </summary>
    [Fact]
    public void Test_FilterByChar_Normal()
    {
        // 过滤字母
        string input1 = "Hello123World";
        var result1 = Strings.FilterByChar(input1, c => char.IsLetter(c)).ToArray();
        Assert.Equal("HelloWorld", new string(result1));

        // 过滤数字
        string input2 = "Hello123World";
        var result2 = Strings.FilterByChar(input2, c => char.IsDigit(c)).ToArray();
        Assert.Equal("123", new string(result2));

        // 过滤大写字母
        string input3 = "HelloWorld";
        var result3 = Strings.FilterByChar(input3, c => char.IsUpper(c)).ToArray();
        Assert.Equal("HW", new string(result3));

        // 过滤小写字母
        string input4 = "HelloWorld";
        var result4 = Strings.FilterByChar(input4, c => char.IsLower(c)).ToArray();
        Assert.Equal("elloorld", new string(result4));

        // 自定义条件
        string input5 = "aeiouAEIOU12345";
        var result5 = Strings.FilterByChar(input5, c => "aeiouAEIOU".Contains(c)).ToArray();
        Assert.Equal("aeiouAEIOU", new string(result5));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 空字符串
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Test_FilterByChar_Empty(string input)
    {
        var result = Strings.FilterByChar(input, c => true).ToArray();
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试 - 过滤字符 - 所有字符都匹配
    /// </summary>
    [Fact]
    public void Test_FilterByChar_AllMatch()
    {
        string input = "Hello123";
        var result = Strings.FilterByChar(input, c => true).ToArray();
        Assert.Equal(input, new string(result));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 所有字符都不匹配
    /// </summary>
    [Fact]
    public void Test_FilterByChar_NoMatch()
    {
        string input = "Hello123";
        var result = Strings.FilterByChar(input, c => false).ToArray();
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试 - 过滤字符 - 空谓词参数
    /// </summary>
    [Fact]
    public void Test_FilterByChar_NullPredicate()
    {
        string input = "Hello123";
        Assert.Throws<ArgumentNullException>(() => Strings.FilterByChar(input, null).ToArray());
    }

    /// <summary>
    /// 测试 - 过滤字符 - 中文字符
    /// </summary>
    [Fact]
    public void Test_FilterByChar_ChineseChars()
    {
        string input = "你好Hello123世界";

        // 过滤中文字符
        var result1 = Strings.FilterByChar(input, c => c >= 0x4E00 && c <= 0x9FFF).ToArray();
        Assert.Equal("你好世界", new string(result1));

        // 过滤非中文字符
        var result2 = Strings.FilterByChar(input, c => !(c >= 0x4E00 && c <= 0x9FFF)).ToArray();
        Assert.Equal("Hello123", new string(result2));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 特殊字符
    /// </summary>
    [Fact]
    public void Test_FilterByChar_SpecialChars()
    {
        string input = "Hello!@#$%^&*()_+World";

        // 过滤特殊字符
        var result = Strings.FilterByChar(input, c => !char.IsLetterOrDigit(c)).ToArray();
        Assert.Equal("!@#$%^&*()_+", new string(result));
    }

    /// <summary>
    /// 测试 - 过滤字符 - 与其他字符串方法组合
    /// </summary>
    [Fact]
    public void Test_FilterByChar_Combination()
    {
        string input = "  Hello  World  ";

        // 过滤非空格字符，然后合并
        var result1 = Strings.Merge(Strings.FilterByChar(input, c => !char.IsWhiteSpace(c)));
        Assert.Equal("HelloWorld", result1);

        // 过滤出数字，转为字符串
        string input2 = "The price is $123.45";
        var result2 = Strings.Merge(Strings.FilterByChar(input2, c => char.IsDigit(c) || c == '.'));
        Assert.Equal("123.45", result2);
    }

    /// <summary>
    /// 测试 - 字符过滤模式 - 标准模式和ASCII模式对比
    /// </summary>
    [Fact]
    public void Test_CharFilterMode_Comparison()
    {
        // 准备含有各种类型字符的测试字符串
        string testString = "Hello123测试世界!@#";

        // 标准模式 - 保留Unicode字母和数字
        var standardLettersAndNumbers = Strings.GetNumbersAndLetters(testString, CharFilterMode.Standard);
        Assert.Equal("Hello123测试世界", standardLettersAndNumbers);

        // ASCII模式 - 只保留ASCII字母和数字
        var asciiLettersAndNumbers = Strings.GetNumbersAndLetters(testString, CharFilterMode.Ascii);
        Assert.Equal("Hello123", asciiLettersAndNumbers);

        // 标准模式 - 数字过滤
        var standardNumbers = Strings.GetNumbers(testString, CharFilterMode.Standard);
        Assert.Equal("123", standardNumbers);

        // ASCII模式 - 数字过滤 (对数字结果应该相同)
        var asciiNumbers = Strings.GetNumbers(testString, CharFilterMode.Ascii);
        Assert.Equal("123", asciiNumbers);

        // 标准模式 - 字母过滤
        var standardLetters = Strings.GetLetters(testString, CharFilterMode.Standard);
        Assert.Equal("Hello测试世界", standardLetters);

        // ASCII模式 - 字母过滤
        var asciiLetters = Strings.GetLetters(testString, CharFilterMode.Ascii);
        Assert.Equal("Hello", asciiLetters);
    }

    /// <summary>
    /// 测试 - 过滤字母和数字 - 各种Unicode字符集
    /// </summary>
    [Fact]
    public void Test_FilterForNumbersAndLetters_UnicodeCharsets()
    {
        // 测试各种Unicode字符集

        // 1. 拉丁字母扩展
        string latinExtended = "ĀāĂăĄąĆćĈĉĊċČč";
        var standardLatinExt = Strings.GetNumbersAndLetters(latinExtended, CharFilterMode.Standard);
        var asciiLatinExt = Strings.GetNumbersAndLetters(latinExtended, CharFilterMode.Ascii);
        Assert.Equal(latinExtended, standardLatinExt); // 标准模式保留所有拉丁扩展字母
        Assert.Equal("", asciiLatinExt);              // ASCII模式过滤掉所有非ASCII字母

        // 2. 希腊字母
        string greek = "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρςστυφχψω";
        var standardGreek = Strings.GetNumbersAndLetters(greek, CharFilterMode.Standard);
        var asciiGreek = Strings.GetNumbersAndLetters(greek, CharFilterMode.Ascii);
        Assert.Equal(greek, standardGreek);  // 标准模式保留希腊字母
        Assert.Equal("", asciiGreek);        // ASCII模式过滤掉希腊字母

        // 3. 西里尔字母
        string cyrillic = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        var standardCyrillic = Strings.GetNumbersAndLetters(cyrillic, CharFilterMode.Standard);
        var asciiCyrillic = Strings.GetNumbersAndLetters(cyrillic, CharFilterMode.Ascii);
        Assert.Equal(cyrillic, standardCyrillic); // 标准模式保留西里尔字母
        Assert.Equal("", asciiCyrillic);         // ASCII模式过滤掉西里尔字母

        // 4. 阿拉伯文
        string arabic = "ابتثجحخدذرزسشصضطظعغفقكلمنهوي";
        var standardArabic = Strings.GetNumbersAndLetters(arabic, CharFilterMode.Standard);
        var asciiArabic = Strings.GetNumbersAndLetters(arabic, CharFilterMode.Ascii);
        Assert.Equal(arabic, standardArabic); // 标准模式保留阿拉伯文
        Assert.Equal("", asciiArabic);       // ASCII模式过滤掉阿拉伯文

        // 5. 中日韩统一表意文字
        string cjk = "你好世界こんにちは안녕하세요";
        var standardCjk = Strings.GetNumbersAndLetters(cjk, CharFilterMode.Standard);
        var asciiCjk = Strings.GetNumbersAndLetters(cjk, CharFilterMode.Ascii);
        Assert.Equal(cjk, standardCjk); // 标准模式保留中日韩文字
        Assert.Equal("", asciiCjk);    // ASCII模式过滤掉中日韩文字
    }

    /// <summary>
    /// 测试 - 过滤数字 - Unicode和ASCII数字
    /// </summary>
    [Fact]
    public void Test_FilterForNumbers_UnicodeDigits()
    {
        // 测试各种数字形式

        // 1. 阿拉伯数字（ASCII数字）
        string arabicNumerals = "0123456789";
        var standardArabic = Strings.GetNumbers(arabicNumerals, CharFilterMode.Standard);
        var asciiArabic = Strings.GetNumbers(arabicNumerals, CharFilterMode.Ascii);
        Assert.Equal(arabicNumerals, standardArabic);
        Assert.Equal(arabicNumerals, asciiArabic);

        // 2. 全角数字
        string fullWidthNumerals = "０１２３４５６７８９";
        var standardFullWidth = Strings.GetNumbers(fullWidthNumerals, CharFilterMode.Standard);
        var asciiFullWidth = Strings.GetNumbers(fullWidthNumerals, CharFilterMode.Ascii);
        Assert.Equal(fullWidthNumerals, standardFullWidth); // 标准模式识别全角数字
        Assert.Equal("", asciiFullWidth);                  // ASCII模式不识别全角数字

        // 3. 其他Unicode数字
        // 例如泰文数字、印度-阿拉伯数字等
        string thaiNumerals = "๐๑๒๓๔๕๖๗๘๙"; // 泰文数字0-9
        var standardThai = Strings.GetNumbers(thaiNumerals, CharFilterMode.Standard);
        var asciiThai = Strings.GetNumbers(thaiNumerals, CharFilterMode.Ascii);
        Assert.Equal(thaiNumerals, standardThai); // 标准模式识别泰文数字
        Assert.Equal("", asciiThai);             // ASCII模式不识别泰文数字
    }

    /// <summary>
    /// 测试 - 过滤字母和数字 - 正常情况
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello123")]
    [InlineData("测试ABC123", "ABC123")] // 中文也是字母的一种
    [InlineData("12345", "12345")]
    [InlineData("ABCDE", "ABCDE")]
    [InlineData("!@#$%^", "")]
    public void Test_FilterForNumbersAndLetters_Normal(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForNumbersAndLetters(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 过滤字母和数字 - 边界情况
    /// </summary>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    public void Test_FilterForNumbersAndLetters_Edge(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForNumbersAndLetters(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 过滤字母和数字 - 国际字符
    /// </summary>
    [Fact]
    public void Test_FilterForNumbersAndLetters_International()
    {
        // 测试中文字符
        string chineseInput = "你好123世界";
        var chineseResult = Strings.Merge(Strings.FilterForNumbersAndLetters(chineseInput, CharFilterMode.Standard));
        Assert.Equal("你好123世界", chineseResult);

        // 测试希腊字母
        string greekInput = "α β γ 123";
        var greekResult = Strings.Merge(Strings.FilterForNumbersAndLetters(greekInput, CharFilterMode.Standard));
        // 只保留希腊字母和数字，空格会被过滤掉
        Assert.Equal("αβγ123", greekResult);

        // 测试西里尔字母
        string cyrillicInput = "Привет123";
        var cyrillicResult = Strings.Merge(Strings.FilterForNumbersAndLetters(cyrillicInput, CharFilterMode.Standard));
        Assert.Equal("Привет123", cyrillicResult);
    }

    /// <summary>
    /// 测试 - 过滤字母和数字 - 混合各种字符
    /// </summary>
    [Fact]
    public void Test_FilterForNumbersAndLetters_Mixed()
    {
        string mixedInput = "Hello世界123!@#$%^&*()_+";
        var mixedResult = Strings.Merge(Strings.FilterForNumbersAndLetters(mixedInput, CharFilterMode.Standard));
        Assert.Equal("Hello世界123", mixedResult);
    }

    /// <summary>
    /// 测试 - 过滤字母和数字 - 与相关方法的比较
    /// </summary>
    [Fact]
    public void Test_FilterForNumbersAndLetters_CompareWithRelated()
    {
        string input = "Hello123!@#";

        // 获取字母和数字
        var lettersAndNumbers = Strings.Merge(Strings.FilterForNumbersAndLetters(input));

        // 分别获取字母和数字，然后合并
        var letters = Strings.Merge(Strings.FilterForLetters(input));
        var numbers = Strings.Merge(Strings.FilterForNumbers(input));
        var combined = letters + numbers;

        // 字母+数字的结果应当与直接获取字母和数字的结果相符（顺序可能不同）
        Assert.Equal(lettersAndNumbers.Length, combined.Length);
        Assert.Equal(lettersAndNumbers.OrderBy(c => c), combined.OrderBy(c => c));
    }

    /// <summary>
    /// 测试 - GetNumbersAndLetters方法
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello123")]
    [InlineData("测试ABC123", "ABC123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_GetNumbersAndLetters(string input, string expected)
    {
        var result = Strings.GetNumbersAndLetters(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 只过滤数字
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "123")]
    [InlineData("测试ABC123", "123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_FilterForNumbers(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForNumbers(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 只过滤字母
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello")]
    [InlineData("测试ABC123", "ABC")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_FilterForLetters(string input, string expected)
    {
        var result = Strings.Merge(Strings.FilterForLetters(input));
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - GetNumbers方法
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "123")]
    [InlineData("测试ABC123", "123")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_GetNumbers(string input, string expected)
    {
        var result = Strings.GetNumbers(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - GetLetters方法
    /// </summary>
    [Theory]
    [InlineData("Hello123!@#", "Hello")]
    [InlineData("测试ABC123", "ABC")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!@#$%^", "")]
    public void Test_GetLetters(string input, string expected)
    {
        var result = Strings.GetLetters(input);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 字符串扩展方法 Where
    /// </summary>
    [Fact]
    public void Test_StringExtension_Where()
    {
        string input = "Hello123";
        var result = input.Where(c => char.IsLetter(c)).ToArray();
        Assert.Equal("Hello", new string(result));
    }

    #endregion

    #region Repeat

    /// <summary>
    /// 测试 - 重复指定次数的字符
    /// </summary>
    [Fact]
    public void Test_Repeat()
    {
        Strings.Repeat("ABC", -1).ShouldBeEmpty();
        Strings.Repeat("ABC", 0).ShouldBeEmpty();
        Strings.Repeat("ABC", 1).ShouldBe("ABC");
        Strings.Repeat("ABC", 2).ShouldBe("ABCABC");
    }

    /// <summary>
    /// 测试 - 重复字符串 - 正常情况
    /// </summary>
    [Theory]
    [InlineData("a", 3, "aaa")]
    [InlineData("abc", 2, "abcabc")]
    [InlineData("测试", 2, "测试测试")]
    [InlineData("abc123", 3, "abc123abc123abc123")]
    public void Test_Repeat_Normal(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 单个字符优化
    /// </summary>
    [Theory]
    [InlineData("a", 5, "aaaaa")]
    [InlineData("#", 3, "###")]
    [InlineData("好", 2, "好好")]
    public void Test_Repeat_SingleChar(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 优化场景 (1-4次)
    /// </summary>
    [Theory]
    [InlineData("abc", 1, "abc")]
    [InlineData("abc", 2, "abcabc")]
    [InlineData("abc", 3, "abcabcabc")]
    [InlineData("abc", 4, "abcabcabcabc")]
    public void Test_Repeat_OptimizedCases(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 边界情况
    /// </summary>
    [Theory]
    [InlineData(null, 3, "")]
    [InlineData("", 3, "")]
    [InlineData("abc", 0, "")]
    [InlineData("abc", -1, "")]
    [InlineData(" ", 5, "     ")]
    public void Test_Repeat_Edge(string input, int times, string expected)
    {
        var result = Strings.Repeat(input, times);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 - 重复字符串 - 性能测试
    /// </summary>
    [Fact]
    public void Test_Repeat_Performance()
    {
        // 大量重复的性能测试
        const int repeatTimes = 10000;
        const string testString = "test";

        var result = Strings.Repeat(testString, repeatTimes);

        // 确保结果长度正确
        Assert.Equal(testString.Length * repeatTimes, result.Length);

        // 验证结果的前部分和后部分
        Assert.Equal(testString, result.Substring(0, testString.Length));
        Assert.Equal(testString, result.Substring(result.Length - testString.Length));
    }

    /// <summary>
    /// 测试 - 重复字符串 - 扩展方法
    /// </summary>
    [Fact]
    public void Test_Repeat_Extension()
    {
        // 测试扩展方法
        const string testString = "xyz";
        const int repeatTimes = 3;

        var resultFromMethod = Strings.Repeat(testString, repeatTimes);
        var resultFromExtension = testString.Repeat(repeatTimes);

        // 验证扩展方法与静态方法结果一致
        Assert.Equal(resultFromMethod, resultFromExtension);
    }

    #endregion

    /// <summary>
    /// 测试 - 从左向右截取字符串
    /// </summary>
    [Fact]
    public void Test_Left()
    {
        Strings.Left("ABCDEFG", 0).ShouldBeEmpty();
        Strings.Left("ABCDEFG", 1).ShouldBe("A");
        Strings.Left("ABCDEFG", 2).ShouldBe("AB");
        Strings.Left("ABCDEFG", 3).ShouldBe("ABC");
        Strings.Left("ABCDEFG", 4).ShouldBe("ABCD");
        Strings.Left("ABCDEFG", 5).ShouldBe("ABCDE");
        Strings.Left("ABCDEFG", 6).ShouldBe("ABCDEF");
        Strings.Left("ABCDEFG", 7).ShouldBe("ABCDEFG");
        Strings.Left("ABCDEFG", 8).ShouldBe("ABCDEFG");
    }

    /// <summary>
    /// 测试 - 从右向左截取字符串
    /// </summary>
    [Fact]
    public void Test_Right()
    {
        Strings.Right("ABCDEFG", 0).ShouldBeEmpty();
        Strings.Right("ABCDEFG", 1).ShouldBe("G");
        Strings.Right("ABCDEFG", 2).ShouldBe("FG");
        Strings.Right("ABCDEFG", 3).ShouldBe("EFG");
        Strings.Right("ABCDEFG", 4).ShouldBe("DEFG");
        Strings.Right("ABCDEFG", 5).ShouldBe("CDEFG");
        Strings.Right("ABCDEFG", 6).ShouldBe("BCDEFG");
        Strings.Right("ABCDEFG", 7).ShouldBe("ABCDEFG");
        Strings.Right("ABCDEFG", 8).ShouldBe("ABCDEFG");
    }

    /// <summary>
    /// 测试 - 获取通用前缀
    /// </summary>
    [Fact]
    public void Test_CommonPrefix()
    {
        var textOne = "AAABBBCCC";
        var textTwo = "AABBCC";

        Strings.CommonPrefix(textOne, textTwo).ShouldBe("AA");
        Strings.CommonPrefix(textOne, textTwo, out var v1).ShouldBe("AA");
        v1.ShouldBe(2);
    }

    /// <summary>
    /// 测试 - 获取通用后缀
    /// </summary>
    [Fact]
    public void Test_CommonSuffix()
    {
        var textOne = "AAABBBCCC";
        var textTwo = "AABBCC";

        Strings.CommonSuffix(textOne, textTwo).ShouldBe("CC");
        Strings.CommonSuffix(textOne, textTwo, out var v1).ShouldBe("CC");
        v1.ShouldBe(2);
    }
}