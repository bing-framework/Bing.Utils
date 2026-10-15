using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 可枚举代理的枚举、查询及源数据联动测试。
/// </summary>
[Trait("Collections", "EnumerableProxy")]
public class EnumerableProxyTest
{
    /// <summary>
    /// 验证构造代理时拒绝空序列。
    /// </summary>
    [Fact]
    public void Constructor_NullEnumerable_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new EnumerableProxy<int>(null!));
    }

    /// <summary>
    /// 验证泛型枚举按顺序返回全部源元素。
    /// </summary>
    [Fact]
    public void GetEnumerator_ValidList_ReturnsAllItems()
    {
        var source = new List<int> { 1, 2, 3 };
        var proxy = new EnumerableProxy<int>(source);
        proxy.ToList().ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 验证空源列表的枚举结果为空。
    /// </summary>
    [Fact]
    public void GetEnumerator_EmptyList_ReturnsEmptySequence()
    {
        var source = new List<string>();
        var proxy = new EnumerableProxy<string>(source);
        proxy.ToList().ShouldBeEmpty();
    }

    /// <summary>
    /// 验证非泛型枚举按顺序返回全部源元素。
    /// </summary>
    [Fact]
    public void NonGenericGetEnumerator_ReturnsItems()
    {
        var source = new List<string> { "a", "b", "c" };
        IEnumerable proxy = new EnumerableProxy<string>(source);
        var results = new List<string>();
        var enumerator = proxy.GetEnumerator();
        while (enumerator.MoveNext())
            results.Add((string)enumerator.Current!);
        results.ShouldBe(new[] { "a", "b", "c" });
    }

    /// <summary>
    /// 验证代理支持 LINQ 计数和筛选。
    /// </summary>
    [Fact]
    public void LinqOperations_WorksCorrectly()
    {
        var source = new[] { 1, 2, 3, 4, 5 };
        var proxy = new EnumerableProxy<int>(source);
        proxy.Count().ShouldBe(5);
        proxy.Where(x => x % 2 == 0).ToList().ShouldBe(new[] { 2, 4 });
    }

    /// <summary>
    /// 验证代理反映源列表后续追加的元素。
    /// </summary>
    [Fact]
    public void GetEnumerator_ReflectsSourceChanges()
    {
        var source = new List<int> { 1 };
        var proxy = new EnumerableProxy<int>(source);
        source.Add(2);
        proxy.ToList().ShouldBe(new[] { 1, 2 });
    }
}
