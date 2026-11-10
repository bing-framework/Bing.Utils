using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bing.Serialization.SystemTextJson;
/// <summary>
/// 严格读取可空 ISO 8601 日期字符串的 JSON 转换器。
/// </summary>
public sealed class StrictNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : StrictDateTimeJsonConverter.ReadValue(ref reader);
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
