# 统一进度、数据定位与表格字段映射

这些类型属于核心 `Bing.Utils`，支持 net8.0、net7.0、net6.0 和 netstandard2.0，无新增运行时依赖。

## 观察流式操作

```csharp
using Bing.IO;
using Bing.Operations;

var progress = new Progress<OperationProgress>(p =>
    Console.WriteLine($"{p.Stage}：{p.Completed} {p.Unit}，序号 {p.Sequence}"));

await StreamTransfer.CopyAsync(input, output,
    new StreamTransferOptions { OperationProgress = progress },
    leaveOpen: true, cancellationToken: token);
```

`StreamTransferOptions.Progress` 保持原来的 `IProgress<long>` 类型；可以和 `OperationProgress` 同时配置。前者仍在成功写入后报告累计字节数。后者分别报告 Read/Write，读取成功但目标写入失败时只增加 Read。

`OperationProgress` 是不可变快照。`Completed` 是当前阶段的累计数量，`Total` 未知时为 null，`Attempt` 从 1 开始，`Sequence` 从 0 开始，在同一次操作内递增。总量来自外部声明时可能小于实际完成量；消费者不应仅凭达到 Total 判定完成。

`MaxBytes`、`MaxRows` 是上限，不能用作总量。当前流复制、CSV、数据库读取都保留 `Total = null`。`IsFinal` 标识该阶段已正常完成；提前停止枚举、取消或该阶段失败不会补发成功的最终事件。Read 完成仍可能发生后续校验或写入失败。

普通流写入不会产生 Commit。`Write + IsFinal` 不表示文件替换、远端保存或磁盘持久化成功。实际提交方应在自己的提交成功边界报告 Commit，或以 `AtomicFile` 等 API 的成功返回为准。

```csharp
using Bing.Serialization.Csv;
using Bing.Utils.Json;

await foreach (var row in CsvStream.ReadAsync(input,
    new CsvOptions { OperationProgress = progress, MaxRows = 10000 },
    leaveOpen: true, cancellationToken: token))
{
    // 按需处理一行；CSV 进度按数据记录计数，不包含表头。
}

await foreach (var row in TabularDataReader.ReadRowsWithProgressAsync(
    reader, progress, leaveOpen: true, cancellationToken: token))
{
    // 只消费当前结果集；不提前计算总行数。
}
```

直接观察器按调用顺序同步接收 `Report`；使用 `Progress<T>` 时由该类型的同步上下文决定回调调度。库不会额外创建后台通知任务。需要确定性取消时，使用同步实现的 `IProgress<T>`。观察器抛出的异常会结束当前操作。

## 兼容已有进度

`ProgressAdapters.FromBytes` 返回 `IProgress<long>`，将累计字节数转为统一事件；`FromHttpDownload`、`FromHttpUpload` 分别返回旧 HTTP 观察器类型。适配器实例用于一次操作，内置序号随报告递增；上传保留 Attempt，HTTP 正文完成只映射为 Write 阶段最终事件。

FTP 保持现有进度类型，在应用层将其已完成字节映射为 Byte，文件数映射为 File；不要将百分比换算成未知的总字节。SSE 通常没有总长度，可以按已消费事件报告 Record；显式重连时由应用维护 Attempt。此次不修改 FTP/SSE 的传输实现。

## 用中文字段名映射对象

```csharp
using Bing.Tabular;
using Bing.Serialization.Csv;

var map = TabularMap<User>.Create(m =>
{
    m.Field(x => x.Name).From("姓名", "用户名")
        .RequiredColumn().RequiredValue();
    m.Field(x => x.Age).From("年龄");
});

await foreach (var row in CsvStream.ReadAsync(input, cancellationToken: token))
{
    var result = map.Map(row, token);
    if (result.IsSuccess)
    {
        Consume(result.Value);
        continue;
    }
    foreach (var issue in result.Issues)
        Console.WriteLine($"{issue.Code}：{issue.Location}，目标 {issue.TargetMember}：{issue.Message}");
}
```

`Field` 只接受直接公共可写属性或字段，不自动映射未配置成员、嵌套对象或集合。未调用 `From` 时按目标成员名匹配；无表头可以使用 `.FromOrdinal(0)`，序号从零开始。支持类和值类型目标，需有公共无参构造函数。

`Create` 返回后映射和字段配置都被冻结。创建时缓存成员写入及标量转换委托，首次看到一个列结构时建立列绑定；后续相同结构复用。`map.ValidateColumns(columnNames)` 可在读取数据值之前预检并预热绑定。

别名用于兼容不同来源：同一行同时存在“姓名”和“用户名”时，不擅自挑选其中一个，而是抛出编码为 `mapping.duplicate_mapping` 的 `TabularMappingException`。重复目标配置、跨目标重用别名/序号，以及名称与序号指向同一源列，也按配置冲突处理。源列改变时建立新的绑定，不沿用旧 ordinal。

## 空值、转换与对象校验

| 配置或输入 | 行为 |
|---|---|
| `RequiredColumn()` | 没有该列时返回 `mapping.missing_column` |
| `FromOrdinal(n).RequiredColumn()` 缺列 | `SourceOrdinal` 保留期望序号 n；表格位置使用零基 ordinal，CSV 使用可表示的一基列号，否则保留行位置 |
| `RequiredValue()` | 列存在但值为 null、DBNull、空字符串或空白时返回 `mapping.required_value` |
| 缺少可选列 | 不调用 setter，保留构造后的默认值 |
| null / DBNull | 可空类型和引用类型得到 null；非空值类型转换失败 |
| 空白字符串到可空值类型 | 转为 null；字符串成员保留原文 |
| 默认名称比较 | `OrdinalIgnoreCase`；可通过 `IgnoreCase = false` 区分大小写 |
| 默认文化 | `InvariantCulture`；选项中的文化在创建映射时复制 |
| 未知列 | 默认忽略；可选择 Warning 或 Error |

内置转换覆盖常用数值、布尔、字符、字符串、枚举、Guid、DateTime、DateTimeOffset、TimeSpan 和 Uri；复杂对象不自动反序列化。用 `.ConvertUsing((value, culture) => ...)` 可自定义字段转换。非取消转换异常成为行问题，默认消息不携带原始值或异常消息。

```csharp
var map = TabularMap<User>.Create(m =>
{
    m.Field(x => x.Name).From("姓名")
        .ConvertUsing((value, culture) => Convert.ToString(value, culture)?.Trim());
    m.Field(x => x.Age).From("年龄");
    m.Validate(user => user.Age < 0 ? new[] { "年龄不能为负数。" } : Array.Empty<string>());
}, new TabularMapOptions
{
    UnknownColumns = UnknownColumnBehavior.Warning,
    Culture = System.Globalization.CultureInfo.GetCultureInfo("zh-CN")
});
```

字段转换全部成功后才创建和赋值目标对象，再执行 DataAnnotations（默认开启）和自定义对象校验。校验失败返回 `mapping.validation`；可以通过 `ValidateDataAnnotations = false` 关闭注解校验。用户提供的校验消息原样保留，应避免在消息中拼接敏感字段。

一行会聚合多个字段问题。`RowResult<T>.Value` 在失败时为默认值，只有 `IsSuccess` 为 true 才能使用对象；Warning 不使结果失败。构造函数、setter 或用户委托自身的外部副作用不属于回滚保证。

## 数据库复用与位置

```csharp
long rowNumber = 0;
while (await reader.ReadAsync(token))
{
    var row = new DbDataReaderRow(reader, ++rowNumber);
    var result = map.Map(row, token);
    // result 的位置使用 TabularLocation。
}
```

`DbDataReaderRow` 捕获当前行的列名和值，不推进、不关闭读取器；复制 byte[] 字段，其他引用类型按引用保留。它会加载当前整行，不用于大 BLOB 的分块读取；大字段继续使用 `TabularDataReader.CopyBinaryFieldAsync` 等入口。

| 位置类型 | 坐标语义 |
|---|---|
| `CsvLocation` | 逻辑记录号 1-based，包含表头；列号 1-based，0 表示整条记录；多行引用字段不按物理行号计数 |
| `TabularLocation` | 行号由数据源决定；ordinal 0-based，-1 表示整行 |
| `JsonPointerLocation` | JSON Pointer，空字符串为根，保留原始转义路径 |
| `JsonSyntaxLocation` | 行号和行内 UTF-8 字节偏移均为 0-based，不是字符列号 |
| `WorksheetCellLocation` | 工作表名、行号和列号；行列均 1-based，仅提供位置扩展点 |

`CsvFormatException.Location` 和 `CsvRow.Location` 保持旧 CSV 坐标含义；字段映射问题同时携带源列名、零基序号和目标成员。JSON Schema 调用方可用原有 `InstancePath` 构造 `JsonPointerLocation`，用语法诊断行号与字节偏移构造 `JsonSyntaxLocation`，核心包不依赖 JsonSchema 包。

## 资源与取消

调用入口参数验证或入口令牌已取消时，不接管调用方资源。开始复制/写入或首次推进异步枚举后，成功、异常、取消和提前结束都遵循 `leaveOpen`；默认 true，false 明确转交释放责任。库创建的内部资源由库释放。

`Map` 不接管数据源资源。传入令牌在字段和校验边界观察；用户委托抛出的 `OperationCanceledException` 保持原异常，不包装成 `DataIssue`。单次同步读取或用户回调不能被抢占。

提前停止读取不验证剩余内容。失败或取消允许已经写入的部分输出存在；需要文件原子替换时使用现有 `AtomicFile`。遇错停止/继续、累计错误上限和进度节流由调用方控制。
