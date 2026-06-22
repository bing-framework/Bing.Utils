using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bing.Collections;
using Xunit;

namespace Bing.Collections;

/// <summary>
/// 测试类：覆盖 <see cref="EnumerableProxy{T}"/> 相关行为。
/// </summary>
[Trait("Collections", "EnumerableProxy")]
public class EnumerableProxyTest
{
    /// <summary>
    /// 测试用例：验证 <see cref="EnumerableProxy{T}"/> 在 `NullEnumerable` 场景下，
    /// 结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void Constructor_NullEnumerable_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new EnumerableProxy<int>(null!));
    }

    /// <summary>
    /// 测试用例：验证 <see cref="EnumerableProxy{T}.GetEnumerator"/> 在 `ValidEnumerable`
    /// 场景下，结果为 `ReturnsAllItems`。
    /// </summary>
    [Fact]
    public void GetEnumerator_ValidList_ReturnsAllItems()
    {
        var source = new List<int> { 1, 2, 3 };
        var proxy = new EnumerableProxy<int>(source);
        proxy.ToList().ShouldBe(new[] { 1, 2, 3 });
    }

    /// <summary>
    /// 测试用例：验证 <see cref="EnumerableProxy{T}"/> 在 `EmptyEnumerable` 场景下，
    /// 结果为 `ReturnsEmptySequence`。
    /// </summary>
    [Fact]
    public void GetEnumerator_EmptyList_ReturnsEmptySequence()
    {
        var source = new List<string>();
        var proxy = new EnumerableProxy<string>(source);
        proxy.ToList().ShouldBeEmpty();
    }

    /// <summary>
    /// 测试用例：验证非泛型 <see cref="IEnumerable.GetEnumerator"/> 可正常枚举。
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
    /// 测试用例：验证 LINQ 操作（Count/Where）可通过代理正常使用。
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
    /// 测试用例：验证代理对修改后的源集合保持实时反映。
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
