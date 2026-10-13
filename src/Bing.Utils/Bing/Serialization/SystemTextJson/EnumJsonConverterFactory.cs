using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bing.Serialization.SystemTextJson;

/// <summary>
/// 枚举Json转换器工厂
/// </summary>
public class EnumJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type type) => type.IsEnum;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(GetEnumConverterType(type));
    }

    /// <summary>
    /// 获取枚举Json转换器类型
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    private static Type GetEnumConverterType(Type enumType) => typeof(EnumJsonConverter<>).MakeGenericType(enumType);
}
