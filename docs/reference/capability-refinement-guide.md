# 工具能力收敛指南

本轮保持既有目标框架和默认表格格式。四项改进的实现与验证状态见 [执行记录](../quality/capability-refinement-execution.md)。

## 大内存流的小写入

`new LargeMemoryStream()` 现在默认使用 **64 KiB** 页面。空实例不租用数据页，首次写入只请求一页；页表从 4 个引用槽位开始按倍数增长。`SetLength` 增长的逻辑区域清零，缩容、清空及释放归还页面。

```csharp
using var smallWrites = new LargeMemoryStream();
using var largeBlocks = new LargeMemoryStream(1024 * 1024);
// 如确实依赖旧布局，可以显式指定旧页大小：
using var oldLayout = new LargeMemoryStream(1024000000);
```

默认布局变化经过授权。`GetSpan/GetMemory/ToArraySegment` 包含页表容量，可能包含空槽位，不能将页数组长度当成逻辑数据长度。通常使用 `GetWrittenSegments()` 获取恰好覆盖 `Length` 的页面片段。所有页面视图都借用池内存，修改、缩容、扩容或释放后不应继续依赖旧视图。显式指定旧页大小也不保证旧页表槽位数量。

## 表格 v2：编辑状态和逐行传输

`TabularJson` 默认仍写 v1；无参 `DataTableConverter/DataSetConverter` 仍写历史格式。显式选择 v2 才保存表内行的 `Added/Unchanged/Modified/Deleted`、`Original/Current/Proposed`、行错误和列错误。`DBNull.Value` 表示数据库空值，缺少的版本用 `null` 表示。

```csharp
var options = new TabularJsonOptions {
    Format = TabularJsonFormat.Version2,
    MaxInputBytes = 128L * 1024 * 1024,
    MaxSchemaBytes = 1024 * 1024,
    MaxRowBytes = 256 * 1024,
    MaxCellBytes = 64 * 1024,
    MaxRowsPerTable = 100000
};
await TabularJson.WriteDataSetAsync(output, dataSet, options,
    leaveOpen: true, cancellationToken: token);
using var restored = await TabularJson.ReadDataSetAsync(input, options,
    leaveOpen: true, cancellationToken: token);
```

恢复时先暂缓约束，重放各行版本，再恢复主外键、关系和只读配置。不会在最终返回前调用 `AcceptChanges` 或提交 Proposed。后续 `AcceptChanges/RejectChanges/CancelEdit/EndEdit` 可继续使用；读取失败不会返回部分 DataTable/DataSet。

需要保留 `EnforceConstraints=false` 时应使用 DataSet 整体入口；该配置属于数据集，单表完整装载会启用并验证表约束。

v2 仍使用明确的类型白名单，不根据 JSON 加载 CLR 类型。表达式、自定义对象列、非空 ExtendedProperties、脱离表的行仍不支持；事件、DataView、私有自增游标不是格式的一部分。

### 不构造完整表的读取与转写

```csharp
using var reader = await TabularJson.OpenReaderAsync(input, options,
    leaveOpen: true, cancellationToken: token);
await foreach (var row in reader.ReadRowsAsync(token)) {
    // row.TableIndex 标识所属表，版本数组及 byte[] 单元格已独立复制。
    Consume(row);
}
bool fullyRead = reader.IsComplete;
```

`OpenReaderAsync` 先读取结构，`ReadRowsAsync` 按需读取单条记录。也可以将 `reader.Schema` 和 `reader.ReadRowsAsync(token)` 交给 `WriteRowsAsync` 转写到另一个流；JSON `null` 的 `Schema` 为 null，需要调用方单独处理。自定义异步行源使用 `TabularRowRecord` 构造记录，`TabularSchema.FromTable/FromDataSet` 只捕获结构，不复制源行。

v2 外层字段顺序固定为 `$format`、`$version`、`schema`、`rows`、`counts`，结构使用不含数据行的 v1 结构模型。流读取要求此顺序，接受 UTF-8 BOM 和正常 JSON 空白。每条行记录包含表序号、状态、三个版本及错误；`counts` 验证每张表实际行数。不支持无穷行序列生成完整文档。

| 入口 | 内存与失败契约 |
|---|---|
| `Serialize/Deserialize`、JsonConverter | 缓冲完整 JSON 模型，支持 v2；不是逐行入口 |
| 默认 `ReadTableAsync/ReadDataSetAsync` | 为兼容自动识别格式，缓冲全文 |
| 显式 Version2 的 `ReadTableAsync/ReadDataSetAsync` | 逐行解析，但返回的完整表仍占用全部数据的内存 |
| 显式 Version2 的 `WriteTableAsync/WriteDataSetAsync` | 惰性捕获源行，逐行编码；调用期间不得并发修改源表 |
| `OpenReaderAsync/ReadRowsAsync` | 固定 4 KiB 预读缓冲，加结构与最大记录；单次枚举，失败不撤回已交付行 |
| `WriteRowsAsync` | 每次编码一条记录，写完才领取下一条；失败可能留下部分文档 |

所有流从当前位置读写，不复位、不截断，默认保留调用方流。`leaveOpen=false` 在进入处理后成功、失败或取消均释放资源；参数错误与开始前取消不接管流。读取器必须释放；提前停止枚举会释放读取器，`IsComplete` 保持 false。完整消费才校验尾部、行数和额外根内容；`IsComplete` 不代表跨行唯一性/外键已验证，这些约束在完整装载成表时验证。

`MaxRowBytes/MaxSchemaBytes` 在流读取缓冲时按原始记录字节限制；`MaxInputBytes` 包括空白、BOM、尾部及预读字节。`MaxCellBytes` 在该行解析后检查紧凑 JSON 单元格大小，必须配合行预算，不能单独充当原始输入内存上限。写入端先编码一条记录再检查其预算，不承诺阻止调用方提前构造的大对象。未配置预算表示不限制。

## 分片下载

固定工作循环直接执行重试，减少逐片 async 状态与闭包。合并过程边复制边验证长度和 SHA-256，避免完整分片摘要预读；所有检查通过后才提交临时目标，失败保留旧目标及分片。

分片复制现在按实际长度租用缓冲区，上限为 1 MiB；每路大分片使用一块，整次合并复用一块。此上限是减少 I/O 次数的内存取舍，不代表总工作集上限。同步刷盘和逐片文件提交仍有固定成本，不建议把大量微小范围作为吞吐优化手段。读取限额在底层读取前约束到剩余额度加一个探测字节，大缓冲不会先读完整个超限响应。

检查点仍逐片生成独立状态快照，在报告进度前持久化。状态文件直接序列化到临时流，保留刷盘和替换时点。公开回调可以持有或修改其快照，不会改变调度器状态。未降低摘要验证、状态保存次数或持久化强度。

状态快照仍随分片数量增长，因此大量微小分片会产生较高的检查点成本。本轮未引入增量日志：当前对照同时包含完整快照、刷盘及原子替换，尚不能把主要耗时单独归因于重复 JSON 序列化。详细数值与可比边界见 [性能证据](../quality/capability-refinement-performance.md)。

C02 后续写盘与合并优化的独立对照、资源取舍及验证结果见 [C02 I/O 报告](../quality/c02-io-performance.md)。

同步 FTP 完整模式见 [FTP 配置矩阵](ftp-capabilities.md)。
