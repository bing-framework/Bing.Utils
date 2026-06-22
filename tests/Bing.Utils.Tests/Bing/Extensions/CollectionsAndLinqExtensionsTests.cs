using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bing.Extensions;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// 测试 <see cref="ArrayExtensions"/>
/// (CombineArray / BlockCopy / RandomGet)
/// </summary>
public class ArrayExtensionsTests
{
    // ──────────────────────────────────────────
    //  CombineArray
    // ──────────────────────────────────────────

    [Fact]
    public void CombineArray_TwoNonEmptyArrays_MergesInOrder()
    {
        int[] a = { 1, 2, 3 };
        int[] b = { 4, 5, 6 };
        var result = a.CombineArray(b);
        result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
    }

    [Fact]
    public void CombineArray_SecondArrayNull_ReturnsFirst()
    {
        int[] a = { 1, 2 };
        var result = a.CombineArray(null);
        result.ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public void CombineArray_FirstArrayNull_ReturnsNull()
    {
        int[] a = null;
        var result = a.CombineArray(new[] { 1, 2 });
        result.ShouldBeNull();
    }

    [Fact]
    public void CombineArray_BothEmpty_ReturnsEmpty()
    {
        int[] a = Array.Empty<int>();
        var result = a.CombineArray(Array.Empty<int>());
        result.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────
    //  BlockCopy (index, length)
    // ──────────────────────────────────────────

    [Fact]
    public void BlockCopy_NormalSlice_ReturnsSlice()
    {
        int[] src = { 1, 2, 3, 4, 5 };
        src.BlockCopy(1, 3).ShouldBe(new[] { 2, 3, 4 });
    }

    [Fact]
    public void BlockCopy_BeyondEnd_NoPad_ReturnsTruncated()
    {
        int[] src = { 1, 2, 3 };
        src.BlockCopy(2, 5).ShouldBe(new[] { 3 });
    }

    [Fact]
    public void BlockCopy_BeyondEnd_WithPad_PadsWithDefault()
    {
        int[] src = { 1, 2, 3 };
        var result = src.BlockCopy(2, 5, padToLength: true);
        result.Length.ShouldBe(5);
        result[0].ShouldBe(3);
        result[1].ShouldBe(0); // padded
        result[4].ShouldBe(0); // padded
    }

    [Fact]
    public void BlockCopy_NullSource_ThrowsNullReferenceException()
    {
        int[] src = null;
        Should.Throw<NullReferenceException>(() => src.BlockCopy(0, 2));
    }

    // ──────────────────────────────────────────
    //  BlockCopy (chunked enumerator)
    // ──────────────────────────────────────────

    [Fact]
    public void BlockCopy_Chunked_SplitsIntoChunks()
    {
        int[] src = { 1, 2, 3, 4, 5, 6 };
        var chunks = src.BlockCopy(2, padToLength: false).ToList();
        chunks.Count.ShouldBe(3);
        chunks[0].ShouldBe(new[] { 1, 2 });
        chunks[1].ShouldBe(new[] { 3, 4 });
        chunks[2].ShouldBe(new[] { 5, 6 });
    }

    [Fact]
    public void BlockCopy_Chunked_WithPad_LastChunkPadded()
    {
        int[] src = { 1, 2, 3 };
        var chunks = src.BlockCopy(2, padToLength: true).ToList();
        chunks.Count.ShouldBe(2);
        chunks[1].Length.ShouldBe(2);
        chunks[1][0].ShouldBe(3);
        chunks[1][1].ShouldBe(0); // padded
    }

    // ──────────────────────────────────────────
    //  RandomGet
    // ──────────────────────────────────────────

    [Fact]
    public void RandomGet_NonEmptyArray_ReturnsElementFromArray()
    {
        int[] src = { 10, 20, 30 };
        var result = src.RandomGet();
        src.ShouldContain(result);
    }

    [Fact]
    public void RandomGet_EmptyArray_ReturnsDefault()
    {
        int[] src = Array.Empty<int>();
        src.RandomGet().ShouldBe(0);
    }

    [Fact]
    public void RandomGet_NullArray_ReturnsDefault()
    {
        int[] src = null;
        src.RandomGet().ShouldBe(0);
    }

    [Fact]
    public void RandomGet_SingleElement_ReturnsThatElement()
    {
        int[] src = { 42 };
        src.RandomGet().ShouldBe(42);
    }
}

/// <summary>
/// 测试 <see cref="EnumerableExtensions"/>
/// (EqualsTo / ExpandAndToString / WhereIf)
/// </summary>
public class EnumerableExtensionsTests
{
    // ──────────────────────────────────────────
    //  EqualsTo
    // ──────────────────────────────────────────

    [Fact]
    public void EqualsTo_IdenticalLists_ReturnsTrue()
    {
        new[] { 1, 2, 3 }.EqualsTo(new[] { 1, 2, 3 }).ShouldBeTrue();
    }

    [Fact]
    public void EqualsTo_DifferentOrder_StillReturnsTrueForSets()
    {
        // Except() is order-independent, so [1,3,2] equals [1,2,3] by set comparison
        new[] { 1, 3, 2 }.EqualsTo(new[] { 1, 2, 3 }).ShouldBeTrue();
    }

    [Fact]
    public void EqualsTo_DifferentElements_ReturnsFalse()
    {
        new[] { 1, 2, 3 }.EqualsTo(new[] { 1, 2, 4 }).ShouldBeFalse();
    }

    [Fact]
    public void EqualsTo_DifferentLengths_ReturnsFalse()
    {
        new[] { 1, 2 }.EqualsTo(new[] { 1, 2, 3 }).ShouldBeFalse();
    }

    [Fact]
    public void EqualsTo_BothEmpty_ReturnsTrue()
    {
        Array.Empty<int>().EqualsTo(Array.Empty<int>()).ShouldBeTrue();
    }

    [Fact]
    public void EqualsTo_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> src = null;
        Should.Throw<ArgumentNullException>(() => src.EqualsTo(new[] { 1 }));
    }

    [Fact]
    public void EqualsTo_NullTarget_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new[] { 1 }.EqualsTo(null));
    }

    // ──────────────────────────────────────────
    //  ExpandAndToString
    // ──────────────────────────────────────────

    [Fact]
    public void ExpandAndToString_DefaultSeparator_CommaJoined()
    {
        new[] { 1, 2, 3 }.ExpandAndToString().ShouldBe("1,2,3");
    }

    [Fact]
    public void ExpandAndToString_CustomSeparator_UsesCustom()
    {
        new[] { "a", "b", "c" }.ExpandAndToString("|").ShouldBe("a|b|c");
    }

    [Fact]
    public void ExpandAndToString_WithWrapItem_WrapsEachItem()
    {
        new[] { "a", "b" }.ExpandAndToString(",", "'").ShouldBe("'a','b'");
    }

    [Fact]
    public void ExpandAndToString_WithCustomFunc_AppliesFunc()
    {
        new[] { 1, 2, 3 }.ExpandAndToString(x => $"[{x}]").ShouldBe("[1],[2],[3]");
    }

    [Fact]
    public void ExpandAndToString_EmptyCollection_ReturnsNull()
    {
        Array.Empty<int>().ExpandAndToString().ShouldBeNull();
    }

    [Fact]
    public void ExpandAndToString_NullCollection_ThrowsArgumentNullException()
    {
        IEnumerable<int> col = null;
        Should.Throw<ArgumentNullException>(() => col.ExpandAndToString());
    }

    // ──────────────────────────────────────────
    //  WhereIf
    // ──────────────────────────────────────────

    [Fact]
    public void WhereIf_ConditionTrue_AppliesFilter()
    {
        var result = new[] { 1, 2, 3, 4 }.WhereIf(x => x > 2, condition: true).ToList();
        result.ShouldBe(new[] { 3, 4 });
    }

    [Fact]
    public void WhereIf_ConditionFalse_ReturnsAll()
    {
        var result = new[] { 1, 2, 3, 4 }.WhereIf(x => x > 2, condition: false).ToList();
        result.ShouldBe(new[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void WhereIf_NullSource_ThrowsArgumentNullException()
    {
        IEnumerable<int> src = null;
        Should.Throw<ArgumentNullException>(() => src.WhereIf(x => x > 0, true).ToList());
    }

    [Fact]
    public void WhereIf_IndexedPredicate_ConditionTrue_FiltersCorrectly()
    {
        // (value, index) => index % 2 == 0 keeps indices 0, 2 → values 10, 30
        var result = new[] { 10, 20, 30, 40 }
            .WhereIf((x, i) => i % 2 == 0, condition: true)
            .ToList();
        result.ShouldBe(new[] { 10, 30 });
    }
}

/// <summary>
/// 测试 <see cref="ListExtensions"/>
/// (InsertIfNotExists / IndexOf / Join / EqualsAll / Slice)
/// </summary>
public class ListExtensionsTests
{
    // ──────────────────────────────────────────
    //  InsertIfNotExists (single)
    // ──────────────────────────────────────────

    [Fact]
    public void InsertIfNotExists_NewItem_InsertsAndReturnsTrue()
    {
        var list = new List<int> { 1, 2 };
        list.InsertIfNotExists(0, 99).ShouldBeTrue();
        list[0].ShouldBe(99);
    }

    [Fact]
    public void InsertIfNotExists_ExistingItem_ReturnsFalse()
    {
        var list = new List<int> { 1, 2, 3 };
        list.InsertIfNotExists(0, 2).ShouldBeFalse();
        list.Count.ShouldBe(3); // unchanged
    }

    // ──────────────────────────────────────────
    //  InsertIfNotExists (batch)
    // ──────────────────────────────────────────

    [Fact]
    public void InsertIfNotExists_Batch_InsertsOnlyNew()
    {
        var list = new List<int> { 1, 2 };
        var inserted = list.InsertIfNotExists(0, new[] { 2, 3, 4 });
        inserted.ShouldBe(2); // 3 and 4 inserted; 2 already exists
        list.ShouldContain(3);
        list.ShouldContain(4);
        list.Count(x => x == 2).ShouldBe(1); // no duplicate
    }

    // ──────────────────────────────────────────
    //  IndexOf (predicate)
    // ──────────────────────────────────────────

    [Fact]
    public void IndexOf_MatchFound_ReturnsCorrectIndex()
    {
        var list = new List<int> { 10, 20, 30 };
        list.IndexOf(x => x == 20).ShouldBe(1);
    }

    [Fact]
    public void IndexOf_NoMatch_ReturnsMinusOne()
    {
        var list = new List<int> { 10, 20, 30 };
        list.IndexOf(x => x == 99).ShouldBe(-1);
    }

    // ──────────────────────────────────────────
    //  Join (char / string)
    // ──────────────────────────────────────────

    [Fact]
    public void Join_CharSeparator_JoinsCorrectly()
    {
        var list = new List<int> { 1, 2, 3 };
        list.Join(',').ShouldBe("1,2,3");
    }

    [Fact]
    public void Join_StringSeparator_JoinsCorrectly()
    {
        var list = new List<string> { "a", "b", "c" };
        list.Join(" | ").ShouldBe("a | b | c");
    }

    [Fact]
    public void Join_EmptyList_ReturnsEmpty()
    {
        var list = new List<int>();
        list.Join(',').ShouldBe(string.Empty);
    }

    [Fact]
    public void Join_SingleElement_ReturnsElement()
    {
        new List<int> { 42 }.Join(',').ShouldBe("42");
    }

    // ──────────────────────────────────────────
    //  EqualsAll
    // ──────────────────────────────────────────

    [Fact]
    public void EqualsAll_SameLists_ReturnsTrue()
    {
        IList<int> a = new[] { 1, 2, 3 };
        IList<int> b = new[] { 1, 2, 3 };
        a.EqualsAll(b).ShouldBeTrue();
    }

    [Fact]
    public void EqualsAll_DifferentOrder_ReturnsFalse()
    {
        IList<int> a = new[] { 1, 2, 3 };
        IList<int> b = new[] { 1, 3, 2 };
        a.EqualsAll(b).ShouldBeFalse();
    }

    [Fact]
    public void EqualsAll_DifferentLengths_ReturnsFalse()
    {
        IList<int> a = new[] { 1, 2 };
        IList<int> b = new[] { 1, 2, 3 };
        a.EqualsAll(b).ShouldBeFalse();
    }

    [Fact]
    public void EqualsAll_BothNull_ReturnsTrue()
    {
        IList<int> a = null;
        IList<int> b = null;
        a.EqualsAll(b).ShouldBeTrue();
    }

    [Fact]
    public void EqualsAll_OneNull_ReturnsFalse()
    {
        IList<int> a = new[] { 1 };
        a.EqualsAll(null).ShouldBeFalse();
    }

    // ──────────────────────────────────────────
    //  Slice
    // ──────────────────────────────────────────

    [Fact]
    public void Slice_NormalRange_ReturnsSubList()
    {
        IList<int> list = new[] { 0, 1, 2, 3, 4 };
        list.Slice(1, 4).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Slice_NegativeStart_ClampsFromEnd()
    {
        IList<int> list = new[] { 0, 1, 2, 3, 4 };
        // -2 → 5-2=3
        list.Slice(-2, 4).ShouldBe(new[] { 3 });
    }

    [Fact]
    public void Slice_NullBounds_ReturnsAll()
    {
        IList<int> list = new[] { 1, 2, 3, 4 };
        // start=0, end=Count=4 → indices 0,1,2,3 (end is exclusive → last index checked < Count)
        // Slice(null,null) → start=0, end=Count=4 → endIndex=Min(4, Count-1)=3 → [1,2,3]
        var result = list.Slice(null, null).ToList();
        result.ShouldBe(new[] { 1, 2, 3 }); // index 0..2 (end = Min(4,3)=3, exclusive)
    }

    [Fact]
    public void Slice_WithStep_SkipsElements()
    {
        IList<int> list = new[] { 0, 1, 2, 3, 4, 5 };
        list.Slice(0, 6, 2).ShouldBe(new[] { 0, 2, 4 });
    }

    [Fact]
    public void Slice_StepZero_ThrowsArgumentException()
    {
        IList<int> list = new[] { 1, 2, 3 };
        Should.Throw<ArgumentException>(() => list.Slice(0, 3, 0).ToList());
    }

    [Fact]
    public void Slice_NullList_ThrowsArgumentNullException()
    {
        IList<int> list = null;
        Should.Throw<ArgumentNullException>(() => list.Slice(0, 1));
    }
}

/// <summary>
/// 测试 <see cref="LambdaExpressionExtensions"/>
/// (ExtractPropertyInfo / ExtractFieldInfo / ExtractMemberInfo)
/// </summary>
public class LambdaExpressionExtensionsTests
{
    private class Sample
    {
        public string Name { get; set; }
        public int Value { get; set; }
#pragma warning disable CS0649
        public double Amount;
#pragma warning restore CS0649
    }

    // ──────────────────────────────────────────
    //  ExtractPropertyInfo
    // ──────────────────────────────────────────

    [Fact]
    public void ExtractPropertyInfo_StringProperty_ReturnsCorrectPropertyInfo()
    {
        Expression<Func<Sample, string>> expr = s => s.Name;
        var pi = expr.ExtractPropertyInfo();
        pi.ShouldNotBeNull();
        pi!.Name.ShouldBe("Name");
        pi.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void ExtractPropertyInfo_IntProperty_ReturnsCorrectPropertyInfo()
    {
        Expression<Func<Sample, int>> expr = s => s.Value;
        var pi = expr.ExtractPropertyInfo();
        pi!.Name.ShouldBe("Value");
    }

    // ──────────────────────────────────────────
    //  ExtractFieldInfo
    // ──────────────────────────────────────────

    [Fact]
    public void ExtractFieldInfo_PublicField_ReturnsFieldInfo()
    {
        Expression<Func<Sample, double>> expr = s => s.Amount;
        var fi = expr.ExtractFieldInfo();
        fi.ShouldNotBeNull();
        fi!.Name.ShouldBe("Amount");
        fi.FieldType.ShouldBe(typeof(double));
    }

    [Fact]
    public void ExtractFieldInfo_Property_ReturnsNull()
    {
        // Property expression → ExtractFieldInfo should return null
        Expression<Func<Sample, string>> expr = s => s.Name;
        var fi = expr.ExtractFieldInfo();
        fi.ShouldBeNull();
    }

    // ──────────────────────────────────────────
    //  ExtractMemberInfo
    // ──────────────────────────────────────────

    [Fact]
    public void ExtractMemberInfo_Property_ReturnsMemberInfo()
    {
        Expression<Func<Sample, string>> expr = s => s.Name;
        var mi = expr.ExtractMemberInfo();
        mi.ShouldNotBeNull();
        mi!.Name.ShouldBe("Name");
    }

    [Fact]
    public void ExtractMemberInfo_Field_ReturnsMemberInfo()
    {
        Expression<Func<Sample, double>> expr = s => s.Amount;
        var mi = expr.ExtractMemberInfo();
        mi!.Name.ShouldBe("Amount");
    }
}
