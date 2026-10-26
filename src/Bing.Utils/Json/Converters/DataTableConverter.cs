using System.Data;
using Bing.Utils.Json.Converters.Internals;
using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// DataTable 转换器
/// </summary>
public class DataTableConverter : JsonConverter
{
    /// <summary>
    /// 保存转换器格式及读取预算的独立配置。
    /// </summary>
    private readonly TabularJsonOptions _options;

    /// <summary>
    /// 初始化保留历史 Rows 写出格式的转换器。
    /// </summary>
    public DataTableConverter() : this(new TabularJsonOptions { Format = TabularJsonFormat.Legacy }) { }

    /// <summary>
    /// 初始化使用显式格式和历史结构的转换器。
    /// </summary>
    /// <param name="options">格式与读取预算；不能为 null。</param>
    public DataTableConverter(TabularJsonOptions options)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Snapshot();
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (_options.Format == TabularJsonFormat.Version2)
        {
            TabularJson.VersionModel(value, _options, default).WriteTo(writer);
            return;
        }
        if (_options.Format == TabularJsonFormat.Version1)
        {
            TabularJson.Model(value, default).WriteTo(writer);
            return;
        }
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
        if (objectType != typeof(DataTable)) throw new JsonSerializationException("双向表格格式只创建 DataTable，不创建其自定义派生类型。");
        return TabularJson.ReadFrom(reader, false, _options);
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return typeof(DataTable).IsAssignableFrom(objectType);
    }
}
