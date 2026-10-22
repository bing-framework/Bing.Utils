# 文档与实现盘点

> 初始盘点：2026-10-13；DependencyInjection 状态更新于 2026-10-15（包含未提交改动）。本表以 `src`、项目文件和对应测试项目为事实来源。

## 盘点结论

- `src` 下共有 19 个项目，均已有实际源码；DependencyInjection 已补齐批量注册、受控扫描及泛型提前闭合。
- 初始补齐 Comments、Drawing.Shared、Guard、Net、Security、Security.Gm；DependencyInjection 的占位文档现已替换为实现与用法说明。
- `docs/modules` 中小写文件名（如 `core.md`、`http.md`）是较早的设计/迁移资料；以 `Bing.Utils*.md` 命名的文件作为当前模块入口，避免维护两套入口造成歧义。
- `docs/quality`、`docs/security*.md` 继续承载专项审查、迁移和验证证据，不替代模块使用文档。

## 对照清单

### 已实现且文档完整

- 原有文档经核对可继续作为标准入口：`Bing.Utils`、Collections、DateTime、Drawing、Drawing.ImageSharp、Drawing.SkiaSharp、Extra、Extra.Drawing.SkiaSharp、Http、IdUtils、Reflection、Text。
- 本轮补齐后文档完整：Comments、Drawing.Shared、Guard、Net、Security、Security.Gm。
- DependencyInjection 原工程骨架缺口已关闭，DI-01～DI-04 状态及验证见 [执行记录](quality/dependency-injection-execution.md)。
- 所有已实现模块的标准文档均包含功能说明、核心实现和路径、接口或示例、依赖配置、限制与注意事项。

### 已实现但原文档缺失或过时

- 原缺少独立模块文档：Comments、Drawing.Shared、Guard、Net、Security、Security.Gm。
- `Drawing.ImageSharp` 和 `Drawing.SkiaSharp` 原文档仍描述为源码链接共享代码，实际已改为 `ProjectReference` 引用 `Drawing.Shared`。
- ImageSharp 原文档声称未发现测试项目，实际存在 `tests/Bing.Utils.Drawing.ImageSharp.Tests` 及多类兼容、回归和内部算法测试。
- Collections、DateTime 原文档仍把已有字典和 DateTimeSpan 测试列为 TODO。
- `01-module-catalog.md` 原为空文件；`api/index.md` 原为英文 DocFX 占位文本。
- `reference/api-reference.md` 只统计部分早期模块，不能作为 19 个项目的实时完整 API 清单；现已明确标记为静态快照并链接标准模块入口。
- `quality/migration-reports/README.md` 原内容存在不可恢复的乱码且引用了目录中不存在的报告；现已按实际存在的两份报告重建索引，缺失历史记录标记为**待确认**。

### 文档描述与代码不符

- 原能力地图遗漏 Drawing.Shared、Security、Security.Gm，并把两个跨平台图像后端描述为只依赖第三方引擎。
- 安全总览原先容易理解为所有高级密码 API 均支持 `netstandard2.0`；实际 AES-GCM、PBKDF2、RSA/ECDSA、PEM、证书及版本化签名通过条件编译仅在较新 .NET 目标提供。
- `FtpClientConfig` 的连接超时、读取超时和主动/被动模式已接入控制及数据通道；代理、其他超时、KeepAlive、二进制切换等仍未全部应用。实际字段与待确认语义见 [Net 模块文档](modules/Bing.Utils.Net.md)，不能把配置模型中的全部字段视为已生效。
- DocFX 原配置排除了 Comments、Guard、Net 及 DependencyInjection；现均纳入源码入口，不在本轮执行 DocFX 发布。
- Comments 原示例使用了不存在的 `Params` 属性和 `CsCommentsParamCollection.Empty`；现改为实际公开的 `Parameters.Values`。

### 尚未实现

- `Bing.Utils.Guard.CharGuard`：公开类型为空，通用 Guard API 标记为**待实现/待确认**；已实现范围仅按 `CharJudge` 和内部辅助说明。
- Security 中的 ZUC、格式保留加密（FPE）及 Security.Gm 中的 SM4 分块流式容器均未实现，不作为现有能力宣传。

## 模块核对表

| 源码项目 | 实现状态 | 当前模块文档 | 核对结果 |
| --- | --- | --- | --- |
| `Bing.Utils` | 已实现 | [Bing.Utils](modules/Bing.Utils.md) | 核心 IO、转换、反射、文本、线程与通用扩展已存在 |
| `Bing.Utils.Collections` | 已实现 | [Collections](modules/Bing.Utils.Collections.md) | 数组、集合、字典和枚举辅助已存在 |
| `Bing.Utils.Comments` | 已实现 | [Comments](modules/Bing.Utils.Comments.md) | XML 注释读取、模型解析、缓存与继承解析已存在 |
| `Bing.Utils.DateTime` | 已实现 | [DateTime](modules/Bing.Utils.DateTime.md) | 日期时间扩展、时间跨度和 NodaTime 适配已存在 |
| `Bing.Utils.DependencyInjection` | 已实现 | [DependencyInjection](modules/Bing.Utils.DependencyInjection.md) | 原生 DI 批量注册、受控扫描、标准开放泛型及复杂提前闭合 |
| `Bing.Utils.Drawing` | 已实现 | [Drawing](modules/Bing.Utils.Drawing.md) | `System.Drawing` 后端及兼容 API 已存在 |
| `Bing.Utils.Drawing.ImageSharp` | 已实现 | [Drawing.ImageSharp](modules/Bing.Utils.Drawing.ImageSharp.md) | ImageSharp 后端已存在 |
| `Bing.Utils.Drawing.Shared` | 已实现 | [Drawing.Shared](modules/Bing.Utils.Drawing.Shared.md) | 公共契约、颜色/矩阵算法和内部管线已存在 |
| `Bing.Utils.Drawing.SkiaSharp` | 已实现 | [Drawing.SkiaSharp](modules/Bing.Utils.Drawing.SkiaSharp.md) | SkiaSharp 后端已存在 |
| `Bing.Utils.Extra` | 已实现 | [Extra](modules/Bing.Utils.Extra.md) | Emoji 识别、转换和元数据能力已存在 |
| `Bing.Utils.Extra.Drawing.SkiaSharp` | 已实现 | [Extra.Drawing.SkiaSharp](modules/Bing.Utils.Extra.Drawing.SkiaSharp.md) | Emoji 字形渲染适配已存在 |
| `Bing.Utils.Guard` | 已实现（范围有限） | [Guard](modules/Bing.Utils.Guard.md) | 字符判断和验证异常辅助已存在；`CharGuard` 为空类型 |
| `Bing.Utils.Http` | 已实现 | [Http](modules/Bing.Utils.Http.md) | ASP.NET Core 请求、响应、上下文和下载辅助已存在 |
| `Bing.Utils.IdUtils` | 已实现 | [IdUtils](modules/Bing.Utils.IdUtils.md) | Snowflake、ObjectId 等生成器已存在 |
| `Bing.Utils.Net` | 已实现（单一协议） | [Net](modules/Bing.Utils.Net.md) | 当前只封装 FTP/FTPS，不是通用网络协议库 |
| `Bing.Utils.Reflection` | 已实现 | [Reflection](modules/Bing.Utils.Reflection.md) | 类型访问、动态创建、缓存与运行时边界已存在 |
| `Bing.Utils.Security` | 已实现 | [Security](modules/Bing.Utils.Security.md) | 现代摘要、认证加密、签名、密钥和请求签名已存在 |
| `Bing.Utils.Security.Gm` | 已实现 | [Security.Gm](modules/Bing.Utils.Security.Gm.md) | SM2、SM3、HMAC-SM3、SM4-GCM 已存在 |
| `Bing.Utils.Text` | 已实现 | [Text](modules/Bing.Utils.Text.md) | 匹配、拼音、简繁转换、分词和 Unicode 文本处理已存在 |

## 待实现/待确认

- `Bing.Utils.DependencyInjection` 的任意程序集裁剪/NativeAOT 兼容不在批准范围内，不从本地普通容器验证推断部署兼容。
- `Bing.Utils.Guard.CharGuard`：当前为空类型，不能作为已实现的字符校验入口；现阶段使用 `CharJudge`。
- `Bing.Utils.Net`：仅发现 FTP/FTPS 封装；SFTP、HTTP、TCP/UDP 等能力不在当前实现范围。
- 小写旧模块文档是否长期保留：本轮不删除历史资料；后续可在确认无外部链接依赖后归档。
