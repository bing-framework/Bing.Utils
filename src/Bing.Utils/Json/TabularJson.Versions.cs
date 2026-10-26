using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

public static partial class TabularJson
{
    /// <summary>
    /// 捕获结构而不复制或修改源行。
    /// </summary>
    internal static TabularSchema CaptureSchema(object value) => ParseSchema((JObject)Model(value, default, false), new TabularJsonOptions());

    /// <summary>
    /// 验证结构模型及预算，不允许把数据藏在结构中。
    /// </summary>
    internal static TabularSchema ParseSchema(JObject model, TabularJsonOptions options)
    {
        if ((string)model["$format"] != "bing.tabular" || (int?)model["$version"] != 1
            || (string)model["kind"] != "table" && (string)model["kind"] != "dataset")
            throw new JsonSerializationException("v2 结构模型无效。");
        var isSet = (string)model["kind"] == "dataset";
        var tables = isSet ? Array(model["tables"]) : new JArray(model.DeepClone());
        foreach (var table in tables)
            if (Array(table["rows"]).Count != 0) throw new JsonSerializationException("结构不能包含数据行。");
        var restored = Restore(model, isSet, options, default);
        if (restored is DataTable single) return new TabularSchema(model, new[] { single });
        using var set = (DataSet)restored;
        return new TabularSchema(model, set.Tables.Cast<DataTable>().Select(t => t.Clone()).ToArray());
    }

    /// <summary>
    /// 编码独立行，验证版本组合、列类型及单元格预算。
    /// </summary>
    internal static JObject EncodeRecord(TabularSchema schema, TabularRowRecord row, TabularJsonOptions options)
    {
        if (row == null) throw new JsonSerializationException("行记录不能为 null。");
        ValidateVersions(schema, row);
        var columns = schema.Tables[row.TableIndex].Columns;
        JToken Values(IReadOnlyList<object> values)
        {
            if (values == null) return JValue.CreateNull();
            var array = new JArray();
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value != DBNull.Value && value != null && value.GetType() != columns[i].DataType)
                    throw new JsonSerializationException($"表 {row.TableIndex} 列 {i} 类型不匹配。");
                var cell = EncodeCell(value, columns[i].DataType);
                Limit(Encoding.UTF8.GetByteCount(cell.ToString(Formatting.None)), options.MaxCellBytes, "单元格字节数");
                array.Add(cell);
            }
            return array;
        }
        return new JObject
        {
            ["table"] = row.TableIndex, ["state"] = row.State.ToString(),
            ["original"] = Values(row.Original), ["current"] = Values(row.Current), ["proposed"] = Values(row.Proposed),
            ["error"] = row.RowError,
            ["columnErrors"] = new JArray(row.ColumnErrors.OrderBy(p => p.Key).Select(p => new JArray(p.Key, p.Value)))
        };
    }

    /// <summary>
    /// 解码并验证单行；每个版本拥有独立值数组。
    /// </summary>
    internal static TabularRowRecord DecodeRecord(TabularSchema schema, JObject model, TabularJsonOptions options)
    {
        var index = Require<int>(model["table"], JTokenType.Integer);
        if (index < 0 || index >= schema.TableCount) throw new JsonSerializationException("表序号越界。");
        var state = EnumProperty(model, "state", DataRowState.Detached);
        var columns = schema.Tables[index].Columns;
        object[] Values(string name)
        {
            var node = model[name] ?? throw new JsonSerializationException($"缺少版本 {name}。");
            if (node.Type == JTokenType.Null) return null;
            var array = Array(node);
            if (array.Count != columns.Count) throw new JsonSerializationException("版本列数量不匹配。");
            var result = new object[array.Count];
            for (var i = 0; i < result.Length; i++)
            {
                Limit(Encoding.UTF8.GetByteCount(array[i].ToString(Formatting.None)), options.MaxCellBytes, "单元格字节数");
                result[i] = DecodeCell(array[i], columns[i].DataType);
            }
            return result;
        }
        var errors = new Dictionary<int, string>();
        foreach (var item in Array(model["columnErrors"]))
        {
            var pair = Array(item);
            if (pair.Count != 2) throw new JsonSerializationException("列错误记录无效。");
            var column = Require<int>(pair[0], JTokenType.Integer);
            if (errors.ContainsKey(column)) throw new JsonSerializationException("列错误序号重复。");
            errors.Add(column, Require<string>(pair[1], JTokenType.String));
        }
        if (state == DataRowState.Detached) throw new JsonSerializationException("不支持 Detached 行。");
        var record = new TabularRowRecord(index, state, Values("original"), Values("current"), Values("proposed"),
            Require<string>(model["error"], JTokenType.String), errors);
        ValidateVersions(schema, record);
        return record;
    }

    /// <summary>
    /// 校验行状态与版本存在性，拒绝无法恢复的组合。
    /// </summary>
    private static void ValidateVersions(TabularSchema schema, TabularRowRecord row)
    {
        if (row.TableIndex >= schema.TableCount) throw new JsonSerializationException("表序号越界。");
        var count = schema.Tables[row.TableIndex].Columns.Count;
        foreach (var values in new[] { row.Original, row.Current, row.Proposed })
        {
            if (values == null) continue;
            if (values.Count != count) throw new JsonSerializationException("版本列数量不匹配。");
            for (var i = 0; i < count; i++)
                if (values[i] != DBNull.Value && values[i] != null && values[i].GetType() != schema.Tables[row.TableIndex].Columns[i].DataType)
                    throw new JsonSerializationException("版本单元格类型不匹配。");
        }
        if ((row.Original != null) != (row.State != DataRowState.Added)
            || (row.Current != null) != (row.State != DataRowState.Deleted)
            || row.State == DataRowState.Deleted && row.Proposed != null)
            throw new JsonSerializationException("行状态与版本组合不一致。");
        if (row.State == DataRowState.Unchanged)
            for (var i = 0; i < count; i++)
            {
                var type = schema.Tables[row.TableIndex].Columns[i].DataType;
                var equal = JToken.DeepEquals(EncodeCell(row.Original[i], type), EncodeCell(row.Current[i], type));
                if (!equal) throw new JsonSerializationException("Unchanged 行的原始与当前版本必须相同。");
            }
        if (row.ColumnErrors.Keys.Any(i => i < 0 || i >= count)) throw new JsonSerializationException("列错误序号越界。");
    }

    /// <summary>
    /// 创建暂缓约束的装载目标，避免重放状态时触发级联修改。
    /// </summary>
    private static DataSet CreateVersionTarget(TabularSchema schema, TabularJsonOptions options)
    {
        var root = schema.Model;
        var result = new DataSet { EnforceConstraints = false };
        try
        {
            if (schema.IsDataSet)
            {
                result.DataSetName = (string)root["name"]; result.Namespace = Property(root, "namespace", "");
                result.Prefix = Property(root, "prefix", ""); result.Locale = CultureInfo.GetCultureInfo(Property(root, "locale", ""));
                result.CaseSensitive = Property(root, "caseSensitive", false);
            }
            foreach (var model in SchemaTables(schema)) result.Tables.Add(ReadTableSchema(Object(model), options));
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    /// <summary>
    /// 按原始版本、当前版本、编辑版本顺序恢复行，保持 Modified 的显式状态。
    /// </summary>
    private static void RestoreRecord(DataSet target, TabularRowRecord record)
    {
        var table = target.Tables[record.TableIndex];
        var row = table.NewRow();
        row.ItemArray = (record.Original ?? record.Current).ToArray();
        table.Rows.Add(row);
        if (record.State != DataRowState.Added) row.AcceptChanges();
        if (record.State == DataRowState.Modified)
        {
            row.ItemArray = record.Current.ToArray();
            if (row.RowState == DataRowState.Unchanged) row.SetModified();
        }
        if (record.State == DataRowState.Deleted) row.Delete();
        if (record.Proposed != null)
        {
            // ItemArray 的赋值会结束编辑；逐列写入才能保留 Proposed 和原 RowState。
            row.BeginEdit();
            for (var i = 0; i < record.Proposed.Count; i++) row[i] = record.Proposed[i];
        }
        row.RowError = record.RowError;
        foreach (var error in record.ColumnErrors) row.SetColumnError(error.Key, error.Value);
    }

    /// <summary>
    /// 恢复约束、关系及只读状态，不接受行更改或结束编辑。
    /// </summary>
    private static object FinishVersionTarget(DataSet target, TabularSchema schema)
    {
        var tables = target.Tables.Cast<DataTable>().ToList();
        RestoreConstraints(tables, SchemaTables(schema));
        if (schema.IsDataSet)
            foreach (var item in Array(schema.Model["relations"] ?? new JArray()))
            {
                var relation = Object(item);
                var parent = ReferencedTable(tables, relation["parentTable"]);
                var child = ReferencedTable(tables, relation["childTable"]);
                target.Relations.Add(new DataRelation((string)relation["name"], ReadColumns(parent, relation["parentColumns"]),
                    ReadColumns(child, relation["childColumns"]), false) { Nested = Property(relation, "nested", false) });
            }
        target.EnforceConstraints = !schema.IsDataSet || Property(schema.Model, "enforceConstraints", true);
        if (schema.IsDataSet) return target;
        var single = tables[0]; target.Tables.Remove(single); target.Dispose(); return single;
    }

    /// <summary>
    /// 获取按表序排列的结构模型。
    /// </summary>
    private static JArray SchemaTables(TabularSchema schema) => schema.IsDataSet ? (JArray)schema.Model["tables"] : new JArray(schema.Model.DeepClone());
}
