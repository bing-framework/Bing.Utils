using Bing.Collections;

namespace Bing.Utils.Tests.Bing.Collections;

/// <summary>
/// <see cref="BingEnumerableExtensions"/> 随机元素扩展单元测试
/// </summary>
public class RandomElementTest
{
    /// <summary>
    /// 测试目的：数组与列表应使用给定随机数产生可重复且有效的结果。
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void RandomElement_IndexedSourceWithSeededRandom_ReturnsDeterministicElement(int seed)
    {
        // Arrange
        var source = new[] { "A", "B", "C", "D" };

        // Act
        var first = source.RandomElement(new Random(seed));
        var second = source.ToList().RandomElement(new Random(seed));

        // Assert
        first.ShouldBe(second);
        source.ShouldContain(first);
    }

    /// <summary>
    /// 测试目的：普通延迟枚举应仅枚举一次，并返回其中元素。
    /// </summary>
    [Fact]
    public void RandomElement_DeferredEnumerable_EnumeratesOnceAndReturnsSourceElement()
    {
        // Arrange
        var enumerationCount = 0;
        IEnumerable<int> Source()
        {
            enumerationCount++;
            yield return 10;
            yield return 20;
            yield return 30;
        }

        // Act
        var result = Source().RandomElement(new Random(3));

        // Assert
        enumerationCount.ShouldBe(1);
        new[] { 10, 20, 30 }.ShouldContain(result);
    }

    /// <summary>
    /// 测试目的：单元素集合应返回唯一元素。
    /// </summary>
    [Fact]
    public void RandomElement_SingleElement_ReturnsOnlyElement()
    {
        // Arrange
        var source = new[] { 42 };

        // Act
        var result = source.RandomElement(new Random(1));

        // Assert
        result.ShouldBe(42);
    }

    /// <summary>
    /// 测试目的：空集合的强制获取应抛出明确异常。
    /// </summary>
    [Fact]
    public void RandomElement_EmptySource_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = Array.Empty<int>();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => source.RandomElement(new Random(1)));
    }

    /// <summary>
    /// 测试目的：空引用源集合应抛出参数异常。
    /// </summary>
    [Fact]
    public void TryRandomElement_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<int> source = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => source.TryRandomElement(out _, new Random(1))).ParamName.ShouldBe("source");
    }

    /// <summary>
    /// 测试目的：Try 方法应区分空集合与非空集合。
    /// </summary>
    [Fact]
    public void TryRandomElement_EmptyAndNonEmptySources_ReturnsExpectedSuccessState()
    {
        // Arrange
        var empty = Array.Empty<int>();
        var source = new[] { 1, 2, 3 };

        // Act
        var emptySuccess = empty.TryRandomElement(out var emptyResult, new Random(1));
        var success = source.TryRandomElement(out var result, new Random(1));

        // Assert
        emptySuccess.ShouldBeFalse();
        emptyResult.ShouldBe(default);
        success.ShouldBeTrue();
        source.ShouldContain(result);
    }
}