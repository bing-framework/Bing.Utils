using Bing.Diagnostics;
using Bing.Operations;

namespace Bing.Utils.Tests.Bing.Operations;

/// <summary>
/// 验证统一进度和数据定位公共类型的边界约束。
/// </summary>
public class OperationProgressAndLocationTest
{
    /// <summary>
    /// 进度快照保留阶段、单位、总量和事件状态。
    /// </summary>
    [Fact]
    public void ProgressSnapshotStoresValues()
    {
        var progress = new OperationProgress(OperationStage.Write, ProgressUnit.Byte, 5, 10, 2, 7, true);

        Assert.Equal(OperationStage.Write, progress.Stage);
        Assert.Equal(ProgressUnit.Byte, progress.Unit);
        Assert.Equal(5, progress.Completed);
        Assert.Equal(10, progress.Total);
        Assert.Equal(2, progress.Attempt);
        Assert.Equal(7, progress.Sequence);
        Assert.True(progress.IsFinal);
    }

    /// <summary>
    /// 进度快照拒绝负值和超过总量的完成数量。
    /// </summary>
    [Fact]
    public void ProgressSnapshotValidatesBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationProgress(OperationStage.Read, ProgressUnit.Record, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationProgress(OperationStage.Read, ProgressUnit.Record, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationProgress(OperationStage.Read, ProgressUnit.Record, 0, attempt: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationProgress(OperationStage.Read, ProgressUnit.Record, 0, sequence: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationProgress((OperationStage)99, ProgressUnit.Record, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationProgress(OperationStage.Read, (ProgressUnit)99, 0));

        var overReportedTotal = new OperationProgress(OperationStage.Write, ProgressUnit.Byte, 11, total: 10);
        Assert.Equal(11, overReportedTotal.Completed);
        Assert.Equal(10, overReportedTotal.Total);
    }

    /// <summary>
    /// 不同格式定位保留各自坐标语义。
    /// </summary>
    [Fact]
    public void LocationsPreserveFormatCoordinates()
    {
        var csv = new CsvLocation(3, 2, "姓名");
        var tabular = new TabularLocation(4, 0, "Id");
        var pointer = new JsonPointerLocation("/users/0/name");
        var syntax = new JsonSyntaxLocation(1, 5);
        var cell = new WorksheetCellLocation("Sheet1", 2, 3);

        Assert.Equal("csv", csv.Kind);
        Assert.Equal(3, csv.RecordNumber);
        Assert.Equal(2, csv.ColumnNumber);
        Assert.Equal("姓名", csv.ColumnName);
        Assert.Equal(4, tabular.RowNumber);
        Assert.Equal(0, tabular.Ordinal);
        Assert.Equal("/users/0/name", pointer.InstancePath);
        Assert.Equal(1, syntax.LineNumber);
        Assert.Equal(5, syntax.BytePositionInLine);
        Assert.Equal("Sheet1", cell.SheetName);
    }
}
