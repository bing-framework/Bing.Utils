# 图像基本性能执行记录

对应质量报告 P2-1，规则见 [性能决策](performance-scope-decisions.md)。本次仅实现 ImageSharp、SkiaSharp 的 8 场景，不修改生产算法、包版本、TFM、CI 或反射探针。

## 实现与用法

- `benchmarks/Bing.Utils.Benchmark/Benchmarks/ImagePerformanceProbe.cs`：固定种子输入、内存解码/等比缩放/编码、8 场景 smoke、3 轮批量测量和 UTF-8 JSON。
- `benchmarks/Bing.Utils.Benchmark/Program.cs`：`--image-performance=smoke|measure` 分发入口。
- `benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj`：增加两个既有图像项目引用，无新包或版本变化。
- `benchmarks/Bing.Utils.Benchmark/Run-ImagePerformance.ps1`：校准前校验固定源码范围；冻结策略；验收检查机器、运行时、程序、输入、样本完整性和预算。

从仓库根目录执行。脚本运行于当前 PowerShell 7 环境，使用已有 .NET 8 SDK；先构建，测量过程中不构建或还原。以下目录只在不存在时创建，不覆盖旧证据：

```powershell
dotnet build benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0 --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
$runRoot = Join-Path $PWD 'benchmarks/Bing.Utils.Benchmark/bin/image-performance/todo-p2-1'
$runner = './benchmarks/Bing.Utils.Benchmark/Run-ImagePerformance.ps1'
$policy = Join-Path $runRoot 'image-local-net8-v1.json'
& $runner -Mode Smoke -OutputDirectory (Join-Path $runRoot 'smoke')
& $runner -Mode Calibrate -OutputDirectory (Join-Path $runRoot 'calibration') -InputDirectory (Join-Path $runRoot 'smoke/inputs') -PolicyPath $policy
& $runner -Mode Verify -OutputDirectory (Join-Path $runRoot 'verification') -PolicyPath $policy
```

首次增加项目引用时需还原，可使用现有全局包缓存作为离线 source。本轮命令：

```powershell
dotnet restore benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -p:TargetFrameworks=net8.0 --source C:/Users/jianx/.nuget/packages --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

校准要求图像、Shared、核心包及共享构建输入与固定 SHA `c6ff0a4bfd5802cbf28ccea72fee9f5f2d8d732e` 无差异、无未跟踪输入。已确认当前相关源码一致，因此复用本次构建测固定基线，不重复归档或构建相同源码。其他目录的未提交修改不归入图像结果。

后续只运行 Verify 并复用已冻结策略；不得换输出目录来绕过 Calibrate 对既有策略的拒绝覆盖。机器、实际 runtime、测量程序或输入变化时停止比较，不自动放宽阈值。脚本不自动构建，调用前必须完成当前源码的 Release 构建。

## 测量范围与限制

- 4 份固定 RGB 输入供两后端共享；输出宽 256，高分别 256/144；JPEG 质量 80，PNG 按既有编码行为，Contain、不放大、移除元数据、不设置目标字节数。
- 每批 8 张，每轮 2 批预热和 5 批测量，3 轮；样本及两级中位数保留在 `results.json`。`Stopwatch` 计批耗时，`GC.GetTotalAllocatedBytes(true)` 计整个进程的托管分配增量，包含后端托管工作线程。
- 输入生成、预热和文件写入在测量外；smoke 额外解码检查不进入性能样本。底层处理释放图像资源；结果字节由 GC 管理，未把强制 GC 计入批处理。
- 托管分配不是 Skia 原生内存、RSS 或泄漏证明；相同生产源码的通过结果验证负载与预算流程，不代表优化收益。
- `identity.json` 保存 CPU、物理内存、OS、电源方案、SDK、实际 runtime、程序和程序集 SHA-256。机器信息查询受限时需要环境权限，不伪造配置。
- 每次探针运行最多 30 分钟，到期终止进程树并报未完成。生成输入、策略和原始样本均位于 Git 忽略的 `bin/` 中，不新增受版本管理的基线快照。

## 本轮验证

已完成离线还原、Release/net8.0 编译、8/8 场景 smoke、固定基线校准和一次候选验收。分配量 8/8 达标、耗时 4/8 达标，整体性能门禁未通过；P2-1 保持 PARTIAL，不关闭。

固定基线校准已完成，实际环境为 Windows 10.0.22631、Intel Core Ultra 9 185H、22 个逻辑处理器、33,685,827,584 字节物理内存、平衡电源方案、SDK 8.0.425、.NET 8.0.31、X64。阈值文件 `image-local-net8-v1.json` 已冻结，以下仅展示舍入值，实际验收使用 JSON 原值：

| 场景 | 基线毫秒/批 | 上限毫秒/批 | 基线托管字节/批 | 上限托管字节/批 |
| --- | ---: | ---: | ---: | ---: |
| ImageSharp 512 JPEG | 54.74 | 65.69 | 1,867,352 | 2,054,088 |
| ImageSharp 512 PNG | 1,688.00 | 2,025.60 | 16,745,688 | 18,420,257 |
| ImageSharp 1920 JPEG | 297.19 | 356.63 | 841,368 | 925,505 |
| ImageSharp 1920 PNG | 8,619.17 | 10,343.00 | 8,264,288 | 9,090,717 |
| SkiaSharp 512 JPEG | 88.61 | 106.33 | 822,464 | 904,711 |
| SkiaSharp 512 PNG | 1,333.84 | 1,600.61 | 7,025,824 | 7,728,407 |
| SkiaSharp 1920 JPEG | 920.30 | 1,104.36 | 315,200 | 346,720 |
| SkiaSharp 1920 PNG | 13,084.09 | 15,700.91 | 3,563,584 | 3,919,943 |

重复校准的拒绝覆盖检查已通过，策略 SHA-256 保持不变；该检查在启动测量前退出，没有重跑基线。

候选结果如下；时间为两级中位数，单位毫秒/8 张，分配为托管字节/8 张：

| 场景 | 候选耗时 | 候选分配 | 预算结果 |
| --- | ---: | ---: | --- |
| ImageSharp 512 JPEG | 144.52 | 1,868,736 | 耗时超限 |
| ImageSharp 512 PNG | 2,111.18 | 16,744,344 | 耗时超限 |
| ImageSharp 1920 JPEG | 418.12 | 841,536 | 耗时超限 |
| ImageSharp 1920 PNG | 9,006.52 | 8,264,288 | 通过 |
| SkiaSharp 512 JPEG | 117.71 | 822,464 | 耗时超限 |
| SkiaSharp 512 PNG | 1,484.29 | 7,035,512 | 通过 |
| SkiaSharp 1920 JPEG | 477.15 | 315,200 | 通过 |
| SkiaSharp 1920 PNG | 9,141.22 | 3,563,296 | 通过 |

两次运行的全部 5 个程序集 SHA-256、程序源码指纹、4 个输入 SHA-256、runtime 和已记录机器字段均相同。ImageSharp 512 JPEG 的三轮批中位数由基线 `78.85/54.74/47.62` 毫秒变为候选 `129.65/168.45/144.52` 毫秒；同一二进制存在显著耗时波动。已有字段不足以区分后台负载、调度或频率等原因，不能据此归因代码回归，也不能声明已定位具体机器原因。

状态：Implementation PASS（负载及运行入口完成）；Tests PASS；Performance Gate NOT_PASS；待稳定测量环境或外部独立复核，`BLOCKED_EXTERNAL`。不修改生产算法、阈值、负载或轮次来追求通过；本机同候选已完成一次校准和一次确认，不继续重跑。环境依赖就绪后再验收，不自动重新校准。

证据根目录：`benchmarks/Bing.Utils.Benchmark/bin/image-performance/todo-p2-1/`。`smoke/results.json` 保存冒烟；`calibration/results.json` 保存 120 个原始样本及聚合结果；`image-local-net8-v1.json` 保存冻结阈值；`verification/results.json` 保存另 120 个样本，`verification/comparison.json` 明确记录 `passed=false`；两侧 `identity.json` 保存身份。基线完成时间为 2026-10-15 09:36:25 UTC，候选为 09:45:50 UTC；所有失败结果保留，没有另跑一份替代。

影响分析：ChangedFiles 为 4 个基准文件及文档；ChangedProjects 为 Bing.Utils.Benchmark；公共契约、生产运行路径、provider、TFM 和生产打包均未改变；构建图仅增加两个既有图像项目引用，BenchmarkHarness 有变。AffectedDependents 为基准程序使用者，Risk LOW。

验证分级：L0 为 Release 编译、PowerShell 静态解析及文本检查；L1 为 8 场景 smoke 和冻结策略拒绝覆盖检查；L5 为上述唯一一次校准与确认。L2/L3/L4 不重跑，生产相关范围未变，复用 [图像实现记录](image-processing-execution.md) 的既有正确性证据，不声称本轮运行全量测试。实现 1/1，本地三项门禁（编译、冒烟、性能）2/3；本条目无剩余可通过改代码解决的已知问题，No-Progress CHANGED，Goal Status STOPPED_BLOCKED。

最终静态检查：`git diff --check` 和 PowerShell 解析通过，9 个本轮变更文件严格 UTF-8 解码通过，5 份文档中的 47 个本地链接有效。生产相关范围仍与固定 SHA 无差异，程序指纹与测量版本相同；未暂存或提交任何文件。

编译的沙箱诊断遇到既有 Reflection、Drawing.Shared 的 obj 缓存写权限限制；相同命令经权限流程通过，保留现有 nullable/字体类型等警告。没有修改生产代码或忽略检查来消除这些警告。
