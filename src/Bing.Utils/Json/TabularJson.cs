using System.Data;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Json;

/// <summary>
/// 读写包含列类型、结构、约束及可选编辑版本的表格 JSON。
/// </summary>
/// <remarks>
/// 版本一不保存行编辑历史及自增内部计数；拒绝删除行、表达式、自定义对象列和扩展属性。
/// 版本二保存表内行状态、Original/Current/Proposed 和错误；不保存事件、DataView 或自增内部计数。
/// 完整对象读取在临时对象中完成。显式选择 Version2 或使用 OpenReaderAsync 可逐行解析；默认自动识别路径缓冲全文。
/// </remarks>
public static partial class TabularJson
{
    /// <summary>
    /// 将表的当前快照编码为 JSON；默认使用版本一。
    /// </summary>
    /// <param name="table">表；null 编码为 JSON null。</param>
    /// <param name="options">格式选项。</param>
    /// <returns>独立的 JSON 文本。</returns>
    public static string SerializeTable(DataTable table, TabularJsonOptions options = null) => Serialize(table, options);

    /// <summary>
    /// 将数据集的当前快照编码为 JSON；默认使用版本一。
    /// </summary>
    /// <param name="dataSet">数据集；null 编码为 JSON null。</param>
    /// <param name="options">格式选项。</param>
    /// <returns>独立的 JSON 文本。</returns>
    public static string SerializeDataSet(DataSet dataSet, TabularJsonOptions options = null) => Serialize(dataSet, options);

    /// <summary>
    /// 从 JSON 创建表，历史 Rows 输入必须提供 TableSchema。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    /// <param name="options">结构与读取预算。</param>
    /// <returns>新表；JSON null 返回 null。</returns>
    public static DataTable DeserializeTable(string json, TabularJsonOptions options = null) => (DataTable)Deserialize(json, false, options, default);

    /// <summary>
    /// 从 JSON 创建数据集，历史 Tables 输入必须提供 DataSetSchema。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    /// <param name="options">结构与读取预算。</param>
    /// <returns>新数据集；JSON null 返回 null。</returns>
    public static DataSet DeserializeDataSet(string json, TabularJsonOptions options = null) => (DataSet)Deserialize(json, true, options, default);

    /// <summary>
    /// 将表以 UTF-8 写入目标当前位置，默认保留流。
    /// </summary>
    /// <param name="destination">可写目标；不复位或截断。</param>
    /// <param name="table">要写入的表。</param>
    /// <param name="options">格式选项。</param>
    /// <param name="leaveOpen">是否保留目标；参数失败或开始前取消不接管流。</param>
    /// <param name="cancellationToken">取消令牌；失败可留下部分输出。</param>
    public static Task WriteTableAsync(Stream destination, DataTable table, TabularJsonOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
        => WriteAsync(destination, table, options, leaveOpen, cancellationToken);

    /// <summary>
    /// 将数据集以 UTF-8 写入目标当前位置，默认保留流。
    /// </summary>
    /// <param name="destination">可写目标；不复位或截断。</param>
    /// <param name="dataSet">要写入的数据集。</param>
    /// <param name="options">格式选项。</param>
    /// <param name="leaveOpen">是否保留目标；参数失败或开始前取消不接管流。</param>
    /// <param name="cancellationToken">取消令牌；失败可留下部分输出。</param>
    public static Task WriteDataSetAsync(Stream destination, DataSet dataSet, TabularJsonOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
        => WriteAsync(destination, dataSet, options, leaveOpen, cancellationToken);

    /// <summary>
    /// 从源当前位置读取 UTF-8 表格 JSON，不要求流可定位。
    /// </summary>
    /// <param name="source">可读源。</param>
    /// <param name="options">读取预算与历史结构。</param>
    /// <param name="leaveOpen">是否保留源；参数失败或开始前取消不接管流。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新表；JSON null 返回 null。</returns>
    public static async Task<DataTable> ReadTableAsync(Stream source, TabularJsonOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
        => (DataTable)await ReadAsync(source, false, options, leaveOpen, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 从源当前位置读取 UTF-8 数据集 JSON，不要求流可定位。
    /// </summary>
    /// <param name="source">可读源。</param>
    /// <param name="options">读取预算与历史结构。</param>
    /// <param name="leaveOpen">是否保留源；参数失败或开始前取消不接管流。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新数据集；JSON null 返回 null。</returns>
    public static async Task<DataSet> ReadDataSetAsync(Stream source, TabularJsonOptions options = null, bool leaveOpen = true, CancellationToken cancellationToken = default)
        => (DataSet)await ReadAsync(source, true, options, leaveOpen, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 编码新格式或显式选择的历史格式。
    /// </summary>
    private static string Serialize(object value, TabularJsonOptions options)
    {
        var snapshot = (options ?? new TabularJsonOptions()).Snapshot();
        if (snapshot.Format == TabularJsonFormat.Legacy)
            return JsonConvert.SerializeObject(value, new Converters.DataTableConverter(), new Converters.DataSetConverter());
        if (snapshot.Format == TabularJsonFormat.Version2)
            return VersionModel(value, snapshot, default).ToString(Formatting.None);
        return Model(value, default).ToString(Formatting.None);
    }

    /// <summary>
    /// 创建版本一模型，并将关系引用限定在快照内。
    /// </summary>
    internal static JToken Model(object value, CancellationToken token, bool includeRows = true)
    {
        token.ThrowIfCancellationRequested();
        if (value == null) return JValue.CreateNull();
        if (value is DataTable table)
        {
            var model = WriteTableModel(table, new[] { table }, token, includeRows);
            model.AddFirst(new JProperty("kind", "table"));
            model.AddFirst(new JProperty("$version", 1));
            model.AddFirst(new JProperty("$format", "bing.tabular"));
            return model;
        }
        var dataSet = (DataSet)value;
        if (dataSet.ExtendedProperties.Count != 0) throw new JsonSerializationException("数据集包含不支持的扩展属性。");
        var tables = dataSet.Tables.Cast<DataTable>().ToList();
        var models = new JArray(tables.Select(t => WriteTableModel(t, tables, token, includeRows)));
        var relations = new JArray();
        foreach (DataRelation relation in dataSet.Relations)
        {
            token.ThrowIfCancellationRequested();
            if (relation.ExtendedProperties.Count != 0) throw new JsonSerializationException($"关系 {relation.RelationName} 包含扩展属性。");
            relations.Add(new JObject
            {
                ["name"] = relation.RelationName, ["parentTable"] = TableIndex(tables, relation.ParentTable),
                ["childTable"] = TableIndex(tables, relation.ChildTable), ["parentColumns"] = ColumnIndexes(relation.ParentColumns),
                ["childColumns"] = ColumnIndexes(relation.ChildColumns), ["nested"] = relation.Nested
            });
        }
        return new JObject
        {
            ["$format"] = "bing.tabular", ["$version"] = 1, ["kind"] = "dataset", ["name"] = dataSet.DataSetName,
            ["namespace"] = dataSet.Namespace, ["prefix"] = dataSet.Prefix, ["locale"] = dataSet.Locale.Name,
            ["caseSensitive"] = dataSet.CaseSensitive, ["enforceConstraints"] = dataSet.EnforceConstraints,
            ["tables"] = models, ["relations"] = relations
        };
    }

    /// <summary>
    /// 读取带预算的文本并拒绝多个根值。
    /// </summary>
    private static object Deserialize(string json, bool dataSet, TabularJsonOptions options, CancellationToken token)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        var snapshot = (options ?? new TabularJsonOptions()).Snapshot();
        token.ThrowIfCancellationRequested();
        Limit(Encoding.UTF8.GetByteCount(json), snapshot.MaxInputBytes, "输入字节数");
        using var text = new StringReader(json);
        using var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None, MaxDepth = snapshot.MaxDepth };
        var result = ReadFrom(reader, dataSet, snapshot, token, true);
        if (reader.Read()) throw new JsonSerializationException("表格 JSON 包含多余根内容。");
        return result;
    }

    /// <summary>
    /// 从已有读取器恢复新对象；调用方提供的结构不会被修改。
    /// </summary>
    internal static object ReadFrom(JsonReader reader, bool dataSet, TabularJsonOptions options, CancellationToken token = default, bool byteBudgetVerified = false)
    {
        var snapshot = options.Snapshot();
        if (snapshot.MaxInputBytes.HasValue && !byteBudgetVerified)
            throw new JsonSerializationException("已有 JsonReader 无法统计原始 UTF-8 字节，请使用 TabularJson 文本或流入口执行字节预算。");
        var previousDates = reader.DateParseHandling;
        var previousDepth = reader.MaxDepth;
        JToken root;
        try
        {
            reader.DateParseHandling = DateParseHandling.None;
            reader.MaxDepth = previousDepth.HasValue ? Math.Min(previousDepth.Value, snapshot.MaxDepth) : snapshot.MaxDepth;
            root = snapshot.TableSchema != null || snapshot.DataSetSchema != null
                ? LoadLegacyModel(reader, snapshot.TableSchema, snapshot.DataSetSchema)
                : JToken.ReadFrom(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        finally { reader.DateParseHandling = previousDates; reader.MaxDepth = previousDepth; }
        token.ThrowIfCancellationRequested();
        if (root.Type == JTokenType.Null) return null;
        var model = Object(root);
        try
        {
            if (model["$format"] == null) return ReadLegacy(model, dataSet, snapshot, token);
            if ((string)model["$format"] == "bing.tabular" && model["$version"]?.Type == JTokenType.Integer && (int)model["$version"] == 2)
                return RestoreVersionModel(model, dataSet, snapshot, token);
            if (Require<string>(model["$format"], JTokenType.String) != "bing.tabular"
                || Require<int>(model["$version"], JTokenType.Integer) != 1
                || Require<string>(model["kind"], JTokenType.String) != (dataSet ? "dataset" : "table"))
                throw new JsonSerializationException("表格格式、版本或对象种类不支持。");
            return Restore(model, dataSet, snapshot, token);
        }
        catch (Exception ex) when (ex is DataException || ex is ArgumentException || ex is OverflowException || ex is FormatException || ex is InvalidOperationException)
        {
            throw new JsonSerializationException($"表格结构或数据无效：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 先恢复全部表和行，再恢复约束、关系与约束启用状态。
    /// </summary>
    private static object Restore(JObject root, bool isDataSet, TabularJsonOptions options, CancellationToken token)
    {
        var models = isDataSet ? Array(root["tables"]) : new JArray(root.DeepClone());
        Limit(models.Count, options.MaxTables, "表数量");
        var result = new DataSet { EnforceConstraints = false };
        try
        {
            if (isDataSet)
            {
                result.DataSetName = Require<string>(root["name"], JTokenType.String);
                result.Namespace = Property(root, "namespace", ""); result.Prefix = Property(root, "prefix", "");
                result.Locale = CultureInfo.GetCultureInfo(Property(root, "locale", ""));
                result.CaseSensitive = Property(root, "caseSensitive", false);
            }
            var tables = new List<DataTable>();
            foreach (var item in models)
            {
                token.ThrowIfCancellationRequested();
                var model = Object(item);
                var table = ReadTableSchema(model, options);
                result.Tables.Add(table); tables.Add(table);
                ReadRows(table, model, options, token, false);
            }
            RestoreConstraints(tables, models);
            if (isDataSet)
            {
                foreach (var item in Array(root["relations"] ?? new JArray()))
                {
                    var relation = Object(item);
                    var parent = ReferencedTable(tables, relation["parentTable"]);
                    var child = ReferencedTable(tables, relation["childTable"]);
                    result.Relations.Add(new DataRelation(Require<string>(relation["name"], JTokenType.String),
                        ReadColumns(parent, relation["parentColumns"]), ReadColumns(child, relation["childColumns"]), false)
                    { Nested = Property(relation, "nested", false) });
                }
            }
            token.ThrowIfCancellationRequested();
            result.EnforceConstraints = !isDataSet || Property(root, "enforceConstraints", true);
            result.AcceptChanges();
            if (isDataSet) return result;
            var standalone = tables[0];
            result.Tables.Remove(standalone);
            standalone.TableName = Require<string>(root["name"], JTokenType.String);
            result.Dispose();
            return standalone;
        }
        catch { result.Dispose(); throw; }
    }

    /// <summary>
    /// 按显式历史结构读取 Rows/Tables，绝不推测列类型。
    /// </summary>
    private static object ReadLegacy(JObject root, bool isDataSet, TabularJsonOptions options, CancellationToken token)
    {
        if (isDataSet && options.DataSetSchema == null || !isDataSet && options.TableSchema == null)
            throw new JsonSerializationException("历史表格格式缺少列类型；请提供 TableSchema/DataSetSchema 或使用版本一。");
        var models = isDataSet ? Array(root["Tables"]) : new JArray(root.DeepClone());
        Limit(models.Count, options.MaxTables, "表数量");
        var result = isDataSet ? options.DataSetSchema.Clone() : new DataSet();
        try
        {
            var enforce = isDataSet ? options.DataSetSchema.EnforceConstraints : true;
            result.EnforceConstraints = false;
            if (!isDataSet) result.Tables.Add(options.TableSchema.Clone());
            if (result.Tables.Count != models.Count) throw new JsonSerializationException("历史表数量与提供的结构不一致。");
            for (var i = 0; i < models.Count; i++)
            {
                var table = result.Tables[i];
                Limit(table.Columns.Count, options.MaxColumnsPerTable, "列数量");
                var readOnly = table.Columns.Cast<DataColumn>().Select(c => c.ReadOnly).ToArray();
                foreach (DataColumn column in table.Columns)
                {
                    TypeName(column.DataType);
                    if (column.Expression.Length != 0) throw new JsonSerializationException("历史结构不支持表达式列。");
                    column.ReadOnly = false;
                }
                ReadRows(table, Object(models[i]), options, token, true);
                for (var c = 0; c < readOnly.Length; c++) table.Columns[c].ReadOnly = readOnly[c];
            }
            result.EnforceConstraints = enforce;
            result.AcceptChanges();
            if (isDataSet) return result;
            var standalone = result.Tables[0];
            result.Tables.Remove(standalone);
            standalone.TableName = options.TableSchema.TableName;
            result.Dispose(); return standalone;
        }
        catch { result.Dispose(); throw; }
    }

    /// <summary>
    /// 将新格式模型异步写入调用方流。
    /// </summary>
    private static async Task WriteAsync(Stream destination, object value, TabularJsonOptions options, bool leaveOpen, CancellationToken token)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException("目标不可写。", nameof(destination));
        var snapshot = (options ?? new TabularJsonOptions()).Snapshot();
        token.ThrowIfCancellationRequested();
        var failed = false;
        try
        {
            if (snapshot.Format == TabularJsonFormat.Version2 && value != null)
            {
                await WriteRowsAsync(destination, CaptureSchema(value), Records(value), snapshot, true, token).ConfigureAwait(false);
                return;
            }
            using var writer = new StreamWriter(destination, new UTF8Encoding(false, true), 4096, true);
            if (snapshot.Format == TabularJsonFormat.Legacy)
            {
                // 旧数字文本不能经 JToken.Parse 的 double 默认解析中转。
                var text = Serialize(value, snapshot);
                token.ThrowIfCancellationRequested();
                await writer.WriteAsync(text).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                return;
            }
            using var json = new JsonTextWriter(writer) { CloseOutput = false };
            var model = Model(value, token);
            await model.WriteToAsync(json, token).ConfigureAwait(false);
            await json.FlushAsync(token).ConfigureAwait(false);
        }
        catch { failed = true; throw; }
        finally { if (!leaveOpen) DisposeResource(destination, failed); }
    }

    /// <summary>
    /// 有界读取并解码 UTF-8，短读不代表流结束。
    /// </summary>
    private static async Task<object> ReadAsync(Stream source, bool dataSet, TabularJsonOptions options, bool leaveOpen, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (!source.CanRead) throw new ArgumentException("源不可读。", nameof(source));
        var snapshot = (options ?? new TabularJsonOptions()).Snapshot();
        token.ThrowIfCancellationRequested();
        if (snapshot.Format == TabularJsonFormat.Version2)
            return await ReadVersionAsync(source, dataSet, snapshot, leaveOpen, token).ConfigureAwait(false);
        try
        {
            using var buffer = new MemoryStream();
            var bytes = new byte[8192];
            int read;
            while ((read = await source.ReadAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false)) != 0)
            {
                token.ThrowIfCancellationRequested();
                Limit(buffer.Length + read, snapshot.MaxInputBytes, "输入字节数");
                buffer.Write(bytes, 0, read);
            }
            var text = new UTF8Encoding(false, true).GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
            if (text.Length != 0 && text[0] == '\uFEFF') text = text.Substring(1);
            return Deserialize(text, dataSet, snapshot, token);
        }
        finally { if (!leaveOpen) source.Dispose(); }
    }
}
