# 数据库读取器与表格流

`TabularDataReader` 在 `DbDataReader` 与 Bing.Utils 的逐行表格记录之间流式转换。它只读取当前结果集，不会自动调用 `NextResult()`。

## 选择输出格式

需要保留列类型、版本化表格结构并可由 `TabularJson.ReadTableAsync` 恢复时，写入 v2：

```csharp
await using var reader = await command.ExecuteReaderAsync(cancellationToken);
await using var output = File.Create(path);
await TabularDataReader.WriteTabularAsync(
    output, reader, new TabularJsonOptions { Format = TabularJsonFormat.Version2 },
    leaveReaderOpen: true, leaveStreamOpen: true, cancellationToken);
```

与普通系统集成交换对象数组时，可写紧凑 JSON 数组：

```csharp
await TabularDataReader.WriteJsonArrayAsync(output, reader, cancellationToken: cancellationToken);
```

逐行消费日志或管道格式时，可写 NDJSON，每个结果行占一行：

```csharp
await TabularDataReader.WriteNdjsonAsync(output, reader, cancellationToken: cancellationToken);
```

普通 JSON 使用 `System.Text.Json` 默认语义，按列名输出对象，`DBNull.Value` 输出为 `null`。整数和 decimal 以其 CLR 数值类型序列化。NDJSON 不接受缩进选项。任何流式写入失败都可能在目标留下部分内容。

## 资源与读取位置

`CreateSchema` 只读取当前结果集的字段元数据，不推进读取器，也不要求先缓存行。重复或空列名会被拒绝；列类型必须属于 TabularJson 的类型白名单。

`ReadRowsAsync` 从读取器的下一行开始读取，每条记录的 `TableIndex` 为 `0`、状态为 `Added`，只提供 `Current` 值；`DBNull` 保留为数据库空值，`byte[]` 会复制。行序列只能枚举一次。`leaveOpen: false` 时，正常结束、提前停止、读取失败或取消都会关闭读取器；调用方法前已取消的令牌不会接管读取器。

写入方法从目标流的当前位置开始，不复位或截断目标。`leaveReaderOpen` 与 `leaveStreamOpen` 分别控制两个资源；参数验证失败或开始前取消不会接管资源。方法正常完成后，读取器位于当前结果集末尾。写入错误或取消可能留下部分文档、数组或 NDJSON 行。

## 反向适配

可将单表结构和异步行记录包装为只读 `DbDataReader`：

```csharp
var schema = TabularSchema.FromTable(table);
await using DbDataReader reader = TabularDataReader.CreateReader(schema, rows, cancellationToken);
while (await reader.ReadAsync(cancellationToken))
{
    var id = reader.GetInt64(0);
    var value = reader.IsDBNull(1) ? null : reader.GetString(1);
}
```

反向读取器只公开每行的 `Current` 版本，并拒绝 Deleted 行、多表结构和非零表索引。它按需读取，不会预先缓存所有行。优先使用 `ReadAsync`；同步 `Read()` 和 `HasRows` 会等待异步行源，在依赖调用方同步上下文的环境中可能阻塞。调用方关闭读取器即可取消并释放当前异步枚举器。

反向读取器的 `NextResult()` / `NextResultAsync()` 返回 false，并结束当前结果集、释放剩余行枚举器；之后 `Read` 不再返回旧行。读取器不支持并发读取或读取过程中并发关闭。生命周期取消后，即使行源忽略令牌，也不会交付后续行；仍需等待该次行源操作自行结束，不能强制中断驱动。

引用 netstandard2.0 产物的宿主可通过 `IAsyncDisposable` 显式调用异步清理：`await ((IAsyncDisposable)reader).DisposeAsync()`。以宿主 `DbDataReader` 基类调用的 `DisposeAsync` 可能退回同步 `Dispose`；同步关闭同样可能等待异步源。

普通 JSON 选项沿用传入实例，执行期间不得修改；所需格式转换器由调用方配置。内存占用与当前行及其序列化内容有关，不随总行数累积；单个大字段仍会由驱动/序列化器物化，本适配器不提供 BLOB 字段内部的分块传输。
