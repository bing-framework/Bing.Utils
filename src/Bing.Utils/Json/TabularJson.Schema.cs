using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

public static partial class TabularJson
{
    /// <summary>
    /// 将当前表结构和行快照编码为版本一对象。
    /// </summary>
    private static JObject WriteTableModel(DataTable table, IList<DataTable> tables, CancellationToken token, bool includeRows = true)
    {
        if (table.ExtendedProperties.Count != 0) throw new JsonSerializationException($"表 {table.TableName} 包含不支持的扩展属性。");
        var columns = new JArray();
        foreach (DataColumn column in table.Columns)
        {
            token.ThrowIfCancellationRequested();
            if (column.Expression.Length != 0 || column.ExtendedProperties.Count != 0)
                throw new JsonSerializationException($"列 {table.TableName}.{column.ColumnName} 包含表达式或扩展属性。");
            columns.Add(new JObject
            {
                ["name"] = column.ColumnName, ["type"] = TypeName(column.DataType), ["allowNull"] = column.AllowDBNull,
                ["caption"] = column.Caption, ["namespace"] = column.Namespace, ["prefix"] = column.Prefix,
                ["mapping"] = column.ColumnMapping.ToString(), ["readOnly"] = column.ReadOnly,
                ["maxLength"] = column.MaxLength, ["autoIncrement"] = column.AutoIncrement,
                ["seed"] = column.AutoIncrementSeed.ToString(CultureInfo.InvariantCulture),
                ["step"] = column.AutoIncrementStep.ToString(CultureInfo.InvariantCulture),
                ["dateTimeMode"] = column.DateTimeMode.ToString(), ["default"] = EncodeCell(column.DefaultValue, column.DataType)
            });
        }
        var rows = new JArray();
        foreach (DataRow row in includeRows ? table.Rows.Cast<DataRow>() : Enumerable.Empty<DataRow>())
        {
            token.ThrowIfCancellationRequested();
            if (row.RowState == DataRowState.Deleted) throw new JsonSerializationException($"表 {table.TableName} 包含已删除行。");
            var values = new JArray();
            foreach (DataColumn column in table.Columns) values.Add(EncodeCell(row[column], column.DataType));
            rows.Add(values);
        }
        var unique = new JArray();
        var foreign = new JArray();
        foreach (Constraint constraint in table.Constraints)
        {
            if (constraint.ExtendedProperties.Count != 0) throw new JsonSerializationException($"约束 {constraint.ConstraintName} 包含扩展属性。");
            if (constraint is UniqueConstraint u)
                unique.Add(new JObject { ["name"] = u.ConstraintName, ["columns"] = ColumnIndexes(u.Columns), ["primary"] = u.IsPrimaryKey });
            else if (constraint is ForeignKeyConstraint f)
                foreign.Add(new JObject
                {
                    ["name"] = f.ConstraintName, ["parentTable"] = TableIndex(tables, f.RelatedTable),
                    ["parentColumns"] = ColumnIndexes(f.RelatedColumns), ["columns"] = ColumnIndexes(f.Columns),
                    ["deleteRule"] = f.DeleteRule.ToString(), ["updateRule"] = f.UpdateRule.ToString(),
                    ["acceptRejectRule"] = f.AcceptRejectRule.ToString()
                });
            else throw new JsonSerializationException($"不支持约束 {constraint.GetType()}。");
        }
        return new JObject
        {
            ["name"] = table.TableName, ["namespace"] = table.Namespace, ["prefix"] = table.Prefix,
            ["locale"] = table.Locale.Name, ["caseSensitive"] = table.CaseSensitive, ["columns"] = columns,
            ["uniqueConstraints"] = unique, ["foreignKeys"] = foreign, ["rows"] = rows
        };
    }

    /// <summary>
    /// 编码列的有序序号。
    /// </summary>
    private static JArray ColumnIndexes(IEnumerable<DataColumn> columns) => new(columns.Select(c => c.Ordinal));

    /// <summary>
    /// 获取本次快照内的表序号，拒绝外部关系。
    /// </summary>
    private static int TableIndex(IList<DataTable> tables, DataTable table)
    {
        var index = tables.IndexOf(table);
        if (index < 0) throw new JsonSerializationException("表结构引用了快照之外的表，请序列化整个 DataSet。");
        return index;
    }

    /// <summary>
    /// 读取可选标量结构属性。
    /// </summary>
    private static T Property<T>(JObject node, string name, T fallback)
    {
        var value = node[name];
        if (value == null) return fallback;
        return Require<T>(value, typeof(T) == typeof(bool) ? JTokenType.Boolean : typeof(T) == typeof(string) ? JTokenType.String : JTokenType.Integer);
    }

    /// <summary>
    /// 读取并校验声明枚举，禁止任意数字或未知枚举值。
    /// </summary>
    private static T EnumProperty<T>(JObject node, string name, T fallback) where T : struct
    {
        var text = Property(node, name, fallback.ToString());
        if (!Enum.TryParse<T>(text, false, out var result) || !Enum.IsDefined(typeof(T), result) || result.ToString() != text)
            throw new JsonSerializationException($"字段 {node.Path}.{name} 的枚举值无效。");
        return result;
    }

    /// <summary>
    /// 创建未启用约束的表结构，行装载后再恢复只读属性。
    /// </summary>
    private static DataTable ReadTableSchema(JObject node, TabularJsonOptions options)
    {
        var definitions = Array(node["columns"]);
        Limit(definitions.Count, options.MaxColumnsPerTable, "列数量");
        var table = new DataTable(Require<string>(node["name"], JTokenType.String))
        {
            Namespace = Property(node, "namespace", ""), Prefix = Property(node, "prefix", ""),
            Locale = CultureInfo.GetCultureInfo(Property(node, "locale", "")), CaseSensitive = Property(node, "caseSensitive", false)
        };
        foreach (var definition in definitions)
        {
            var columnNode = Object(definition);
            var alias = Require<string>(columnNode["type"], JTokenType.String);
            if (!CellTypes.TryGetValue(alias, out var type)) throw new JsonSerializationException($"未知列类型 {alias}。");
            var name = Require<string>(columnNode["name"], JTokenType.String);
            if (name.Length == 0 || table.Columns.Cast<DataColumn>().Any(c => c.ColumnName == name))
                throw new JsonSerializationException($"列名为空或重复：{name}。");
            var column = new DataColumn(name, type)
            {
                AllowDBNull = Property(columnNode, "allowNull", true), Caption = Property(columnNode, "caption", name),
                Namespace = Property(columnNode, "namespace", ""), Prefix = Property(columnNode, "prefix", ""),
                ColumnMapping = EnumProperty(columnNode, "mapping", MappingType.Element)
            };
            var maximumLength = Property(columnNode, "maxLength", -1);
            if (maximumLength != -1) column.MaxLength = maximumLength;
            if (type == typeof(DateTime)) column.DateTimeMode = EnumProperty(columnNode, "dateTimeMode", DataSetDateTime.UnspecifiedLocal);
            column.AutoIncrementSeed = long.Parse(Property(columnNode, "seed", "0"), CultureInfo.InvariantCulture);
            column.AutoIncrementStep = long.Parse(Property(columnNode, "step", "1"), CultureInfo.InvariantCulture);
            column.AutoIncrement = Property(columnNode, "autoIncrement", false);
            if (column.DataType != type) throw new JsonSerializationException($"列 {name} 的类型不支持自增。");
            if (columnNode["default"] != null) column.DefaultValue = DecodeCell(columnNode["default"], type);
            table.Columns.Add(column);
        }
        return table;
    }

    /// <summary>
    /// 装载行值并附带明确的表名、行号诊断。
    /// </summary>
    private static void ReadRows(DataTable table, JObject model, TabularJsonOptions options, CancellationToken token, bool legacy)
    {
        var rows = Array(model[legacy ? "Rows" : "rows"]);
        Limit(rows.Count, options.MaxRowsPerTable, "行数量");
        for (var index = 0; index < rows.Count; index++)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var values = new object[table.Columns.Count];
                if (legacy)
                {
                    var row = Object(rows[index]);
                    if (row.Count != values.Length) throw new JsonSerializationException("行的字段数量与结构不一致。");
                    foreach (DataColumn column in table.Columns) values[column.Ordinal] = DecodeCell(row[column.ColumnName], column.DataType, true);
                }
                else
                {
                    var row = Array(rows[index]);
                    if (row.Count != values.Length) throw new JsonSerializationException("行的单元格数量与结构不一致。");
                    foreach (DataColumn column in table.Columns) values[column.Ordinal] = DecodeCell(row[column.Ordinal], column.DataType);
                }
                table.Rows.Add(values);
            }
            catch (Exception ex) when (ex is DataException || ex is JsonException || ex is ArgumentException || ex is OverflowException || ex is FormatException)
            {
                throw new JsonSerializationException($"表 {table.TableName} 第 {index + 1} 行读取失败：{ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 根据严格的有序列序号解析约束引用。
    /// </summary>
    private static DataColumn[] ReadColumns(DataTable table, JToken token)
    {
        var indexes = Array(token).Select(t => Require<int>(t, JTokenType.Integer)).ToArray();
        if (indexes.Length == 0 || indexes.Distinct().Count() != indexes.Length || indexes.Any(i => i < 0 || i >= table.Columns.Count))
            throw new JsonSerializationException($"表 {table.TableName} 的列引用无效。");
        return indexes.Select(i => table.Columns[i]).ToArray();
    }

    /// <summary>
    /// 解析数据集内部的表引用。
    /// </summary>
    private static DataTable ReferencedTable(IList<DataTable> tables, JToken token)
    {
        var index = Require<int>(token, JTokenType.Integer);
        if (index < 0 || index >= tables.Count) throw new JsonSerializationException("表引用越界。");
        return tables[index];
    }

    /// <summary>
    /// 在所有行装载后恢复唯一和外键约束，防止加载顺序影响结果。
    /// </summary>
    private static void RestoreConstraints(IList<DataTable> tables, JArray models)
    {
        for (var i = 0; i < tables.Count; i++)
        {
            var model = Object(models[i]);
            foreach (var item in Array(model["uniqueConstraints"] ?? new JArray()))
            {
                var node = Object(item);
                tables[i].Constraints.Add(new UniqueConstraint(Require<string>(node["name"], JTokenType.String),
                    ReadColumns(tables[i], node["columns"]), Property(node, "primary", false)));
            }
        }
        for (var i = 0; i < tables.Count; i++)
        {
            var model = Object(models[i]);
            foreach (var item in Array(model["foreignKeys"] ?? new JArray()))
            {
                var node = Object(item);
                var parent = ReferencedTable(tables, node["parentTable"]);
                var constraint = new ForeignKeyConstraint(Require<string>(node["name"], JTokenType.String),
                    ReadColumns(parent, node["parentColumns"]), ReadColumns(tables[i], node["columns"]))
                {
                    DeleteRule = EnumProperty(node, "deleteRule", Rule.Cascade),
                    UpdateRule = EnumProperty(node, "updateRule", Rule.Cascade),
                    AcceptRejectRule = EnumProperty(node, "acceptRejectRule", AcceptRejectRule.None)
                };
                tables[i].Constraints.Add(constraint);
            }
            var columns = Array(model["columns"]);
            for (var c = 0; c < columns.Count; c++) tables[i].Columns[c].ReadOnly = Property(Object(columns[c]), "readOnly", false);
        }
    }
}
