using System.Data;
using Bing.Utils.Json.Converters.Internals;
using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// DataTable 转换器
/// </summary>
public class DataTableConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        DataTable table = value as DataTable;
        DataRowConverter dataRowConverter = new DataRowConverter();
        writer.WriteStartObject();
        writer.WritePropertyName("Rows");
        writer.WriteStartArray();
        foreach (DataRow row in table.Rows)
        {
            dataRowConverter.WriteJson(writer, row, serializer);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return typeof(DataTable).IsAssignableFrom(objectType);
    }
}
