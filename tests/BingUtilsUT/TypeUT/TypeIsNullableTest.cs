using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.Nullable")]
public class TypeIsNullableTest
{
    /// <summary>
    /// 测试 - 类型 - 是否为可空类型【typeof】
    /// </summary>
    [Fact]
    public void Test_Types_IsNullableType_1()
    {
        Types.IsNullableType(typeof(int)).ShouldBeFalse();
        Types.IsNullableType(typeof(int?)).ShouldBeTrue();
        Types.IsNullableType(typeof(Nullable)).ShouldBeFalse();
        Types.IsNullableType(typeof(Nullable<int>)).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为可空类型【泛型】
    /// </summary>
    [Fact]
    public void Test_Types_IsNullableType_2()
    {
        Types.IsNullableType<int>().ShouldBeFalse();
        Types.IsNullableType<int?>().ShouldBeTrue();
        Types.IsNullableType<Nullable<int>>().ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为可空类型【类型转换】
    /// </summary>
    [Fact]
    public void Test_Types_IsNullableType_3()
    {
        int a = 0;
        int? b = 0;
        int? c = null;

        Types.IsNullableType(a).ShouldBeFalse();
        Types.IsNullableType(b).ShouldBeTrue();
        Types.IsNullableType(c).ShouldBeTrue();
        Types.IsNullableType(new object()).ShouldBeFalse();
    }
}