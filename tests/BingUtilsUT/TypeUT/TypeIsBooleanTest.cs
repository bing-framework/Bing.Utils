using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.BooleanType")]
public class TypeIsBooleanTest
{
    /// <summary>
    /// 测试 - 类型反射 - 是否为布尔类型
    /// </summary>
    [Fact]
    public void Test_Type_Reflection_IsBoolean()
    {
        Func<MemberInfo, bool> filter = member => member.MemberType == MemberTypes.TypeInfo
                                                  || member.MemberType == MemberTypes.Field
                                                  || member.MemberType == MemberTypes.Property;
        var t = typeof(NormalValueTypeClass);
        var m0 = (MemberInfo)t;
        var allMembers = t.GetMembers().Where(filter).ToList();

        allMembers.ShouldNotBeNull();
        allMembers.ShouldNotBeEmpty();
        var m1 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.BooleanV1));
        var m2 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.BooleanV2));
        var m3 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.BooleanV3));
        var m4 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.BooleanV4));
        var m5 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Str));

        m1.MemberType.ShouldBe(MemberTypes.Property);
        m2.MemberType.ShouldBe(MemberTypes.Field);
        m3.MemberType.ShouldBe(MemberTypes.Property);
        m4.MemberType.ShouldBe(MemberTypes.Field);
        m5.MemberType.ShouldBe(MemberTypes.Property);

        TypeReflections.IsBoolean(m0).ShouldBeFalse();
        TypeReflections.IsBoolean(m1).ShouldBeTrue();
        TypeReflections.IsBoolean(m2).ShouldBeTrue();
        TypeReflections.IsBoolean(m3).ShouldBeFalse();
        TypeReflections.IsBoolean(m4).ShouldBeFalse();
        TypeReflections.IsBoolean(m3, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsBoolean(m4, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsBoolean(m5).ShouldBeFalse();
    }
}