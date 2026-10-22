namespace Bing.Reflection;
/// <summary>
/// 验证可空类型转换及类型集合的访问与隔离。
/// </summary>
[Trait("Bing.Reflection", "TypeConvAndTypesVal")]
public class TypeConvAndTypesValTest
{
    /// <summary>
    /// 验证空类型输入抛出参数异常并标明参数名。
    /// </summary>
    [Fact]
    public void GetNonNullableType_NullInput_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeConv.GetNonNullableType(null!))
            .ParamName.ShouldBe("type");
    }
    /// <summary>
    /// 验证可空类型转换为底层类型。
    /// </summary>
    [Fact]
    public void GetNonNullableType_NullableType_ReturnsUnderlyingType()
    {
        var result = TypeConv.GetNonNullableType(typeof(int?));
        result.ShouldBe(typeof(int));
    }
    /// <summary>
    /// 验证可空元素数组转换为底层元素数组。
    /// </summary>
    [Fact]
    public void GetNonNullableType_ArrayOfNullable_ReturnsArrayOfUnderlying()
    {
        var result = TypeConv.GetNonNullableType(typeof(int?[]));
        result.ShouldBe(typeof(int[]));
    }
    /// <summary>
    /// 验证键值对的可空值类型转换为底层类型。
    /// </summary>
    [Fact]
    public void GetNonNullableType_KeyValuePairWithNullableValue_ReturnsNonNullableValueType()
    {
        var result = TypeConv.GetNonNullableType(typeof(KeyValuePair<string, int?>));
        result.ShouldBe(typeof(KeyValuePair<string, int>));
    }
    /// <summary>
    /// 验证字典的可空值类型转换为底层类型。
    /// </summary>
    [Fact]
    public void GetNonNullableType_DictionaryWithNullableValue_ReturnsDictionaryWithNonNullableValue()
    {
        var result = TypeConv.GetNonNullableType(typeof(Dictionary<string, int?>));
        result.ShouldBe(typeof(Dictionary<string, int>));
    }
    /// <summary>
    /// 验证列表的可空元素类型转换为底层类型。
    /// </summary>
    [Fact]
    public void GetNonNullableType_ListOfNullable_ReturnsListOfNonNullable()
    {
        var result = TypeConv.GetNonNullableType(typeof(List<int?>));
        result.ShouldBe(typeof(List<int>));
    }
    /// <summary>
    /// 验证空类型数组返回空集合单例。
    /// </summary>
    [Fact]
    public void Create_NullTypes_ReturnsEmptySingleton()
    {
        var result = TypesVal.Create(null);
        result.ShouldBeSameAs(TypesVal.Empty);
        result.IsEmpty().ShouldBeTrue();
    }
    /// <summary>
    /// 验证类型集合保存元素数量与顺序。
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
    /// 验证索引器拒绝负数及超出集合长度的索引。
    /// </summary>
    [Fact]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var result = TypesVal.Create(typeof(int));
        Should.Throw<ArgumentOutOfRangeException>(() => _ = result[-1]).ParamName.ShouldBe("index");
        Should.Throw<ArgumentOutOfRangeException>(() => _ = result[1]).ParamName.ShouldBe("index");
    }
    /// <summary>
    /// 验证修改返回数组不影响内部类型集合。
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
    /// 验证枚举结果保持全部类型的原始顺序。
    /// </summary>
    [Fact]
    public void Types_Enumeration_ReturnsAllTypesInOrder()
    {
        var result = TypesVal.Create(typeof(int), typeof(string), typeof(DateTime));
        result.Types.ToArray().ShouldBe([typeof(int), typeof(string), typeof(DateTime)]);
    }
}

