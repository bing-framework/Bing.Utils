# Bing.Utils 能力地图
## 1. 仓库定位与设计目标
- `Bing.Utils` 是多包（multi-package）基础设施工具库集合，解决通用能力（集合、时间、文本、反射、网络、ID、图像、HTTP），不是业务域模型。证据：解决方案同时挂载多个 `src` 子包与对应 `tests` 项目（`Bing.Utils.sln:6`, `Bing.Utils.sln:22`, `Bing.Utils.sln:24`, `Bing.Utils.sln:26`, `Bing.Utils.sln:42`, `Bing.Utils.sln:44`, `Bing.Utils.sln:71`, `Bing.Utils.sln:87`, `Bing.Utils.sln:93`, `Bing.Utils.sln:99`, `Bing.Utils.sln:103`）。
- 测试资产在解决方案层以 `02-tests` 逻辑分组管理（Solution Folder），物理工程位于 `tests/*`。证据：`Bing.Utils.sln:28`, `Bing.Utils.sln:30`, `Bing.Utils.sln:73`, `Bing.Utils.sln:83`, `Bing.Utils.sln:99`, `Bing.Utils.sln:103`。
- 构建层采用统一公共属性文件而非 `Directory.Build.props`（仓库内显式使用 `common.props` / `common.tests.props`）。证据：解决方案项引用公共 props（`Bing.Utils.sln:60`, `Bing.Utils.sln:61`）；各库普遍 `Import ..\\..\\common.props`（如 `src/Bing.Utils.Text/Bing.Utils.Text.csproj:8`, `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:12`）；测试统一 `Import common.tests.props`（如 `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:8`）。
- 目标框架策略是“跨版本兼容优先”：主库统一支持 `net8.0;net7.0;net6.0;netstandard2.0`。证据：`common.props:3`。
- 测试体系是“通用测试基座 + 分包测试 + 聚合测试”。证据：`common.tests.props` 统一引入 xUnit/Shouldly/Moq（`common.tests.props:10`, `common.tests.props:12`, `common.tests.props:31`, `common.tests.props:33`）；分包测试项目存在（如 `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11`, `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11`）；聚合测试引用多个子包（`tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` 到 `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20`）。

## 2. 子包职责矩阵（包名 / 职责 / 典型类型 / 依赖）
| 包名 | 职责（解决什么） | 边界（不解决什么） | 典型类型 | 依赖 |
|---|---|---|---|---|
| `Bing.Utils` | 核心通用工具、扩展与基础能力承载 | 不聚焦某一垂直领域（如仅图像或仅HTTP） | `FileHelper`（`src/Bing.Utils/Bing/IO/FileHelper.cs:11`），`BooleanExtensions`（`src/Bing.Utils/Bing/Extensions.Boolean.cs:8`），`JsonOptions`（`src/Bing.Utils/Bing/JsonOptions.cs:6`） | 外部包 `Newtonsoft.Json`、`AspectCore.Extensions.Reflection` 等（`src/Bing.Utils/Bing.Utils.csproj:16` 到 `src/Bing.Utils/Bing.Utils.csproj:20`） |
| `Bing.Utils.Collections` | 集合/字典操作工具与只读集合转换 | 不实现持久化、缓存中间件 | `Dicts`（`src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:6`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Collections/references.props:4`） |
| `Bing.Utils.DateTime` | 时间跨度、日期扩展、NodaTime 扩展 | 不提供调度系统/定时任务框架 | `DateTimeSpanExtensions`（`src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:8`），`TimeOptions`（`src/Bing.Utils/Bing/Date/TimeOptions.cs:6`） | 依赖 `Bing.Utils`（`src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:11`），外部 `NodaTime`（`src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:15`） |
| `Bing.Utils.Drawing` | 基于 `System.Drawing` 的图像处理与验证码 | 不做前端渲染组件 | `CaptchaBuilder`（`src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:11`），`ImageHelper`（`src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.cs:10`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:15`），外部 `System.Drawing.Common`（`src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:19`） |
| `Bing.Utils.Drawing.ImageSharp` | 基于 ImageSharp 的图像操作封装 | 不依赖 `Bing.Utils` 核心包（当前无项目引用） | `ImageSharpHelper`（`src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:10`） | 外部 `SixLabors.ImageSharp`（`src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj:15`） |
| `Bing.Utils.Drawing.SkiaSharp` | 基于 SkiaSharp 的图像格式/处理扩展 | 不与 `System.Drawing` API 绑定 | `SKEncodedImageFormatExtensions`（`src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:10`） | 外部 `SkiaSharp`（`src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj:15`） |
| `Bing.Utils.Http` | HTTP 上下文、请求响应、下载、参数与IPv6工具 | 不实现完整 HTTP 客户端 SDK（更偏 Web 运行时辅助） | `Web`（`src/Bing.Utils.Http/Bing/Helpers/Web.cs:17`），`HttpRequestExtensions`（`src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:12`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Http/references.props:4`）；按 TFM 引用 ASP.NET Core（`src/Bing.Utils.Http/dependency.props:15` 到 `src/Bing.Utils.Http/dependency.props:33`） |
| `Bing.Utils.IdUtils` | 多种 ID 生成（Snowflake/ObjectId/Guid风格） | 不做分布式协调服务（仅算法与本地生成） | `SnowflakeGenerator`（`src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:8`），`ObjectId`（`src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18`） | 依赖 `Bing.Utils`（`src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj:11`） |
| `Bing.Utils.Reflection` | 反射访问、类型访问器、动态类型辅助 | 不做 DI 容器本体 | `TypeMetaVisitExtensions`（`src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:38`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Reflection/dependency.props:3`） |
| `Bing.Utils.Text` | 字符串分割/拼接/截断等文本处理 | 不做全文检索引擎 | `Splitter`（`src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Text/Bing.Utils.Text.csproj:11`） |
| `Bing.Utils.Net` | FTP 能力封装 | 不覆盖完整网络协议栈 | `FtpClient`（`src/Bing.Utils.Net/Bing/Net/FTP/FtpClient.cs:10`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Net/references.props:4`），外部 `FluentFTP`（`src/Bing.Utils.Net/dependency.props:3`） |
| `Bing.Utils.Comments` | 读取 C# XML 注释、枚举注释映射 | 不提供源码解析器（Roslyn） | `CsCommentReader`（`src/Bing.Utils.Comments/Bing/Comments/CsCommentReader.cs:11`） | 当前无项目引用（`src/Bing.Utils.Comments/project.dependency.props:1`） |
| `Bing.Utils.Guard` | 参数守护/验证辅助（当前更偏雏形） | 待确认：完整守护 API 仍不完整 | `CharGuard`（空壳，`src/Bing.Utils.Guard/Bing/Text/CharGuard.cs:23`），`ValidationExceptionHelper`（`src/Bing.Utils.Guard/Bing/Validation/ValidationExceptionHelper.cs:11`） | 依赖 `Bing.Utils`（`src/Bing.Utils.Guard/project.dependency.props:3`） |
| `Bing.Utils.DependencyInjection` | 名义上是 DI 扩展包 | 待确认：当前仅项目壳，无实现代码 | 仅声明文件夹（`src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj:8`） | 当前未声明项目引用（`src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj:1` 到 `src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj:12`） |
| `Bing.Utils.Extra` | 扩展能力占位包 | 待确认：当前仅项目壳，无实现代码 | 仅声明文件夹（`src/Bing.Utils.Extra/Bing.Utils.Extra.csproj:7`） | 当前未声明项目引用（`src/Bing.Utils.Extra/Bing.Utils.Extra.csproj:1` 到 `src/Bing.Utils.Extra/Bing.Utils.Extra.csproj:10`） |

## 3. 公共设计约定
- 约定1：大量使用“扩展方法 + 静态工具类”。证据：`BooleanExtensions`（`src/Bing.Utils/Bing/Extensions.Boolean.cs:8`）、`DateTimeSpanExtensions`（`src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:8`）、`HttpResponseMessageExtensions`（`src/Bing.Utils.Http/Bing/Http/Extensions/HttpResponseMessageExtensions.cs:9`）、`SKEncodedImageFormatExtensions`（`src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:10`）。
- 约定2：`Options` 风格存在两类。证据：可实例化配置（`JsonOptions`，`src/Bing.Utils/Bing/JsonOptions.cs:6`）；静态运行时选项（`TimeOptions`，`src/Bing.Utils/Bing/Date/TimeOptions.cs:6`）；组件内私有 options（`SplitterOptions`，`src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:43`）。
- 约定3：异常策略以“参数前置校验 + 语义化异常 + 封装再抛”为主。证据：`Web` 中大量 `ArgumentException/InvalidOperationException`（`src/Bing.Utils.Http/Bing/Helpers/Web.cs:413`, `src/Bing.Utils.Http/Bing/Helpers/Web.cs:563`）；`FtpClient` 捕获 `FluentFTP.FtpException` 后包装为 `FtpClientException`（`src/Bing.Utils.Net/Bing/Net/FTP/FtpClient.cs:101` 到 `src/Bing.Utils.Net/Bing/Net/FTP/FtpClient.cs:104`）；`ExceptionHelper.PrepareForRethrow` 保持堆栈（`src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:14` 到 `src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:19`）。
- 约定4：工程级统一编译策略。证据：统一 `LangVersion=latest`（`common.props:8`，`common.tests.props:3`）；统一输出目录（`common.props:14`, `common.props:18`）；统一文档输出（`common.props:22`, `common.props:26`）。
- 约定5：测试基线统一使用 xUnit + Shouldly + Moq。证据：`common.tests.props:12`, `common.tests.props:31`, `common.tests.props:33`。

## 4. 跨包依赖关系
- 主干依赖拓扑（源码包）：
`Bing.Utils` <- `Bing.Utils.Collections`/`Bing.Utils.Text`/`Bing.Utils.DateTime`/`Bing.Utils.Drawing`/`Bing.Utils.Http`/`Bing.Utils.IdUtils`/`Bing.Utils.Reflection`/`Bing.Utils.Net`/`Bing.Utils.Guard`
- 证据：
`src/Bing.Utils.Collections/references.props:4`
`src/Bing.Utils.Text/Bing.Utils.Text.csproj:11`
`src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:11`
`src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:15`
`src/Bing.Utils.Http/references.props:4`
`src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj:11`
`src/Bing.Utils.Reflection/dependency.props:3`
`src/Bing.Utils.Net/references.props:4`
`src/Bing.Utils.Guard/project.dependency.props:3`
- 相对独立包：
`Bing.Utils.Drawing.ImageSharp` 与 `Bing.Utils.Drawing.SkiaSharp` 仅依赖第三方图像库，不依赖 `Bing.Utils`。证据：`src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj:15`，`src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj:15`，且其 `csproj` 无 `ProjectReference`。
- 循环依赖倾向判断：
当前源码层未见 A->B->A 的项目级闭环，依赖形态接近“单核心星型”。证据：全仓 `src` 下 `ProjectReference` 仅指向 `Bing.Utils`（`src/Bing.Utils.Collections/references.props:4`, `src/Bing.Utils.Http/references.props:4`, `src/Bing.Utils.Reflection/dependency.props:3`, `src/Bing.Utils.Net/references.props:4`, `src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj:11`, `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:11`, `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:15`, `src/Bing.Utils.Text/Bing.Utils.Text.csproj:11`, `src/Bing.Utils.Guard/project.dependency.props:3`）。
- 测试对源码映射：
`Bing.Utils.Tests`/`BingUtilsUT` 是聚合回归测试，覆盖多数子包（`tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` 到 `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20`; `tests/BingUtilsUT/BingUtilsUT.csproj:10` 到 `tests/BingUtilsUT/BingUtilsUT.csproj:19`）；分包测试一一对应（如 `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11`, `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11`, `tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj:11`）。

## 5. 新人上手路径（30/120 分钟版本）
- `30` 分钟快速上手：
1. 先看解决方案与分层：`Bing.Utils.sln:6` 到 `Bing.Utils.sln:105`。
2. 看统一构建约束：`common.props:3` 到 `common.props:44`。
3. 看核心风格入口：`src/Bing.Utils/Bing/Extensions.Boolean.cs:8`、`src/Bing.Utils/Bing/IO/FileHelper.cs:11`、`src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:8`。
4. 选一个垂直包深读：推荐 `Http`（`src/Bing.Utils.Http/Bing/Helpers/Web.cs:17`）或 `IdUtils`（`src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18`）。
5. 对应跑测试入口（只读先看映射）：`tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11` 或 `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11`。
- `120` 分钟贡献准备：
1. 建依赖心智图：先梳理所有 `ProjectReference`（`src/Bing.Utils.Collections/references.props:4` 等）。
2. 深入公共约定：异常策略与 options 模式（`src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:14`, `src/Bing.Utils/Bing/JsonOptions.cs:6`, `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:43`）。
3. 选 2 个包对照阅读：
`Text`（API 设计）`src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8`
`Reflection`（元数据能力）`src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:38`
4. 对照集成测试基座：
`tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` 到 `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20`
`tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:40`
5. 再看边缘包状态（是否可贡献）：
`src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj:8`
`src/Bing.Utils.Extra/Bing.Utils.Extra.csproj:7`

## 6. 风险与待确认项
- 待确认：仓库文档存在编码可读性问题，`README.md` 中文出现乱码，可能影响新贡献者理解设计目标。证据：`README.md:5`, `README.md:22`, `README.md:45`。
- 待确认：`Bing.Utils.DependencyInjection` 与 `Bing.Utils.Extra` 当前看起来仅项目骨架（仅 `Folder Include`，未发现源码类型），是否计划后续实现需产品/维护者确认。证据：`src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj:8`, `src/Bing.Utils.Extra/Bing.Utils.Extra.csproj:7`。
- 待确认：`Bing.Utils.Guard` 公开入口 `CharGuard` 为空类型，可能处于迁移中。证据：`src/Bing.Utils.Guard/Bing/Text/CharGuard.cs:23`。
- 风险：`System.Drawing` 在 Linux/Docker 需要额外系统依赖，跨平台部署可能踩坑。证据：`src/Bing.Utils.Drawing/README.md:1` 到 `src/Bing.Utils.Drawing/README.md:6`，`src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:19`。
- 风险：仓库含 `Bing - Backup.Utils.Guard.csproj` 备份工程文件，可能造成维护歧义。证据：`src/Bing.Utils.Guard/Bing - Backup.Utils.Guard.csproj:1`。
