using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

public static partial class TabularJson
{
    /// <summary>
    /// 按显式旧结构保留 decimal 原始数字文本，避免经 double 中转丢失精度。
    /// </summary>
    private static JToken LoadLegacyModel(JsonReader reader, DataTable table, DataSet dataSet,
        bool rows = false, bool row = false, bool tables = false)
    {
        if (reader.TokenType == JsonToken.None && !ReadNext(reader)) throw new JsonSerializationException("JSON 内容为空。");
        if (reader.TokenType == JsonToken.StartObject)
        {
            var result = new JObject();
            while (ReadNext(reader) && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName) throw new JsonSerializationException("需要对象字段。");
                var name = (string)reader.Value;
                if (result.Property(name) != null) throw new JsonSerializationException($"重复字段 {name}。");
                if (row && table?.Columns.Contains(name) == true && table.Columns[name].DataType == typeof(decimal))
                    reader.ReadAsString();
                else if (!ReadNext(reader)) throw new JsonSerializationException("字段缺少值。");
                result.Add(name, LoadLegacyModel(reader, table, dataSet,
                    rows: !row && name == "Rows" && table != null,
                    tables: name == "Tables" && dataSet != null));
            }
            if (reader.TokenType != JsonToken.EndObject) throw new JsonSerializationException("对象未结束。");
            return result;
        }
        if (reader.TokenType == JsonToken.StartArray)
        {
            var result = new JArray();
            while (ReadNext(reader) && reader.TokenType != JsonToken.EndArray)
            {
                var currentTable = tables && result.Count < dataSet.Tables.Count ? dataSet.Tables[result.Count] : table;
                result.Add(LoadLegacyModel(reader, currentTable, null, row: rows));
            }
            if (reader.TokenType != JsonToken.EndArray) throw new JsonSerializationException("数组未结束。");
            return result;
        }
        if (reader.TokenType == JsonToken.Null) return JValue.CreateNull();
        if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Boolean ||
            reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
            return new JValue(reader.Value);
        throw new JsonSerializationException($"不支持的 JSON 标记 {reader.TokenType}。");
    }

    /// <summary>
    /// 跳过注释，沿用 JsonReader 的深度和格式校验。
    /// </summary>
    private static bool ReadNext(JsonReader reader)
    {
        while (reader.Read()) if (reader.TokenType != JsonToken.Comment) return true;
        return false;
    }
}
