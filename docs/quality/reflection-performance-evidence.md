# 反射性能逐轮证据（2026-10-04）

本文保留 2026-10-04 至 2026-10-09 的历史测量；其中“本轮”“当前候选”“尚未提交”均指各节记录时的源码快照，不代表最新 HEAD。历史数字不可作为本次提交的性能结论或正式预算。最新提交验证见 [验证证据](reflection-validation-evidence.md)。

固定基线：`49fd7fa0e9b50ef179a89ec07685cd37c9693622`。候选：记录时的工作树。运行时：Microsoft.NETCore.App 8.0.31；两个版本的库和 `ReflectionComparisonProbe` 均按 Release/net8.0 构建。各场景预热 10000 次，单进程测 5 组各 50000 次并取组内中位数；每轮按基线、候选交替启动。单位为 ns/次；每组的分配量 B/次在同轮五次运行中保持不变。

## 第一轮：数组签名修复后

| 场景 | 基线五轮 | 候选五轮 | 基线 B/次 | 候选 B/次 |
| --- | --- | --- | ---: | ---: |
| 构造函数 | 256.6, 399.1, 494.0, 394.0, 448.0 | 180.7, 259.4, 188.4, 240.9, 622.6 | 240 | 24 |
| 属性 getter | 5.8, 9.0, 7.6, 11.1, 6.8 | 24.0, 27.2, 17.5, 21.6, 38.8 | 24 | 24 |
| 属性 setter | 7.7, 9.7, 8.6, 12.1, 8.1 | 9.7, 15.1, 12.5, 9.2, 21.9 | 24 | 24 |
| 方法签名 | 801.5, 1485.0, 1304.7, 1769.9, 2028.2 | 271.5, 259.4, 253.2, 257.1, 427.8 | 448 | 88 |
| 交替方法签名 | 1620.5, 1150.6, 1815.7, 832.1, 980.7 | 316.5, 419.9, 323.0, 253.1, 513.3 | 552 | 120 |
| 成员路径读取 | 781.8, 1206.1, 729.2, 952.4, 918.6 | 698.7, 822.6, 445.5, 568.1, 924.0 | 472 | 200 |
| 成员路径写入 | 620.9, 975.9, 639.8, 740.3, 814.8 | 542.0, 656.5, 556.4, 664.0, 1195.7 | 408 | 144 |
| 显式选项读取 | 696.4, 1203.6, 904.6, 855.4, 1538.5 | 698.0, 924.8, 800.9, 757.7, 1335.4 | 448 | 224 |
| 显式选项写入 | 622.5, 425.3, 812.9, 892.1, 604.3 | 685.2, 733.9, 889.0, 641.8, 1019.7 | 384 | 168 |
| 相同字典动态类型 | 1715.8, 2130.8, 1809.3, 1726.2, 2278.2 | 409.6, 485.3, 306.4, 396.3, 725.3 | 728 | 56 |
| 全新等价字典 | 1221.1, 1493.7, 1334.8, 1264.6, 2733.9 | 622.6, 606.7, 541.6, 618.9, 1521.7 | 976 | 304.4 |

探针源码位于 `benchmarks/ReflectionComparisonProbe/Program.cs`。属性 getter/setter 为直接委托调用；其他入口主要通过反射调用，因此绝对时间包含探针开销。当前没有经批准的性能发布阈值。

## 第二轮：加入可关闭的缓存诊断后

此轮诊断开关关闭，测量的是默认运行路径。

| 场景 | 基线五轮 | 候选五轮 | 基线 B/次 | 候选 B/次 |
| --- | --- | --- | ---: | ---: |
| 构造函数 | 466.6, 355.4, 423.1, 360.3, 305.3 | 209.9, 143.1, 292.9, 243.5, 194.6 | 240 | 24 |
| 属性 getter | 7.4, 7.0, 7.7, 6.2, 7.6 | 23.6, 20.1, 27.1, 18.7, 17.6 | 24 | 24 |
| 属性 setter | 8.3, 7.9, 8.5, 6.9, 8.3 | 10.9, 8.7, 11.5, 8.8, 8.0 | 24 | 24 |
| 方法签名 | 2005.4, 1446.5, 1338.6, 960.5, 1756.3 | 221.1, 191.8, 261.8, 271.3, 207.7 | 448 | 88 |
| 交替方法签名 | 934.7, 851.0, 1312.9, 1772.5, 933.2 | 305.1, 263.0, 352.6, 369.0, 259.5 | 552 | 120 |
| 成员路径读取 | 1155.9, 846.1, 1590.8, 876.8, 1100.9 | 573.4, 440.3, 576.4, 597.6, 412.8 | 472 | 200 |
| 成员路径写入 | 761.6, 777.1, 1386.8, 679.0, 1099.7 | 591.4, 442.7, 489.1, 449.0, 449.2 | 408 | 144 |
| 显式选项读取 | 1217.2, 977.1, 518.8, 722.8, 1031.9 | 623.9, 433.9, 548.0, 510.5, 476.4 | 448 | 224 |
| 显式选项写入 | 485.1, 1018.7, 437.9, 680.6, 380.7 | 603.8, 457.5, 671.0, 516.3, 433.3 | 384 | 168 |
| 相同字典动态类型 | 1929.9, 1869.4, 2279.0, 1977.2, 1992.8 | 415.1, 367.1, 381.6, 465.2, 273.1 | 728 | 56 |
| 全新等价字典 | 1354.9, 1085.2, 1882.0, 1291.7, 1286.4 | 612.8, 534.8, 556.7, 637.4, 469.9 | 976 | 304.4 |

缓存诊断探针另行在 .NET 8.0.31 下运行。并发首次构造、属性 getter/setter、方法签名和成员路径访问各 32 次，均只发生 1 次扫描或编译；后续各 32 次重复请求的实际查找状态均为命中。8 次并发动态模式请求为 1 次未命中、7 次等待、1 次 Emit；之后 8 次重复请求均命中。首次请求的命中数受线程调度影响，不作为性能阈值。

## 独立 getter 复核与失败缓存诊断

使用相同 .NET 8.0.31、Release/net8.0 库与探针，仅执行 `--getter-only` 场景；基线和候选交替启动五轮。直接属性读取用于观察运行时与进程波动，委托 getter 是被测入口。下表每格为 `ns/次`，两种入口的每轮分配量在基线和候选中均为 `24 B/次`。

| 轮次 | 基线直接读取 | 候选直接读取 | 基线委托 getter | 候选委托 getter |
| --- | ---: | ---: | ---: | ---: |
| 1 | 14.8 | 15.3 | 18.5 | 14.2 |
| 2 | 14.8 | 15.6 | 16.0 | 13.6 |
| 3 | 15.9 | 14.9 | 14.7 | 16.1 |
| 4 | 15.2 | 15.2 | 15.9 | 16.2 |
| 5 | 14.9 | 15.2 | 16.9 | 14.7 |
| 中位数 | 14.9 | 15.2 | 16.0 | 14.7 |

混合探针测得的 getter 回退没有在独立场景复现，现有数据不支持为 getter 增加专门的性能改写。缓存探针还对不存在的构造函数连续查询两次，结果为未命中 1、失败复用 1、等待 0；重复热访问断言构造扫描、委托编译、动态类型 Emit 次数不增长。

复核命令：`dotnet benchmarks/ReflectionComparisonProbe/bin/Release/net8.0/ReflectionComparisonProbe.dll <Bing.Utils.dll> <Bing.Utils.Reflection.dll> --getter-only`；缓存诊断使用 `dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj --no-build -c ReflectionTodo -f net8.0 -- --reflection-cache` 及 `--reflection-concurrent`。独立 getter 复核所用 DLL 的 SHA-256：基线核心 `3870B72CA2E0EFE4DDDCE87F8EEEB5C128890C6BCD9A7EFC35FFB97BC8FF7B77`、基线反射 `28368F9A137185DD68D1A57793479F2C819DC4D590C144BF277838B8B0E50648`、候选核心 `7637F4A4B2BFEDA3E76ACB77B5C33B31F3A6EBAF9C96CD9ED07076A7A633CFDF`、候选反射 `E991B87DB6BFA2B35E58473155A399424DA709A32B9E16CFC118DB8F1FD8E524`；探针 DLL 为 `B329F329CDE0673669AD3D2E23C40863A75462150E0FE2301D46900ABF70D904`。候选源码尚未提交，以上校验值固定了测量二进制，但不能代替可重建的源码版本。

## 源码快照冷启动及独立写入复核

运行命令：`./benchmarks/ReflectionComparisonProbe/Run-ReflectionComparison.ps1 -BaselineRoot C:/Users/jianx/.codex/worktrees/reflection-baseline/Bing.Utils -Rounds 5`。基线仍为 `49fd7fa0e9b50ef179a89ec07685cd37c9693622`，两份源码从归档中离线还原并按同配置 Release/net8.0 构建，运行时为 .NET 8.0.31。候选归档包含当时两个库的完整源码与共享构建输入，因此也包含工作区已有的核心包改动；数据衡量整个快照，不能把全部差异归因于本轮反射改动。

每个冷启动场景、每个版本、每一轮都使用独立进程，仅测量首次入口调用（包含 JIT、扫描/编译和缓存初始化，不包含进程启动时间）。稳态写入仍按 10000 次预热、5 组各 50000 次测量。下表时间均为五轮中位数。

| 场景 | 单位/次 | 基线 | 候选 | 基线 B/次 | 候选 B/次 |
| --- | --- | ---: | ---: | ---: | ---: |
| 冷构造 | μs | 21777.0 | 31095.5 | 13120 | 38008 |
| 冷 getter 委托 | μs | 22289.4 | 20737.5 | 30656 | 23040 |
| 冷 setter 委托 | μs | 17029.4 | 19326.5 | 29240 | 22112 |
| 冷方法签名 | μs | 1787.0 | 13350.8 | 7032 | 31096 |
| 冷成员路径读 | μs | 2239.9 | 11668.0 | 5880 | 26776 |
| 冷成员路径写 | μs | 2804.8 | 15483.5 | 5984 | 27576 |
| 冷动态类型 | μs | 201351.3 | 21397.5 | 19944 | 16888 |
| 独立稳态路径写 | ns | 823.6 | 565.3 | 408 | 144 |
| 独立稳态显式选项写 | ns | 1266.6 | 618.5 | 384 | 168 |

逐轮时间：

| 场景 | 基线五轮 | 候选五轮 |
| --- | --- | --- |
| 冷构造 | 19466.4, 28956.2, 30668.6, 21777.0, 20973.4 | 39341.9, 32757.0, 31095.5, 30581.9, 30462.0 |
| 冷 getter | 23951.8, 20225.2, 27855.4, 19845.0, 22289.4 | 20737.5, 24413.6, 22007.9, 19553.2, 19653.3 |
| 冷 setter | 23882.4, 18723.3, 17029.4, 16944.6, 16472.9 | 19992.6, 19638.7, 19326.5, 15542.9, 17008.7 |
| 冷签名 | 1769.9, 2091.2, 1761.7, 2042.1, 1787.0 | 13411.3, 19306.7, 13350.8, 11169.7, 11118.6 |
| 冷路径读 | 2141.2, 1913.1, 2386.4, 2239.9, 2320.3 | 11668.0, 12685.9, 12576.9, 11534.3, 10966.0 |
| 冷路径写 | 2735.7, 2804.8, 3926.3, 2451.4, 4397.1 | 19014.1, 12288.3, 14574.1, 15483.5, 21640.7 |
| 冷动态类型 | 186969.3, 201351.3, 199898.3, 203825.3, 206730.7 | 20048.0, 20378.7, 24374.6, 21397.5, 25194.5 |
| 稳态默认写 | 1432.3, 823.6, 777.2, 873.1, 805.4 | 629.1, 565.3, 561.5, 636.0, 502.6 |
| 稳态显式写 | 1242.2, 1266.6, 1235.8, 1332.2, 1377.0 | 640.4, 524.6, 618.5, 683.3, 527.7 |

这组数据未复现稳态显式选项写入回退，但确认若干入口的首次调用成本上升；当前无经批准的发布阈值，保留该取舍，不据此声称全面提速。

本机证据目录：`benchmarks/ReflectionComparisonProbe/bin/evidence/20261004-155153-577`，包含源码 ZIP、逐文件 SHA-256 清单、二进制及 SDK/运行时清单、`raw.txt` 和 `results.csv`。该目录位于忽略的构建输出中，分享时应连同源码归档保留。基线 ZIP SHA-256 为 `02340E3AB7D87808A40A1356F5AC3A1D9B2EFF7BA54FF5AFE1BF0858C75F923D`，候选为 `874A35F693E6513A81AD3DA60FDFE5467E2560901B836706BB362BD9CCFC65FC`。脚本可创建新的独立证据目录；需要对应版本的 NuGet 包已存在本机缓存。

新增并发失败探针中，32 次构造请求只发生 1 次扫描，并复用同一个原始异常对象；本次观察为未命中 1、等待 15、失败复用 16，具体分布受调度影响。

以上归档固定的是测量时点的候选源码。后续独立审查补充的 `ReflectedType` 检测和特性非法输入统一校验不在该归档内；本组探针不调用这些特性查询入口，不能将已测数据描述为最终工作树所有入口的性能结果。

## 最终源码快照全场景复核（2026-10-05）

脚本补齐 `--steady` 和 `--getter-only` 后，使用相同固定基线、Release/net8.0、.NET 8.0.31 再执行五轮。候选源码归档包含共享严格构造绑定器、DBNull 严格赋值、属性预校验、非法枚举校验及长键绕过等本轮最终生产实现。归档仍包含工作区已有的核心包改动；测量的是两个完整库快照，不能把全部差异归因于本轮改动。普通测试和构建完成后单独运行比较，不把此前与验证任务重叠的探索性运行作为本节证据。

稳态场景沿用每进程 10000 次预热、5 组各 50000 次，冷启动场景每版本每轮使用独立进程，仅测首次调用。以下为五轮时间中位数，完整 220 条逐轮记录在 CSV 中。多数入口通过反射调用，数字包含探针开销，不是应用端的直接调用耗时。

| 场景 | 单位/次 | 基线 | 候选 | 基线 B/次 | 候选 B/次 |
| --- | --- | ---: | ---: | ---: | ---: |
| 稳态构造 | ns | 277.7 | 157.8 | 240 | 24 |
| 稳态 getter（混合进程） | ns | 6.6 | 17.4 | 24 | 24 |
| 稳态 setter | ns | 7.0 | 7.7 | 24 | 24 |
| 稳态方法签名 | ns | 1034.3 | 175.5 | 448 | 88 |
| 交替方法签名 | ns | 1130.3 | 212.8 | 552 | 120 |
| 稳态路径读 | ns | 779.1 | 369.4 | 472 | 200 |
| 稳态路径写 | ns | 707.4 | 357.2 | 408 | 144 |
| 显式选项路径读 | ns | 733.1 | 409.4 | 448 | 224 |
| 显式选项路径写 | ns | 633.0 | 376.2 | 384 | 168 |
| 相同字典动态类型 | ns | 1799.3 | 293.5 | 728 | 56 |
| 全新等价字典 | ns | 1171.8 | 582.1 | 976 | 304.4 |
| 独立直接属性读 | ns | 14.4 | 13.0 | 24 | 24 |
| 独立 getter 委托 | ns | 12.2 | 12.9 | 24 | 24 |
| 独立路径写 | ns | 491.8 | 360.0 | 408 | 144 |
| 独立显式选项路径写 | ns | 460.8 | 343.8 | 384 | 168 |
| 冷构造 | μs | 14070.4 | 21487.7 | 13120 | 37368 |
| 冷 getter 编译 | μs | 21384.0 | 18225.1 | 30656 | 23144 |
| 冷 setter 编译 | μs | 26025.1 | 24665.5 | 29240 | 22112 |
| 冷方法签名 | μs | 3536.3 | 16063.3 | 7032 | 30440 或 36632 |
| 冷路径读 | μs | 2836.0 | 18824.9 | 5880 | 26776 |
| 冷路径写 | μs | 2801.0 | 19566.6 | 5984 | 27576 |
| 冷动态类型 | μs | 188561.5 | 19653.0 | 19944 | 16888 |

冷签名候选分配量在进程间有两种观察值，因此保留两者。混合 getter 的回退仍未在独立 getter 中以相同幅度复现，且直接属性读取也有进程波动；数据支持构造、签名、路径及动态 schema 的稳态分配下降，不能概括为所有入口全面提速。构造、签名及路径的冷启动成本仍高于基线，B8 保持为依赖业务目标的专项优化 TODO，没有设置未经批准的发布阈值。

最终证据目录：`benchmarks/ReflectionComparisonProbe/bin/evidence/20261004-235810-163`。包含完整源码 ZIP、逐文件清单、探针源码/项目/执行脚本、二进制与 SDK/运行时清单、`raw.txt` 和 `results.csv`。基线 ZIP SHA-256 为 `02340E3AB7D87808A40A1356F5AC3A1D9B2EFF7BA54FF5AFE1BF0858C75F923D`，候选为 `0E596F3B8DFD830DB34ABDB13572A7C9219D5AEDFAD59FC5A66DA7428C12F939`。分享时需保留该忽略目录中的归档；脚本可从快照离线重建。

并发缓存探针另行验证：核心 Reflections 与子包 TypeVisit 混合发起 32 次首次构造，扫描和委托构建各 1 次；getter/setter、方法签名及成员路径各 32 次首次请求也仅构建 1 次，后续 32 次均命中。失败构造连续查询为未命中 1、失败复用 1；并发失败仅扫描 1 次并复用原始异常，未命中/等待/失败复用的观察数为 4/16/12。动态 schema 的 8 次请求为 1 次未命中、7 次等待、1 次 Emit，后续 8 次均命中。调度相关计数和时间不作为门禁。

## 严格容量准入复核（2026-10-06）

在 .NET 8.0.31、Release 构建下重新运行 `--reflection-cache` 和 `--reflection-concurrent`。32 次并发构造、方法签名和成员路径请求的诊断计数均与调用数一致，构造扫描/委托编译各 1 次；8 次动态 schema 请求返回同一类型且 Emit 1 次，后续 8 次全部命中。准入锁的二次检查保证缓存满时旁路解析，不再由并发 `Count` 竞态超出容量；旁路次数和稳态延迟仍需按业务高基数输入持续观测。

同一 Release/net8.0 基准工具执行 `--reflection-capacity`，为单一动态类型生成 1025 个唯一方法签名：1024 次请求进入类型级名称缓存，第 1025 次旁路解析，诊断计数为 `admitted 1024, bypass 1`，总计数与 1025 次请求一致。该探针验证资源上限和旁路观测链路，不代表业务高基数输入的延迟阈值已经确定。

## 结构体映射缓存容量复核（2026-10-09）

隔离 `net8.0` 基准工具执行 `--reflection-capacity`，新增 `struct-map` 场景，为 `MapperHelper` 生成 1025 组唯一动态结构体源/目标类型并调用公开 `Map` 入口。结果为容量 1024、请求 1025、缓存准入 1024、容量旁路 1、`Compiles=1025`，整体 `structuralPassed=true`；完整报告 schema 已升级为 3，并列出 `structMap=1024`。该场景验证有界缓存、超限旁路和重复编译计数，不把 5.27 秒的同机高基数耗时设为业务门禁。

## 结构体映射稳态复核（2026-10-09）

隔离 `net8.0` 基准工具执行 `--reflection-struct-map-steady`，为每次运行生成唯一动态结构体源/目标类型，冷调用编译 1 次，随后 256 次稳态调用编译 0 次，并验证映射后的 `Value=17`。最终确认运行的冷启动耗时/分配为 `14367.7 us / 42288 B`，稳态为 `542.1 us / 18520 B`，`structuralPassed=true`。分配量包含对象包装和探针调用开销，只证明缓存命中后的编译不重复，不作为跨机器性能阈值。

## Git 引用基线归档复核（2026-10-06）

`Run-ReflectionComparison.ps1` 新增 `-BaselineRef` 参数。使用 `-BaselineRef HEAD -Rounds 1` 时，脚本解析 commit `49fd7fa0e9b50ef179a89ec07685cd37c9693622`，通过 `git archive` 展开临时基线，离线构建基线与候选快照，并执行 `--steady`、`--getter-only`、`--path-write-only` 及 7 个冷启动模式；manifest 记录 `baselineSource=git-ref:HEAD`，运行结束后临时目录已清理。证据目录为 `benchmarks/ReflectionComparisonProbe/bin/evidence/20261006-baseline-ref-smoke`。外部干净工作树的 `-BaselineRoot` 兼容入口仍保留。

## 可选容量预算和 JSON 证据（2026-10-06）

`--reflection-capacity` 继续默认验证 1024 条目准入、1 次超限旁路和请求计数完整性；新增
`--reflection-capacity-max-bypass`、`--reflection-capacity-max-time-us` 两个可选预算，以及
`--reflection-capacity-output` UTF-8 JSON 输出。高阈值运行使用
`--reflection-capacity-max-bypass=1 --reflection-capacity-max-time-us=10000000`，报告
`structuralPassed=true`、`budgetPassed=true`、`bypass=1`、`bypassRatio=0.0009756`、`passed=true`；将旁路预算设为 0
的负向运行以非零退出，报告保留 `structuralPassed=true`、`budgetPassed=false`、`passed=false`。
预算为空时不引入业务门禁，后续可按实际输入规模和运行时规格配置 CI 数值。

## 可选冷启动和稳态预算门（2026-10-06）

`Run-ReflectionComparison.ps1` 增加四个显式预算参数：

- `-ColdStartMaxTimeNs`：限制 candidate 冷启动样本的单次时间；微秒输出会统一换算为纳秒。
- `-ColdStartMaxBytes`：限制 candidate 冷启动样本的单次线程分配。
- `-SteadyStateMaxTimeNs`：限制 candidate `--steady` 场景的单次稳态时间；微秒输出会统一换算为纳秒。
- `-SteadyStateMaxBytes`：限制 candidate `--steady` 场景的单次稳态分配。

不传参数时不执行预算判断，不改变原有比较流程；传入任一参数后，所有 candidate
`--cold=*` 行都会校验，结果写入证据目录的 `cold-start-budget.json`；传入稳态参数时，
`--steady` 行会写入 `steady-state-budget.json`。任一超限则以非零退出码结束；完整性
校验要求每轮 baseline/candidate 的 7 个冷启动模式以及所有稳态场景各恰好一条结果，缺失或重复也会失败并写入 `completenessIssues`。
本轮使用 `-BaselineRef HEAD -Rounds 1` 和仅用于验证链路的高阈值运行通过，
`expectedRows=14`、`candidateRows=7`、`complete=true`、`passed=true`；将时间预算设为 0 的负向 smoke
以非零退出并记录 7 个超限项。高阈值不是业务门禁，B8 仍需
业务提供机器规格、输入规模和正式预算。

同一轮还传入 `-SteadyStateMaxTimeNs 1e12 -SteadyStateMaxBytes 1e9` 验证稳态门，
`steady-state-budget.json` 报告 `expectedRows=22`、`candidateRows=11`、11 个稳态场景、
`complete=true`、`passed=true`；将稳态时间预算设为 0 的负向 smoke 以非零退出并记录 11 个超限项。
这些阈值同样只用于验证门禁链路，不代表业务批准的默认发布目标。

## 默认 PR 基线 smoke（2026-10-07）

`reflection.yml` 的 `baseline-comparison` job 现在在 `pull_request` 事件中自动使用
`github.event.pull_request.base.sha` 作为 `-BaselineRef`，执行三轮 `-Rounds 3` 的
`--steady`、`--getter-only`、`--path-write-only` 和 7 个冷启动模式，并上传源码快照、逐轮结果、
`summary.csv/json`、`comparison.csv` 和输入清单。该 job 只验证基线/候选都能在同一运行时和输入下完成，
未传入性能阈值，不代表业务性能预算；手动 `workflow_dispatch` 通过 `comparison_rounds`（1-20）支持指定轮数、
baseline ref 和预算。默认三轮解决了多轮证据采集问题，但默认性能门禁仍属于 B8 条件 TODO。

## 缓存指标结构化输出（2026-10-07）

`ReflectionComparisonProbe` 现在按每个测量场景输出一行 `cache-metrics` JSON，读取核心与反射缓存的命中、未命中、等待、旁路、失败和结构签名旁路计数，并记录构造委托与动态类型 Emit 构建次数。`Run-ReflectionComparison.ps1` 将这些记录保存为 `cache-metrics.csv` 和 `cache-metrics.json`，同时要求每个 baseline/candidate、round、mode、scope 恰好有一条记录。

使用既有的 baseline/candidate Release/net8.0 DLL 逐模式复核 10 个模式共 44 条记录：稳态 11 条、getter 2 条、路径写 2 条、7 个冷启动各 1 条，全部可解析且 scope 完整。冷构造指标为 1 次核心未命中和 1 次构造委托构建；冷动态类型指标为 1 次反射未命中和 1 次 Emit 构建。该证据证明指标链路可观测，不代表已经批准缓存旁路或性能预算。

使用 `-BaselineRef HEAD -Rounds 1` 的完整快照 smoke 已通过，证据目录为
`benchmarks/ReflectionComparisonProbe/bin/evidence/20261007-cache-metrics-smoke4`。
`cache-metrics.json` 报告 `expectedRows=44`、`actualRows=44`、`complete=true`；冷启动与稳态结果完整性也分别通过。
本次 baseline commit 尚未包含本轮诊断类型，因此 `availableByVariant.baseline=0`、`availableByVariant.candidate=22`；脚本保留该状态，不能将这轮结果描述为基线与候选的完整指标数值对比。

## 多轮场景摘要（2026-10-07）

`Run-ReflectionComparison.ps1` 现在将逐轮 `results.csv` 压缩为场景级 `summary.csv/json`，记录 baseline/candidate 的中位数、最小值和最大值，并生成 `comparison.csv`，记录时间/分配的绝对差值和相对变化百分比。摘要阶段要求每个场景同时存在 baseline 与 candidate，避免只看单侧结果；预算字段明确标记为关闭，不替代业务批准的阈值门禁。

使用 `-BaselineRef HEAD -Rounds 3` 的完整 smoke 已通过，证据目录为
`benchmarks/ReflectionComparisonProbe/bin/evidence/20261007-summary-rounds3`。`summary.json` 报告：

- `rounds=3`
- `summaryRows=44`
- `comparisonRows=22`
- `complete=true`
- `thresholdEnabled=false`

`summary.csv` 的 44 行覆盖两个版本和 22 个场景，`comparison.csv` 的 22 行为成对比较；`manifest.json` 同时保留缓存指标和冷启动/稳态完整性结果。该能力解决了多轮结果的可审阅性，但没有设置默认性能阈值，因此 B8 的业务预算和常规 CI 门禁仍未完成。

## 性能证据机器上下文（2026-10-07）

`Run-ReflectionComparison.ps1` 现在在 `manifest.json` 写入 `machine` 和 `probeRuntime`：
`machine` 包含操作系统描述、OS/进程架构、处理器数量、64 位进程标记以及脚本宿主框架；`probeRuntime`
来自 `ReflectionComparisonProbe` 自身的 `RuntimeInformation.FrameworkDescription`，不再把 PowerShell
脚本宿主版本误当作实际测量运行时。SDK 和已安装 runtime 列表仍单独保留。

`-BaselineRef HEAD -Rounds 1` smoke 证据目录为
`benchmarks/ReflectionComparisonProbe/bin/evidence/20261007-machine-metadata-smoke-v2`，其中：

- `probeRuntime=.NET 8.0.31`
- `sdk=8.0.425`
- `osArchitecture=X64`
- `processArchitecture=X64`
- `processorCount=22`
- `summary.complete=true`
- `cacheMetrics.complete=true`
- `coldStartBudget.complete=true`
- `steadyStateBudget.complete=true`

本轮只补齐可复现的测量上下文，没有设定业务性能阈值，也没有把该机器规格推广为发布门禁。

## 性能比较可比性契约（2026-10-07）

`Run-ReflectionComparison.ps1` 现在把比较输入形状作为强制契约：固定测量 TFM 为 `net8.0`，每个
baseline/candidate、round、mode 必须产生预期场景集合，缺失、重复或未知场景都会失败；每次 probe
还必须输出一个 runtime header，所有样本必须共享同一个 runtime 描述。manifest 同时记录 mode、场景集合、
runtime 行数和契约状态，并额外写出 `results-completeness.json`。

`-BaselineRef HEAD -Rounds 1` smoke 证据目录为
`benchmarks/ReflectionComparisonProbe/bin/evidence/20261007-comparison-contract-m66`：

- `targetFramework=net8.0`
- `expectedRows=44`、`actualRows=44`、`complete=true`
- `expectedProbeRuntimeLines=20`、`actualProbeRuntimeLines=20`
- `probeRuntime=.NET 8.0.31`、`runtimeContractPassed=true`
- `summary.complete=true`、`cacheMetrics.complete=true`

这项校验解决了“两个版本同时遗漏同一场景仍生成摘要”的证据风险，但仍不设置业务性能阈值，
也不替代正式 baseline、机器规格和发布门禁决策。

## 可审计性能策略门禁（2026-10-07）

`Run-ReflectionComparison.ps1` 新增 `-PerformancePolicyPath`。策略文件使用
`docs/quality/reflection-performance-policy.schema.json` 的 schema 1，必须提供非空
`policyId`、`targetFramework=net8.0` 和至少一个有限非负预算；可选绑定 `rounds` 与
40 位 `baselineCommit`。未知字段、TFM/轮数/基线不一致、空策略和非有限数值都会在构建前失败。

启用策略后，摘要的 `thresholdEnabled`/`thresholdSource` 会标记门禁来源，证据目录额外写出
`performance-policy.json`，记录策略 ID、目标 TFM、轮数、基线/候选 commit、冷启动/稳态完整性、
超限项和最终 `passed` 状态。workflow 的 `performance_policy_path` 只接受仓库内文件，默认空值，
因此当前 PR 流程仍不携带未经批准的数值。

本机验证：高阈值策略 smoke 通过，报告 `enabled=true`、`source=policy`、`passed=true`；零阈值
策略负向 smoke 非零退出，并在 `performance-policy.json` 中记录 14 个冷启动超限项。该能力解决
了策略输入和证据审计问题，但业务预算、机器规格和默认 PR 门禁仍需批准。

## 动态类型 schema 校验后的候选稳态诊断（2026-10-09）

针对 `TypeFactory.CreateType` 增加非标准 `IDictionary` 枚举快照和重复键校验后，使用当前工作树
构建的候选程序集执行 `ReflectionComparisonProbe --steady`。环境为 Windows x64、.NET 8.0.31；
schema 为 `Value:int, Name:string`，场景包含 10,000 次预热和 5 轮各 50,000 次测量，报告中每轮
取中位数。结果：标准 schema 缓存命中 `201.8 ns/op`、`56 B/op`；等价新 `Dictionary` 命中
`342.0 ns/op`、`304 B/op`。动态 schema 场景合计记录 1 次未命中、259,999 次命中和 1 次 Emit；
等价新字典场景为 260,000 次命中、0 次 Emit。

本项为单候选诊断，没有同环境 before 配对值，不据此声称性能提升或无回归，也不设置业务阈值。
候选程序集 SHA-256：`Bing.Utils.dll=5895EFC854F08FDBAB7F4168C771BA5083B75BFCC0B9F1090D87F08A07714D5A`；
`Bing.Utils.Reflection.dll=0E3389F7FF3B6DBAFEE64A60C7C26018D99EE709743355A8C3223E9AC1B81645`；
probe SHA-256 为 `3580CE2DFE4A19D887B013270A95F2527C47B9F61D411F1FC117B5FF9687F2A3`。
