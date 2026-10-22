using System.Reflection;

namespace Bing.Reflection;
/// <summary>
/// 验证类型默认值、可空类型、元组及泛型实现判定。
/// </summary>
[Trait("Bing.Reflection", "TypesAndTypeJudge")]
public class TypesAndTypeJudgeTest
{
    /// <summary>
    /// 验证泛型默认值入口返回整数零值。
    /// </summary>
    [Fact]
    public void DefaultValue_GenericValueType_ReturnsTypeDefault()
    {
        var result = Types.DefaultValue<int>();
        result.ShouldBe(0);
    }
    /// <summary>
    /// 验证引用类型的默认值为空。
    /// </summary>
    [Fact]
    public void DefaultValue_ByType_ReferenceType_ReturnsNull()
    {
        var result = Types.DefaultValue(typeof(string));
        result.ShouldBeNull();
    }
    /// <summary>
    /// 验证空类型的默认值查询抛出参数异常。
    /// </summary>
    [Fact]
    public void DefaultValue_ByType_NullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Types.DefaultValue((Type)null));
    }
    /// <summary>
    /// 验证泛型与对象重载均识别默认值及非默认值。
    /// </summary>
    [Fact]
    public void IsDefaultValue_GenericAndObjectOverloads_ReturnExpected()
    {
        Types.IsDefaultValue(0).ShouldBeTrue();
        Types.IsDefaultValue(1).ShouldBeFalse();
        Types.IsDefaultValue((object)0).ShouldBeTrue();
        Types.IsDefaultValue((object)1).ShouldBeFalse();
        Types.IsDefaultValue((object)null).ShouldBeTrue();
    }
    /// <summary>
    /// 验证类型选项决定保留可空包装或返回底层类型。
    /// </summary>
    [Fact]
    public void Of_GenericWithUnderlyingOption_UnwrapsNullable()
    {
        var ownerType = Types.Of<int?>(TypeOfOptions.Owner);
        var underlyingType = Types.Of<int?>(TypeOfOptions.Underlying);
        ownerType.ShouldBe(typeof(int?));
        underlyingType.ShouldBe(typeof(int));
    }
    /// <summary>
    /// 验证对象数组的类型提取过滤空元素并保留顺序。
    /// </summary>
    [Fact]
    public void Of_ObjectArray_WithNullItems_FiltersNullsAndReturnsOwnerTypes()
    {
        object[] values = [1, null, "x"];
        var types = Types.Of(values, TypeOfOptions.Owner);
        types.ShouldBe([typeof(int), typeof(string)]);
    }
    /// <summary>
    /// 验证空对象数组的类型提取抛出参数异常并标明参数名。
    /// </summary>
    [Fact]
    public void Of_ObjectArray_NullArray_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Types.Of((object[])null!))
            .ParamName.ShouldBe("objects");
    }
    /// <summary>
    /// 验证引用元组与值元组均满足元组类型判定。
    /// </summary>
    [Fact]
    public void IsTupleType_WithTupleAndValueTuple_ReturnsTrue()
    {
        Types.IsTupleType(typeof(Tuple<int, string>)).ShouldBeTrue();
        Types.IsTupleType(typeof((int, string))).ShouldBeTrue();
    }
    /// <summary>
    /// 验证仅值元组满足值元组类型判定。
    /// </summary>
    [Fact]
    public void IsValueTupleType_WithTupleAndValueTuple_ReturnsExpected()
    {
        Types.IsValueTupleType(typeof(Tuple<int, string>)).ShouldBeFalse();
        Types.IsValueTupleType(typeof((int, string))).ShouldBeTrue();
    }
    /// <summary>
    /// 验证可空整数仅在忽略可空包装时满足数值判定。
    /// </summary>
    [Fact]
    public void IsNumericType_WithNullableAndIgnoreNullableOption_ReturnsExpected()
    {
        Types.IsNumericType(typeof(int?)).ShouldBeFalse();
        Types.IsNumericType(typeof(int?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }
    /// <summary>
    /// 验证对象值重载当前使用静态对象类型进行数值判定。
    /// </summary>
    [Fact]
    public void IsNumericType_ObjectValueContainingInt_CurrentlyUsesCompileTimeType()
    {
        object value = 123;
        Types.IsNumericType(value).ShouldBeFalse();
    }

    /// <summary>
    /// 测试用例：验证 `IsStructType` 的 TypeInfo 重载在空值和有效结构类型场景下返回预期结果。
    /// </summary>
    [Fact]
    public void IsStructType_TypeInfo_NullAndValueType_ReturnsExpected()
    {
        Types.IsStructType((TypeInfo)null).ShouldBeFalse();
        Types.IsStructType(typeof(DateTime).GetTypeInfo()).ShouldBeTrue();
    }

    /// <summary>
    /// 验证空集合值在忽略可空选项下按声明类型识别。
    /// </summary>
    [Fact]
    public void IsCollectionType_NullValueWithIgnoreNullable_ReturnsTypeBasedResult()
    {
        List<int> value = null;
        Types.IsCollectionType(value).ShouldBeFalse();
        Types.IsCollectionType(value, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }
    /// <summary>
    /// 验证可空枚举的识别受可空选项控制。
    /// </summary>
    [Fact]
    public void TypeJudge_IsEnumType_WithNullableOption_ReturnsExpected()
    {
        TypeJudge.IsEnumType<TestEnum?>(mayNullable: false).ShouldBeFalse();
        TypeJudge.IsEnumType<TestEnum?>(mayNullable: true).ShouldBeTrue();
    }
    /// <summary>
    /// 验证空类型不满足可空类型判定。
    /// </summary>
    [Fact]
    public void TypeJudge_IsNullableType_NullInput_ReturnsFalse()
    {
        TypeJudge.IsNullableType((Type)null).ShouldBeFalse();
    }
    /// <summary>
    /// 验证泛型实现判定分别拒绝空类型与空泛型定义。
    /// </summary>
    [Fact]
    public void TypeJudge_IsGenericImplementation_NullArguments_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsGenericImplementation(null, typeof(IEnumerable<>))).ParamName.ShouldBe("type");
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsGenericImplementation(typeof(List<int>), null)).ParamName.ShouldBe("genericType");
    }
    /// <summary>
    /// 验证非泛型接口不满足泛型定义匹配要求。
    /// </summary>
    [Fact]
    public void TypeJudge_IsGenericImplementation_NonGenericTypeDefinition_ReturnsFalse()
    {
        TypeJudge.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable)).ShouldBeFalse();
    }
    /// <summary>
    /// 验证能够识别泛型接口实现及泛型基类继承。
    /// </summary>
    [Fact]
    public void TypeJudge_IsGenericImplementation_InterfaceAndBaseClass_ReturnsTrue()
    {
        TypeJudge.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable<>)).ShouldBeTrue();
        TypeJudge.IsGenericImplementation(typeof(DerivedGeneric), typeof(BaseGeneric<>)).ShouldBeTrue();
    }
    /// <summary>
    /// 验证泛型实现与原始类型查询包装器可匹配闭合泛型类型。
    /// </summary>
    [Fact]
    public void Types_IsGenericImplementationAndRawType_WrapperMethods_AcceptClosedGeneric()
    {
        Types.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable<>)).ShouldBeTrue();
        Types.IsGenericImplementation<DerivedGeneric, BaseGeneric<int>>().ShouldBeTrue();
        Types.IsGenericImplementation<DerivedGeneric, BaseGeneric<string>>().ShouldBeFalse();
        Types.GetRawTypeFromGenericClass(typeof(DerivedGeneric), typeof(BaseGeneric<int>)).ShouldBe(typeof(int));
        Types.GetRawTypeFromGenericClass<DerivedGeneric, BaseGeneric<int>>().ShouldBe(typeof(int));
        Types.GetRawTypeFromGenericClass(typeof(DerivedGeneric), typeof(BaseGeneric<string>)).ShouldBeNull();
    }
    /// <summary>
    /// 提供可空枚举类型判定的测试枚举。
    /// </summary>
    private enum TestEnum
    {
        /// <summary>
        /// 用于可空枚举判定的测试值。
        /// </summary>
        A = 1
    }
    /// <summary>
    /// 提供泛型继承判定的基类样例。
    /// </summary>
    /// <typeparam name="T">用于形成泛型基类的测试类型。</typeparam>
    private class BaseGeneric<T>
    {
    }
    /// <summary>
    /// 提供继承闭合泛型基类的测试类型。
    /// </summary>
    private class DerivedGeneric : BaseGeneric<int>
    {
    }
}

