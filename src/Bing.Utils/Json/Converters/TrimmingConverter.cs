using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// 清除空格转换器
/// </summary>
public class TrimmingConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var text = value as string;
        if (text == null)
            writer.WriteNull();
        else
            writer.WriteValue(text.Trim());
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
            if (reader.Value != null)
                return (reader.Value as string)?.Trim();
        return reader.Value;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => objectType == typeof(string);
}
