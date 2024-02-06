using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.DateTimeType")]
public class TypeIsDateTimeTest
{
    /// <summary>
    /// 测试 - 类型反射 - 是否为日期类型
    /// </summary>
    [Fact]
    public void Test_Type_Reflection_IsDateTime()
    {
        Func<MemberInfo, bool> filter = member => member.MemberType == MemberTypes.TypeInfo
                                                  || member.MemberType == MemberTypes.Field
                                                  || member.MemberType == MemberTypes.Property;
        var t = typeof(NormalValueTypeClass);
        var m0 = (MemberInfo)t;
        var allMembers = t.GetMembers().Where(filter).ToList();

        allMembers.ShouldNotBeNull();
        allMembers.ShouldNotBeEmpty();
        var m1 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.DateTimeV1));
        var m2 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.DateTimeV2));
        var m3 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.DateTimeV3));
        var m4 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.DateTimeV4));
        var m5 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Str));

        m1.MemberType.ShouldBe(MemberTypes.Property);
        m2.MemberType.ShouldBe(MemberTypes.Field);
        m3.MemberType.ShouldBe(MemberTypes.Property);
        m4.MemberType.ShouldBe(MemberTypes.Field);
        m5.MemberType.ShouldBe(MemberTypes.Property);

        TypeReflections.IsDateTime(m0).ShouldBeFalse();
        TypeReflections.IsDateTime(m1).ShouldBeTrue();
        TypeReflections.IsDateTime(m2).ShouldBeTrue();
        TypeReflections.IsDateTime(m3).ShouldBeFalse();
        TypeReflections.IsDateTime(m4).ShouldBeFalse();
        TypeReflections.IsDateTime(m3, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsDateTime(m4, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsDateTime(m5).ShouldBeFalse();
    }
}