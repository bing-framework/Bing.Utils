namespace Bing.Collections;
/// <summary>
/// 投影键相等比较器的去重和哈希一致性测试。
/// </summary>
[Trait("Bing.Collections", "EqualityHelper")]
public class EqualityHelperTest
{
    /// <summary>
    /// 验证按投影标识符去重。
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
    /// 验证投影键比较遵循指定比较器且相等值哈希一致。
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
    /// <summary>
    /// 投影键相等比较的测试模型。
    /// </summary>
    private class EqualityItem
    {
        /// <summary>
        /// 获取或设置标识符。
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 获取或设置名称。
        /// </summary>
        public string Name { get; set; }
    }
}

