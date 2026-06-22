using Bing.Date;
using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="IntExtensions"/> / <see cref="LongExtensions"/> 单元测试
/// </summary>
public class IntAndLongExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — Times
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_Times_Action_ExecutesNTimes()
    {
        var count = 0;
        5.Times(() => count++);
        count.ShouldBe(5);
    }

    [Fact]
    public void Int_Times_ActionWithIndex_PassesCorrectIndices()
    {
        var list = new List<int>();
        3.Times((i) => list.Add(i));
        list.ShouldBe(new[] { 0, 1, 2 });
    }

    [Fact]
    public void Int_Times_Zero_NeverExecutes()
    {
        var count = 0;
        0.Times(() => count++);
        count.ShouldBe(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — IsEven / IsOdd
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(-4, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    public void Int_IsEven(int value, bool expected) =>
        value.IsEven().ShouldBe(expected);

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(-3, true)]
    [InlineData(0, false)]
    [InlineData(4, false)]
    public void Int_IsOdd(int value, bool expected) =>
        value.IsOdd().ShouldBe(expected);

    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — InRange
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_InRange_InsideRange_ReturnsTrue() =>
        5.InRange(1, 10).ShouldBeTrue();

    [Fact]
    public void Int_InRange_OutsideRange_ReturnsFalse() =>
        15.InRange(1, 10).ShouldBeFalse();

    [Fact]
    public void Int_InRange_WithDefault_InsideRange_ReturnsValue() =>
        5.InRange(1, 10, 99).ShouldBe(5);

    [Fact]
    public void Int_InRange_WithDefault_OutsideRange_ReturnsDefault() =>
        15.InRange(1, 10, 99).ShouldBe(99);

    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — IsPrime
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(7, true)]
    [InlineData(17, true)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(9, false)]
    public void Int_IsPrime(int value, bool expected) =>
        value.IsPrime().ShouldBe(expected);

    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — ToOrdinal
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    public void Int_ToOrdinal(int value, string expected) =>
        value.ToOrdinal().ShouldBe(expected);

    [Fact]
    public void Int_ToOrdinal_WithFormat_WrapsCorrectly() =>
        1.ToOrdinal("Position: {0}").ShouldBe("Position: 1st");

    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — AsLong / GetArrayIndex / IsIndexInArray
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_AsLong_ReturnsLong() =>
        5.AsLong().ShouldBeOfType<long>().ShouldBe(5L);

    [Fact]
    public void Int_GetArrayIndex_Zero_ReturnsZero() =>
        0.GetArrayIndex().ShouldBe(0);

    [Fact]
    public void Int_GetArrayIndex_NonZero_ReturnsMinusOne() =>
        3.GetArrayIndex().ShouldBe(2);

    [Fact]
    public void Int_IsIndexInArray_Valid_ReturnsTrue()
    {
        var arr = new[] { 10, 20, 30 };
        1.IsIndexInArray(arr).ShouldBeTrue();
        3.IsIndexInArray(arr).ShouldBeTrue();
    }

    [Fact]
    public void Int_IsIndexInArray_OutOfBounds_ReturnsFalse()
    {
        var arr = new[] { 10, 20, 30 };
        4.IsIndexInArray(arr).ShouldBeFalse();
    }

    // ─────────────────────────────────────────────────────────────────
    // IntExtensions — TimeSpan factories
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_Days_ReturnsCorrectTimeSpan() =>
        IntExtensions.Days(3).ShouldBe(TimeSpan.FromDays(3));

    [Fact]
    public void Int_Hours_ReturnsCorrectTimeSpan() =>
        IntExtensions.Hours(2).ShouldBe(TimeSpan.FromHours(2));

    [Fact]
    public void Int_Minutes_ReturnsCorrectTimeSpan() =>
        IntExtensions.Minutes(30).ShouldBe(TimeSpan.FromMinutes(30));

    [Fact]
    public void Int_Seconds_ReturnsCorrectTimeSpan() =>
        IntExtensions.Seconds(45).ShouldBe(TimeSpan.FromSeconds(45));

    [Fact]
    public void Int_Milliseconds_ReturnsCorrectTimeSpan() =>
        IntExtensions.Milliseconds(500).ShouldBe(TimeSpan.FromMilliseconds(500));

    [Fact]
    public void Int_Ticks_ReturnsCorrectTimeSpan() =>
        IntExtensions.Ticks(1000).ShouldBe(TimeSpan.FromTicks(1000));

    // ─────────────────────────────────────────────────────────────────
    // LongExtensions — Times
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Long_Times_Action_ExecutesNTimes()
    {
        var count = 0;
        4L.Times(() => count++);
        count.ShouldBe(4);
    }

    [Fact]
    public void Long_Times_ActionWithIndex_PassesCorrectIndices()
    {
        var list = new List<long>();
        3L.Times((i) => list.Add(i));
        list.ShouldBe(new[] { 0L, 1L, 2L });
    }

    // ─────────────────────────────────────────────────────────────────
    // LongExtensions — IsEven / IsOdd / IsPrime / InRange
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Long_IsEven_Even_ReturnsTrue() => 8L.IsEven().ShouldBeTrue();

    [Fact]
    public void Long_IsOdd_Odd_ReturnsTrue() => 7L.IsOdd().ShouldBeTrue();

    [Fact]
    public void Long_IsPrime_Prime_ReturnsTrue() => 13L.IsPrime().ShouldBeTrue();

    [Fact]
    public void Long_IsPrime_NotPrime_ReturnsFalse() => 9L.IsPrime().ShouldBeFalse();

    [Fact]
    public void Long_InRange_InRange_ReturnsTrue() =>
        5L.InRange(1L, 10L).ShouldBeTrue();

    [Fact]
    public void Long_InRange_OutOfRange_ReturnsFalse() =>
        15L.InRange(1L, 10L).ShouldBeFalse();

    [Fact]
    public void Long_InRange_WithDefault_ReturnsDefault() =>
        15L.InRange(1L, 10L, 99L).ShouldBe(99L);

    // ─────────────────────────────────────────────────────────────────
    // LongExtensions — ToOrdinal
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1L, "1st")]
    [InlineData(2L, "2nd")]
    [InlineData(3L, "3rd")]
    [InlineData(4L, "4th")]
    [InlineData(11L, "11th")]
    [InlineData(21L, "21st")]
    public void Long_ToOrdinal(long value, string expected) =>
        value.ToOrdinal().ShouldBe(expected);

    // ─────────────────────────────────────────────────────────────────
    // LongExtensions — TimeSpan factories
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Long_Days_ReturnsCorrectTimeSpan() =>
        1L.Days().ShouldBe(TimeSpan.FromDays(1));

    [Fact]
    public void Long_Hours_ReturnsCorrectTimeSpan() =>
        6L.Hours().ShouldBe(TimeSpan.FromHours(6));

    [Fact]
    public void Long_Ticks_ReturnsCorrectTimeSpan() =>
        LongExtensions.Ticks(9999L).ShouldBe(TimeSpan.FromTicks(9999));

    // ─────────────────────────────────────────────────────────────────
    // LongExtensions — ToDateTime (Unix timestamp → DateTime)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Long_ToDateTime_Zero_ReturnsEpochPlusUTCPlus8()
    {
        // 0 unix timestamp → 1970-01-01 08:00:00 (UTC+8 built-in)
        var dt = 0L.ToDateTime();
        dt.Year.ShouldBe(1970);
        dt.Month.ShouldBe(1);
        dt.Day.ShouldBe(1);
        dt.Hour.ShouldBe(8);
    }

    [Fact]
    public void Long_ToDateTime_KnownTimestamp_ReturnsCorrectDate()
    {
        // 1609459200 = 2021-01-01 00:00:00 UTC => 2021-01-01 08:00:00 (UTC+8)
        var dt = 1609459200L.ToDateTime();
        dt.Year.ShouldBe(2021);
        dt.Month.ShouldBe(1);
        dt.Day.ShouldBe(1);
        dt.Hour.ShouldBe(8);
    }
}
