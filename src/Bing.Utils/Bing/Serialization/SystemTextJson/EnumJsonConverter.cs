using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bing.Serialization.SystemTextJson;

/// <summary>
/// 枚举Json转换器
/// </summary>
/// <typeparam name="T">枚举类型</typeparam>
public class EnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    /// <summary>
    /// 读取数据
    /// </summary>
    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var isSuccess = reader.TryGetSingle(out var floatValue);
        if (isSuccess)
            return (T)Enum.Parse(type, floatValue.ToString(CultureInfo.InvariantCulture), true);
        isSuccess = reader.TryGetInt32(out var intValue);
        if (isSuccess)
            return (T)Enum.Parse(type, intValue.ToString(CultureInfo.InvariantCulture), true);
        return default;
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var code = Type.GetTypeCode(typeof(T));
        switch (code)
        {
            case TypeCode.Byte:
                writer.WriteNumberValue(Unsafe.As<T, byte>(ref value));
                break;
            case TypeCode.Int32:
                writer.WriteNumberValue(Unsafe.As<T, int>(ref value));
                break;
        }
    }
}