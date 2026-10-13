using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// 长整型转字符串转换器
/// </summary>
public class LongToStringConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.ToString());
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return JToken.ReadFrom(reader).Value<long>();
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return typeof(long) == objectType;
    }
}
