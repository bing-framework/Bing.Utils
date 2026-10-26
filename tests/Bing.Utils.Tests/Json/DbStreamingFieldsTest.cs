using System.Data;
using System.Threading;
using Bing.Utils.Json;

namespace Bing.Utils.Tests.Json;

public class DbStreamingFieldsTest
{
    [Fact]
    public async Task MetadataAndMultipleResultsAreSequential()
    {
        using var first = new DataTable("first");
        first.Columns.Add("id", typeof(int)).AllowDBNull = false;
        first.Columns.Add("name", typeof(string)).MaxLength = 12; first.Rows.Add(1, "a");
        using var second = new DataTable("second"); second.Columns.Add("flag", typeof(bool)); second.Rows.Add(true);
        using var reader = new DataTableReader(new[] { first, second });
        var columns = TabularDataReader.ReadSchema(reader);
        Assert.False(columns[0].AllowDBNull); Assert.Equal(12, columns[1].ColumnSize);
        var seen = new List<string>();
        await TabularDataReader.ForEachResultAsync(reader, async (index, current, token) =>
        { while (await current.ReadAsync(token)) seen.Add(index + ":" + current.GetValue(0)); }, leaveOpen: false);
        Assert.Equal(new[] { "0:1", "1:True" }, seen); Assert.True(reader.IsClosed);
    }
    [Fact]
    public async Task LargeFieldsCopyInBlocksAndRespectBudgets()
    {
        using var table = new DataTable("source"); table.Columns.Add("blob", typeof(byte[])); table.Columns.Add("text", typeof(string));
        var blob = Enumerable.Range(0, 100000).Select(x => (byte)x).ToArray(); var text = new string('字', 10000);
        async IAsyncEnumerable<TabularRowRecord> Rows()
        { await Task.Yield(); yield return new TabularRowRecord(0, DataRowState.Added, null, new object[] { blob, text }); }
        using var reader = TabularDataReader.CreateReader(TabularSchema.FromTable(table), Rows()); Assert.True(await reader.ReadAsync());
        using var output = new MemoryStream(); using var writer = new StringWriter();
        Assert.Equal(blob.Length, await TabularDataReader.CopyBinaryFieldAsync(reader, 0, output, blob.Length));
        Assert.Equal(blob, output.ToArray());
        Assert.Equal(text.Length, await TabularDataReader.CopyTextFieldAsync(reader, 1, writer, text.Length)); Assert.Equal(text, writer.ToString());
        output.SetLength(0); output.Position = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => TabularDataReader.CopyBinaryFieldAsync(reader, 0, output, 10)); Assert.InRange(output.Length, 0, 10);
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TabularDataReader.CopyTextFieldAsync(reader, 1, writer, cancellationToken: canceled.Token));
    }
}
