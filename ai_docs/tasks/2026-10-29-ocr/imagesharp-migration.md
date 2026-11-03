# ImageSharp 既有漏洞修复的兼容性边界

**最终决策（2026-10-29）：DEFERRED_BY_USER。** 用户明确选择“保留 netstandard2.0，此项另行迁移”。本轮保持现有目标框架与 ImageSharp 版本；以下方案留作单独迁移，不再等待本轮确认，也不登记漏洞豁免。OCR 的 SkiaSharp 识别路径不依赖该包。

本轮已核对 NuGet 官方注册元数据（2026-10-29）：

| 版本 | 框架资产 | 已登记漏洞 | 处理影响 |
| --- | --- | --- | --- |
| 2.1.11（当前） | netstandard2.0 | 3 项 High、2 项 Moderate | 当前四目标框架均引用该版本 |
| 2.1.13（2.x 最新） | netstandard2.0 | 同样 5 项 | 小版本更新不能修复这些漏洞 |
| 3.2.0 | net6.0 | 无登记项 | 可覆盖 net6/7/8，不能提供 netstandard2.0 资产；Drawing/Fonts 需配套迁移及适配 |
| 4.1.3 | net8.0 | 无登记项 | 需要同时取消 net6/net7/netstandard2.0 支持 |

当前 OCR SkiaSharp 路径不依赖 ImageSharp。ImageSharp 包公开 API 暴露 SixLabors 类型，不能无损替换为 SkiaSharp。不能通过登记漏洞基线、仅更新到 2.1.13 或静默删除旧目标框架宣称问题已修复。

可审查的迁移方案：仅将 Bing.Utils.Drawing.ImageSharp 迁移到 ImageSharp 3.2.0 及兼容 Drawing/Fonts，目标改为 net8/net7/net6；保留其他包的 netstandard2.0。需先确认允许该包停止支持 netstandard2.0，再修改对应公开返回类型兼容适配与图像集成测试。此前保持四目标框架的范围要求仍有效，因此该决策单独记录。

元数据来源：
- https://api.nuget.org/v3/registration5-gz-semver2/sixlabors.imagesharp/2.1.13.json
- https://api.nuget.org/v3/registration5-gz-semver2/sixlabors.imagesharp/3.2.0.json
- https://api.nuget.org/v3/registration5-gz-semver2/sixlabors.imagesharp/4.1.3.json
