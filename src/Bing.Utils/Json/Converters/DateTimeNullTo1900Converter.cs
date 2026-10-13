using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// 日期时间转换器
/// </summary>
public class DateTimeNullTo1900Converter : JsonConverter
{
    /// <summary>
    /// 默认日期
    /// </summary>
    private static readonly DateTime DefaultDate = new(1900, 1, 1);

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTime) || objectType == typeof(DateTime?);

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var dateTime = value as DateTime?;
        if (dateTime == null || dateTime.Value == DefaultDate)
            writer.WriteNull();
        else
            writer.WriteValue(dateTime.Value);
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var isNullable = (objectType == typeof(DateTime?));
        if (reader.TokenType == JsonToken.Null)
            return isNullable ? (DateTime?)DefaultDate : DefaultDate;
        if (reader.TokenType == JsonToken.String)
        {
            var str = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(str))
                return isNullable ? (DateTime?)DefaultDate : DefaultDate;
            if (DateTime.TryParse(str, out var result))
                return result;
        }
        if (reader.TokenType == JsonToken.Date && reader.Value is DateTime dt)
            return dt;
        return isNullable ? (DateTime?)DefaultDate : DefaultDate;
    }
}
