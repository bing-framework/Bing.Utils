# 模块文档索引

以下 `Bing.Utils*.md` 文件是当前源码项目的标准入口。小写文件名文档保留为历史设计、迁移或深度实现资料。

| 模块 | 说明 | 状态 |
| --- | --- | --- |
| [Bing.Utils](Bing.Utils.md) | 核心 IO、转换、反射、文本、线程与通用扩展 | 已实现 |
| [Bing.Utils.Collections](Bing.Utils.Collections.md) | 数组、集合、字典与枚举辅助 | 已实现 |
| [Bing.Utils.Comments](Bing.Utils.Comments.md) | C# XML 注释读取、缓存与继承解析 | 已实现 |
| [Bing.Utils.DateTime](Bing.Utils.DateTime.md) | 日期时间、时间跨度与 NodaTime 适配 | 已实现 |
| [Bing.Utils.DependencyInjection](Bing.Utils.DependencyInjection.md) | 原生 DI 批量注册、受控扫描与泛型提前闭合 | 已实现 |
| [Bing.Utils.Drawing](Bing.Utils.Drawing.md) | `System.Drawing` 图像后端 | 已实现 |
| [Bing.Utils.Drawing.ImageSharp](Bing.Utils.Drawing.ImageSharp.md) | ImageSharp 图像后端 | 已实现 |
| [Bing.Utils.Drawing.Shared](Bing.Utils.Drawing.Shared.md) | 图像公共契约与纯算法 | 已实现 |
| [Bing.Utils.Drawing.SkiaSharp](Bing.Utils.Drawing.SkiaSharp.md) | SkiaSharp 图像后端 | 已实现 |
| [Bing.Utils.Extra](Bing.Utils.Extra.md) | Emoji 识别、转换与元数据 | 已实现 |
| [Bing.Utils.Extra.Drawing.SkiaSharp](Bing.Utils.Extra.Drawing.SkiaSharp.md) | Emoji 字形渲染适配 | 已实现 |
| [Bing.Utils.Guard](Bing.Utils.Guard.md) | 字符判断与验证异常辅助 | 已实现（范围有限） |
| [Bing.Utils.Http](Bing.Utils.Http.md) | ASP.NET Core 请求、响应和上下文辅助 | 已实现 |
| [Bing.Utils.IdUtils](Bing.Utils.IdUtils.md) | Snowflake、ObjectId 等 ID 生成 | 已实现 |
| [Bing.Utils.JsonSchema](Bing.Utils.JsonSchema.md) | JSON Schema 校验、结构化诊断与预注册引用（.NET 8） | 已实现 |
| [Bing.Utils.Net](Bing.Utils.Net.md) | FTP/FTPS 客户端封装 | 已实现（单一协议） |
| [Bing.Utils.Reflection](Bing.Utils.Reflection.md) | 类型访问、动态创建与反射缓存 | 已实现 |
| [Bing.Utils.Security](Bing.Utils.Security.md) | 现代密码学、密钥、证书和请求签名 | 已实现 |
| [Bing.Utils.Security.Gm](Bing.Utils.Security.Gm.md) | SM2、SM3、HMAC-SM3、SM4-GCM | 已实现 |
| [Bing.Utils.Text](Bing.Utils.Text.md) | 匹配、拼音、中文转换、分词与 Unicode 文本处理 | 已实现 |

完整盘点、缺口和待确认项见 [文档与实现盘点](../00-documentation-audit.md)。

