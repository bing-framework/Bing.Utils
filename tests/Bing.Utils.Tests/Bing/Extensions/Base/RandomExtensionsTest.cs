namespace Bing;

/// <summary>
/// 测试类：覆盖 `RandomExtensions` 相关行为。
/// </summary>
[Trait("ExtensionsUT", "Random")]
public class RandomExtensionsTest
{
    /// <summary>
    /// 测试用例：验证 `NextBool` 在 `ProbabilityBoundary` 场景下，结果为 `ReturnsDeterministicValue`。
    /// </summary>
    [Theory]
    [InlineData(-1d, false)]
    [InlineData(0d, false)]
    [InlineData(1d, true)]
    [InlineData(2d, true)]
    public void NextBool_ProbabilityBoundary_ReturnsDeterministicValue(double probability, bool expected)
    {
        var random = new Random(2024);

        for (var i = 0; i < 32; i++)
            random.NextBool(probability).ShouldBe(expected);
    }

    /// <summary>
    /// 测试用例：验证 `NextEnum` 在 `EnumType` 场景下，结果为 `ReturnsDefinedMember`。
    /// </summary>
    [Fact]
    public void NextEnum_EnumType_ReturnsDefinedMember()
    {
        var random = new Random(123);

        var values = Enumerable.Range(0, 20)
            .Select(_ => random.NextEnum<SampleEnum>())
            .ToArray();

        values.ShouldAllBe(v => Enum.IsDefined(typeof(SampleEnum), v));
    }

    /// <summary>
    /// 测试用例：验证 `NextEnum` 在 `NonEnumType` 场景下，结果为 `ThrowsInvalidOperationException`。
    /// </summary>
    [Fact]
    public void NextEnum_NonEnumType_ThrowsInvalidOperationException()
    {
        var random = new Random(123);

        Should.Throw<InvalidOperationException>(() => random.NextEnum<int>());
    }

    /// <summary>
    /// 测试用例：验证 `NextBytes` 在 `LengthInput` 场景下，结果为 `ReturnsExpectedLength`。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(32)]
    public void NextBytes_LengthInput_ReturnsExpectedLength(int length)
    {
        var random = new Random(2024);

        var bytes = random.NextBytes(length);

        bytes.Length.ShouldBe(length);
    }

    /// <summary>
    /// 测试用例：验证 `NextLong` 在 `RangeInput` 场景下，结果为 `ReturnsValueWithinBounds`。
    /// </summary>
    [Fact]
    public void NextLong_RangeInput_ReturnsValueWithinBounds()
    {
        var random = new Random(42);

        var values = Enumerable.Range(0, 64)
            .Select(_ => random.NextLong(10, 50))
            .ToArray();

        values.ShouldAllBe(v => v >= 10 && v < 50);
    }

    /// <summary>
    /// 测试用例：验证 `NextLong` 在 `MinEqualsMax` 场景下，结果为 `ThrowsDivideByZeroException`。
    /// </summary>
    [Fact]
    public void NextLong_MinEqualsMax_ThrowsDivideByZeroException()
    {
        var random = new Random(42);

        Should.Throw<DivideByZeroException>(() => random.NextLong(100, 100));
    }

    /// <summary>
    /// 测试用例：验证 `NextDateTime` 在 `MinEqualsMax` 场景下，结果为 `ReturnsSameDate`。
    /// </summary>
    [Fact]
    public void NextDateTime_MinEqualsMax_ReturnsSameDate()
    {
        var random = new Random(42);
        var point = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var value = random.NextDateTime(point, point);

        value.ShouldBe(point);
    }

    /// <summary>
    /// 测试用例：验证 `OneOf` 在 `EmptyValues` 场景下，结果为 `ThrowsIndexOutOfRangeException`。
    /// </summary>
    [Fact]
    public void OneOf_EmptyValues_ThrowsIndexOutOfRangeException()
    {
        var random = new Random(42);

        Should.Throw<IndexOutOfRangeException>(() => random.OneOf(Array.Empty<int>()));
    }

    /// <summary>
    /// 测试用例：验证 `NormalDouble` 在 `SameSeed` 场景下，结果为 `ReturnsStableSequence`。
    /// </summary>
    [Fact]
    public void NormalDouble_SameSeed_ReturnsStableSequence()
    {
        var first = new Random(7);
        var second = new Random(7);

        var left = Enumerable.Range(0, 16).Select(_ => first.NormalDouble()).ToArray();
        var right = Enumerable.Range(0, 16).Select(_ => second.NormalDouble()).ToArray();

        left.ShouldBe(right);
    }

    private enum SampleEnum
    {
        A = 1,
        B = 2,
        C = 3
    }
}
