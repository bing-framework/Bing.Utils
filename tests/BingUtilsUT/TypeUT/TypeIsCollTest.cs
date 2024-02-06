using System.Collections;
using System.Collections.Generic;
using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.CollType")]
public class TypeIsCollTest
{
    /// <summary>
    /// 测试 - 基于类型的数组
    /// </summary>
    [Fact]
    public void Test_DirectType_NormalArray()
    {
        Types.IsCollectionType(typeof(int[])).ShouldBeTrue();
        Types.IsCollectionType(typeof(int?[])).ShouldBeTrue();
        Types.IsCollectionType(typeof(string[])).ShouldBeTrue();
        Types.IsCollectionType(typeof(Array)).ShouldBeTrue();
        Types.IsCollectionType(typeof(ArrayList)).ShouldBeTrue();
        Types.IsCollectionType(typeof(List<int>)).ShouldBeTrue();
        Types.IsCollectionType(typeof(ArraySegment<int>)).ShouldBeTrue();
        Types.IsCollectionType(typeof(IEnumerable<string>)).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 基于泛型的数组
    /// </summary>
    [Fact]
    public void Test_GenericType_NormalArray()
    {
        Types.IsCollectionType<int[]>().ShouldBeTrue();
        Types.IsCollectionType<int?[]>().ShouldBeTrue();
        Types.IsCollectionType<string[]>().ShouldBeTrue();
        Types.IsCollectionType<Array>().ShouldBeTrue();
        Types.IsCollectionType<ArrayList>().ShouldBeTrue();
        Types.IsCollectionType<List<int>>().ShouldBeTrue();
        Types.IsCollectionType<ArraySegment<int>>().ShouldBeTrue();
        Types.IsCollectionType<IEnumerable<string>>().ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 基于对象的数组
    /// </summary>
    [Fact]
    public void Test_Object_NormalArray()
    {
        var nullList = new List<int>();
        nullList = null;

        Types.IsCollectionType(Array.Empty<int>()).ShouldBeTrue();
        Types.IsCollectionType(Array.Empty<int?>()).ShouldBeTrue();
        Types.IsCollectionType(new ArrayList()).ShouldBeTrue();
        Types.IsCollectionType(new List<int>()).ShouldBeTrue();
        Types.IsCollectionType(new ArraySegment<int>()).ShouldBeTrue();
        Types.IsCollectionType("nice").ShouldBeTrue(); // String, IEnumerable<Char>
        Types.IsCollectionType(nullList).ShouldBeFalse();
        Types.IsCollectionType(nullList, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }
}