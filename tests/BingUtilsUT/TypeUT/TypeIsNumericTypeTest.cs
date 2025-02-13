using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.NumericType")]
public class TypeIsNumericTypeTest
{
    /// <summary>
    /// 测试 - 类型 - 是否为数值类型【typeof】
    /// </summary>
    [Fact]
    public void Test_Types_IsNumericType_1()
    {
        Types.IsNumericType(typeof(byte)).ShouldBeTrue();
        Types.IsNumericType(typeof(sbyte)).ShouldBeTrue();
        Types.IsNumericType(typeof(short)).ShouldBeTrue();
        Types.IsNumericType(typeof(ushort)).ShouldBeTrue();
        Types.IsNumericType(typeof(int)).ShouldBeTrue();
        Types.IsNumericType(typeof(uint)).ShouldBeTrue();
        Types.IsNumericType(typeof(long)).ShouldBeTrue();
        Types.IsNumericType(typeof(ulong)).ShouldBeTrue();
        Types.IsNumericType(typeof(float)).ShouldBeTrue();
        Types.IsNumericType(typeof(double)).ShouldBeTrue();
        Types.IsNumericType(typeof(decimal)).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为数值类型【泛型】
    /// </summary>
    [Fact]
    public void Test_Types_IsNumericType_2()
    {
        Types.IsNumericType<byte>().ShouldBeTrue();
        Types.IsNumericType<sbyte>().ShouldBeTrue();
        Types.IsNumericType<short>().ShouldBeTrue();
        Types.IsNumericType<ushort>().ShouldBeTrue();
        Types.IsNumericType<int>().ShouldBeTrue();
        Types.IsNumericType<uint>().ShouldBeTrue();
        Types.IsNumericType<long>().ShouldBeTrue();
        Types.IsNumericType<ulong>().ShouldBeTrue();
        Types.IsNumericType<float>().ShouldBeTrue();
        Types.IsNumericType<double>().ShouldBeTrue();
        Types.IsNumericType<decimal>().ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为数值类型【typeof】
    /// </summary>
    [Fact]
    public void Test_Types_IsNumericType_Nullable_1()
    {
        Types.IsNumericType(typeof(byte?)).ShouldBeFalse();
        Types.IsNumericType(typeof(sbyte?)).ShouldBeFalse();
        Types.IsNumericType(typeof(short?)).ShouldBeFalse();
        Types.IsNumericType(typeof(ushort?)).ShouldBeFalse();
        Types.IsNumericType(typeof(int?)).ShouldBeFalse();
        Types.IsNumericType(typeof(uint?)).ShouldBeFalse();
        Types.IsNumericType(typeof(long?)).ShouldBeFalse();
        Types.IsNumericType(typeof(ulong?)).ShouldBeFalse();
        Types.IsNumericType(typeof(float?)).ShouldBeFalse();
        Types.IsNumericType(typeof(double?)).ShouldBeFalse();
        Types.IsNumericType(typeof(decimal?)).ShouldBeFalse();

        Types.IsNumericType(typeof(byte?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(sbyte?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(short?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(ushort?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(int?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(uint?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(long?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(ulong?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(float?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(double?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(typeof(decimal?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为数值类型【泛型】
    /// </summary>
    [Fact]
    public void Test_Types_IsNumericType_Nullable_2()
    {
        Types.IsNumericType<byte?>().ShouldBeFalse();
        Types.IsNumericType<sbyte?>().ShouldBeFalse();
        Types.IsNumericType<short?>().ShouldBeFalse();
        Types.IsNumericType<ushort?>().ShouldBeFalse();
        Types.IsNumericType<int?>().ShouldBeFalse();
        Types.IsNumericType<uint?>().ShouldBeFalse();
        Types.IsNumericType<long?>().ShouldBeFalse();
        Types.IsNumericType<ulong?>().ShouldBeFalse();
        Types.IsNumericType<float?>().ShouldBeFalse();
        Types.IsNumericType<double?>().ShouldBeFalse();
        Types.IsNumericType<decimal?>().ShouldBeFalse();

        Types.IsNumericType<byte?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<sbyte?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<short?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<ushort?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<int?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<uint?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<long?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<ulong?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<float?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<double?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType<decimal?>(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 扩展方法 - 是否为数值类型
    /// </summary>
    [Fact]
    public void Test_ExtensionsMethod_IsNumeric_1()
    {
        typeof(byte).IsNumeric().ShouldBeTrue();
        typeof(sbyte).IsNumeric().ShouldBeTrue();
        typeof(short).IsNumeric().ShouldBeTrue();
        typeof(ushort).IsNumeric().ShouldBeTrue();
        typeof(int).IsNumeric().ShouldBeTrue();
        typeof(uint).IsNumeric().ShouldBeTrue();
        typeof(long).IsNumeric().ShouldBeTrue();
        typeof(ulong).IsNumeric().ShouldBeTrue();
        typeof(float).IsNumeric().ShouldBeTrue();
        typeof(double).IsNumeric().ShouldBeTrue();
        typeof(decimal).IsNumeric().ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 扩展方法 - 是否为数值类型
    /// </summary>
    [Fact]
    public void Test_ExtensionsMethod_IsNumeric_2()
    {
        typeof(int).IsNumeric().ShouldBeTrue();
        typeof(int?).IsNumeric().ShouldBeFalse();
        typeof(int?).IsNumeric(TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否为数值类型【类型转换】
    /// </summary>
    [Fact]
    public void Test_Types_IsNumericType_3()
    {
        int a = 1;
        int? b = 1;
        int? c = null;

        Types.IsNumericType(a).ShouldBeTrue();
        Types.IsNumericType(b).ShouldBeFalse();
        Types.IsNumericType(b, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsNumericType(c).ShouldBeFalse();
        Types.IsNumericType(c, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型反射 - 是否为数值类型
    /// </summary>
    [Fact]
    public void Test_Type_Reflection_IsNumericType()
    {
        Func<MemberInfo, bool> filter = member => member.MemberType == MemberTypes.TypeInfo
                                                  || member.MemberType == MemberTypes.Field
                                                  || member.MemberType == MemberTypes.Property;
        var t = typeof(NormalValueTypeClass);
        var m0 = (MemberInfo)t;
        var allMembers = t.GetMembers().Where(filter).ToList();

        allMembers.ShouldNotBeNull();
        allMembers.ShouldNotBeEmpty();
        var m1 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int16V1));
        var m2 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int16V2));
        var m3 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int16V3));
        var m4 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Int16V4));
        var m5 = allMembers.Single(x => x.Name == nameof(NormalValueTypeClass.Str));

        m1.MemberType.ShouldBe(MemberTypes.Property);
        m2.MemberType.ShouldBe(MemberTypes.Field);
        m3.MemberType.ShouldBe(MemberTypes.Property);
        m4.MemberType.ShouldBe(MemberTypes.Field);
        m5.MemberType.ShouldBe(MemberTypes.Property);

        TypeReflections.IsNumeric(m0).ShouldBeFalse();
        TypeReflections.IsNumeric(m1).ShouldBeTrue();
        TypeReflections.IsNumeric(m2).ShouldBeTrue();
        TypeReflections.IsNumeric(m3).ShouldBeFalse();
        TypeReflections.IsNumeric(m4).ShouldBeFalse();
        TypeReflections.IsNumeric(m3, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsNumeric(m4, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        TypeReflections.IsNumeric(m5).ShouldBeFalse();
    }
}