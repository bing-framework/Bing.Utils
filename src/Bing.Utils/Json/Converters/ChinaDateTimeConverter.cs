using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// 中国时间转换器
/// <para>
/// [JsonConverter(typeof(ChinaDateTimeConverter))]
/// public DateTime Birthday { get; set; }
/// </para>
/// </summary>
public class ChinaDateTimeConverter : DateTimeConverterBase
{
    /// <summary>
    /// 时间转换器
    /// </summary>
    private static readonly IsoDateTimeConverter DtConverter = new() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        DtConverter.WriteJson(writer, value, serializer);
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return DtConverter.ReadJson(reader, objectType, existingValue, serializer);
    }
}
