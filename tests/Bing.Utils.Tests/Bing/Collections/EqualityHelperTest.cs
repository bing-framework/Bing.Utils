namespace Bing.Collections;
/// <summary>
/// 测试类：覆盖 `EqualityHelper` 相关行为。
/// </summary>
[Trait("Bing.Collections", "EqualityHelper")]
public class EqualityHelperTest
{
    /// <summary>
    /// 测试用例：验证 `CreateComparer` 在 `KeySelector` 场景下，结果为 `DeduplicatesBySelectedKey`。
    /// </summary>
    [Fact]
    public void CreateComparer_KeySelector_DeduplicatesBySelectedKey()
    {
        var data = new[]
        {
            new EqualityItem { Id = 1, Name = "A" },
            new EqualityItem { Id = 1, Name = "B" },
            new EqualityItem { Id = 2, Name = "C" }
        };
        var comparer = EqualityHelper<EqualityItem>.CreateComparer(x => x.Id);
        var distinct = data.Distinct(comparer).ToList();
        distinct.Count.ShouldBe(2);
        distinct[0].Id.ShouldBe(1);
        distinct[1].Id.ShouldBe(2);
    }
    /// <summary>
    /// 测试用例：验证 `CreateComparer` 在 `WithCustomComparer` 场景下，结果为 `UsesProvidedComparer`。
    /// </summary>
    [Fact]
    public void CreateComparer_WithCustomComparer_UsesProvidedComparer()
    {
        var left = new EqualityItem { Id = 1, Name = "ALPHA" };
        var right = new EqualityItem { Id = 2, Name = "alpha" };
        var comparer = EqualityHelper<EqualityItem>.CreateComparer(x => x.Name, StringComparer.OrdinalIgnoreCase);
        comparer.Equals(left, right).ShouldBeTrue();
        comparer.GetHashCode(left).ShouldBe(comparer.GetHashCode(right));
    }
    private class EqualityItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

