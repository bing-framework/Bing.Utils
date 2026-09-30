# Conv GC 与性能验收补充报告

本轮补齐上次列出的五类 TODO，日期为 2026-09-29。新增基准、独立进程首次调用入口和可复现脚本；生产改动仅为六个专用整数方法增加 int 输入的直接范围检查。没有修改包版本、提交或发布。

## 实现与测量覆盖

| 项目 | 本轮结果 |
| --- | --- |
| .NET 6/7 基准工具链 | 基准项目明确声明 net8.0/net7.0/net6.0，三个目标均构建成功。消除切换单目标造成资产覆盖的问题。 |
| 标量同类型 | int/long/double/decimal/bool/Guid/DateTime 各有直接返回、预装箱、调用端装箱、类型化入口四组。 |
| 泛型跨数值 | long/double/decimal 到 int，成功和越界分别测量，以 invariant ChangeType 作成本控制。 |
| 其他专用整数 | sbyte/byte/short/uint/long/ulong，整数与小数、成功与越界均有旧方法体对照，Setup 检查固定输入结果一致。 |
| 日期及对象 | 新增日期解析成功/失败；JSON、自定义转换比较当前预装箱与类型化入口；字典、列表比较旧集合算法方法体。 |
| JIT | 保存 To<int,int>、TryToCoreTyped<int,int>、自定义 TryTo/TryConvert、条目实现和具体委托目标方法的完整指令。 |
| 首次调用 | 七个场景各用新进程，结果读取先于日志格式化；配置、模型和委托创建仍在测量区外。 |

不把直接返回、ChangeType 控制、当前预装箱入口、旧算法方法体混称历史版本 before。直接返回耗时接近零，相关 BDN Ratio 不用于性能判断。

## 分配与耗时

.NET 8 新增矩阵共 100 个场景；.NET 6.0.36 与 7.0.20 各执行 40 个代表场景。使用 BenchmarkDotNet 0.13.11、Release、InProcess、Concurrent Workstation GC。诊断为 3 次预热、5 次测量、每次 20,000 调用；对可疑路径做一次每次 1,000,000 调用的确认。Gen0/1/2、误差与逐次测量保存在原始日志和 CSV 中。

| 场景 | 当前观察 |
| --- | --- |
| 七种类型化同类型转换 | .NET 6/7/8 均为 0 B/op；预装箱入口也是 0 B/op。 |
| 调用端同类型装箱 | int/long/double/bool/DateTime 为 24 B/op；decimal/Guid 为 32 B/op。 |
| 泛型跨数值成功（.NET 8） | 24 B/op，与 ChangeType 控制相同，属于结果装箱热点。 |
| 泛型跨数值越界（.NET 8） | decimal/double/long 输入分别为 224/344/560 B/op，控制路径相同。 |
| 日期文本（.NET 8） | 泛型成功 48 B/op，ChangeType 控制 24 B/op；失败均为 544 B/op。差额仍需专门归因，不直接全部称作装箱。 |
| .NET 6/7 字符串转 long | 当前成功、失败均为 0 B/op；旧方法体成功 24 B/op，失败分别为 584/760 B/op。 |
| JSON / 自定义 | 当前类型化与预装箱控制分别为 72 / 0 B/op。 |
| 字典 / 列表 | 字典双方 368 B/op；列表当前约 464–465 B/op，旧算法 672 B/op。 |

初次诊断发现 int(123) 转 sbyte 等路径的 decimal 中转成本，确认中 sbyte 当前/旧方法体为 30.51/24.22 ns。为六个目标增加 int 直接转换后，定向数据为 sbyte 29.94/29.12 ns、ulong 21.94/22.21 ns；原先的明确回退未复现，但不据此宣称所有类型都提速。新候选 long 36.43±33.03 ns 与控制 26.85±0.53 ns 仍有较大不确定性，不能判为已证明提速。

JSON 确认为 218.35±12.49 / 204.60±16.49 ns，区间重叠；字典为 483.26/490.62 ns，诊断中的明显回退未复现。列表为 215.42/473.13 ns，分配也下降。自定义类型化入口与预装箱入口的耗时区间重叠。

**.NET 6/7 的 long 解析耗时未收敛。** 确认均值当前/旧分别为 79.29/47.54 ns、126.39/52.16 ns，当前方向较慢，但误差分别为 ±37.34、±139.60 ns，逐次数据存在明显下降趋势。已达到本次诊断加确认的次数上限，不能宣称跨运行时吞吐验收完全通过，也不继续重复相同实验。分配收益成立；后续需要改变测量方法以排除分层 JIT 与短窗口影响，不能仅延长同一命令或据此臆测生产缺陷。

## JIT 与首次调用

`jit-net8.txt` 中，TryToCoreTyped<int,int> 为 12 字节的写结果/返回代码，没有装箱或分配辅助调用。自定义匹配成功路径通过类型化接口和委托传递 int/long；外层出现的 NEWSFAST 属于空转换器的 ArgumentNullException 分支，不属于匹配成功路径。该证据仅针对记录中的 .NET 8 优化 JIT 和固定闭合类型，不承诺任意委托或回退路径零分配。

七个独立进程首次调用的分配分别为：JSON 124,872 B、long 解析 0 B、专用整数文本 2,096 B、类型化 int 0 B、自定义转换 2,272 B、字典 11,488 B、列表 464 B。耗时包含方法 JIT 及尚未完成的共享初始化，不是纯业务转换成本，不与预热后 ns/op 比较，也不冒充历史首次调用 before。

## 证据与复现

原始结果位于 `CONV_EVIDENCE_ROOT/conv-completion/`。`CONV_EVIDENCE_ROOT` 默认指向仓库内被 Git 忽略的 `artifacts/conv-evidence`，也可以由调用方指定仓库外的统一证据目录。

- `net8-matrix`、`net8-additional`：100 个新增场景的诊断。
- `net6.0-Representative`、`net7.0-Representative`：各 40 场景。
- `net8-confirmation`：int 快路径修复前的 20 项确认；`net8-final-integer`：修复后的 12 项。
- `net6-confirmation`、`net7-confirmation`：同类型与 long 成功解析，各 30 项。
- `jit-net8.txt`：目标方法完整指令；`net8.0-FirstCall`：独立进程首次调用日志。

最终 Conv.cs SHA-256 为 `32E31F1B431DDB36D7C3AF9DA5F29AD7979D529F715C8B6382B9D215E6073719`。修复前诊断、代表场景及 JIT 使用上一候选 `A336D7BB30FAAEB874C6566E61C025BEDB059AF8989EE7019847A43939ED2F69`；这部分仅对未变化的泛型、JSON、自定义和集合范围复用。脚本运行目录中的 identity.json 记录实际源码、生产 DLL、Harness DLL 身份与运行参数。最早两个 .NET 8 诊断目录未使用该脚本，不补造当时 Harness DLL 哈希。

旧报告的冻结基线只留下 54 位哈希，仓库内没有可恢复的完整身份或源码快照。已更正为历史身份缺失；不能用当前文件覆盖。新类型化 API 的历史 before 不适用，既有能力缺少历史采样则属于证据缺失，二者分开记录。

```powershell
dotnet build benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -p:GenerateDocumentationFile=false
& ./benchmarks/Bing.Utils.Benchmark/Run-ConvCompletion.ps1 -Framework net8.0 -Mode Matrix
& ./benchmarks/Bing.Utils.Benchmark/Run-ConvCompletion.ps1 -Framework net6.0 -Mode Representative
& ./benchmarks/Bing.Utils.Benchmark/Run-ConvCompletion.ps1 -Framework net7.0 -Mode Representative
& ./benchmarks/Bing.Utils.Benchmark/Run-ConvCompletion.ps1 -Framework net8.0 -Mode FirstCall
& ./benchmarks/Bing.Utils.Benchmark/Run-ConvCompletion.ps1 -Framework net8.0 -Mode Jit
```

成功路径定向确认可设置 `CONV_DEDICATED_CONFIRM=1` 或 `CONV_SCALAR_SUCCESS_ONLY=1`，并传入 `-Filter`、`-InvocationCount 1000000` 和新的 `-OutputDirectory`。默认输出目录带 UTC 时间戳，避免覆盖上次结果。基准项目现为多目标，使用 dotnet run 时需指定 `-f`；脚本直接选择对应 Release DLL。复现命令供后续使用，不意味着每次修改都应重跑整套测量。

## 验证与剩余事项

生产库 net8.0/net7.0/net6.0/netstandard2.0 四目标框架编译通过；基准项目 net8.0/net7.0/net6.0 编译通过；主包 net8.0 完整测试 6,970/6,970，Conv 定向测试在 net6.0/net7.0/net8.0 各为 52/52。新增 14 个边界用例覆盖 int 极值及目标边界。PowerShell 脚本语法、UTF-8 和 git diff --check 检查通过。本次没有改变消费者调用契约，既有 Net/HTTP 测试证据按未改变的相应范围复用。

- CLOSED：旧框架基准构建与运行、缺失场景实现和测量、目标 JIT 指令、独立首次调用日志、小整数直接路径及边界测试。
- ACCEPTED_LIMITATION：.NET 6/7 long 解析耗时在计划允许次数内仍不稳定；这是有限测量的证据限制，跨运行时吞吐结论保留，不能写作完全 PASS。后续可单独开展“Conv 稳态吞吐与分层 JIT 归因”，本轮不批准吞吐发布门禁。
- DEFERRED：不可恢复的历史基线身份；影响历史完整版本 before/after 的可审计性，不影响当前功能正确性。后续任务应使用新的、明确标识的基线，不能倒填旧记录。
- DEFERRED：nullable 剩余分配、Guid/枚举失败异常、日期解析差额、列表子字符串与跨数值结果装箱。它们属于后续专项优化，不在本轮测量补齐范围内。

自然 GC、弱引用回收与对象生命周期未改变，继续复用上一轮证据；未重复执行无关的 10 秒窗口。

## 2026-09-29 后续优化与稳态确认

本轮针对上节剩余的分配热点增加了内部可空值构造、泛型整数跨类型转换、日期与 Guid 的非异常解析，以及 `List<int>` 的无子字符串解析。可空结果使用目标类型封闭的 `ConvTypeInfo<T>`，没有增加保存业务对象或委托的静态缓存。这里使用 `Unsafe.As<TValue?, T>`，与原计划“无需引入 Unsafe”的实现预期不同；调用前以 `typeof(T) == typeof(TValue?)` 精确检查布局，四目标框架编译和可空行为测试通过，零逐次分配已由基准确认。该实现复用项目已有的 Unsafe 依赖，没有新增对象生命周期。`long` 到 `int` 在转换前检查范围，越界直接返回失败。`double`、`decimal` 到 `int` 的直接计算在确认中出现成功路径耗时回退，已撤回；枚举 `TryParse(Type, ...)` 虽消除了失败异常，但有效枚举解析约慢一倍，也已撤回。保留原有 `Enum.Parse`、`ChangeType` 语义与成本。

新增测试覆盖跨数值越界、负中点、ulong 边界、区域性、日期、Guid、枚举、可空源和目标，以及整数列表行为。最终主库源码 SHA-256 为 `262FEBA5ABB1CF2E2A48F1ABEFB4AB026C672B498CC1AF58F3C9957DFA61DE4C`；新测量的源码、生产 DLL 与基准 DLL 身份保存在各 evidence 目录的 `identity.json`。撤回快路径之前的矩阵仅用于未改变的日期、Guid、可空标量和列表路径，不作为最终跨数值或枚举路径的性能结论。旧历史基线的完整身份仍不可恢复。

| 路径 | 观察与处理 |
| --- | --- |
| 字符串到 `long?`、`double?`、`decimal?`、`bool?` | 诊断中当前实现均为 0 B/op；旧方法体分别为 56、56、64、56 B/op。 |
| 日期文本 | 成功路径当前为 0 B/op，`ChangeType` 控制为 24 B/op；失败路径当前 24 B/op，对照 688 B/op。耗时短窗口波动较大，不声明确定的成功路径提速。 |
| Guid 文本 | 成功和失败路径当前均为 0 B/op；旧类型转换器分别为 32、560 B/op。 |
| `List<int>` | 当前约 216 B/op，旧集合算法 672 B/op；结果列表及其存储仍会分配。 |
| `long` 到 `int` 越界 | 诊断中的 checked 转换为 224 B/op；增加范围检查后的确认实现为 0 B/op。 |
| 枚举与 `double`、`decimal` 到 `int` | 快路径测量发现有效输入耗时回退，最终回到原方法体。失败异常和跨数值结果装箱仍是候选优化，不把已撤回的分配数据算作收益。 |

新的交错稳态探针每条路径预热 50,000 次，随后交替执行 20 组、每组 10,000 次，记录每组耗时、分配与自然 GC。它直接启动独立进程，避免 BenchmarkDotNet 短窗口的额外调度。旧候选在 .NET 7 的确认结果为当前/控制 48.57/38.19 ns/op，存在吞吐回退。把字符串到 long 的快路径前移到无转换器的 `TryTo<T>` 后，.NET 7 诊断与确认分别为 37.95/38.69 和 37.32/45.76 ns/op，回退未复现；.NET 8 为 23.15/57.20 ns/op。.NET 6 单次代表性结果为 86.49/115.51 ns/op，受调度波动影响，仅判断方向。三个运行时当前路径均为 0 B/op，控制路径为 24 B/op。同一候选内部成对比较，不跨候选计算收益。逐样本 CSV 位于 `CONV_EVIDENCE_ROOT/conv-next/*public-entry*`。

最终候选 `List<int>` 单场景自然 GC 窗口：.NET 8.0.31、Workstation/Interactive，持续 10 秒、50,040,930 次，分配 216.12 B/op、约 1.08 GB/s，自然 Gen0/1/2 为 862/0/0，运行时累计暂停 126.804 ms；窗口后完整 GC 的存活内存为 315,464 B。原始日志和身份位于 `CONV_EVIDENCE_ROOT/conv-next/net8-list-sustained-entry-final/`。此前 464 B/op 的列表自然 GC 数据属于旧候选，不能用来代表本次 Span 列表路径。对象回收结论仅针对固定类型和该十秒窗口。

本轮入口调整后，生产库 net8.0/net7.0/net6.0/netstandard2.0 均构建成功，前三个运行时的基准和测试项目也已构建。测试项目通过本地 NuGet 缓存离线还原并从最新源码构建，Conv 契约测试在 net6.0/net7.0/net8.0 各通过 57/57。主包 net8.0 完整测试通过 6975/6975；加载最终主库 DLL 的 Net 与 HTTP 消费方分别通过 21/21、1530/1530。

已关闭上一轮的 .NET 7 稳态吞吐回退、最新测试源码跨运行时验证，以及列表自然 GC 证据缺口。后续独立优化候选为：枚举失败异常，`double`、`decimal` 跨数值转换的装箱与越界异常，以及其他列表元素类型的子字符串分配；实施时仍需兼顾成功路径吞吐。旧历史基线没有完整源码快照，不能恢复或倒填。当前没有对象持续保留的新证据，不把短生命周期分配称为内存泄漏。
