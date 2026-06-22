using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Bing.Extensions;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// BingExtensions.Validate (IsEmpty / NotEmpty / IsDefault / IsNull / NotNull /
/// IsZeroOrMinus / IsPercentage / IsZeroOrPercentage / IsBetween / CheckNull)
/// BingExtensions.Numeric (KeepDigits / IsIn)
/// </summary>
public class BingExtensionsValidateAndNumericTests
{
    // ─────────────────────────────────────────────────────────────────
    // IsEmpty — bool / bool?
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsEmpty_Bool_False_ReturnsTrue() => false.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Bool_True_ReturnsFalse() => true.IsEmpty().ShouldBeFalse();
    [Fact] public void IsEmpty_NullableBool_Null_ReturnsTrue() => ((bool?)null).IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_NullableBool_False_ReturnsTrue() => ((bool?)false).IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_NullableBool_True_ReturnsFalse() => ((bool?)true).IsEmpty().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // IsEmpty — int / long / float / double / decimal
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsEmpty_Int_Zero_ReturnsTrue() => 0.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Int_NonZero_ReturnsFalse() => 42.IsEmpty().ShouldBeFalse();
    [Fact] public void IsEmpty_NullableInt_Null_ReturnsTrue() => ((int?)null).IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_NullableInt_Zero_ReturnsTrue() => ((int?)0).IsEmpty().ShouldBeTrue();

    [Fact] public void IsEmpty_Long_Zero_ReturnsTrue() => 0L.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Long_NonZero_ReturnsFalse() => 1L.IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_Float_Zero_ReturnsTrue() => 0f.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Float_NonZero_ReturnsFalse() => 1.5f.IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_Double_Zero_ReturnsTrue() => 0d.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Double_NonZero_ReturnsFalse() => 3.14d.IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_Decimal_Zero_ReturnsTrue() => 0m.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Decimal_NonZero_ReturnsFalse() => 1.5m.IsEmpty().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // IsEmpty — string
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsEmpty_String_Null_ReturnsTrue() => ((string)null).IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_String_Empty_ReturnsTrue() => "".IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_String_Whitespace_ReturnsTrue() => "  ".IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_String_NonEmpty_ReturnsFalse() => "hello".IsEmpty().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // IsEmpty — DateTime / DateTimeOffset / TimeSpan / Guid / StringBuilder
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsEmpty_DateTime_MinValue_ReturnsTrue() => DateTime.MinValue.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_DateTime_Default_ReturnsTrue() => default(DateTime).IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_DateTime_Now_ReturnsFalse() => DateTime.Now.IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_DateTimeOffset_MinValue_ReturnsTrue() => DateTimeOffset.MinValue.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_DateTimeOffset_Now_ReturnsFalse() => DateTimeOffset.Now.IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_TimeSpan_Zero_ReturnsTrue() => TimeSpan.Zero.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_TimeSpan_NonZero_ReturnsFalse() => TimeSpan.FromHours(1).IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_Guid_Empty_ReturnsTrue() => Guid.Empty.IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Guid_NewGuid_ReturnsFalse() => Guid.NewGuid().IsEmpty().ShouldBeFalse();

    [Fact]
    public void IsEmpty_StringBuilder_Empty_ReturnsTrue()
    {
        var sb = new StringBuilder();
        sb.IsEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_StringBuilder_NonEmpty_ReturnsFalse()
    {
        var sb = new StringBuilder("hello");
        sb.IsEmpty().ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // IsEmpty — IEnumerable<T> / IDictionary
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsEmpty_Enumerable_Null_ReturnsTrue() => ((IEnumerable<int>)null).IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Enumerable_Empty_ReturnsTrue() => new List<int>().IsEmpty().ShouldBeTrue();
    [Fact] public void IsEmpty_Enumerable_HasItems_ReturnsFalse() => new[] { 1, 2 }.IsEmpty().ShouldBeFalse();

    [Fact] public void IsEmpty_GenericDict_Null_ReturnsTrue()
    {
        IDictionary<string, int>? d = null;
        BingExtensions.IsEmpty<string, int>(d).ShouldBeTrue();
    }
    [Fact] public void IsEmpty_GenericDict_Empty_ReturnsTrue()
    {
        var d = new Dictionary<string, int>();
        BingExtensions.IsEmpty<string, int>(d).ShouldBeTrue();
    }
    [Fact]
    public void IsEmpty_GenericDict_HasEntries_ReturnsFalse()
    {
        var d = new Dictionary<string, int> { ["a"] = 1 };
        BingExtensions.IsEmpty<string, int>(d).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // NotEmpty
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void NotEmpty_String_NonEmpty_ReturnsTrue() => "hello".NotEmpty().ShouldBeTrue();
    [Fact] public void NotEmpty_String_Empty_ReturnsFalse() => "".NotEmpty().ShouldBeFalse();
    [Fact] public void NotEmpty_Guid_NonEmpty_ReturnsTrue() => Guid.NewGuid().NotEmpty().ShouldBeTrue();
    [Fact] public void NotEmpty_Guid_Empty_ReturnsFalse() => Guid.Empty.NotEmpty().ShouldBeFalse();
    [Fact] public void NotEmpty_Enumerable_HasItems_ReturnsTrue() => new[] { 1 }.NotEmpty().ShouldBeTrue();
    [Fact] public void NotEmpty_Enumerable_Empty_ReturnsFalse() => new int[0].NotEmpty().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // IsDefault
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsDefault_Int_Zero_ReturnsTrue() => 0.IsDefault().ShouldBeTrue();
    [Fact] public void IsDefault_Int_NonZero_ReturnsFalse() => 5.IsDefault().ShouldBeFalse();
    [Fact] public void IsDefault_Object_Null_ReturnsTrue() => ((object)null).IsDefault().ShouldBeTrue();

    // ─────────────────────────────────────────────────────────────────
    // IsNull / NotNull
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsNull_Null_ReturnsTrue() => ((object)null).IsNull().ShouldBeTrue();
    [Fact] public void IsNull_NonNull_ReturnsFalse() => new object().IsNull().ShouldBeFalse();
    [Fact] public void NotNull_NonNull_ReturnsTrue() => new object().NotNull().ShouldBeTrue();
    [Fact] public void NotNull_Null_ReturnsFalse() => ((object)null).NotNull().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // IsZeroOrMinus
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsZeroOrMinus_Int_Zero_ReturnsTrue() => 0.IsZeroOrMinus().ShouldBeTrue();
    [Fact] public void IsZeroOrMinus_Int_Negative_ReturnsTrue() => (-1).IsZeroOrMinus().ShouldBeTrue();
    [Fact] public void IsZeroOrMinus_Int_Positive_ReturnsFalse() => 1.IsZeroOrMinus().ShouldBeFalse();
    [Fact] public void IsZeroOrMinus_Long_Zero_ReturnsTrue() => 0L.IsZeroOrMinus().ShouldBeTrue();
    [Fact] public void IsZeroOrMinus_Double_Negative_ReturnsTrue() => (-0.5d).IsZeroOrMinus().ShouldBeTrue();

    // ─────────────────────────────────────────────────────────────────
    // IsPercentage / IsZeroOrPercentage
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsPercentage_Float_ValidRange_ReturnsTrue() => 0.5f.IsPercentage().ShouldBeTrue();
    [Fact] public void IsPercentage_Float_One_ReturnsTrue() => 1f.IsPercentage().ShouldBeTrue();
    [Fact] public void IsPercentage_Float_Zero_ReturnsFalse() => 0f.IsPercentage().ShouldBeFalse();
    [Fact] public void IsPercentage_Float_OverOne_ReturnsFalse() => 1.1f.IsPercentage().ShouldBeFalse();

    [Fact] public void IsZeroOrPercentage_Float_Zero_ReturnsTrue() => 0f.IsZeroOrPercentage().ShouldBeTrue();
    [Fact] public void IsZeroOrPercentage_Float_Half_ReturnsTrue() => 0.5f.IsZeroOrPercentage().ShouldBeTrue();
    [Fact] public void IsZeroOrPercentage_Float_Negative_ReturnsFalse() => (-0.1f).IsZeroOrPercentage().ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // IsBetween
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsBetween_Int_InRange_ReturnsTrue() => 5.IsBetween(1, 10).ShouldBeTrue();
    [Fact] public void IsBetween_Int_AtMin_ReturnsTrue() => 1.IsBetween(1, 10).ShouldBeTrue();
    [Fact] public void IsBetween_Int_AtMax_ReturnsTrue() => 10.IsBetween(1, 10).ShouldBeTrue();
    [Fact] public void IsBetween_Int_Below_ReturnsFalse() => 0.IsBetween(1, 10).ShouldBeFalse();
    [Fact] public void IsBetween_Int_Above_ReturnsFalse() => 11.IsBetween(1, 10).ShouldBeFalse();

    // ─────────────────────────────────────────────────────────────────
    // CheckNull
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CheckNull_NonNull_DoesNotThrow()
    {
        var obj = new object();
        Should.NotThrow(() => obj.CheckNull("obj"));
    }

    [Fact]
    public void CheckNull_Null_ThrowsArgumentNullException()
    {
        object? obj = null;
        Should.Throw<ArgumentNullException>(() => obj.CheckNull("obj"));
    }

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Numeric — KeepDigits
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void KeepDigits_Float_RoundsAwayFromZero() => 1.255f.KeepDigits(2).ShouldBe(1.26f);
    [Fact] public void KeepDigits_Double_RoundsAwayFromZero() => 2.555d.KeepDigits(2).ShouldBe(2.56d);
    [Fact] public void KeepDigits_Decimal_RoundsAwayFromZero() => 3.455m.KeepDigits(2).ShouldBe(3.46m);

    // ─────────────────────────────────────────────────────────────────
    // BingExtensions.Numeric — IsIn (numeric range, not DateTime.In)
    // ─────────────────────────────────────────────────────────────────

    [Fact] public void IsIn_Byte_InRange_ReturnsTrue() => ((byte)5).IsIn(1, 10).ShouldBeTrue();
    [Fact] public void IsIn_Byte_OutOfRange_ReturnsFalse() => ((byte)15).IsIn(1, 10).ShouldBeFalse();
    [Fact] public void IsIn_Int_InRange_ReturnsTrue() => 5.IsIn(1, 10).ShouldBeTrue();
    [Fact] public void IsIn_Int_AtBoundary_ReturnsTrue() => 10.IsIn(1, 10).ShouldBeTrue();
    [Fact] public void IsIn_Int_MinGtMax_ThrowsArgumentOutOfRangeException()
        => Should.Throw<ArgumentOutOfRangeException>(() => 5.IsIn(10, 1));
    [Fact] public void IsIn_Long_InRange_ReturnsTrue() => 5L.IsIn(1L, 10L).ShouldBeTrue();
    [Fact] public void IsIn_Double_InRange_ReturnsTrue() => 1.5d.IsIn(1d, 2d).ShouldBeTrue();
    [Fact] public void IsIn_Decimal_OutOfRange_ReturnsFalse() => 0.5m.IsIn(1m, 10m).ShouldBeFalse();
}
