# 测试质量报告（阶段5）

> 2026-10-15 P0-2 验收补齐：核对原计划发现，异步写入生产修复及单次覆盖／追加测试已完成，但本节要求的并发／重复写入回归此前缺失。本轮在 `tests/Bing.Utils.Tests/Bing/IO/FileHelperWriteRegressionTest.cs` 新增 2 项回归，验证重复覆盖（含短内容、空内容）、返回后独占重开及 8 个不同文件并发写入的完整内容。net8.0 Release 编译通过，相关测试 5/5 通过、0 失败、0 跳过；P0-2 关闭。生产代码未改，同一路径并发串行化不属于本项契约。命令及证据见 [TODO 执行记录](todo-execution.md) 当前批次；下文保留原阶段记录。

> 2026-10-15 核对：本文 P0、P1-1 已完成；P1-2 的 net8 目标及 net6/net8 nightly 已实现，实际 CI 待验证。覆盖计划 P2-1 的 HTTP 失败链路此前已完成。本轮完成本文 P2-2 的异常输入模板化回归：补格式化空输入、非法正则及显式 null/空选择器，复用既有非法签名用例；net8 Release 编译通过，Text 定向 72/72、Reflection 定向 67/67。P2-1 图像性能预算仍待批准，不将本轮正确性结果作为性能验收。当前范围与证据见 [TODO 执行记录](todo-execution.md)，HTTP 历史结果见 [HTTP 后续记录](http-security-reliability-report.md)。下文保留原阶段记录。

> 2026-10-15 P2-1 实施：已按 [性能决策](performance-scope-decisions.md) 完成 8 场景负载、smoke、校准和预算检查；net8 Release 编译及 smoke 8/8 通过，候选分配量 8/8、耗时 4/8 达标，整体性能门禁未通过。P2-1 为 PARTIAL / BLOCKED_EXTERNAL；同程序集耗时差异待稳定环境复核，不放宽阈值或自动重跑。见 [执行证据](image-performance-execution.md)。上文“预算待批准”是此前状态。

> 2026-10-15 决策补充：P2-1 的两后端负载、机器、测量轮次及校准预算规则已确定，转为可执行，尚未实施或验收。以 [图像性能与反射决策](performance-scope-decisions.md) 为准；上文“预算待批准”是此前状态。

## 1. 本轮完成范围

- 已补齐 P0（图像相关核心单测）：`ImageSharpHelper`、`SkiaSharpHelper`、`CaptchaBuilder` 的正向与边界行为。  
  [证据] `tests/Bing.Utils.Drawing.ImageSharp.Tests/Bing/Drawing/ImageSharpHelperTest.cs:10`  
  [证据] `tests/Bing.Utils.Drawing.ImageSharp.Tests/Bing/Drawing/ImageSharpHelperTest.cs:118`  
  [证据] `tests/Bing.Utils.Drawing.SkiaSharp.Tests/Bing/Drawing/SkiaSharpHelperTest.cs:8`  
  [证据] `tests/Bing.Utils.Drawing.SkiaSharp.Tests/Bing/Drawing/SkiaSharpHelperTest.cs:119`  
  [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:52`

- 已补齐 P1（基础库扩展单测）：Collections、DateTime、Http、IdUtils、Reflection、Text。  
  [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/DictsAndCollConvTest.cs:10`  
  [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Conversions/ConvertersAndNodaExtensionsTest.cs:8`  
  [证据] `tests/Bing.Utils.DateTime.Tests/NodaTime/NodaExtensionsTest.cs:6`  
  [证据] `tests/Bing.Utils.Http.Tests/Bing/Net/IPv6/IPv6AddressAnalyzerAndPoolTest.cs:13`  
  [证据] `tests/Bing.Utils.Http.Tests/Bing/Parameters/UrlParameterBuilderExtensionsTest.cs:11`  
  [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/GuidJudgeTest.cs:6`  
  [证据] `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisitPropertiesTest.cs:6`  
  [证据] `tests/Bing.Utils.Tests/Bing/Text/StringCollectionExtensionsTest.cs:6`

- 已补必要集成测试（Http 真实链路）：BearerToken、成功/失败回调、流读取。  
  [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Advanced.cs:15`  
  [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Advanced.cs:25`  
  [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Advanced.cs:40`  
  [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Advanced.cs:65`

## 2. 覆盖面与深度判断

| 子包 | 当前深度 | 判断依据 |
|---|---|---|
| `Bing.Utils` | 高 | 仍由聚合测试工程承载大量基础能力回归（I/O/Helpers/Extensions 等）。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:19` |
| `Bing.Utils.Collections` | 中 | 有独立测试工程并补齐 `Dicts/ReadOnlyDicts/CollConv`，但仍非全量 API 覆盖。[证据] `tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj:11` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/DictsAndCollConvTest.cs:7` |
| `Bing.Utils.DateTime` | 高 | 独立工程 + 转换器/Noda 扩展边界已补，原有日期计算用例较多。[证据] `tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj:11` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Conversions/ConvertersAndNodaExtensionsTest.cs:26` |
| `Bing.Utils.Drawing` | 中 | `CaptchaBuilder` 已加入异常路径断言，但图像渲染质量与并发尚未系统化压测。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:52` |
| `Bing.Utils.Drawing.ImageSharp` | 中-高 | 关键 I/O 与参数边界已覆盖，主要风险转向格式兼容与性能回归。[证据] `tests/Bing.Utils.Drawing.ImageSharp.Tests/Bing/Drawing/ImageSharpHelperTest.cs:91` [证据] `tests/Bing.Utils.Drawing.ImageSharp.Tests/Bing/Drawing/ImageSharpHelperTest.cs:143` |
| `Bing.Utils.Drawing.SkiaSharp` | 中-高 | 关键转换与异常路径已覆盖，仍缺跨平台编解码差异用例。[证据] `tests/Bing.Utils.Drawing.SkiaSharp.Tests/Bing/Drawing/SkiaSharpHelperTest.cs:43` [证据] `tests/Bing.Utils.Drawing.SkiaSharp.Tests/Bing/Drawing/SkiaSharpHelperTest.cs:144` |
| `Bing.Utils.Http` | 高（但有实现风险） | 单测 + Integration 双层覆盖，新增回调与流链路；同时暴露实现层缺陷需修复。[证据] `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11` [证据] `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:40` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Advanced.cs:40` |
| `Bing.Utils.IdUtils` | 高 | 独立工程覆盖广，新增 `GuidJudge` 补齐基础判定分支。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/GuidJudgeTest.cs:22` |
| `Bing.Utils.Reflection` | 中 | 新增 `TypeVisit` 属性访问边界；仍主要依赖聚合工程回归。[证据] `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisitPropertiesTest.cs:26` |
| `Bing.Utils.Text` | 中 | 新增集合拼接语义覆盖，但格式化、解析类仍需继续加密度。[证据] `tests/Bing.Utils.Tests/Bing/Text/StringCollectionExtensionsTest.cs:14` |

## 3. 关键风险（按优先级）

### P0

1. `HttpRequest.SendBefore` 逻辑条件疑似错误：当前判断 `SendAfterAction` 而非 `SendBeforeAction`，可能导致回调失效或空引用。  
   [证据] `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs:956`  
   [证据] `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs:958`

2. `FileHelper.WriteAsync(byte[], ...)` 在目标文件存在时调用 `File.Create(filePath)` 后未释放句柄，随后以 `FileMode.Open` 打开，存在文件锁冲突风险。  
   [证据] `src/Bing.Utils/Bing/IO/FileHelper.Write.cs:269`  
   [证据] `src/Bing.Utils/Bing/IO/FileHelper.Write.cs:270`  
   [证据] `src/Bing.Utils/Bing/IO/FileHelper.Write.cs:274`

### P1

1. 上传集成链路对“仅文件名路径”写入不稳：控制器直接以 `file.FileName` 写文件，底层目录创建对无目录路径会抛异常。  
   [证据] `tests/Bing.Utils.Http.Tests.Integration/Controllers/Test6Controller.cs:23`  
   [证据] `tests/Bing.Utils.Http.Tests.Integration/Controllers/Test6Controller.cs:42`  
   [证据] `src/Bing.Utils/Bing/IO/DirectoryHelper.cs:30`  
   [证据] `src/Bing.Utils/Bing/IO/DirectoryHelper.cs:33`

2. Integration 工程多目标框架未做全矩阵回归（当前只做 net7.0 定向回归）。  
   [证据] `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:4`

## 4. 后续建议（可执行）

### P0（下一个提交优先）

1. 修复 `SendBefore` 判断条件，并新增 2 个回归用例：`OnSendBefore` 被调用、`OnSendAfter` 存在但 `OnSendBefore` 为空时不抛异常。  
2. 修复 `FileHelper.WriteAsync(byte[], ...)` 文件句柄问题，并新增并发/重复写入回归用例。

### P1

1. 为上传链路增加“文件名无目录”“重复文件名”“并发上传”三类集成用例。  
2. 将 Http Integration 回归扩展到 `net6.0/net8.0`（至少 nightly 全跑一次）。

### P2

1. 给 Drawing（ImageSharp/SkiaSharp）补充基本性能基准（分配量 + 批量处理耗时阈值）。  
2. 为 Reflection/Text 增加“异常输入模板化测试”（null/empty/invalid）。

## 5. 本阶段执行记录

- 新增集成测试提交：`e161264`。  
- 通过的定向命令：  
  - `dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -f net7.0 --filter "FullyQualifiedName~HttpClientServiceTest.Test_Get_|FullyQualifiedName~HttpClientServiceTest.Test_GetStream_1"`
