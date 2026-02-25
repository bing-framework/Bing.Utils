namespace Bing.Reflection;
/// <summary>
/// 测试类：覆盖 `TypeConvAndTypesVal` 相关行为。
/// </summary>
[Trait("Bing.Reflection", "TypeConvAndTypesVal")]
public class TypeConvAndTypesValTest
{
    /// <summary>
    /// 测试用例：验证 `GetNonNullableType` 在 `NullInput` 场景下，结果为 `ReturnsNull`。
    /// </summary>
    [Fact]
    public void GetNonNullableType_NullInput_ReturnsNull()
    {
        TypeConv.GetNonNullableType(null).ShouldBeNull();
    }
    /// <summary>
    /// 测试用例：验证 `GetNonNullableType` 在 `NullableType` 场景下，结果为 `ReturnsUnderlyingType`。
    /// </summary>
    [Fact]
    public void GetNonNullableType_NullableType_ReturnsUnderlyingType()
    {
        var result = TypeConv.GetNonNullableType(typeof(int?));
        result.ShouldBe(typeof(int));
    }
    /// <summary>
    /// 测试用例：验证 `GetNonNullableType` 在 `ArrayOfNullable` 场景下，结果为 `ReturnsArrayOfUnderlying`。
    /// </summary>
    [Fact]
    public void GetNonNullableType_ArrayOfNullable_ReturnsArrayOfUnderlying()
    {
        var result = TypeConv.GetNonNullableType(typeof(int?[]));
        result.ShouldBe(typeof(int[]));
    }
    /// <summary>
    /// 测试用例：验证 `GetNonNullableType` 在 `KeyValuePairWithNullableValue` 场景下，结果为 `ReturnsNonNullableValueType`。
    /// </summary>
    [Fact]
    public void GetNonNullableType_KeyValuePairWithNullableValue_ReturnsNonNullableValueType()
    {
        var result = TypeConv.GetNonNullableType(typeof(KeyValuePair<string, int?>));
        result.ShouldBe(typeof(KeyValuePair<string, int>));
    }
    /// <summary>
    /// 测试用例：验证 `GetNonNullableType` 在 `DictionaryWithNullableValue` 场景下，结果为 `ReturnsDictionaryWithNonNullableValue`。
    /// </summary>
    [Fact]
    public void GetNonNullableType_DictionaryWithNullableValue_ReturnsDictionaryWithNonNullableValue()
    {
        var result = TypeConv.GetNonNullableType(typeof(Dictionary<string, int?>));
        result.ShouldBe(typeof(Dictionary<string, int>));
    }
    /// <summary>
    /// 测试用例：验证 `GetNonNullableType` 在 `ListOfNullable` 场景下，结果为 `ReturnsListOfNonNullable`。
    /// </summary>
    [Fact]
    public void GetNonNullableType_ListOfNullable_ReturnsListOfNonNullable()
    {
        var result = TypeConv.GetNonNullableType(typeof(List<int?>));
        result.ShouldBe(typeof(List<int>));
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `NullTypes` 场景下，结果为 `ReturnsEmptySingleton`。
    /// </summary>
    [Fact]
    public void Create_NullTypes_ReturnsEmptySingleton()
    {
        var result = TypesVal.Create(null);
        result.ShouldBeSameAs(TypesVal.Empty);
        result.IsEmpty().ShouldBeTrue();
    }
    /// <summary>
    /// 测试用例：验证 `Create` 在 `WithTypes` 场景下，结果为 `ReturnsExpectedCountAndItems`。
    /// </summary>
    [Fact]
    public void Create_WithTypes_ReturnsExpectedCountAndItems()
    {
        var result = TypesVal.Create(typeof(int), typeof(string));
        result.Count.ShouldBe(2);
        result[0].ShouldBe(typeof(int));
        result[1].ShouldBe(typeof(string));
    }
    /// <summary>
    /// 测试用例：验证 `Indexer` 在 `OutOfRange` 场景下，结果为 `ThrowsArgumentOutOfRangeException`。
    /// </summary>
    [Fact]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var result = TypesVal.Create(typeof(int));
        Should.Throw<ArgumentOutOfRangeException>(() => _ = result[-1]).ParamName.ShouldBe("index");
        Should.Throw<ArgumentOutOfRangeException>(() => _ = result[1]).ParamName.ShouldBe("index");
    }
    /// <summary>
    /// 测试用例：验证 `TypeArray` 在 `ModifiedOutside` 场景下，结果为 `DoesNotAffectInternalCollection`。
    /// </summary>
    [Fact]
    public void TypeArray_ModifiedOutside_DoesNotAffectInternalCollection()
    {
        var result = TypesVal.Create(typeof(int), typeof(string));
        var externalArray = result.TypeArray;
        externalArray[0] = typeof(decimal);
        result[0].ShouldBe(typeof(int));
        result.TypeArray[0].ShouldBe(typeof(int));
    }
    /// <summary>
    /// 测试用例：验证 `Types` 在 `Enumeration` 场景下，结果为 `ReturnsAllTypesInOrder`。
    /// </summary>
    [Fact]
    public void Types_Enumeration_ReturnsAllTypesInOrder()
    {
        var result = TypesVal.Create(typeof(int), typeof(string), typeof(DateTime));
        result.Types.ToArray().ShouldBe([typeof(int), typeof(string), typeof(DateTime)]);
    }
}

