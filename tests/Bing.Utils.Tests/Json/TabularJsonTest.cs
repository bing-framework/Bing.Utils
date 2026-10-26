using System.Data;
using System.Globalization;
using System.Threading;
using Bing.Utils.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// 验证表格快照格式的精度、结构恢复与失败边界。
/// </summary>
public class TabularJsonTest
{
    /// <summary>
    /// 旧格式在显式结构下保持 decimal 精度和常见日期格式，并支持大 double。
    /// </summary>
    [Fact]
    public void LegacyTypedValuesPreservePrecision()
    {
        using var table = new DataTable("values");
        table.Columns.Add("Amount", typeof(decimal)); table.Columns.Add("Float", typeof(double));
        table.Columns.Add("Time", typeof(DateTime)); table.Columns[2].DateTimeMode = DataSetDateTime.Utc;
        var amount = 1.2345678901234567890123456789m;
        var date = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);
        table.Rows.Add(amount, double.MaxValue, date);
        var text = TabularJson.SerializeTable(table, new TabularJsonOptions { Format = TabularJsonFormat.Legacy });
        using var restored = TabularJson.DeserializeTable(text, new TabularJsonOptions { TableSchema = table });
        Assert.Equal(amount, restored.Rows[0][0]); Assert.Equal(double.MaxValue, restored.Rows[0][1]); Assert.Equal(date, restored.Rows[0][2]);
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new global::Bing.Utils.Json.Converters.DataTableConverter(new TabularJsonOptions { TableSchema = table }));
        using var throughConverter = JsonConvert.DeserializeObject<DataTable>(text, settings);
        Assert.Equal(amount, throughConverter.Rows[0][0]);
    }
    /// <summary>
    /// 提供包含边界值的类型矩阵。
    /// </summary>
    public static IEnumerable<object[]> Cells()
    {
        yield return new object[] { typeof(string), "中文\n\"" };
        yield return new object[] { typeof(bool), false };
        yield return new object[] { typeof(char), '\0' };
        yield return new object[] { typeof(byte), byte.MaxValue };
        yield return new object[] { typeof(sbyte), sbyte.MinValue };
        yield return new object[] { typeof(short), short.MinValue };
        yield return new object[] { typeof(ushort), ushort.MaxValue };
        yield return new object[] { typeof(int), int.MinValue };
        yield return new object[] { typeof(uint), uint.MaxValue };
        yield return new object[] { typeof(long), long.MinValue };
        yield return new object[] { typeof(ulong), ulong.MaxValue };
        yield return new object[] { typeof(decimal), decimal.MaxValue };
        yield return new object[] { typeof(decimal), 1.2345678901234567890123456789m };
        yield return new object[] { typeof(double), double.MaxValue };
        yield return new object[] { typeof(double), double.NaN };
        yield return new object[] { typeof(float), float.NegativeInfinity };
        yield return new object[] { typeof(DateTime), new DateTime(2026, 10, 23, 10, 5, 3, DateTimeKind.Utc).AddTicks(1234) };
        yield return new object[] { typeof(DateTimeOffset), new DateTimeOffset(2026, 10, 23, 10, 5, 3, TimeSpan.FromHours(8)).AddTicks(9876) };
        yield return new object[] { typeof(TimeSpan), TimeSpan.FromTicks(-1234567890123) };
        yield return new object[] { typeof(Guid), Guid.Parse("8da77a7c-0c5b-499f-a4bc-b11fd08eb82e") };
        yield return new object[] { typeof(byte[]), new byte[] { 0, 255, 1 } };
    }

    /// <summary>
    /// 各类型和数据库空值必须在不同区域设置下保持原值和类型。
    /// </summary>
    [Theory]
    [MemberData(nameof(Cells))]
    public void CellsRoundTrip(Type type, object value)
    {
        using var source = new DataTable("数据");
        var column = source.Columns.Add("值", type);
        if (type == typeof(DateTime)) column.DateTimeMode = DataSetDateTime.Utc;
        source.Rows.Add(new[] { value }); source.Rows.Add(new[] { DBNull.Value });
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var json = TabularJson.SerializeTable(source);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            using var restored = TabularJson.DeserializeTable(json);
            Assert.Equal(type, restored.Columns[0].DataType);
            if (value is byte[] bytes) Assert.Equal(bytes, (byte[])restored.Rows[0][0]);
            else Assert.Equal(value, restored.Rows[0][0]);
            Assert.Same(DBNull.Value, restored.Rows[1][0]);
            Assert.Equal(column.DateTimeMode, restored.Columns[0].DateTimeMode);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    /// <summary>
    /// 空表和全空列仍保留声明类型及表结构。
    /// </summary>
    [Fact]
    public void EmptyTablePreservesSchema()
    {
        using var source = new DataTable("schema") { Namespace = "urn:sample", CaseSensitive = true, Locale = CultureInfo.GetCultureInfo("tr-TR") };
        var id = source.Columns.Add("Id", typeof(int)); id.AutoIncrement = true; id.AutoIncrementSeed = 10; id.AutoIncrementStep = 2;
        source.PrimaryKey = new[] { id };
        var name = source.Columns.Add("Name", typeof(string)); name.MaxLength = 20; name.DefaultValue = "默认"; name.ReadOnly = true;
        using var result = TabularJson.DeserializeTable(TabularJson.SerializeTable(source));
        Assert.Empty(result.Rows.Cast<DataRow>());
        Assert.Equal("urn:sample", result.Namespace); Assert.True(result.CaseSensitive);
        Assert.Equal("tr-TR", result.Locale.Name); Assert.Single(result.PrimaryKey);
        Assert.Equal(10, result.Columns[0].AutoIncrementSeed); Assert.Equal(2, result.Columns[0].AutoIncrementStep);
        Assert.Equal(20, result.Columns[1].MaxLength); Assert.Equal("默认", result.Columns[1].DefaultValue); Assert.True(result.Columns[1].ReadOnly);
        Assert.Equal(10, result.Rows.Add()[0]);
    }

    /// <summary>
    /// 数据集恢复复合键、外键规则和关系，并继续执行约束。
    /// </summary>
    [Fact]
    public void DataSetRestoresRelationsAndConstraints()
    {
        using var source = new DataSet("orders");
        var parent = source.Tables.Add("parent"); parent.Columns.Add("Id", typeof(int)); parent.Columns.Add("Zone", typeof(string));
        parent.PrimaryKey = new[] { parent.Columns[0], parent.Columns[1] };
        var child = source.Tables.Add("child"); child.Columns.Add("Id", typeof(int)); child.Columns.Add("Zone", typeof(string));
        var relation = source.Relations.Add("items", parent.PrimaryKey, new[] { child.Columns[0], child.Columns[1] });
        relation.ChildKeyConstraint.DeleteRule = Rule.None;
        parent.Rows.Add(1, "a"); child.Rows.Add(1, "a");
        using var result = TabularJson.DeserializeDataSet(TabularJson.SerializeDataSet(source));
        Assert.Equal("orders", result.DataSetName); Assert.Single(result.Relations.Cast<DataRelation>());
        Assert.Equal(2, result.Tables[0].PrimaryKey.Length);
        Assert.Equal(Rule.None, result.Relations[0].ChildKeyConstraint.DeleteRule);
        Assert.Single(result.Tables[0].Rows[0].GetChildRows("items"));
        Assert.Throws<InvalidConstraintException>(() => result.Tables[1].Rows.Add(2, "a"));
    }

    /// <summary>
    /// 禁用约束的数据集按声明保留状态。
    /// </summary>
    [Fact]
    public void DisabledConstraintsRemainDisabled()
    {
        using var source = new DataSet { EnforceConstraints = false };
        var table = source.Tables.Add("t"); table.Columns.Add("id", typeof(int)); table.PrimaryKey = new[] { table.Columns[0] };
        table.Rows.Add(1); table.Rows.Add(1);
        using var result = TabularJson.DeserializeDataSet(TabularJson.SerializeDataSet(source));
        Assert.False(result.EnforceConstraints); Assert.Equal(2, result.Tables[0].Rows.Count);
    }

    /// <summary>
    /// 历史写出保持原格式，读取需要显式结构且不修改原表。
    /// </summary>
    [Fact]
    public void LegacyFormatRequiresSchema()
    {
        using var source = new DataTable("t"); source.Columns.Add("Id", typeof(int)); source.Rows.Add(1);
        var json = JsonConvert.SerializeObject(source, new global::Bing.Utils.Json.Converters.DataTableConverter());
        Assert.Equal("{\"Rows\":[{\"Id\":1}]}", json);
        Assert.Throws<JsonSerializationException>(() => TabularJson.DeserializeTable(json));
        using var result = TabularJson.DeserializeTable(json, new TabularJsonOptions { TableSchema = source });
        Assert.Single(source.Rows.Cast<DataRow>()); Assert.Equal(1, result.Rows[0][0]);
        var converter = new global::Bing.Utils.Json.Converters.DataTableConverter(new TabularJsonOptions());
        var versioned = JsonConvert.SerializeObject(source, converter);
        using var converted = JsonConvert.DeserializeObject<DataTable>(versioned, converter);
        Assert.Equal(1, converted.Rows[0][0]);
    }

    /// <summary>
    /// 拒绝重复属性、未知版本、类型注入和不完整行。
    /// </summary>
    [Theory]
    [InlineData("duplicate")]
    [InlineData("version")]
    [InlineData("type")]
    [InlineData("row")]
    public void MalformedInputIsRejected(string mutation)
    {
        using var table = new DataTable("t"); table.Columns.Add("Id", typeof(int)); table.Rows.Add(1);
        var model = JObject.Parse(TabularJson.SerializeTable(table));
        if (mutation == "version") model["$version"] = 99;
        if (mutation == "type") model["columns"][0]["type"] = "System.IO.FileInfo, System.Private.CoreLib";
        if (mutation == "row") ((JArray)model["rows"][0]).Add(2);
        var json = model.ToString();
        if (mutation == "duplicate") json = json.Replace("\"kind\": \"table\"", "\"kind\": \"table\", \"kind\": \"table\"");
        Assert.ThrowsAny<JsonException>(() => TabularJson.DeserializeTable(json));
    }

    /// <summary>
    /// 读取限额包含边界，超限不会返回部分表。
    /// </summary>
    [Fact]
    public void BudgetsAreEnforced()
    {
        using var source = new DataTable("t"); source.Columns.Add("x", typeof(int)); source.Rows.Add(1);
        var json = TabularJson.SerializeTable(source); var bytes = Encoding.UTF8.GetByteCount(json);
        using var result = TabularJson.DeserializeTable(json, new TabularJsonOptions { MaxInputBytes = bytes, MaxTables = 1, MaxColumnsPerTable = 1, MaxRowsPerTable = 1 });
        Assert.Throws<JsonSerializationException>(() => TabularJson.DeserializeTable(json, new TabularJsonOptions { MaxInputBytes = bytes - 1 }));
        Assert.Throws<JsonSerializationException>(() => TabularJson.DeserializeTable(json, new TabularJsonOptions { MaxRowsPerTable = 0 }));
        Assert.Throws<JsonSerializationException>(() => TabularJson.DeserializeTable(json, new TabularJsonOptions { MaxColumnsPerTable = 0 }));
        Assert.Throws<JsonSerializationException>(() => TabularJson.DeserializeTable(json, new TabularJsonOptions { MaxTables = 0 }));
    }

    /// <summary>
    /// 流入口保持位置和所有权，取消保留调用方令牌。
    /// </summary>
    [Fact]
    public async Task StreamOwnershipAndCancellation()
    {
        using var source = new DataTable("t"); source.Columns.Add("x", typeof(int)); source.Rows.Add(7);
        using var stream = new MemoryStream(); stream.WriteByte(42);
        await TabularJson.WriteTableAsync(stream, source);
        Assert.True(stream.CanWrite); stream.Position = 1;
        using var result = await TabularJson.ReadTableAsync(stream);
        Assert.Equal(7, result.Rows[0][0]); Assert.Equal(stream.Length, stream.Position);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TabularJson.ReadTableAsync(stream, leaveOpen: false, cancellationToken: canceled.Token));
        Assert.Equal(canceled.Token, ex.CancellationToken); Assert.True(stream.CanRead);
        stream.Position = 1;
        await TabularJson.ReadTableAsync(stream, leaveOpen: false);
        Assert.False(stream.CanRead);
    }

    /// <summary>
    /// 无法保存的结构和行状态不能静默丢失。
    /// </summary>
    [Fact]
    public void UnsupportedStateFailsExplicitly()
    {
        using var table = new DataTable("t"); table.Columns.Add("id", typeof(int)); table.Rows.Add(1); table.AcceptChanges(); table.Rows[0].Delete();
        Assert.Throws<JsonSerializationException>(() => TabularJson.SerializeTable(table));
        table.RejectChanges(); table.Columns.Add("calculated", typeof(int), "id + 1");
        Assert.Throws<JsonSerializationException>(() => TabularJson.SerializeTable(table));
        Assert.Equal("null", TabularJson.SerializeTable(null)); Assert.Null(TabularJson.DeserializeTable("null"));
        using var empty = TabularJson.DeserializeDataSet(TabularJson.SerializeDataSet(new DataSet("empty")));
        Assert.Empty(empty.Tables.Cast<DataTable>());
    }
}
