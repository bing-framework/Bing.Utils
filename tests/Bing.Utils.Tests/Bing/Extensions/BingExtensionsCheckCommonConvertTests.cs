using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bing.Extensions;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// BingExtensions.Check  — Required / CheckNotNull / CheckNotNullOrEmpty /
///                         CheckNotEmpty / CheckLessThan / CheckGreaterThan / CheckBetween
/// BingExtensions.Common — SafeValue / Value / Description(enum) / Join /
///                         IsMatch / GetMatch / GetMatchingValues
/// BingExtensions.Convert — SafeString / ToBool / ToInt / ToLong / ToDouble /
///                          ToDecimal / ToDate / ToGuid / ToSnakeCase / ToCamelCase
/// BingExtensions.DateTime — Description(TimeSpan)
/// </summary>
public class BingExtensionsCheckCommonConvertTests
{
    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Check — Required
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Required_AssertionTrue_DoesNotThrow()
    {
        int val = 5;
        Should.NotThrow(() => val.Required(v => v > 0, "must be positive"));
    }

    [Fact]
    public void Required_AssertionFalse_ThrowsException()
    {
        int val = -1;
        Should.Throw<Exception>(() => val.Required(v => v > 0, "must be positive"));
    }

    [Fact]
    public void Required_TypedExceptionFalse_ThrowsArgumentException()
    {
        string s = "bad";
        Should.Throw<ArgumentException>(() => s.Required<string, ArgumentException>(v => v == "ok", "not ok"));
    }

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Check — CheckNotNull / CheckNotNullOrEmpty / CheckNotEmpty
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CheckNotNull_NonNull_DoesNotThrow()
    {
        var obj = new object();
        Should.NotThrow(() => obj.CheckNotNull("obj"));
    }

    [Fact]
    public void CheckNotNull_Null_ThrowsArgumentNullException()
    {
        object? obj = null;
        Should.Throw<ArgumentNullException>(() => obj.CheckNotNull("obj"));
    }

    [Fact]
    public void CheckNotNullOrEmpty_NonEmpty_DoesNotThrow()
        => Should.NotThrow(() => "hello".CheckNotNullOrEmpty("s"));

    [Fact]
    public void CheckNotNullOrEmpty_NullString_Throws()
        => Should.Throw<ArgumentException>(() => ((string)null).CheckNotNullOrEmpty("s"));

    [Fact]
    public void CheckNotEmpty_Guid_NonEmpty_DoesNotThrow()
        => Should.NotThrow(() => Guid.NewGuid().CheckNotEmpty("g"));

    [Fact]
    public void CheckNotEmpty_GuidEmpty_ThrowsArgumentException()
        => Should.Throw<ArgumentException>(() => Guid.Empty.CheckNotEmpty("g"));

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Check — Range checks
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CheckLessThan_ValueLessThan_DoesNotThrow()
        => Should.NotThrow(() => 3.CheckLessThan("x", 5));

    [Fact]
    public void CheckLessThan_ValueEqual_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(() => 5.CheckLessThan("x", 5));

    [Fact]
    public void CheckLessThan_CanEqualTrue_DoesNotThrow()
        => Should.NotThrow(() => 5.CheckLessThan("x", 5, canEqual: true));

    [Fact]
    public void CheckGreaterThan_ValueGreater_DoesNotThrow()
        => Should.NotThrow(() => 10.CheckGreaterThan("x", 5));

    [Fact]
    public void CheckBetween_InRange_DoesNotThrow()
        => Should.NotThrow(() => 5.CheckBetween("x", 1, 10, startEqual: true, endEqual: true));

    [Fact]
    public void CheckBetween_OutOfRange_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(() => 0.CheckBetween("x", 1, 10, startEqual: true, endEqual: true));

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Common — SafeValue
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void SafeValue_NullableInt_Null_ReturnsDefault() => ((int?)null).SafeValue().ShouldBe(0);
    [Fact] public void SafeValue_NullableInt_HasValue_ReturnsValue() => ((int?)42).SafeValue().ShouldBe(42);

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Common — Join
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Join_IntList_ReturnsCommaSeparated()
    {
        var result = new[] { 1, 2, 3 }.Join();
        result.ShouldBe("1,2,3");
    }

    [Fact]
    public void Join_WithQuotes_WrapsEachItem()
    {
        var result = new[] { "a", "b" }.Join(quotes: "'");
        result.ShouldBe("'a','b'");
    }

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Common — IsMatch / GetMatch / GetMatchingValues
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsMatch_Matching_ReturnsTrue() => "abc123".IsMatch(@"\d+").ShouldBeTrue();
    [Fact] public void IsMatch_NotMatching_ReturnsFalse() => "abcdef".IsMatch(@"\d+").ShouldBeFalse();
    [Fact] public void IsMatch_NullValue_ReturnsFalse() => ((string)null).IsMatch(@"\d+").ShouldBeFalse();

    [Fact]
    public void IsMatch_WithOptions_ReturnsCorrectResult()
        => "Hello".IsMatch("hello", RegexOptions.IgnoreCase).ShouldBeTrue();

    [Fact]
    public void GetMatch_ReturnsFirstMatch()
        => "abc 123 def 456".GetMatch(@"\d+").ShouldBe("123");

    [Fact]
    public void GetMatch_EmptyString_ReturnsEmpty()
        => "".GetMatch(@"\d+").ShouldBe(string.Empty);

    [Fact]
    public void GetMatchingValues_ReturnsAllMatches()
    {
        var matches = "abc 12 def 34 ghi 56".GetMatchingValues(@"\d+");
        matches.ShouldBe(new[] { "12", "34", "56" }, ignoreOrder: false);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Convert — SafeString
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void SafeString_Null_ReturnsEmpty() => ((object)null).SafeString().ShouldBe(string.Empty);
    [Fact] public void SafeString_WithSpaces_Trims() => ((object)"  hi  ").SafeString().ShouldBe("hi");
    [Fact] public void SafeString_Int_ReturnsString() => ((object)42).SafeString().ShouldBe("42");

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Convert — ToBool / ToBoolOrNull
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void ToBool_True_ReturnsTrue() => "true".ToBool().ShouldBeTrue();
    [Fact] public void ToBool_False_ReturnsFalse() => "false".ToBool().ShouldBeFalse();
    [Fact] public void ToBool_One_ReturnsTrue() => "1".ToBool().ShouldBeTrue();
    [Fact] public void ToBoolOrNull_Null_ReturnsNull() => ((string)null).ToBoolOrNull().ShouldBeNull();

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Convert — ToInt / ToLong / ToDouble / ToDecimal
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void ToInt_ValidString_ReturnsInt() => "42".ToInt().ShouldBe(42);
    [Fact] public void ToInt_InvalidString_ReturnsZero() => "abc".ToInt().ShouldBe(0);
    [Fact] public void ToIntOrNull_Null_ReturnsNull() => ((string)null).ToIntOrNull().ShouldBeNull();

    [Fact] public void ToLong_ValidString_ReturnsLong() => "9999999999".ToLong().ShouldBe(9999999999L);
    [Fact] public void ToLongOrNull_Invalid_ReturnsNull() => "xyz".ToLongOrNull().ShouldBeNull();

    [Fact] public void ToDouble_ValidString_ReturnsDouble() => "3.14".ToDouble().ShouldBe(3.14d, 0.001d);
    [Fact] public void ToDoubleOrNull_Null_ReturnsNull() => ((string)null).ToDoubleOrNull().ShouldBeNull();

    [Fact] public void ToDecimal_ValidString_ReturnsDecimal() => "1.5".ToDecimal().ShouldBe(1.5m);
    [Fact] public void ToDecimalOrNull_Invalid_ReturnsNull() => "abc".ToDecimalOrNull().ShouldBeNull();

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Convert — ToDate / ToGuid / ToGuidList
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDate_ValidString_ReturnsDateTime()
    {
        var dt = "2024-01-15".ToDate();
        dt.Year.ShouldBe(2024);
        dt.Month.ShouldBe(1);
        dt.Day.ShouldBe(15);
    }

    [Fact] public void ToDateOrNull_Invalid_ReturnsNull() => "not-a-date".ToDateOrNull().ShouldBeNull();

    [Fact]
    public void ToGuid_ValidString_ReturnsGuid()
    {
        var g = "83B0233C-A24F-49FD-8083-1337209EBC9A".ToGuid();
        g.ShouldNotBe(Guid.Empty);
    }

    [Fact] public void ToGuidOrNull_Invalid_ReturnsNull() => "not-a-guid".ToGuidOrNull().ShouldBeNull();

    [Fact]
    public void ToGuidList_CommaSeparated_ReturnsList()
    {
        var list = "83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A".ToGuidList();
        list.Count.ShouldBe(2);
    }

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Convert — ToSnakeCase / ToCamelCase
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void ToSnakeCase_PascalCase_ReturnsSnake() => "HelloWorld".ToSnakeCase().ShouldBe("hello_world");
    [Fact] public void ToCamelCase_PascalCase_ReturnsCamel() => "UserName".ToCamelCase().ShouldBe("userName");

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.DateTime — Description(TimeSpan)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Description_TimeSpan_Days_ReturnsChineseString()
    {
        var span = new TimeSpan(2, 3, 4, 5);
        var result = span.Description();
        result.ShouldContain("天");
        result.ShouldContain("小时");
    }

    [Fact]
    public void Description_TimeSpan_OnlyMilliseconds_ReturnsMillisecondsString()
    {
        var span = TimeSpan.FromMilliseconds(500);
        var result = span.Description();
        result.ShouldContain("毫秒");
    }

    [Fact]
    public void Description_TimeSpan_ZeroTotalMs_ReturnsZeroMilliseconds()
    {
        var span = TimeSpan.Zero;
        var result = span.Description();
        result.ShouldBe("0毫秒");
    }

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Format — FormatInvariant / FormatCurrent
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void FormatInvariant_ReturnsFormattedString()
    {
        var result = "Value is {0}".FormatInvariant(42);
        result.ShouldBe("Value is 42");
    }

    [Fact]
    public void FormatCurrent_ReturnsFormattedString()
    {
        var result = "Hello {0}".FormatCurrent("World");
        result.ShouldBe("Hello World");
    }
}
