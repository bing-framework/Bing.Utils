namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypesAndTypeJudge` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypesAndTypeJudge")]
public class TypesAndTypeJudgeTest
{
    /// <summary>
    /// 测试用例：验证 `DefaultValue` 在 `GenericValueType` 场景下，结果为 `ReturnsTypeDefault`。
    /// </summary>
    [Fact]
    public void DefaultValue_GenericValueType_ReturnsTypeDefault()
    {
        var result = Types.DefaultValue<int>();
        result.ShouldBe(0);
    }
    /// <summary>
    /// 测试用例：验证 `DefaultValue` 在 `ByType_ReferenceType` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void DefaultValue_ByType_ReferenceType_ReturnsNull()
    {
        var result = Types.DefaultValue(typeof(string));
        result.ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `DefaultValue` 在 `ByType_NullType` 场景下，结果为 `CurrentlyThrowsNullReferenceException`。
    /// </summary>
    [Fact]
    public void DefaultValue_ByType_NullType_CurrentlyThrowsNullReferenceException()
    {
        Should.Throw<NullReferenceException>(() => Types.DefaultValue((Type)null));
    }
    /// <summary>
    /// 测试用例：验证 `IsDefaultValue` 在 `GenericAndObjectOverloads` 场景下，结果为 `ReturnExpected`。
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
    /// 测试用例：验证 `Of` 在 `GenericWithUnderlyingOption` 场景下，结果为 `UnwrapsNullable`。
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
    /// 测试用例：验证 `Of` 在 `ObjectArray_WithNullItems` 场景下，结果为 `FiltersNullsAndReturnsOwnerTypes`。
    /// </summary>
    [Fact]
    public void Of_ObjectArray_WithNullItems_FiltersNullsAndReturnsOwnerTypes()
    {
        object[] values = [1, null, "x"];
        var types = Types.Of(values, TypeOfOptions.Owner);
        types.ShouldBe([typeof(int), typeof(string)]);
    }
    /// <summary>
    /// 测试用例：验证 `Of` 在 `ObjectArray_NullArray` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void Of_ObjectArray_NullArray_ReturnsNull()
    {
        Types.Of(null).ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `IsTupleType` 在 `WithTupleAndValueTuple` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void IsTupleType_WithTupleAndValueTuple_ReturnsTrue()
    {
        Types.IsTupleType(typeof(Tuple<int, string>)).ShouldBeTrue();
        Types.IsTupleType(typeof((int, string))).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsValueTupleType` 在 `WithTupleAndValueTuple` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void IsValueTupleType_WithTupleAndValueTuple_ReturnsExpected()
    {
        Types.IsValueTupleType(typeof(Tuple<int, string>)).ShouldBeFalse();
        Types.IsValueTupleType(typeof((int, string))).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsNumericType` 在 `WithNullableAndIgnoreNullableOption` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void IsNumericType_WithNullableAndIgnoreNullableOption_ReturnsExpected()
    {
        Types.IsNumericType(typeof(int?)).ShouldBeFalse();
        Types.IsNumericType(typeof(int?), TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `IsNumericType` 在 `ObjectValueContainingInt` 场景下，结果为 `CurrentlyUsesCompileTimeType`。
    /// </summary>
    [Fact]
    public void IsNumericType_ObjectValueContainingInt_CurrentlyUsesCompileTimeType()
    {
        object value = 123;
        Types.IsNumericType(value).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `IsCollectionType` 在 `NullValueWithIgnoreNullable` 场景下，结果为 `ReturnsTypeBasedResult`。
    /// </summary>
    [Fact]
    public void IsCollectionType_NullValueWithIgnoreNullable_ReturnsTypeBasedResult()
    {
        List<int> value = null;
        Types.IsCollectionType(value).ShouldBeFalse();
        Types.IsCollectionType(value, TypeIsOptions.IgnoreNullable).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `TypeJudge` 在 `IsEnumType_WithNullableOption` 场景下，结果为 `ReturnsExpected`。
    /// </summary>
    [Fact]
    public void TypeJudge_IsEnumType_WithNullableOption_ReturnsExpected()
    {
        TypeJudge.IsEnumType<TestEnum?>(mayNullable: false).ShouldBeFalse();
        TypeJudge.IsEnumType<TestEnum?>(mayNullable: true).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `TypeJudge` 在 `IsNullableType_NullInput` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void TypeJudge_IsNullableType_NullInput_ReturnsFalse()
    {
        TypeJudge.IsNullableType((Type)null).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `TypeJudge` 在 `IsGenericImplementation_NullArguments` 场景下，结果为 `ThrowsArgumentNullException`。
    /// </summary>
    [Fact]
    public void TypeJudge_IsGenericImplementation_NullArguments_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsGenericImplementation(null, typeof(IEnumerable<>))).ParamName.ShouldBe("type");
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsGenericImplementation(typeof(List<int>), null)).ParamName.ShouldBe("genericType");
    }
    /// <summary>
    /// 测试用例：验证 `TypeJudge` 在 `IsGenericImplementation_NonGenericTypeDefinition` 场景下，结果为 `ReturnsFalse`。
    /// </summary>
    [Fact]
    public void TypeJudge_IsGenericImplementation_NonGenericTypeDefinition_ReturnsFalse()
    {
        TypeJudge.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable)).ShouldBeFalse();
    }
    /// <summary>
    /// 测试用例：验证 `TypeJudge` 在 `IsGenericImplementation_InterfaceAndBaseClass` 场景下，结果为 `ReturnsTrue`。
    /// </summary>
    [Fact]
    public void TypeJudge_IsGenericImplementation_InterfaceAndBaseClass_ReturnsTrue()
    {
        TypeJudge.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable<>)).ShouldBeTrue();
        TypeJudge.IsGenericImplementation(typeof(DerivedGeneric), typeof(BaseGeneric<>)).ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `Types` 在 `IsGenericImplementation_WrapperMethod` 场景下，结果为 `CurrentlyRequiresOpenGenericAndReturnsFalseForClosedGeneric`。
    /// </summary>
    [Fact]
    public void Types_IsGenericImplementation_WrapperMethod_CurrentlyRequiresOpenGenericAndReturnsFalseForClosedGeneric()
    {
        Types.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable<>)).ShouldBeTrue();
        Types.IsGenericImplementation<DerivedGeneric, BaseGeneric<int>>().ShouldBeFalse();
    }
    private enum TestEnum
    {
        A = 1
    }
    private class BaseGeneric<T>
    {
    }
    private class DerivedGeneric : BaseGeneric<int>
    {
    }
}

