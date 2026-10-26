using System.Data;
using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters.Internals;

/// <summary>
/// DataRow 转换器
/// </summary>
internal class DataRowConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanRead => false;
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
        throw new NotSupportedException("行读取需要所属表的列结构，请使用 TabularJson 或 DataTableConverter。");
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return typeof(DataRow).IsAssignableFrom(objectType);
    }
}
