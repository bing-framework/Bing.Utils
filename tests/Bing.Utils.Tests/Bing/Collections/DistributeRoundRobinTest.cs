using Bing.Collections;

namespace Bing.Utils.Tests.Bing.Collections;

/// <summary>
/// <see cref="BingEnumerableExtensions"/> 轮询分配扩展单元测试
/// </summary>
public class DistributeRoundRobinTest
{
    /// <summary>
    /// 测试目的：多容器分配应按容器顺序轮询，并保持源元素相对顺序。
    /// </summary>
    [Fact]
    public void DistributeRoundRobin_MultipleKeys_DistributesInRoundRobinOrder()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5, 6, 7 };
        var keys = new[] { "A", "B", "C" };

        // Act
        var result = source.DistributeRoundRobin(keys);

        // Assert
        result.Keys.ShouldBe(keys);
        result["A"].ShouldBe(new[] { 1, 4, 7 });
        result["B"].ShouldBe(new[] { 2, 5 });
        result["C"].ShouldBe(new[] { 3, 6 });
    }

    /// <summary>
    /// 测试目的：空源集合应保留所有容器及其空列表。
    /// </summary>
    [Fact]
    public void DistributeRoundRobin_EmptySource_ReturnsAllKeysWithEmptyLists()
    {
        // Arrange
        var keys = new[] { "A", "B" };

        // Act
        var result = Array.Empty<int>().DistributeRoundRobin(keys);

        // Assert
        result.Keys.ShouldBe(keys);
        result["A"].ShouldBeEmpty();
        result["B"].ShouldBeEmpty();
    }

    /// <summary>
    /// 测试目的：单容器应接收所有元素。
    /// </summary>
    [Fact]
    public void DistributeRoundRobin_SingleKey_AssignsAllItemsToOnlyBucket()
    {
        // Arrange
        var source = new[] { "a", "b", "c" };

        // Act
        var result = source.DistributeRoundRobin(new[] { 1 });

        // Assert
        result[1].ShouldBe(source);
    }

    /// <summary>
    /// 测试目的：空容器、重复容器和空引用参数应抛出明确异常。
    /// </summary>
    [Fact]
    public void DistributeRoundRobin_InvalidInput_ThrowsExpectedExceptions()
    {
        // Arrange
        var source = new[] { 1 };
        IEnumerable<int> nullSource = null;
        IEnumerable<string> nullKeys = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => nullSource.DistributeRoundRobin(new[] { "A" })).ParamName.ShouldBe("source");
        Should.Throw<ArgumentNullException>(() => source.DistributeRoundRobin(nullKeys)).ParamName.ShouldBe("keys");
        Should.Throw<ArgumentException>(() => source.DistributeRoundRobin(Array.Empty<string>())).ParamName.ShouldBe("keys");
        Should.Throw<ArgumentException>(() => source.DistributeRoundRobin(new[] { "A", "A" })).ParamName.ShouldBe("keys");
    }

    /// <summary>
    /// 测试目的：元素数量差最多为一，且容器键仅枚举一次。
    /// </summary>
    [Fact]
    public void DistributeRoundRobin_OneTimeKeyEnumerable_BalancesBucketCounts()
    {
        // Arrange
        var keyEnumerationCount = 0;
        IEnumerable<string> Keys()
        {
            keyEnumerationCount++;
            yield return "A";
            yield return "B";
            yield return "C";
        }

        // Act
        var result = Enumerable.Range(1, 11).DistributeRoundRobin(Keys());

        // Assert
        keyEnumerationCount.ShouldBe(1);
        var counts = result.Values.Select(list => list.Count).ToArray();
        (counts.Max() - counts.Min()).ShouldBeLessThanOrEqualTo(1);
    }
}