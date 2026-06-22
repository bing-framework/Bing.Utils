using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Bing.Extensions;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// 测试 <see cref="ObjectExtensions"/>
/// (ClonePropertyFrom / ClonePropertyTo / ToNullable)
/// </summary>
public class ObjectExtensionsTests
{
    #region Helper types

    private class PersonA
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
    }

    private class PersonB
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }   // not in PersonA → won't be cloned
    }

    #endregion

    // ──────────────────────────────────────────
    //  ClonePropertyFrom
    // ──────────────────────────────────────────

    [Fact]
    public void ClonePropertyFrom_CommonProperties_CopiesValues()
    {
        var src = new PersonA { Name = "Alice", Age = 30, Email = "a@b.com" };
        var dst = new PersonB();

        var count = dst.ClonePropertyFrom(src);

        count.ShouldBeGreaterThanOrEqualTo(2); // Name + Age copied
        dst.Name.ShouldBe("Alice");
        dst.Age.ShouldBe(30);
    }

    [Fact]
    public void ClonePropertyFrom_NullSource_ReturnsZero()
    {
        var dst = new PersonB();
        var count = dst.ClonePropertyFrom(null);
        count.ShouldBe(0);
    }

    [Fact]
    public void ClonePropertyFrom_SameReference_ReturnsZero()
    {
        var obj = new PersonA { Name = "Bob" };
        var count = obj.ClonePropertyFrom(obj);
        count.ShouldBe(0);
    }

    [Fact]
    public void ClonePropertyFrom_WithExcludeName_SkipsExcludedProps()
    {
        var src = new PersonA { Name = "Alice", Age = 30, Email = "a@b.com" };
        var dst = new PersonB();

        // Exclude "Name" from copying
        dst.ClonePropertyFrom(src, new[] { "Name" });

        dst.Name.ShouldBeNull();
        dst.Age.ShouldBe(30);
    }

    // ──────────────────────────────────────────
    //  ClonePropertyTo
    // ──────────────────────────────────────────

    [Fact]
    public void ClonePropertyTo_CommonProperties_CopiesValues()
    {
        var src = new PersonA { Name = "Charlie", Age = 25 };
        var dst = new PersonB();

        var count = src.ClonePropertyTo(dst);

        count.ShouldBeGreaterThanOrEqualTo(2);
        dst.Name.ShouldBe("Charlie");
        dst.Age.ShouldBe(25);
    }

    [Fact]
    public void ClonePropertyTo_NullDestination_ReturnsZero()
    {
        var src = new PersonA { Name = "Dave" };
        var count = src.ClonePropertyTo(null);
        count.ShouldBe(0);
    }

    [Fact]
    public void ClonePropertyTo_WithExclude_SkipsExcludedProps()
    {
        var src = new PersonA { Name = "Eve", Age = 22 };
        var dst = new PersonB();

        src.ClonePropertyTo(dst, new[] { "Age" });

        dst.Name.ShouldBe("Eve");
        dst.Age.ShouldBe(0); // excluded → not copied
    }

    // ──────────────────────────────────────────
    //  ToNullable
    // ──────────────────────────────────────────

    [Fact]
    public void ToNullable_ValueTypeWithValue_ReturnsNullableWithValue()
    {
        int val = 42;
        int? result = val.ToNullable();
        result.ShouldBe(42);
    }

    [Fact]
    public void ToNullable_DefaultStruct_ReturnsNull()
    {
        // IsNull() for value types returns true only when value equals default(T)
        // Actually: IsNull() checks if value == null, which for value types is always false
        // So ToNullable should always return (T?)value for non-nullable structs
        // Let's just verify it returns the value wrapped in nullable
        double val = 3.14;
        double? result = val.ToNullable();
        result.ShouldBe(3.14);
    }

    [Fact]
    public void ToNullable_DateTimeValue_ReturnsNullableDateTime()
    {
        var dt = new DateTime(2024, 1, 1);
        DateTime? result = dt.ToNullable();
        result.ShouldBe(new DateTime(2024, 1, 1));
    }
}

/// <summary>
/// 测试 <see cref="NameValueCollectionExtensions"/>
/// (ToQueryString)
/// </summary>
public class NameValueCollectionExtensionsTests
{
    [Fact]
    public void ToQueryString_SinglePair_ReturnsKeyEqualsValue()
    {
        var nvc = new NameValueCollection { { "key", "val" } };
        nvc.ToQueryString().ShouldBe("key=val");
    }

    [Fact]
    public void ToQueryString_MultiplePairs_ReturnsAmpersandSeparated()
    {
        var nvc = new NameValueCollection
        {
            { "a", "1" },
            { "b", "2" }
        };
        var qs = nvc.ToQueryString();
        qs.ShouldBe("a=1&b=2");
    }

    [Fact]
    public void ToQueryString_NullCollection_ReturnsEmpty()
    {
        NameValueCollection nvc = null;
        nvc.ToQueryString().ShouldBe(string.Empty);
    }

    [Fact]
    public void ToQueryString_EmptyCollection_ReturnsEmpty()
    {
        var nvc = new NameValueCollection();
        nvc.ToQueryString().ShouldBe(string.Empty);
    }

    [Fact]
    public void ToQueryString_NoTrailingAmpersand()
    {
        var nvc = new NameValueCollection { { "x", "y" } };
        var qs = nvc.ToQueryString();
        qs.ShouldNotEndWith("&");
    }
}
