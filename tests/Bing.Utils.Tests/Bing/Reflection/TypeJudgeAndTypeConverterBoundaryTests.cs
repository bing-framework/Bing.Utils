using System.Reflection;
using BingTypeConverter = Bing.Conversions.TypeConverter;

namespace Bing.Reflection;

/// <summary>
/// 验证类型判断器和类型转换器的 TypeInfo 空参数及非 nullable 边界契约。
/// </summary>
[Trait("Bing.Reflection", "TypeJudge.TypeInfoBoundary")]
public sealed class TypeJudgeAndTypeConverterBoundaryTests
{
    [Fact]
    public void TypeJudge_TypeInfoOverloads_Null_ThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsEnumType((TypeInfo)null!))
            .ParamName.ShouldBe("typeInfo");
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsEnumType((TypeInfo)null!, mayNullable: true))
            .ParamName.ShouldBe("typeInfo");
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsNumericType((TypeInfo)null!))
            .ParamName.ShouldBe("typeInfo");
        Should.Throw<ArgumentNullException>(() => TypeJudge.IsNullableType((TypeInfo)null!))
            .ParamName.ShouldBe("typeInfo");
    }

    [Fact]
    public void TypeConverter_NullArguments_ThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => BingTypeConverter.ToNonNullableType(null!))
            .ParamName.ShouldBe("type");
        Should.Throw<ArgumentNullException>(() => BingTypeConverter.ToNonNullableTypeInfo(null!))
            .ParamName.ShouldBe("typeInfo");
        Should.Throw<ArgumentNullException>(() => BingTypeConverter.ToSafeNonNullableType(null!))
            .ParamName.ShouldBe("type");
        Should.Throw<ArgumentNullException>(() => BingTypeConverter.ToSafeNonNullableTypeInfo(null!))
            .ParamName.ShouldBe("typeInfo");
    }

    [Fact]
    public void TypeConverter_ToNonNullableTypeInfo_NonNullableType_ReturnsNull()
    {
        BingTypeConverter.ToNonNullableTypeInfo(typeof(int).GetTypeInfo()).ShouldBeNull();
        BingTypeConverter.ToSafeNonNullableTypeInfo(typeof(int).GetTypeInfo())
            .ShouldBe(typeof(int).GetTypeInfo());
    }
}
