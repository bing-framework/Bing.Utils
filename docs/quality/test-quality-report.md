# 测试质量报告（阶段5）

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
