# CSV/TSV 流式读写

`CsvStream` 从当前流位置逐条处理 UTF-8 CSV 或 TSV，不需要定位流，也不缓存整个数据集。读取支持短读、UTF-8 BOM、双引号转义和带换行的引用字段；写入不添加 BOM。

```csharp
await using var input = File.OpenRead(path);
await foreach (var row in CsvStream.ReadAsync(input))
{
    var id = row.Get<int>("Id");
    var amount = row.Get<decimal>("Amount", CultureInfo.GetCultureInfo("fr-FR"));
}
```

默认首条记录是表头，列名按不区分大小写的方式映射。设置 `HasHeader = false` 时，可通过 `Headers` 指定列名；未配置表头时使用字段位置读取。`CsvRow.Get<T>` 只执行显式的基础类型转换，不会反射创建对象。默认转换文化为 `InvariantCulture`；空字段读取为字符串时仍为空字符串，转换到可空或引用类型时返回默认值，转换到不可空值类型时报告格式错误。

写入同样逐条消费异步序列：

```csharp
async IAsyncEnumerable<CsvRow> CreateRows()
{
    yield return new CsvRow(new[] { "1", "first, quoted" }, new[] { "Id", "Name" });
    await Task.Yield();
}

await using var output = File.Create(path);
await CsvStream.WriteAsync(output, CreateRows());
```

TSV 使用制表符分隔：

```csharp
var tsvOptions = new CsvOptions { Delimiter = '\t' };
await foreach (var row in CsvStream.ReadAsync(input, tsvOptions))
{
    // 逐行处理字段
}
```

默认限制为每条记录 4 MiB、每个字段 1 MiB 和每条记录 4096 个字段。`MaxRowBytes` 按原始 UTF-8 字节计数，包含分隔符和引用语法，不包含记录结束符；`MaxFieldBytes` 包含字段自己的引号和转义语法，不包含分隔符。将预算设为 `null` 可取消对应字节限制。格式、转换或预算错误抛出 `CsvFormatException`，其中包含从一开始的逻辑记录号和字段号。

整次输入和数据行数默认不设上限，可用 `MaxInputBytes`、`MaxRows` 配置。`MaxInputBytes` 仅用于读取，从当前输入位置按原始字节累计，包含 BOM、表头、分隔符和 CRLF/LF；零只接受空输入。超限在枚举读取到超预算输入时抛出 `CsvFormatException`，流式读取中此前已经交付的行不会撤回。`MaxRows` 同时限制读取和写入的数据行数，不计表头；零允许空数据或仅含表头，超限的行不会交付或写出。两个选项都接受 `null` 表示无限制，负值会被拒绝。

读取和写入都从流当前位置开始。读取从首次枚举开始接管流；若 `leaveOpen` 为 `false`，提前结束枚举、解析错误和执行中取消也会关闭源。方法调用时已取消的令牌不会接管流。写入默认保留目标，记录格式和预算会在写出该记录前检查；底层写入失败或取消仍可能留下当前记录的部分字节。调用方负责在失败后处理目标内容。
