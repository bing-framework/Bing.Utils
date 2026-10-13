using System.Data;
using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters.Internals;

/// <summary>
/// DataRow 转换器
/// </summary>
internal class DataRowConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        DataRow row = value as DataRow;
        writer.WriteStartObject();
        foreach (DataColumn column in row.Table.Columns)
        {
            writer.WritePropertyName(column.ColumnName);
            serializer.Serialize(writer, row[column]);
        }
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
        return typeof(DataRow).IsAssignableFrom(objectType);
    }
}
