# Conv 转换优化与迁移说明

最新补齐结果见 [GC 与性能验收补充报告](conv-gc-completion.md)：已增加 .NET 6/7 代表性测量、完整目标方法反汇编和缺失的标量基准，并修复 int 输入的专用转换中转成本。下文保留前几轮历史数据，其未验证状态以补充报告为准。

## 源码组织

`Conv` 保持在单独的 `Conv.cs`，不再拆成 partial，位于 `Bing.Helpers` 作为静态快捷入口；其类型元数据辅助类位于 `Bing.Helpers.Internals`。`ConvTryConverter`、`ConvConverterBuilder`、`ConvConverter` 各自单独成文件，位于 `Bing.Conversions`；注册条目的接口与实现位于 `Bing.Conversions.Internals`。`Conv` 的方法签名和转换行为保持不变；使用注册器时需要额外引用 `Bing.Conversions`。

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
using Bing.Conversions;
using Bing.Helpers;

var converter = new ConvConverterBuilder()
    .Register<string, int>((string text, out int value) => int.TryParse(text, out value))
    .Build();

bool success = converter.TryTo("42", out int number);
int value = Conv.To<int>("42", converter);
```

注册按输入**运行时类型**和声明目标类型精确匹配；命中注册后，即使返回 `false`，也不会回退内置转换。未注册时使用内置转换。`Build()` 创建注册快照；builder 不支持并发修改，转换器支持并发读取，委托自身的线程安全由调用方负责。重复类型对注册抛 `InvalidOperationException`，空委托或空转换器参数抛 `ArgumentNullException`。普通委托异常视为失败；取消和内存不足异常继续向调用方传播。

值类型源可使用双泛型入口，避免已支持路径在调用端先装箱：

```csharp
using Bing.Conversions;
using Bing.Helpers;

int same = Conv.To<int, int>(42);
bool parsed = Conv.TryTo<string, long>("42", out long number);
var converter = new ConvConverterBuilder()
    .Register<int, long>((int input, out long result) => { result = input; return true; })
    .Build();
long value = converter.To<int, long>(42);
bool converted = Conv.TryTo<int, long>(42, converter, out long result);
```

基类变量按实际运行时类型匹配；nullable 源和未支持组合会回退既有逻辑，因此双泛型入口不承诺所有组合零分配。

## 性能证据

### 本轮 .NET 8 冻结基线

本轮后续优化的 before 原记录为 `Conv.cs` 哈希 `D24B92CEA2D86435BE5E3FD54FB8991C82EBE8086C40EBB5D8B77C`，但该记录只有 54 位，不能作为完整 SHA-256。仓库内未找回完整记录或对应工作区快照，故该历史基线的源码身份不可独立核验；HEAD `f98a15c65b6c9c1553e297da27a5a0b022ddffe6` 也不能代表当时所有未提交改动。以下历史测量保留，不用当前源码补填基线。Windows 11 10.0.22631，.NET 8.0.31，Release，固定输入预热 2,000 次、测量 20,000 次。探针已在计时前创建 `Stopwatch`，且不再对整数校验值调用 `GC.KeepAlive`。命令：`dotnet run --no-restore --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0 -- --conv-baseline`。

| 场景 | 20,000 次耗时 | 分配 | B/op | 自然 Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: |
| 字符串 `12345.67` → `ToInt` | 17.425 ms | 0 B | 0 | 0/0/0 |
| 字符串 `12345` → `To<int>` | 1.617 ms | 0 B | 0 | 0/0/0 |
| 预装箱 `int` → `ToInt` | 0.563 ms | 0 B | 0 | 0/0/0 |
| 调用端装箱 `int` → `ToInt` | 0.802 ms | 480,000 B | 24 | 0/0/0 |
| 自定义字符串 → `int` | 3.085 ms | 0 B | 0 | 0/0/0 |
| `JsonElement` → 对象 | 65.788 ms | 2,080,000 B | 104 | 0/0/0 |
| 对象 → 字典 | 46.092 ms | 7,360,000 B | 368 | 0/0/0 |
| 逗号字符串 → `List<int>` | 30.741 ms | 9,280,000 B | 464 | 0/0/0 |
| 无效字符串 → `To<int>` | 1.430 ms | 0 B | 0 | 0/0/0 |

这里的 B/op 属于端到端入口：调用端装箱、结果集合、JSON 结果对象均计入。固定循环短测耗时受 JIT、CPU 调度影响，只有同运行时同输入的重复对照才可支持速度结论；分配归因比单次耗时稳定。旧版 .NET 6 数字保留在下文，不能与此 .NET 8 基线直接比较。

### 本轮候选对照

候选 `Conv.cs` SHA-256 `09B3DAA67F31B7AF7DB15D5641F2FAC87C2673C58F2F170769F62E91B0A826BF`，同为 Windows 11、.NET 8.0.31、Release、相同预热和输入。对冻结基线可直接对照的 9 个路径，分配量依次仍为 `0/0/0/480000/0/2080000/7360000/9280000/0 B`（每项 20,000 次），没有新增分配。耗时短测在本机不同采样间波动显著，本轮不从单次短测宣称速度提升或回退。

| 候选路径 | 20,000 次分配 | B/op | 归因 |
| --- | ---: | ---: | --- |
| 泛型字符串 → `long/double/decimal/bool` | 各 0 B | 0 | 非异常解析路径未产生可观测逐次分配 |
| 泛型字符串 → `long?` | 480,000 B | 24 | `(T)(object)` 中间装箱；`ConvTypeInfo<T>` 消除了额外的类型解包分配 |
| 预装箱 `int` → 自定义 `long` | 0 B | 0 | 已有目标类型化执行 |
| 调用端 `int` → 自定义 `long` 旧入口 | 480,000 B | 24 | 调用端装箱 |
| 类型化 `int` → 自定义 `long` | 0 B | 0 | 源和目标保持类型化 |
| 类型化同类型 `int` | 0 B | 0 | 无需装箱 |
| `JsonElement` 直接传入旧入口 → 对象 | 2,080,000 B | 104 | 包含每次传参装箱及结果对象 |
| 预装箱 `JsonElement` 旧入口 → 对象 | 1,440,000 B | 72 | 去除调用端装箱后的原入口成本 |
| 类型化 `JsonElement` → 对象 | 1,440,000 B | 72 | 与预装箱旧入口相同，无每次输入装箱 |

可空 `long` 的一次定向归因：修正前候选为 56 B/op；直接 `long.TryParse` 加 `(long?)(object)value` 为 24 B/op；单独 `Common.GetType<long?>()` 为约 32 B/op；旧 `Convert.ChangeType` 对照为 56 B/op。缓存可空类型信息后，候选降到 24 B/op。这是本轮过程内的诊断，不冒充最初冻结的 before 基线。其余既有转换场景在冻结时未测量，状态为历史采样缺失；只有历史版本不存在的新增类型化 API，其 before 才不适用。

BenchmarkDotNet 0.13.11 的 .NET 8 进程内短配置（1 次预热、3 次测量）得到：调用端装箱 `24 B/op`、类型化同类型 `int` `0 B/op`、泛型 `long?` `24 B/op`、类型化 JSON `72 B/op`；Gen0 分别为 `0.0019/0/0.0019/0.0057` 次/千操作。对应均值约 `26.7/3.3/43.8/204.1 ns`，但前三次迭代置信区间宽，不据此作速度结论。独立进程工具链需再次 NuGet restore，本机网络签名源失败，故本次使用 `--inProcess`；固定循环数据与 BDN 分配量相互印证。可复现命令：

JIT 反汇编核查状态：按 [.NET 运行时 JIT 文档](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/viewing-jit-dumps.md)设置 `DOTNET_JitDisasm='*TypedSameTypeInt*'`、`DOTNET_TieredCompilation=0`、`DOTNET_ReadyToRun=0`，对 Release net8.0 基准 DLL 运行 `--conv-baseline`，观察到 `TypedSameTypeInt():int:this (FullOpts)`，生成代码总计 12 字节；同场景 BDN 测得 0 B/op。该次命令只保留了方法头和代码长度，没有保存完整指令，故尚不能逐条核实 `box` 或 `CORINFO_HELP_NEWSFAST` 是否出现。自定义 `int`→`long` 快路径的首次定向文件输出未命中，也没有可审查的汇编文本；其“无逐次分配”结论仅来自短测和 BDN 对照，不宣称已通过反汇编证明绝对无分配调用。本项按实验次数上限标记为**未验证**，不继续重复运行。

首次调用单独运行 `--conv-first-call`：JSON `112.051 ms/124,432 B`，泛型字符串转 `long` `1.404 ms/0 B`，专用数值字符串转整数 `11.193 ms/0 B`，类型化同类型整数 `0.577 ms/0 B`。基准对象创建不在测量区，四个路径在同一进程按上述顺序各调用一次，因此这些数字同时包含各自 JIT 和共享运行时初始化，不能当作纯 `Conv` 初始化成本，也不与下面的预热后数据直接比较。首次初始化没有冻结的修改前同语义数字。

```powershell
dotnet run --no-build --no-restore --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0 -- --filter "*ConvBenchmarks.TypedSameTypeInt" "*ConvBenchmarks.CallerBoxingNumericConversion" "*ConvBenchmarks.GenericNullableLongConversion" "*ConvBenchmarks.TypedJsonConversion" --job Short --warmupCount 1 --iterationCount 3 --inProcess
```

### 连续自然 GC

候选在 .NET 8.0.31、Workstation GC、`Interactive` 延迟模式下，对固定类型分别预热后连续运行 10 秒。窗口内不强制 GC；窗口结束后完整 GC 只用于观察存活内存。`allocatedBytes` 为进程总分配，包含探针及运行时少量开销；`naturalLiveBytes` 是未回收时的可达堆估计，`forcedLiveBytes` 是窗口后完整回收的结果。下面是最终确认窗口：

| 场景 | B/op | 分配速率 | 自然 Gen0/1/2 | 运行时 GC 暂停 | 事件完全暂停区间 | 强制 GC 后存活 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 预装箱 `int` | 约 0 | 0.002 MB/s | 0/0/0 | 0 ms | 1.189 ms | 311,536 B |
| 调用端装箱 `int` | 24.01 | 392 MB/s | 312/0/0 | 65.683 ms | 56.600 ms | 316,712 B |
| JSON 对象 | 104.06 | 216 MB/s | 172/0/0 | 37.005 ms | 32.016 ms | 405,832 B |
| 对象转字典 | 368.21 | 467 MB/s | 372/0/0 | 75.121 ms | 66.076 ms | 416,176 B |
| 字符串转列表 | 464.27 | 812 MB/s | 647/0/0 | 155.469 ms | 140.352 ms | 416,240 B |

事件监听使用 .NET 运行时 `GCSuspendEEEnd_V1`（ID 8）到 `GCRestartEEBegin_V1`（ID 7）的时间戳计算线程**完全暂停**区间，符合[微软运行时事件定义](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events)。事件计数比 `GC.CollectionCount` 略多，且零自然回收的预装箱窗口也有少量暂停事件，因此这些事件包含非 GC 引起的暂停，不能直接称作精确 GC 暂停。真正的 GC 暂停总时长来自运行时的 [`GC.GetTotalPauseDuration()`](https://learn.microsoft.com/en-us/dotnet/api/system.gc.gettotalpauseduration) 差值，不从 GC 次数推算；它比事件完全暂停区间长，因为覆盖范围不同。事件最大单次完全暂停依次为 `0.768/0.645/0.489/0.444/1.315 ms`。`net6.0` 没有该累计暂停 API，只保留事件数据和自然 GC 次数，不伪造精确暂停值。

固定类型窗口中强制 GC 后存活值维持在约 0.3–0.4 MB，场景切换带来的类型元数据和 JSON 初始化使绝对值上升；结合既有弱引用测试，目前没有持续保留输入对象的证据。单次 10 秒窗口无法证明所有业务输入不会泄漏。调用端装箱是最清晰的减压目标：旧 `object` 入口约 24 B/op，新增类型化入口在已覆盖的同类型和精确注册路径测得 0 B/op。

### 本轮 TODO 补充测量

原始证据保存在 `CONV_EVIDENCE_ROOT/conv-gc-followup/`。`CONV_EVIDENCE_ROOT` 默认指向仓库内被 Git 忽略的 `artifacts/conv-evidence`，也可以由调用方指定仓库外的统一证据目录。源码候选 SHA-256 仍为 `09B3DAA67F31B7AF7DB15D5641F2FAC87C2673C58F2F170769F62E91B0A826BF`，被测生产 DLL SHA-256 为 `28514D83A9CBA0E14742878473F47162F9D5B997A3F8146587E3562750E80D14`。没有重建历史 before；新场景用 `ConvScalarBenchmarks<T>.LegacyBody` 与 `ConvIntegerBenchmarks.LegacyBody` 在同进程复现旧标量分支，属于**同语义方法体对照**，不代表历史完整版本或首次初始化。新增 typed API 的历史 before 仍不适用。

BDN 诊断配置为 .NET 8.0.31、Release、InProcess、预热 1 次、测量 3 次、每次 20,000 调用，44 个场景全部完成。耗时详见原始 CSV；短迭代的置信区间较宽，主要用于分配归因：

| 路径 | 旧方法体 B/op | 当前 B/op |
| --- | ---: | ---: |
| 字符串→long/double/bool 成功 | 24 | 0 |
| 字符串→decimal 成功 | 32 | 0 |
| 字符串→long?/double?/bool? 成功 | 56 | 24 |
| 字符串→decimal? 成功 | 64 | 32 |
| 上述字符串失败 | 736–800 | 0 |
| 预装箱 long→专用 int | 32 | 0 |
| 预装箱 double→专用 int | 40 | 0 |
| 预装箱 decimal→专用 int | 40 | 40 |

专用 long→int 的诊断均值为当前 133.77 ns、旧方法体 75.96 ns；一次确认（预热 3 次、测量 5 次、每次 1,000,000 调用）为 68.54/47.11 ns，方向复现但置信区间重叠。实现侧随后增加 long 直接范围检查，避免 decimal 中转；新候选仅对该路径确认，结果为当前 9.767 ns/0 B、旧方法体 32.951 ns/32 B，原始证据在 long-final/results/。该回退 Finding 已关闭。double→int 确认为 20.44/287.09 ns。确认数据位于 `confirmation/results/`。

分配来源基准：枚举成功 24 B/op，枚举失败 712 B/op，Guid 失败 560 B/op；列表拆分数组与子字符串 352 B/op，预容量 10 的结果 List<int> 96 B/op；相同三键字典结果及 int/bool 装箱 264 B/op。它们是隔离成本，不能机械相加还原所有端到端路径（容量、反射及失败机制不同）。原列表总量 464 B/op、字典总量 368 B/op 包含这些结果和中间分配。

自然 GC 探针已修正：使用事件自身时间戳确定窗口，以锁同步回调和快照；窗口结束后执行独立完整 GC 产生水位事件，并最多等待 2 秒排空。只接受 SuspendEE Reason 1/6（GC/GC preparation），依据 [runtime SUSPEND_REASON](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/threadsuspend.h) 和[运行时 GC 事件文档](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events)。所有场景 `eventsDrained=True`。完全暂停区间是 SuspendEEEnd 到 RestartEEBegin；总 GC 暂停仍以 `GC.GetTotalPauseDuration()` 差值为准。早期报告的未过滤事件数据保留为历史，**由下面修正窗口替代**：

| 场景 | B/op | 自然 Gen0/1/2 | GC 完全暂停事件数/总毫秒 | 运行时 GC 总暂停 ms | 完整 GC 后存活 B |
| --- | ---: | --- | --- | ---: | ---: |
| 预装箱 int | 0.00 | 0/0/0 | 0/0 | 0 | 323328 |
| 调用端装箱 int | 24.01 | 611/0/0 | 612/64.130 | 72.598 | 319944 |
| JSON | 104.06 | 123/0/0 | 123/28.362 | 33.514 | 399848 |
| 字典 | 368.22 | 162/1/0 | 162/51.481 | 226.609 | 416336 |
| 列表 | 464.26 | 949/0/0 | 949/138.338 | 152.005 | 416400 |

计数器读取与事件窗口存在微小边界差，事件区间数不强制等于 GC 次数。每场景持续 10 秒，窗口内无强制回收；原始日志同时保存分配速率和堆大小。探针创建及事件分配仍包含在进程总量内。

`jit-net8.txt` 本次已保存 TypedSameTypeInt 完整包装方法指令（加载输入、tail jump；12 字节），但被调用的泛型方法体和自定义快路径未匹配到，所以完整调用链仍未验证。按实验次数上限不再重复。net6/net7 基准定向诊断及一次确认最终仍遇到 NETSDK1005（资产缺目标）；不以生产库编译成功替代基准运行证据。

复现 BDN：`dotnet benchmarks/Bing.Utils.Benchmark/bin/Release/net8.0/Bing.Utils.Benchmark.dll --filter "*ConvScalarBenchmarks*" "*ConvIntegerBenchmarks*" "*ConvAllocationBenchmarks*" --inProcess --warmupCount 1 --iterationCount 3 --invocationCount 20000 --unrollFactor 1`。自然 GC 入口仍为 `--conv-sustained-gc`。

新候选 Conv.cs SHA-256 为 A336D7BB30FAAEB874C6566E61C025BEDB059AF8989EE7019847A43939ED2F69，生产 DLL 为 3FA356DAAF3CB1806D15168022A39B38773C9307D979957022B4659BDCD906C7。前述 44 个场景、自然 GC、JIT 文件绑定前一候选；本轮唯一生产差异为 ToIntOrNull(long)，其它路径的证据按 REUSED_UNCHANGED_SCOPE 复用，不能把历史文件改称新候选 fresh run。long 定向确认设置环境变量 CONV_INTEGER_SOURCE=long，使用同一 ConvIntegerBenchmarks，预热 3 次、测量 5 次、每次 1,000,000 调用。

状态：测量工具修正、标量同语义对照、分配归因与 long 回退修复为 CLOSED；.NET6/7 基准资产验证、完整 JIT 调用链为未验证，按实验次数上限停止；没有把未验证项转换成生产代码缺陷。

### 后续优化候选

1. 将泛型可空值结果路径剩余的 24 B/op 与类型化结果构造作专门对照，再决定是否移除 `(T)(object)`；须验证 `Nullable<T>`、自定义成功返回默认值和运行时差异。
2. 对泛型其他跨类型数值转换和专用数值路径按真实业务类型分布采样；存在 `Convert.ChangeType` 返回值装箱与字符串中间值候选，但本轮没有逐路径归因，不承诺统一零分配。
3. Guid/枚举失败异常、列表子字符串、字典结果分配保持为候选项；只有在独立基准证明收益且保留历史语义后再改动。
4. 对要求严格吞吐的服务，在目标机器的 GC 模式和实际类型集上复测长窗口；本机 `Workstation/Interactive` 数据不能代表 Server GC 或生产暂停分布。

### 上一轮历史数据（.NET 6）

基线源码为 `4b2150452e6c77858758ca34030e9d8342ec324c`，候选为当时未提交的 `working-tree`，Windows 11 x64、.NET 6.0.36、Release。上一轮按职责拆分源码并调整内部命名空间，未重新采样性能数据；这些数字不能作为本轮 .NET 8 候选结果。基准入口会自动输出 `baselineCommit` 和 `candidate`，不再出现 `commit=unprovided`。固定输入先预热 2,000 次，再执行 20,000 次；采用 `Stopwatch`、`GC.GetAllocatedBytesForCurrentThread` 和 `GC.CollectionCount`。字符串输入是引用类型；JSON 场景的 `JsonElement` 从值类型字段传入 `object` 参数，会在每次调用发生装箱；新增的数值对照分别使用一次性预先装箱的 `int` 和每次从 `int` 字段传入 `object` 参数。结果只是本机短测，不能替代正式 BenchmarkDotNet 统计结论。可用基准项目的 `--conv-baseline` 入口复现，`--conv-memory` 复现内存探针，`--conv-collection-compare` 复现集合算法对照，并可运行 `ConvBenchmarks` 的 BenchmarkDotNet 方法。

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

当前实现通过 `IConverterEntry<TTarget>` 的目标类型化执行路径传递自定义转换结果，避免了每次把 `int` 结果装箱为 `object`。修复后的 .NET 6 Release 短测为 `4.231 ms / 40 B`（20,000 次）。此前基准 Harness 使用 `out object` 入口时曾测得 `2.268 ms / 480,040 B`；该数据保留为历史诊断记录，不能代表当前实现。正式门禁若需要性能百分比，应另行制定阈值并使用重复的 BenchmarkDotNet 运行。

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

上一轮历史验证为：`Bing.Utils` 四目标框架编译通过；主包 net8.0 完整测试 `6,941/6,941`；HTTP net8.0 完整测试 `1,530/1,530`；转换契约测试在 net6.0、net7.0、net8.0 各 `23/23`。本轮验证以最终源码为准：生产项目 net8.0、net7.0、net6.0、netstandard2.0 Release 编译通过；`Conv` 定向测试在 net6.0、net7.0、net8.0 各 `38/38`；主包 net8.0 完整测试 `6,956/6,956`，Net 消费方 `21/21`、HTTP 消费方 `1,530/1,530`。Benchmark 项目默认使用 net8.0，Release 编译并运行短测、BDN 与自然 GC 探针；尝试为 net6.0/net7.0 定向还原时，本机仍只生成 net8.0 资产目标，因此这两项基准编译/运行未验证。生产库和定向测试的旧框架验证不受此基准工具链问题影响。编译存在仓库原有警告。未把本地结果写作外部 CI 或生产环境证据。
