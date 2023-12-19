using Bing.Reflection;
using BingUtilsUT.Samples;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.EnumType")]
public class TypeIsEnumTest
{
    /// <summary>
    /// 测试 - 类型 - 是否为枚举类型【typeof】
    /// </summary>
    [Fact]
    public void Test_Types_IsEnum_1()
    {
        Types.IsEnumType(typeof(int)).ShouldBeFalse();
        Types.IsEnumType(typeof(Int16Enum)).ShouldBeTrue();
        Types.IsEnumType(typeof(Int16Enum?)).ShouldBeFalse();
        Types.IsEnumType(typeof(Int16Enum?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为枚举类型【泛型】
    /// </summary>
    [Fact]
    public void Test_Types_IsEnum_2()
    {
        Types.IsEnumType<int>().ShouldBeFalse();
        Types.IsEnumType<Int16Enum>().ShouldBeTrue();
        Types.IsEnumType<Int16Enum?>().ShouldBeFalse();
        Types.IsEnumType<Int16Enum?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为枚举类型【类型转换】
    /// </summary>
    [Fact]
    public void Test_Types_IsEnum_3()
    {
        Types.IsEnumType(Int16Enum.A).ShouldBeTrue();
        Types.IsEnumType((Int16Enum)1).ShouldBeTrue();
        Types.IsEnumType((Int16Enum)0).ShouldBeFalse();
        Types.IsEnumType(1).ShouldBeFalse();
        Types.IsEnumType(null).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 类型反射 - 是否为枚举类型
    /// </summary>
    [Fact]
    public void Test_Type_Reflection_IsEnum()
    {
        Func<MemberInfo, bool> filter = member => member.MemberType == MemberTypes.TypeInfo
                                                  || member.MemberType == MemberTypes.Field
                                                  || member.MemberType == MemberTypes.Property;
        var t = typeof(NormalValueTypeClass);
        var m0 = (MemberInfo)t;
        var allMembers = t.GetMembers().Where(filter).ToList();

        allMembers.ShouldNotBeNull();
        allMembers.ShouldNotBeEmpty();
        var m1 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int99V1));
        var m2 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int99V2));
        var m3 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int99V3));
        var m4 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int99V4));
        var m5 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Str));

        m1.MemberType.ShouldBe(MemberTypes.Property);
        m2.MemberType.ShouldBe(MemberTypes.Field);
        m3.MemberType.ShouldBe(MemberTypes.Property);
        m4.MemberType.ShouldBe(MemberTypes.Field);
        m5.MemberType.ShouldBe(MemberTypes.Property);

        TypeReflections.IsEnum(m0).ShouldBeFalse();
        TypeReflections.IsEnum(m1).ShouldBeTrue();
        TypeReflections.IsEnum(m2).ShouldBeTrue();
        TypeReflections.IsEnum(m3).ShouldBeFalse();
        TypeReflections.IsEnum(m4).ShouldBeFalse();
        TypeReflections.IsEnum(m3, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsEnum(m4, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsEnum(m5).ShouldBeFalse();
    }
}