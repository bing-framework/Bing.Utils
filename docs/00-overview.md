# Bing.Utils 项目概览

## 项目定位

Bing.Utils 是面向 .NET 的多包通用工具库。仓库按能力拆分 NuGet 项目，覆盖核心工具、集合、日期时间、文本、反射、HTTP、FTP、ID、图像、XML 注释、Emoji 和密码学。它不承载业务域模型，也不提供统一应用框架运行时。

## 模块分组

| 分组 | 项目 | 主要职责 |
| --- | --- | --- |
| 核心 | `Bing.Utils` | IO、转换、基础类型扩展、异常、线程、文本与反射基础能力 |
| 通用扩展 | `Collections`、`DateTime`、`Text`、`Reflection`、`IdUtils` | 集合、时间、文本、反射和 ID 生成 |
| Web 与网络 | `Http`、`Net` | ASP.NET Core 辅助和 FTP/FTPS 客户端 |
| 图像 | `Drawing.Shared`、`Drawing`、`Drawing.ImageSharp`、`Drawing.SkiaSharp` | 公共图像契约与三个具体后端 |
| 内容与元数据 | `Comments`、`Extra`、`Extra.Drawing.SkiaSharp` | XML 注释、Emoji 目录与字形渲染 |
| 安全 | `Security`、`Security.Gm` | 现代密码学与国密扩展 |
| 校验与注册 | `Guard`、`DependencyInjection` | Guard 当前能力有限；DependencyInjection 提供原生 DI 批量注册与泛型提前闭合 |

每个项目的准确状态和文档链接见 [文档与实现盘点](00-documentation-audit.md) 与 [模块文档索引](modules/README.md)。

## 依赖主线

- 多数传统扩展包依赖核心 `Bing.Utils`。
- 三个具体图像后端共享 `Bing.Utils.Drawing.Shared`；ImageSharp 和 SkiaSharp 后端不依赖核心 `Bing.Utils`。
- `Bing.Utils.Extra.Drawing.SkiaSharp` 依赖 `Bing.Utils.Extra` 和 SkiaSharp。
- `Bing.Utils.Security.Gm` 依赖 `Bing.Utils.Security` 和 BouncyCastle；主安全包不依赖第三方密码学库。
- 当前未发现源码项目级循环引用。

## 构建与测试约定

- 公共库默认目标框架由 `common.props` 定义：`net8.0;net7.0;net6.0;netstandard2.0`；个别项目会覆盖目标框架或使用条件编译。
- 测试项目位于 `tests`，公共依赖由 `common.tests.props` 管理，主要使用 xUnit、Shouldly 和 Moq。
- 需要文件系统、ASP.NET Core 或 FTP 服务的场景分布在 `*.Tests.Integration` 项目；外部服务测试需要显式配置，不能用单元测试结果替代。
- 性能与专项探针位于 `benchmarks`，质量和迁移证据位于 `docs/quality`。

## 当前边界

- `Bing.Utils.DependencyInjection` 已提供显式接口批量注册和受控扫描；复杂泛型仅支持启动时列出的有限闭合服务，见 [模块文档](modules/Bing.Utils.DependencyInjection.md)。
- `Bing.Utils.Guard` 的公开实现目前仅有字符闭区间判断，不能视为完整 Guard 库。
- `Bing.Utils.Net` 当前仅封装 FTP/FTPS，且证书验证和未接入配置项存在已知限制。
- 安全主包在 `netstandard2.0` 仅提供兼容能力，高级密码 API 只在 `.NET 6/7/8` 提供。
- 图像后端、字体、平台原生依赖和编码器能力存在差异，使用前应查阅对应模块文档。
