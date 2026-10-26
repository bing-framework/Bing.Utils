using System.Data;
using System.Text;
using System.Threading;
using Bing.Utils.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// 验证 v2 编辑版本、逐行处理与资源边界。
/// </summary>
public class TabularJsonVersion2Test
{
    /// <summary>
    /// 创建显式 v2 配置。
    /// </summary>
    private static TabularJsonOptions Options() => new() { Format = TabularJsonFormat.Version2 };

    /// <summary>
    /// v2 复用类型白名单并保留精度和数据库空值。
    /// </summary>
    [Theory]
    [MemberData(nameof(TabularJsonTest.Cells), MemberType = typeof(TabularJsonTest))]
    public async Task TypedCellsRoundTrip(Type type, object value)
    {
        using var table = new DataTable("typed"); var column = table.Columns.Add("Value", type);
        if (type == typeof(DateTime)) column.DateTimeMode = DataSetDateTime.Utc;
        table.Rows.Add(new[] { value }); table.Rows.Add(new[] { DBNull.Value });
        using var document = new MemoryStream();
        await TabularJson.WriteTableAsync(document, table, Options()); document.Position = 0;
        using var result = await TabularJson.ReadTableAsync(document, Options());
        if (value is byte[] bytes) Assert.Equal(bytes, (byte[])result.Rows[0][0]);
        else Assert.Equal(value, result.Rows[0][0]);
        Assert.Same(DBNull.Value, result.Rows[1][0]); Assert.Equal(DataRowState.Added, result.Rows[0].RowState);
    }

    /// <summary>
    /// 空表、空数据集和 null 均保留约定，字节预算包含边界。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyAndNullDocuments(bool withBom)
    {
        foreach (var text in new[] { "null", TabularJson.SerializeTable(new DataTable("empty"), Options()) })
        {
            var bytes = Encoding.UTF8.GetBytes((withBom ? "\uFEFF" : "") + text);
            using var input = new ShortInput(bytes);
            var options = Options(); options.MaxInputBytes = bytes.Length;
            using var restored = await TabularJson.ReadTableAsync(input, options);
            if (text == "null") Assert.Null(restored); else Assert.Empty(restored.Rows.Cast<DataRow>());
        }
        using var dataSet = new DataSet("empty"); using var output = new MemoryStream();
        await TabularJson.WriteDataSetAsync(output, dataSet, Options()); output.Position = 0;
        using var result = await TabularJson.ReadDataSetAsync(output, Options()); Assert.Empty(result.Tables.Cast<DataTable>());
    }

    /// <summary>
    /// 创建覆盖所有表内行状态和 Proposed 的表。
    /// </summary>
    private static DataTable EditedTable()
    {
        var table = new DataTable("edits"); table.Columns.Add("Id", typeof(int)); table.Columns.Add("Value", typeof(string));
        for (var i = 0; i < 4; i++) table.Rows.Add(i, "old");
        table.AcceptChanges();
        table.Rows[0].BeginEdit(); table.Rows[0][1] = "proposed";
        table.Rows[1][1] = "current"; table.Rows[1].BeginEdit(); table.Rows[1][1] = "next";
        table.Rows[2].Delete(); table.Rows[3].SetModified();
        var added = table.Rows.Add(4, "added"); added.BeginEdit(); added[1] = "pending";
        table.Rows[1].RowError = "row error"; table.Rows[1].SetColumnError(1, "column error");
        return table;
    }

    /// <summary>
    /// 完整恢复版本后接受、拒绝和取消编辑具有相同结果，序列化不改变源行。
    /// </summary>
    [Theory]
    [InlineData("none")]
    [InlineData("accept")]
    [InlineData("reject")]
    [InlineData("cancel")]
    [InlineData("end")]
    public void EditingOperationsRemainEquivalent(string operation)
    {
        using var source = EditedTable();
        var json = TabularJson.SerializeTable(source, Options());
        using var restored = TabularJson.DeserializeTable(json);
        Assert.Equal(json, TabularJson.SerializeTable(restored, Options()));
        Assert.True(source.Rows[0].HasVersion(DataRowVersion.Proposed));
        Assert.Equal(DataRowState.Deleted, restored.Rows[2].RowState);
        Assert.Equal(DataRowState.Modified, restored.Rows[3].RowState);
        foreach (var table in new[] { source, restored })
        {
            if (operation == "accept") table.AcceptChanges();
            if (operation == "reject") table.RejectChanges();
            if (operation == "cancel") foreach (DataRow row in table.Rows) row.CancelEdit();
            if (operation == "end") foreach (DataRow row in table.Rows) row.EndEdit();
        }
        Assert.Equal(TabularJson.SerializeTable(source, Options()), TabularJson.SerializeTable(restored, Options()));
    }

    /// <summary>
    /// 主外键变更与级联规则不会在恢复期间被重复触发。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RelationsAndReadOnlyColumnsSurvive(bool reject)
    {
        using var source = new DataSet("data");
        var parent = source.Tables.Add("parent"); parent.Columns.Add("Id", typeof(int));
        var child = source.Tables.Add("child"); child.Columns.Add("Parent", typeof(int));
        var relation = source.Relations.Add("relation", parent.Columns[0], child.Columns[0]);
        relation.ChildKeyConstraint.AcceptRejectRule = AcceptRejectRule.Cascade;
        parent.Rows.Add(1); child.Rows.Add(1); source.AcceptChanges();
        parent.Rows[0][0] = 2;
        parent.Columns[0].ReadOnly = true;
        using var stream = new MemoryStream();
        await TabularJson.WriteDataSetAsync(stream, source, Options()); stream.Position = 0;
        using var restored = await TabularJson.ReadDataSetAsync(stream, Options());
        Assert.True(restored.Tables[0].Columns[0].ReadOnly);
        Assert.Equal(1, restored.Tables[0].Rows[0][0, DataRowVersion.Original]);
        Assert.Equal(2, restored.Tables[1].Rows[0][0, DataRowVersion.Current]);
        if (reject) { source.RejectChanges(); restored.RejectChanges(); }
        else { source.AcceptChanges(); restored.AcceptChanges(); }
        Assert.Equal(TabularJson.SerializeDataSet(source, Options()), TabularJson.SerializeDataSet(restored, Options()));
    }

    /// <summary>
    /// 使用短读输入逐行产出，完整消费前不宣称文档完整。
    /// </summary>
    [Fact]
    public async Task ShortReadsYieldIndependentRowsBeforeEnd()
    {
        using var source = EditedTable();
        var bytes = Encoding.UTF8.GetBytes(TabularJson.SerializeTable(source, Options()));
        using var input = new ShortInput(bytes);
        using var reader = await TabularJson.OpenReaderAsync(input);
        Assert.Equal(1, reader.Schema.TableCount); Assert.False(reader.IsComplete);
        var records = new List<TabularRowRecord>();
        await foreach (var row in reader.ReadRowsAsync())
        {
            if (records.Count == 0) Assert.True(input.BytesRead < bytes.Length);
            records.Add(row);
        }
        Assert.True(reader.IsComplete); Assert.Equal(5, records.Count);
        Assert.Equal("proposed", records[0].Proposed[1]); Assert.Equal("next", records[1].Proposed[1]);
        Assert.True(input.CanRead);
    }

    /// <summary>
    /// 提前停止枚举按所有权策略释放源，且不能继续使用旧枚举。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EarlyStopHonorsOwnership(bool leaveOpen)
    {
        using var source = EditedTable();
        using var input = new ShortInput(Encoding.UTF8.GetBytes(TabularJson.SerializeTable(source, Options())));
        using var reader = await TabularJson.OpenReaderAsync(input, leaveOpen: leaveOpen);
        await foreach (var row in reader.ReadRowsAsync()) break;
        Assert.False(reader.IsComplete); Assert.Equal(leaveOpen, input.CanRead);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => { await foreach (var row in reader.ReadRowsAsync()) { } });
    }

    /// <summary>
    /// 流式写入先提交已有行再推进数据源，并保持 v2 编辑语义。
    /// </summary>
    [Fact]
    public async Task WriterDoesNotEnumerateAheadAndConverterRoundTrips()
    {
        using var table = EditedTable(); using var output = new MemoryStream();
        var schema = TabularSchema.FromTable(table);
        async IAsyncEnumerable<TabularRowRecord> Rows()
        {
            yield return TabularRowRecord.FromRow(table.Rows[0]);
            Assert.Contains("proposed", Encoding.UTF8.GetString(output.ToArray()));
            await Task.Yield(); yield return TabularRowRecord.FromRow(table.Rows[1]);
        }
        await TabularJson.WriteRowsAsync(output, schema, Rows());
        output.Position = 0;
        using var restored = await TabularJson.ReadTableAsync(output, Options());
        Assert.Equal(2, restored.Rows.Count); Assert.True(restored.Rows[1].HasVersion(DataRowVersion.Proposed));
        var settings = new JsonSerializerSettings(); settings.Converters.Add(new global::Bing.Utils.Json.Converters.DataTableConverter(Options()));
        var json = JsonConvert.SerializeObject(table, settings);
        using var converted = JsonConvert.DeserializeObject<DataTable>(json, settings);
        Assert.Equal(json, JsonConvert.SerializeObject(converted, settings));
    }

    /// <summary>
    /// 预算和损坏尾部正常失败，完整对象读取不返回半成品。
    /// </summary>
    [Theory]
    [InlineData("input")]
    [InlineData("schema")]
    [InlineData("row")]
    [InlineData("cell")]
    [InlineData("count")]
    [InlineData("truncated")]
    [InlineData("extra")]
    public async Task LimitsAndMalformedDataFail(string kind)
    {
        using var table = EditedTable();
        var text = TabularJson.SerializeTable(table, Options()); var options = Options();
        if (kind == "input") options.MaxInputBytes = 8;
        if (kind == "schema") options.MaxSchemaBytes = 16;
        if (kind == "row") options.MaxRowBytes = 16;
        if (kind == "cell") options.MaxCellBytes = 1;
        if (kind == "count") options.MaxRowsPerTable = 2;
        if (kind == "truncated") text = text.Substring(0, text.Length - 3);
        if (kind == "extra") text += "{}";
        using var input = new ShortInput(Encoding.UTF8.GetBytes(text));
        await Assert.ThrowsAnyAsync<JsonException>(() => TabularJson.ReadTableAsync(input, options, leaveOpen: false));
        Assert.False(input.CanRead);
    }

    /// <summary>
    /// 开始前取消不接管流，执行中取消则按既定策略收尾。
    /// </summary>
    [Fact]
    public async Task CancellationPreservesTokenAndResourceBoundary()
    {
        using var table = EditedTable(); using var cancel = new CancellationTokenSource();
        using var input = new ShortInput(Encoding.UTF8.GetBytes(TabularJson.SerializeTable(table, Options())));
        cancel.Cancel();
        var before = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TabularJson.OpenReaderAsync(input, leaveOpen: false, cancellationToken: cancel.Token));
        Assert.Equal(cancel.Token, before.CancellationToken); Assert.True(input.CanRead); Assert.Equal(0, input.BytesRead);
        using var executing = new CancellationTokenSource();
        using var reader = await TabularJson.OpenReaderAsync(input, leaveOpen: false, cancellationToken: executing.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in reader.ReadRowsAsync()) executing.Cancel();
        });
        Assert.False(input.CanRead);
    }

    /// <summary>
    /// 拒绝矛盾的行状态、版本及伪造行计数。
    /// </summary>
    [Theory]
    [InlineData("state")]
    [InlineData("counts")]
    [InlineData("original")]
    public void InvalidVersionsAreRejected(string kind)
    {
        using var table = EditedTable(); var model = JObject.Parse(TabularJson.SerializeTable(table, Options()));
        if (kind == "state") model["rows"][0]["state"] = "Deleted";
        if (kind == "counts") model["counts"][0] = 99;
        if (kind == "original") model["rows"][0]["original"][0] = 99;
        Assert.ThrowsAny<JsonException>(() => TabularJson.DeserializeTable(model.ToString()));
    }

    /// <summary>
    /// 行预算在超大字符串读完之前生效，非法 UTF-8 不会被替换为合法字符。
    /// </summary>
    [Fact]
    public async Task RawRowBudgetAndUtf8AreEnforcedBeforeDelivery()
    {
        using var table = new DataTable("large"); table.Columns.Add("Text", typeof(string));
        table.Rows.Add(new string('a', 100000));
        var text = TabularJson.SerializeTable(table, Options());
        using var input = new ShortInput(Encoding.UTF8.GetBytes(text));
        using var reader = await TabularJson.OpenReaderAsync(input, new TabularJsonOptions { MaxRowBytes = 128 });
        await Assert.ThrowsAnyAsync<JsonException>(async () => { await foreach (var row in reader.ReadRowsAsync()) { } });
        Assert.True(input.BytesRead < 4096);
        var bytes = Encoding.UTF8.GetBytes(text); bytes[System.Array.IndexOf(bytes, (byte)'a', text.IndexOf("\"rows\"", StringComparison.Ordinal))] = 255;
        using var invalid = new ShortInput(bytes);
        using var invalidReader = await TabularJson.OpenReaderAsync(invalid);
        await Assert.ThrowsAsync<DecoderFallbackException>(async () => { await foreach (var row in invalidReader.ReadRowsAsync()) { } });
    }

    /// <summary>
    /// 写入失败停止领取后续记录，并按既定策略关闭目标和释放枚举器。
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteFailureStopsRowsAndHonorsOwnership(bool leaveOpen)
    {
        using var table = EditedTable(); var yielded = 0; var disposed = false;
        using var output = new FailingOutput();
        async IAsyncEnumerable<TabularRowRecord> Rows()
        {
            try
            {
                yielded++; output.Fail = true; yield return TabularRowRecord.FromRow(table.Rows[0]);
                yielded++; await Task.Yield(); yield return TabularRowRecord.FromRow(table.Rows[1]);
            }
            finally { disposed = true; }
        }
        await Assert.ThrowsAsync<IOException>(() => TabularJson.WriteRowsAsync(output, TabularSchema.FromTable(table), Rows(), leaveOpen: leaveOpen));
        Assert.Equal(1, yielded); Assert.True(disposed); Assert.Equal(leaveOpen, output.CanWrite);
        Assert.True(output.ToArray().Length > 0);
    }

    /// <summary>
    /// 关闭约束的冲突状态和只读列上的未结束编辑可以恢复，且不提前提交编辑。
    /// </summary>
    [Fact]
    public async Task DisabledConstraintsAndReadOnlyProposedRemainEditable()
    {
        using var set = new DataSet { EnforceConstraints = false };
        var table = set.Tables.Add("conflicts"); table.Columns.Add("Id", typeof(int)); table.PrimaryKey = new[] { table.Columns[0] };
        table.Rows.Add(1); table.Rows.Add(1); table.AcceptChanges();
        table.Rows[0].BeginEdit(); table.Rows[0][0] = 2; table.Columns[0].ReadOnly = true;
        using var stream = new MemoryStream(); await TabularJson.WriteDataSetAsync(stream, set, Options()); stream.Position = 0;
        using var restored = await TabularJson.ReadDataSetAsync(stream, Options());
        Assert.False(restored.EnforceConstraints); Assert.True(restored.Tables[0].Columns[0].ReadOnly);
        Assert.Equal(2, restored.Tables[0].Rows[0][0, DataRowVersion.Proposed]);
        set.RejectChanges(); restored.RejectChanges();
        Assert.Equal(TabularJson.SerializeDataSet(set, Options()), TabularJson.SerializeDataSet(restored, Options()));
    }

    /// <summary>
    /// 转义输入按紧凑 JSON 值计数，原始行大小由独立行预算约束。
    /// </summary>
    [Fact]
    public async Task CellBudgetCountsCompactValueWhileRowBudgetCountsRawBytes()
    {
        using var table = new DataTable("escapes"); table.Columns.Add("Text", typeof(string)); table.Rows.Add("a");
        var text = TabularJson.SerializeTable(table, Options()).Replace("\"a\"", "\"\\u0061\"");
        using var input = new ShortInput(Encoding.UTF8.GetBytes(text));
        var options = Options(); options.MaxCellBytes = 3; options.MaxRowBytes = 256;
        using var restored = await TabularJson.ReadTableAsync(input, options);
        Assert.Equal("a", restored.Rows[0][0]);
    }

    /// <summary>
    /// 同一违反唯一约束的文档在文本和流入口均报告表格序列化错误。
    /// </summary>
    [Fact]
    public async Task ConstraintErrorsHaveConsistentExceptionType()
    {
        using var set = new DataSet { EnforceConstraints = false };
        var table = set.Tables.Add("duplicates"); table.Columns.Add("Id", typeof(int)); table.PrimaryKey = new[] { table.Columns[0] };
        table.Rows.Add(1); table.Rows.Add(1);
        var model = JObject.Parse(TabularJson.SerializeDataSet(set, Options())); model["schema"]["enforceConstraints"] = true;
        var text = model.ToString(Formatting.None);
        Assert.Throws<JsonSerializationException>(() => TabularJson.DeserializeDataSet(text));
        var input = new ThrowingDisposeStream(Encoding.UTF8.GetBytes(text));
        var error = await Assert.ThrowsAsync<JsonSerializationException>(() => TabularJson.ReadDataSetAsync(input, Options(), leaveOpen: false));
        Assert.IsAssignableFrom<DataException>(error.InnerException); Assert.Equal(1, input.Disposals);
    }

    /// <summary>
    /// 解析、写入和取消错误优先于资源清理错误；成功时清理错误仍传播。
    /// </summary>
    [Fact]
    public async Task CleanupDoesNotReplacePrimaryFailures()
    {
        var malformed = new ThrowingDisposeStream(Encoding.UTF8.GetBytes("{"));
        await Assert.ThrowsAnyAsync<JsonException>(() => TabularJson.OpenReaderAsync(malformed, leaveOpen: false));
        Assert.Equal(1, malformed.Disposals);
        using var table = EditedTable();
        var write = new ThrowingDisposeStream(System.Array.Empty<byte>()) { RejectWrites = true };
        var writeError = await Assert.ThrowsAsync<IOException>(() => TabularJson.WriteTableAsync(write, table, Options(), leaveOpen: false));
        Assert.Equal("write failed", writeError.Message); Assert.Equal(1, write.Disposals);
        using var cancel = new CancellationTokenSource();
        var source = new ThrowingDisposeStream(Encoding.UTF8.GetBytes(TabularJson.SerializeTable(table, Options())));
        using var reader = await TabularJson.OpenReaderAsync(source, leaveOpen: false, cancellationToken: cancel.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => { await foreach (var row in reader.ReadRowsAsync()) cancel.Cancel(); });
        Assert.Equal(1, source.Disposals);
        var successful = new ThrowingDisposeStream(Encoding.UTF8.GetBytes("null"));
        var closeError = await Assert.ThrowsAsync<IOException>(() => TabularJson.ReadTableAsync(successful, Options(), leaveOpen: false));
        Assert.Equal("dispose failed", closeError.Message);
    }

    /// <summary>
    /// 模拟处理失败和清理失败同时发生的调用方流。
    /// </summary>
    private sealed class ThrowingDisposeStream : MemoryStream
    {
        /// <summary>
        /// 获取释放次数。
        /// </summary>
        public int Disposals { get; private set; }
        /// <summary>
        /// 获取或设置是否拒绝写入。
        /// </summary>
        public bool RejectWrites { get; set; }
        /// <summary>
        /// 创建可读输入。
        /// </summary>
        public ThrowingDisposeStream(byte[] bytes) : base(bytes) { }
        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => RejectWrites ? Task.FromException(new IOException("write failed")) : base.WriteAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Disposals++; base.Dispose(disposing); throw new IOException("dispose failed");
        }
    }

    /// <summary>
    /// 在进入行枚举后模拟目标写入失败。
    /// </summary>
    private sealed class FailingOutput : MemoryStream
    {
        /// <summary>
        /// 获取或设置是否拒绝后续写入。
        /// </summary>
        public bool Fail { get; set; }
        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Fail ? Task.FromException(new IOException("write failed")) : base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <summary>
    /// 每次最多读取两个字节，并记录实际读取量。
    /// </summary>
    private sealed class ShortInput : MemoryStream
    {
        /// <summary>
        /// 获取实际读取的字节数。
        /// </summary>
        public long BytesRead { get; private set; }
        /// <summary>
        /// 创建短读源。
        /// </summary>
        public ShortInput(byte[] bytes) : base(bytes) { }
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            var read = await base.ReadAsync(buffer, offset, Math.Min(count, 2), token);
            BytesRead += read; return read;
        }
    }
}
