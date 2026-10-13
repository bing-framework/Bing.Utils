using System.Data;
using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// DataSet 转换器
/// </summary>
public class DataSetConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        DataSet dataSet = value as DataSet;
        DataTableConverter dataTableConverter = new DataTableConverter();
        writer.WriteStartObject();
        writer.WritePropertyName("Tables");
        writer.WriteStartArray();
        foreach (DataTable table in dataSet.Tables)
        {
            dataTableConverter.WriteJson(writer, table, serializer);
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
        return typeof(DataSet).IsAssignableFrom(objectType);
    }
}
