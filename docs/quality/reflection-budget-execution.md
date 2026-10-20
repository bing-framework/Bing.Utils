# 反射四项预算校准与策略落地

日期：2026-10-15。依据 [已确定规则](performance-scope-decisions.md)，独立于质量 P2-1 执行；未重跑图像验证。

## 当前结论

- 实现完成：沿用现有 comparison 和 schema 1，冻结四项本机预算，补齐测量限时和超限证据保存。
- 构建与工具校验通过：两个阶段的基线、候选和探针均 Release/net8.0 编译成功；PowerShell 语法、策略 schema、结果/缓存指标完整性通过。
- 性能验收未通过：冷启动耗时/分配及稳态分配通过，稳态耗时 32/33 条通过；第一轮 `method signature` 为 2744.2 ns，上限 2376 ns。正式命令退出码 1，`performance-policy.json` 的 `passed=false`。
- `Implementation: 2/2 PASS`（比较流程必要修订、校准策略落地）；`Budget Gates: 3/4`；`Performance: NOT_PASS`；`External Gate / Release: BLOCKED_EXTERNAL`；`Goal: STOPPED_BLOCKED`。本批 `OPEN_ACTIONABLE=0`，不把超限转换成未经定位的生产代码修改任务。

## 实现与用法

关键文件：

- `benchmarks/ReflectionComparisonProbe/Program.cs`：原有 7 个冷启动、11 个稳态场景，外加 getter/path-write 独立复核；本轮未修改。
- `benchmarks/ReflectionComparisonProbe/Run-ReflectionComparison.ps1`：源码归档、离线还原/构建、交替测量、完整性检查及逐轮预算判断。新增构建后开始的 30 分钟总测量窗口；超时终止当前进程树并写 `measurement-timeout.json`。预算失败也先保存完整 manifest、脚本和程序集身份再非零退出。
- [冻结策略](reflection-performance-policy.local-net8.json)：仅含既有 schema 1 字段，不新增比较器、宿主或配置字段。
- [既有 schema](reflection-performance-policy.schema.json)：保持通用策略契约，不把本机数值设为所有调用的默认值。

本轮实际执行（在仓库根目录，两个输出目录在执行前均不存在）：

```powershell
& ./benchmarks/ReflectionComparisonProbe/Run-ReflectionComparison.ps1 `
  -BaselineRef c6ff0a4bfd5802cbf28ccea72fee9f5f2d8d732e -Rounds 3 `
  -OutputDirectory D:/Bing_Framework/Bing.Utils/benchmarks/ReflectionComparisonProbe/bin/evidence/20261015-budget-calibration

# 从上述运行的 baseline 样本计算并冻结策略后，执行唯一一次正式确认
& ./benchmarks/ReflectionComparisonProbe/Run-ReflectionComparison.ps1 `
  -BaselineRef c6ff0a4bfd5802cbf28ccea72fee9f5f2d8d732e -Rounds 3 `
  -PerformancePolicyPath D:/Bing_Framework/Bing.Utils/docs/quality/reflection-performance-policy.local-net8.json `
  -OutputDirectory D:/Bing_Framework/Bing.Utils/benchmarks/ReflectionComparisonProbe/bin/evidence/20261015-budget-verification
```

以上是已执行命令，不是建议立即重跑。后续显式验收需使用新的输出目录，并先确认机器环境与本策略适用条件一致。已有 workflow 的手动 `performance_policy_path` 可引用仓库内策略；本机策略不适用于未知或共享 runner，默认 PR 仍只报告趋势。本轮不修改 CI。

## 校准依据与冻结值

两阶段均 `-Rounds 3`，每阶段应有 132 条结果、132 条缓存指标和 60 行 runtime header。首次运行不启用预算，校准只选择 `variant=baseline`：`mode=--cold=*` 的 21 条、`mode=--steady` 的 33 条；排除 getter/path-write 独立复核。确认无缺失、重复及非法数值，将 us 乘 1000 转为 ns，再分别取全部轮次和场景的最大值 B。没有使用候选值计算上限，正式确认中的新基线样本也不反写阈值。

| 字段 | 基线最大值 B | 批准公式 | 冻结上限 |
| --- | ---: | --- | ---: |
| `coldStartMaxTimeNs` | 25186700 ns | `max(ceil(B*1.25), B+1000000)` | 31483375 ns |
| `coldStartMaxBytes` | 47736 B | `max(ceil(B*1.10), B+1024)` | 52510 B |
| `steadyStateMaxTimeNs` | 1979.7 ns | `max(ceil(B*1.20), B+50)` | 2376 ns |
| `steadyStateMaxBytes` | 544 B | `max(ceil(B*1.10), B+16)` | 599 B |

`policyId=reflection-local-net8-v1`，`rounds=3`，`baselineCommit=c6ff0a4bfd5802cbf28ccea72fee9f5f2d8d732e`，`targetFramework=net8.0`。策略 SHA-256：`30980471CA88B09C00930643046832230EC0FDE973B26408F6E42499FCAE9234`；正式运行输入副本与冻结文件一致，未放宽或重新校准。

## 正式确认结果

| 指标 | 候选样本数 | 候选最大值 | 上限 | 达标数 |
| --- | ---: | ---: | ---: | ---: |
| 冷启动耗时 | 21 | 28365300 ns | 31483375 ns | 21/21 |
| 冷启动分配 | 21 | 41544 B | 52510 B | 21/21 |
| 稳态耗时 | 33 | 2744.2 ns | 2376 ns | 32/33 |
| 稳态分配 | 33 | 544 B | 599 B | 33/33 |

唯一超限场景 `--steady / method signature` 的逐轮值如下；字节数均为 392 B/次：

| 轮次 | 校准基线 ns | 正式确认基线 ns | 正式确认候选 ns |
| --- | ---: | ---: | ---: |
| 1 | 1979.7 | 3031.1 | 2744.2 |
| 2 | 1456.2 | 1947.9 | 1529.5 |
| 3 | 1545.5 | 1756.5 | 2056.0 |

正式确认第一轮基线本身也高于冻结时间上限；现有门禁按设计只判断 candidate。两阶段同侧源码清单、探针及脚本完全相同，仍观察到时间波动，当前证据不能定位为生产代码回归，也不能证明只是某一种环境原因。保留失败，等待稳定专用环境复核；不因总体中位数或另外两轮通过而覆盖逐轮失败。

## 环境、依赖与身份

- Windows x64，OSDescription `Microsoft Windows 10.0.22631`；Intel Core Ultra 9 185H，22 逻辑处理器；物理内存 33685827584 B；平衡电源方案 `381b4222-f694-41f0-9685-ff5bb260df2e`。未修改电源设置。
- SDK 8.0.425（`global.json`），被测 runtime `.NET 8.0.31`；PowerShell 宿主 `.NET 10.0.12` 与被测 runtime 分开记录。使用已有 NuGet 本地缓存，无依赖升级。
- 运行前未发现并行 dotnet/MSBuild 基准或构建；两个阶段顺序执行。原始测量文件时间分别约为 17:56:36–17:56:54、17:59:02–17:59:26（UTC+8），未达到 30 分钟窗口；未执行超时强制注入测试。
- HEAD 为固定 SHA，反射生产 Git 范围无差异/未跟踪文件，原 15 个未提交文件保留。未修改生产代码、API、TFM、包配置或诊断基线。
- 校准/确认的同侧源码清单逐项一致。基线/候选共享 461 个路径，其中 92 个文件仅 CRLF/LF 不同；候选归档另含 26 个既有忽略输出/文档产物。这些额外文件不是测量 DLL；实际加载的是快照本次构建的 `output/release/net8.0` DLL，路径及哈希均在 raw/manifest 中。没有删除用户原有产物。
- 不同快照编译的库 DLL 哈希不相同，不声称二进制完全相同。探针 DLL 两次相同：`F6B6BEE061A1CF5C952EED634A42992E321920F7F55BBEB4D95E29259E0F8E5B`；比较脚本两次相同：`B7470FFC1961D7CD1D35A7939DC3BB7920588543DDD13D9E72A95A4E100BAB52`。全部库 DLL 身份保存在各 manifest。

## 证据位置与校验

原始输出沿用既有忽略目录，未提交生成快照：

- `benchmarks/ReflectionComparisonProbe/bin/evidence/20261015-budget-calibration/`：首次无预算结果及固定基线源码归档。
- `benchmarks/ReflectionComparisonProbe/bin/evidence/20261015-budget-verification/`：正式策略输入副本、逐轮结果、失败预算报告、完整 manifest 和源码归档。

两目录均有 `raw.txt`、`results.csv`、`results-completeness.json`、`summary.csv/json`、`comparison.csv`、`cache-metrics.csv/json`、`cold-start-budget.json`、`steady-state-budget.json`、`performance-policy.json`，以及现有流程的源码/程序集身份记录。正式目录额外有 `performance-policy-input.json`。分享证据时需一并保留这些本地原始文件；文档中的摘要不能替代原始数据。

| 文件 | SHA-256 |
| --- | --- |
| 校准 `results.csv` | `8461D1D72C08CBC7F18F2478FF1DD7ADAF8A57EE7A7701CAD972005983BEDDCB` |
| 校准 `manifest.json` | `C87E0B4B3DE563F21DC347A390CA36B44650CD623230E992E6B155FAF02ACFBE` |
| 确认 `results.csv` | `E6EF13274A6FFD89E6D3A26754BA158FE7027CE4EAE33C710550A5E2616FACAC` |
| 确认 `manifest.json` | `8480A0E83D2BC6BF2465E61B2F73D3EB1D390F897E6F80A0E35CBA2B6B27E602` |

## 限制与后续状态

- 四项预算是跨场景统一上限；可能掩盖较快场景的局部退化。门禁逐轮判断，不只判断三轮中位数。
- 冷启动为现有探针的首次被测调用，包含其反射/诊断开销，不是整个进程启动时长；稳态同样包含既有探针调用开销。托管分配不是 RSS、原生内存或泄漏证明。
- 策略 schema 不携带机器身份；脚本校验 TFM、轮次、基线和运行时一致性，使用者还需按本页核对机器及环境。不能直接移作生产 SLA 或默认共享 CI 上限。
- 已复用此前反射功能/契约验证，生产及探针源码未变，不重跑完整单元、trim/AOT、容量矩阵、图像或视觉测试。必要工具验证为脚本语法、schema、结果完整性、上述两次构建/运行和文档静态检查。
- `CLOSED`：校准、冻结策略和现有消费链路；`BLOCKED_EXTERNAL`：稳定环境的性能验收、远程 CI/真实消费者/发布；`DEFERRED`：默认 PR 绝对数值门禁、业务容量/告警和更多插件矩阵；新增 `BLOCKED_APPROVAL` / `NOT_APPLICABLE` / `VERIFIED_BOUNDARY`：无。统一预算限制为既有 `ACCEPTED_LIMITATION`。
- No-Progress：CHANGED；已完成一次校准及一次正式确认，失败原因尚未定位，无本批可执行代码缺陷。下一动作 STOP，等待环境变化或新的可定位问题，不以失败状态驱动自动第三次实验。
