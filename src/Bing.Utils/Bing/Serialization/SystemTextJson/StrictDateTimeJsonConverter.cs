using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bing.Serialization.SystemTextJson;
/// <summary>
/// 严格读取 ISO 8601 日期字符串并保留日期时间精度的 JSON 转换器。
/// </summary>
public sealed class StrictDateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadValue(ref reader);
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
    /// <summary>
    /// 将有效的 JSON 日期字符串解析为日期时间。
    /// </summary>
    /// <param name="reader">指向当前 JSON 值的读取器。</param>
    /// <returns>解析后的日期时间。</returns>
    /// <exception cref="JsonException">当前值不是有效的日期字符串。</exception>
    internal static DateTime ReadValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var value))
            throw new JsonException("日期时间必须为有效的 ISO 8601 字符串。");
        return value;
    }
}
