using System.ComponentModel;

namespace Bing.Helpers;

/// <summary>
/// 测试类：`Common` 与 `Conv` 组合契约测试
/// </summary>
[Trait("Bing.Helpers", "CommonAndConv.Contract")]
public class CommonAndConvContractTest
{
    /// <summary>
    /// 测试用例：`JoinPath` 不应修改输入数组内容（顺序与元素保持不变）
    /// </summary>
    [Theory]
    [MemberData(nameof(GetJoinPathInputCases))]
    public void JoinPath_DoesNotMutateInputArray(string[] parts)
    {
        var snapshot = parts.ToArray();

        var result = Common.JoinPath(parts);

        result.ShouldNotBeNullOrWhiteSpace();
        parts.ShouldBe(snapshot);
    }

    /// <summary>
    /// 测试用例：`ToDictionary(null)` 在不同 `useDisplayName` 配置下都应返回空字典
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToDictionary_NullInput_ReturnsEmptyDictionary(bool useDisplayName)
    {
        var result = Conv.ToDictionary(null, useDisplayName);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    /// <summary>
    /// 测试用例：`ToDictionary(object)` 转换后不应修改源对象状态，且 `useDisplayName` 仅影响键名
    /// </summary>
    [Theory]
    [InlineData(false, nameof(ConvObjectSample.Name), nameof(ConvObjectSample.Count))]
    [InlineData(true, "名称", "数量")]
    public void ToDictionary_ObjectInput_DoesNotMutateSourceObject(
        bool useDisplayName,
        string expectedNameKey,
        string expectedCountKey)
    {
        var source = new ConvObjectSample
        {
            Name = "alpha",
            Count = 2,
            Items = new List<string> { "x", "y" }
        };
        var beforeName = source.Name;
        var beforeCount = source.Count;
        var beforeItems = source.Items.ToArray();

        var dict = Conv.ToDictionary(source, useDisplayName);

        dict[expectedNameKey].ShouldBe("alpha");
        dict[expectedCountKey].ShouldBe(2);
        source.Name.ShouldBe(beforeName);
        source.Count.ShouldBe(beforeCount);
        source.Items.ShouldBe(beforeItems);
    }

    /// <summary>
    /// 测试用例：`ToDictionary(IEnumerable<KeyValuePair<,>>)` 返回的新字典应与源集合解耦
    /// </summary>
    [Fact]
    public void ToDictionary_KeyValueEnumerableInput_ReturnedDictionaryIsDetachedFromSource()
    {
        var source = new Dictionary<string, object>
        {
            ["Code"] = "A1",
            ["Count"] = 2
        };

        var result = Conv.ToDictionary(source);

        source["Code"] = "B2";
        source["NewField"] = "X";

        result["Code"].ShouldBe("A1");
        result.ContainsKey("NewField").ShouldBeFalse();
    }

    /// <summary>
    /// 测试数据：`JoinPath` 的输入数组样例（均用于验证不变性契约）
    /// </summary>
    public static IEnumerable<object[]> GetJoinPathInputCases()
    {
        yield return new object[] { new[] { "api", "v1", "users" } };
        yield return new object[] { new[] { "/api/", "/v1/", "users/" } };
        yield return new object[] { new[] { "root", "child-item", "leaf.txt" } };
    }

    private sealed class ConvObjectSample
    {
        [Description("名称")]
        public string Name { get; set; }

        [DisplayName("数量")]
        public int Count { get; set; }

        public List<string> Items { get; set; }
    }
}
