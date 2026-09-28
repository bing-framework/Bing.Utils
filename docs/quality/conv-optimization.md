# Conv 转换优化与迁移说明

## 源码组织

`Conv` 保持在单独的 `Conv.cs`，不再拆成 partial。`ConvTryConverter`、`ConvConverterBuilder`、`ConvConverter` 各自单独成文件，仍处于 `Bing.Helpers`；注册条目的接口与实现各自单独成文件，位于 `Bing.Helpers.Internal`。公开类型与方法签名未变。

## 内存结论

`Conv` 原实现没有长期持有输入对象的静态集合或事件订阅，不能据此认定存在输入对象泄漏。本次新增转换器为调用方持有的独立实例；弱引用回归测试验证转换器不会保留已处理输入，丢弃 builder 和转换器后，注册委托捕获的对象可回收。

JSON 转换现在复用一个 `JsonSerializerOptions` 实例。它会缓存接触过的类型元数据，固定类型集合的服务因此避免每次转换重复建立缓存。这是进程级缓存，不是逐次输入保留。动态生成类型或可卸载程序集不属于本次验证范围，不应把此实现视作支持插件卸载的承诺。

基准对象现在实现 `IDisposable`，直接运行短测时用 `using` 释放 `JsonDocument`，BenchmarkDotNet 场景通过 `[GlobalCleanup]` 释放它。独立内存探针以固定 JSON 类型预热 2,000 次，再执行 4 批、每批 20,000 次转换。每批分配均为 `2,080,024 B`；批内不主动收集时存活内存约 `2.28 MB`，自然 GC 为 `0`；每批结束执行一次完整、阻塞、压缩 GC 后，存活内存为 `180,088`、`181,136`、`181,136`、`181,136 B`，强制收集各产生 2 次 gen0/gen1/gen2 收集。可用 `--conv-memory` 入口复现。强制 GC 后的存活内存稳定，说明固定类型工作负载未出现持续保留；该探针不能代替任意业务输入、动态类型或可卸载程序集的泄漏证明。

## 行为变化

- `Conv.ToDateOrNull` 收到 `DateTime` 时直接返回原值，保留完整 ticks 和 `Kind`。
- `Conv.ToBoolOrNull` 可处理底层类型为 `long`、`ulong` 且超出 `Int32` 范围的枚举值。
- `Conv.To<T>` 使用真实类型判断字符串和 Guid，避免同名业务类型误入内置分支；相同目标类型直接返回原值。
- `Conv.To<T>` 遇到 `DBNull.Value` 返回默认值。`TryTo<T>` 对 `null`、`DBNull`、空白字符串和转换失败返回 `false`；成功转换为零值返回 `true`。
- `Conv.To<int>` 对字符串继续使用 invariant 整数语义，非法值和小数字符串仍返回默认值；`Conv.ToInt` 的舍入规则保持原状。已有 `To<T>`、`ToDictionary`、`ToList` 签名不变。

## 自定义转换

```csharp
var converter = new ConvConverterBuilder()
    .Register<string, int>((string text, out int value) => int.TryParse(text, out value))
    .Build();

bool success = converter.TryTo("42", out int number);
int value = Conv.To<int>("42", converter);
```

注册按输入**运行时类型**和声明目标类型精确匹配；命中注册后，即使返回 `false`，也不会回退内置转换。未注册时使用内置转换。`Build()` 创建注册快照；builder 不支持并发修改，转换器支持并发读取，委托自身的线程安全由调用方负责。重复类型对注册抛 `InvalidOperationException`，空委托或空转换器参数抛 `ArgumentNullException`。普通委托异常视为失败；取消和内存不足异常继续向调用方传播。

## 性能证据

基线源码为 `4b2150452e6c77858758ca34030e9d8342ec324c`，候选为未提交的 `working-tree`，Windows 11 x64、.NET 6.0.36、Release。本轮按职责拆分源码并调整内部命名空间，未重新采样性能数据；候选仍以未提交的工作区为准。基准入口会自动输出 `baselineCommit` 和 `candidate`，不再出现 `commit=unprovided`。固定输入先预热 2,000 次，再执行 20,000 次；采用 `Stopwatch`、`GC.GetAllocatedBytesForCurrentThread` 和 `GC.CollectionCount`。字符串输入是引用类型；JSON 场景的 `JsonElement` 从值类型字段传入 `object` 参数，会在每次调用发生装箱；新增的数值对照分别使用一次性预先装箱的 `int` 和每次从 `int` 字段传入 `object` 参数。结果只是本机短测，不能替代正式 BenchmarkDotNet 统计结论。可用基准项目的 `--conv-baseline` 入口复现，`--conv-memory` 复现内存探针，`--conv-collection-compare` 复现集合算法对照，并可运行 `ConvBenchmarks` 的 BenchmarkDotNet 方法。

| 场景（20,000 次） | 修改前耗时 / 分配 | 修改后耗时 / 分配 |
| --- | ---: | ---: |
| 数值字符串到整数 | 12.386 ms / 480,040 B | 5.351 ms / 184 B |
| 泛型字符串到整数 | 8.884 ms / 1,120,224 B | 3.612 ms / 40 B |
| JSON 对象转换 | 9,599.102 ms / 298,962,600 B | 26.665 ms / 2,080,040 B |
| 对象转字典 | 25.951 ms / 7,360,040 B | 39.928 ms / 7,360,040 B |
| 逗号分隔字符串转列表 | 41.268 ms / 24,640,408 B | 56.399 ms / 9,280,040 B |
| 无效整数输入 | 177.938 ms / 11,840,272 B | 3.574 ms / 40 B |

候选当前短测（同一运行时，20,000 次）还覆盖以下新增场景：

| 场景 | 当前耗时 / 分配 |
| --- | ---: |
| 预先装箱 `int` 到整数 | 0.284 ms / 40 B |
| 调用端每次装箱 `int` 到整数 | 0.625 ms / 480,040 B |
| `ConvConverter` 自定义字符串到整数 | 4.231 ms / 40 B |
| JSON 对象转换 | 32.458 ms / 2,080,040 B |
| 对象转字典 | 33.081 ms / 7,360,040 B |
| 逗号分隔字符串转列表 | 41.356 ms / 9,280,040 B |

当前实现通过 `IConvConverterEntry<TTarget>` 的目标类型化执行路径传递自定义转换结果，避免了每次把 `int` 结果装箱为 `object`。修复后的 .NET 6 Release 短测为 `4.231 ms / 40 B`（20,000 次）。此前基准 Harness 使用 `out object` 入口时曾测得 `2.268 ms / 480,040 B`；该数据保留为历史诊断记录，不能代表当前实现。正式门禁若需要性能百分比，应另行制定阈值并使用重复的 BenchmarkDotNet 运行。

## 集合算法对照

`--conv-collection-compare` 在同一当前运行时、相同固定输入和相同 20,000 次循环下，交错执行 5 次并取中位数。旧方法体是在 Benchmark 项目内依据基线源码局部复现的 `ToDictionary`/`ToList` 算法；列表元素转换共用当前 `Conv.To<T>`，因此这项数据只隔离集合算法成本，不能当作完整历史版本 before/after。

| 场景 | 第一次诊断：当前 / 旧方法体 | 第二次确认：当前 / 旧方法体 | 结论 |
| --- | ---: | ---: | --- |
| 对象转字典耗时 | 26.618 / 25.056 ms | 21.879 / 22.250 ms | 分配均为 7,360,064 B；方向不稳定，不形成回退 Finding |
| 对象转字典分配 | 7,360,064 / 7,360,064 B | 7,360,064 / 7,360,064 B | 当前预分配未改变此固定对象的总分配 |
| 逗号分隔字符串转列表耗时 | 26.668 / 32.410 ms | 21.489 / 27.665 ms | 当前更快，约为旧方法体的 0.82/0.78 倍 |
| 逗号分隔字符串转列表分配 | 9,280,064 / 13,440,064 B | 9,280,064 / 13,440,064 B | 当前减少约 31% 分配 |

两次字典耗时方向相反且分配完全相同，当前证据不支持继续修复字典算法；该疑似回退 Finding 关闭。列表的耗时和分配均支持当前实现。没有预先批准的性能百分比门禁，因此不把短测波动转成新的性能阈值。

## 验证范围

新增回归测试位于 `Bing.Utils.Tests`：主包自身功能按测试归属规则放主包测试项目，未迁移既有测试。验证包括 nullable、边界舍入、区域性、日期精度、枚举、JSON、动态属性、列表、自定义转换、并发调用及弱引用回收。发布、包版本和外部 CI 不在本次变更内。

本地 Release 验证：`Bing.Utils` 的 net8.0、net7.0、net6.0、netstandard2.0 编译均通过；主包 net8.0 完整测试 `6,941/6,941`；HTTP net8.0 完整测试 `1,530/1,530`；新增转换契约测试在 net6.0、net7.0、net8.0 各 `23/23`。Benchmark 项目 net6.0 Release 编译通过，BenchmarkDotNet 列表包含预先装箱、调用端装箱和自定义转换场景。编译存在仓库原有警告。未把本地结果写作外部 CI 或生产环境证据。
