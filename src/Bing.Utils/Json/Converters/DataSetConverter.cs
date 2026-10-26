using System.Data;
using Newtonsoft.Json;

namespace Bing.Utils.Json.Converters;

/// <summary>
/// DataSet 转换器
/// </summary>
public class DataSetConverter : JsonConverter
{
    /// <summary>
    /// 保存转换器格式及读取预算的独立配置。
    /// </summary>
    private readonly TabularJsonOptions _options;

    /// <summary>
    /// 初始化保留历史 Tables 写出格式的转换器。
    /// </summary>
    public DataSetConverter() : this(new TabularJsonOptions { Format = TabularJsonFormat.Legacy }) { }

    /// <summary>
    /// 初始化使用显式格式和历史结构的转换器。
    /// </summary>
    /// <param name="options">格式与读取预算；不能为 null。</param>
    public DataSetConverter(TabularJsonOptions options)
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
        if (objectType != typeof(DataSet)) throw new JsonSerializationException("双向表格格式只创建 DataSet，不创建其自定义派生类型。");
        return TabularJson.ReadFrom(reader, true, _options);
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return typeof(DataSet).IsAssignableFrom(objectType);
    }
}
