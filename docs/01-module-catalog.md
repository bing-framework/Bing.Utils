# 模块目录

本页按能力域汇总标准模块文档。模块实现状态、历史缺口和待确认项见 [文档与实现盘点](00-documentation-audit.md)。

## 核心与通用能力

| 模块 | 简介 |
| --- | --- |
| [Bing.Utils](modules/Bing.Utils.md) | IO、转换、基础类型扩展、异常、线程、文本与反射基础能力 |
| [Bing.Utils.Collections](modules/Bing.Utils.Collections.md) | 数组、集合、字典和只读集合操作 |
| [Bing.Utils.DateTime](modules/Bing.Utils.DateTime.md) | 日期时间、时间跨度、农历和 NodaTime 适配 |
| [Bing.Utils.Cron](modules/Bing.Utils.Cron.md) | 五段/六段 Cron、时区与有界枚举 |
| [Bing.Utils.Diff](modules/Bing.Utils.Diff.md) | 可取消、有预算的大文本行级 Diff |
| [Bing.Utils.Text](modules/Bing.Utils.Text.md) | 关键词匹配、拼音、中文转换、分词和 Unicode 文本处理 |
| [Bing.Utils.Reflection](modules/Bing.Utils.Reflection.md) | 类型访问、成员操作、动态创建和反射缓存 |
| [Bing.Utils.IdUtils](modules/Bing.Utils.IdUtils.md) | Snowflake、ObjectId、Guid 等 ID 生成能力 |

## Web、网络与基础设施

| 模块 | 简介 |
| --- | --- |
| [Bing.Utils.Http](modules/Bing.Utils.Http.md) | ASP.NET Core 请求、响应、上下文和下载辅助 |
| [Bing.Utils.Net](modules/Bing.Utils.Net.md) | 基于 FluentFTP 的同步 FTP/FTPS 客户端 |
| [Bing.Utils.Guard](modules/Bing.Utils.Guard.md) | 当前提供字符闭区间判断，完整 Guard 能力尚未实现 |
| [Bing.Utils.DependencyInjection](modules/Bing.Utils.DependencyInjection.md) | 原生 DI 批量注册、受控扫描、标准开放泛型与复杂提前闭合 |

## 图像与内容

| 模块 | 简介 |
| --- | --- |
| [Bing.Utils.Drawing.Shared](modules/Bing.Utils.Drawing.Shared.md) | 图像后端共用契约、颜色/矩阵算法和处理管线 |
| [Bing.Utils.Drawing](modules/Bing.Utils.Drawing.md) | 基于 `System.Drawing` 的兼容图像后端 |
| [Bing.Utils.Drawing.ImageSharp](modules/Bing.Utils.Drawing.ImageSharp.md) | 基于 ImageSharp 的跨平台图像后端 |
| [Bing.Utils.Drawing.SkiaSharp](modules/Bing.Utils.Drawing.SkiaSharp.md) | 基于 SkiaSharp 的跨平台图像后端 |
| [Bing.Utils.Comments](modules/Bing.Utils.Comments.md) | C# XML 注释读取、继承解析和缓存 |
| [Bing.Utils.Extra](modules/Bing.Utils.Extra.md) | Emoji 识别、检索、转换、本地化和肤色处理 |
| [Bing.Utils.Extra.Drawing.SkiaSharp](modules/Bing.Utils.Extra.Drawing.SkiaSharp.md) | 使用调用方字体将 Emoji 渲染为 `SKImage` |

## 安全

| 模块 | 简介 |
| --- | --- |
| [Bing.Utils.Security](modules/Bing.Utils.Security.md) | 现代摘要、认证加密、密码散列、密钥、证书和请求签名 |
| [Bing.Utils.Security.Gm](modules/Bing.Utils.Security.Gm.md) | SM2、SM3、HMAC-SM3 和 SM4-GCM 国密扩展 |
