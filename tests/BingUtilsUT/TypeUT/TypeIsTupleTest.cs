using Bing.Reflection;

namespace BingUtilsUT.TypeUT;

[Trait("TypeUT", "TypeIs.TupleType")]
public class TypeIsTupleTest
{
    /// <summary>
    /// 测试 - 类型 - 是否元组类型【隐式】
    /// </summary>
    [Fact]
    public void Test_Types_IsTupleType_Implicit()
    {
        Types.IsTupleType(typeof((int, int))).ShouldBeTrue();
        Types.IsTupleType(typeof((int, int, int))).ShouldBeTrue();

        Types.IsTupleType<string>().ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 类型 - 是否元组类型【显式】
    /// </summary>
    [Fact]
    public void Test_Types_IsTupleType_Explicit()
    {
        Types.IsTupleType(typeof(ValueTuple)).ShouldBeTrue();
        Types.IsTupleType(typeof(ValueTuple<int>)).ShouldBeTrue();
        Types.IsTupleType(typeof(ValueTuple<int, int>)).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否元组类型【显式】
    /// </summary>
    [Fact]
    public void Test_Types_IsTupleType_Explicit_2()
    {
        Types.IsTupleType(typeof(Tuple)).ShouldBeTrue();
        Types.IsTupleType(typeof(Tuple<int>)).ShouldBeTrue();
        Types.IsTupleType(typeof(Tuple<int, int>)).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 类型 - 是否元组类型 - 继承元组
    /// </summary>
    [Fact]
    public void Test_Types_IsTupleType_Parent()
    {
        Types.IsTupleType<ChildTupleType>().ShouldBeFalse();
        Types.IsTupleType<ChildTupleType>(TypeOfOptions.Underlying).ShouldBeTrue();
    }

    /// <summary>
    /// 测试 - 扩展方法 - 是否元组类型
    /// </summary>
    [Fact]
    public void Test_ExtensionsMethod_IsTupleType()
    {
        typeof((int, int)).IsTupleType().ShouldBeTrue();
        typeof(Tuple).IsTupleType().ShouldBeTrue();
        typeof(Tuple<int, int>).IsTupleType().ShouldBeTrue();
        typeof(ValueTuple).IsTupleType().ShouldBeTrue();
        typeof(ValueTuple<int, int>).IsTupleType().ShouldBeTrue();
        typeof(ChildTupleType).IsTupleType().ShouldBeFalse();
        typeof(ChildTupleType).IsTupleType(TypeOfOptions.Underlying).ShouldBeTrue();
        typeof(object).IsTupleType().ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 类型 - 是否元组类型【类型转换】
    /// </summary>
    [Fact]
    public void Test_Types_IsTupleType_1()
    {
        var tuple = Tuple.Create(1, 2, 3);
        var valueTuple = ValueTuple.Create(1, 2, 3);
        var childTuple = new ChildTupleType(1, 2);
        Types.IsTupleType(tuple).ShouldBeTrue();
        Types.IsTupleType(valueTuple).ShouldBeTrue();
        Types.IsTupleType(childTuple).ShouldBeFalse();
        Types.IsTupleType(childTuple, TypeOfOptions.Underlying).ShouldBeTrue();
        Types.IsTupleType(123).ShouldBeFalse();
        Types.IsTupleType((object)null).ShouldBeFalse();
        Types.IsTupleType(null).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 - 类型 - 是否元组类型【可空】
    /// </summary>
    [Fact]
    public void Test_Types_IsTupleType_Nullable()
    {
        Types.IsTupleType(typeof((int, int))).ShouldBeTrue();
        Types.IsTupleType(typeof((int, int, int))).ShouldBeTrue();
        Types.IsTupleType(typeof((int, int)?)).ShouldBeFalse();
        Types.IsTupleType(typeof((int, int, int)?)).ShouldBeFalse();
        Types.IsTupleType(typeof((int, int)?), isOptions: TypeIsOptions.IgnoreNullable).ShouldBeTrue();
        Types.IsTupleType(typeof((int, int, int)?), isOptions: TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }
}