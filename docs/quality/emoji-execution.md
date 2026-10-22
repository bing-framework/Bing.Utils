# Emoji 默认数据精简与离线导入验证记录

- Task：Emoji 默认数据精简与离线本地化导入；Round：72；日期：2026-09-28。
- Candidate：基于 3db1b87c 的未提交工作区；未调整版本、提交或发布。
- 当前状态：Implementation 5/5 PASS；本轮本地验证门禁 5/5 PASS。历史 61 语言默认支持说明已被本轮计划替代。

## 变更影响分析

| 项目 | 范围 |
| --- | --- |
| ChangedFiles | 数据生成器、内置本地化、目录离线导入、元数据说明、Extra 测试和相关文档 |
| ChangedProjects | Bing.Utils.Extra、Bing.Utils.Extra.Tests |
| ChangedPublicContracts | 新增 WithLocalizations(Stream)、目录 GetLocales/TryGetLocalization 及目录肤色操作；默认本地化收缩为 zh/zh-Hant |
| ChangedRuntimePaths | 内置数据初始化缩减；新增调用方流读取、JSON 校验、目录快照合并及实例肤色索引调用 |
| ChangedProviders | 保留 Unicode 18.0、gemoji 4.1.0、CLDR 48.2 简繁中文；外部 JSON 由调用方提供 |
| ChangedTFMs | 不变：netstandard2.0、net6.0、net7.0、net8.0 |
| ChangedBuildPackaging | 同一包保留四个目标框架；不添加运行时第三方依赖 |
| ChangedBenchmarkHarness | 无 |
| ChangedDocsOnly | 否 |
| AffectedDependents | Extra 消费者、SkiaSharp 适配包 |
| RiskLevel | MEDIUM：默认查询数据范围收缩；新增公开流导入契约 |

## 数据与包体证据

基线为修改前现有 Release 包，单独保存于忽略目录 output/emoji-localization-baseline，不覆盖基线。
数据由 61 个 locale / 240,196 条翻译，缩减为 zh、zh-Hant / 7,888 条翻译。
保留 3,963 个规范表情、5,235 个序列和 1,913 个别名；删除 118 份非默认语言 XML 快照。
英文仍通过 Unicode 名称和 gemoji 别名/标签提供；默认不引入 en locale。

当前 NuGet 包从 29,235,764 字节降至 1,310,461 字节，减少 27,925,303 字节（95.52%）。上轮未增加目录肤色公开入口的包为 1,308,502 字节。

| 程序集 | 基线字节 | 最终字节 |
| --- | ---: | ---: |
| net6.0 | 15,980,544 | 1,016,320 |
| net7.0 | 15,980,544 | 1,016,320 |
| net8.0 | 15,980,032 | 1,015,808 |
| netstandard2.0 | 15,980,544 | 1,015,808 |

当前包位于 output/release/Bing.Utils.Extra.1.5.0.nupkg；output/emoji-localization-consumer/final-sizes.json 保存的是上轮包体记录，不能作为本轮数值。
四个目标框架、许可证均齐全；包依赖与基线相同。
新接口使用框架内置 DataContractJsonSerializer，
导入前校验 JSON 类型，读取异常与格式异常分开处理；导入结果仅影响新目录。

## 验证记录

- L0：生成器 --check 通过；Extra 四个目标框架 Release 构建通过，0 警告、0 错误。
- L1：目录本地化定向用例 62 项通过。
- L2：当前 Extra net8.0、net7.0、net6.0 各 104 项通过。
- L3：当前 Extra 四个目标框架构建通过，0 警告、0 错误；NuGet 包构建通过。使用隔离缓存从当前包离线还原，消费者已验证本地化导入、非法 JSON 拒绝及目录肤色接口。SkiaSharp 适配构建沿用上轮未变源码的证据。
- 差异检查：生成器 --check、UTF-8 检查和 git diff --check 通过；无版本变更或发布。
- L4/L5：未运行无关全解决方案或重型性能测试。
- 构建期间遇到既有输出文件写入权限问题，按权限流程重试后成功；消费者使用本地包源和已有缓存离线还原。
- 打包仍有既有“缺少包自述文件”提示，不影响生成。

## 当前 TODO / Finding 分类

- Completed：默认数据精简、独立目录导入、目录肤色操作、重复 JSON 字段与肤色交叉回归、中文注释、文档与打包验证。
- Open Actionable：无。
- Blocked Approval：本轮无。
- Blocked External：本轮无。
- Not Applicable：性能 Benchmark 和其他语言包制作。
- Accepted Limitations：缺少 .NET 5 / Core 3.1 运行时，旧目标保留但运行未验证；本机没有 DocFX 工具，文档构建未执行；离线导入读取整个输入，未设容量门槛；外部数据来源和许可由调用方管理。
- Verified Boundaries：仅报告实测包体，不声明性能或容量上限。
- Deferred：旧 Emoji API 迁移仍为独立后续议题；其他语言包不在本轮范围，不再自动逐个扩充内置语言。
- Reused Evidence：未修改匹配器算法、HTML 实体算法及 SkiaSharp 渲染实现。
- No-Progress Check：CHANGED。
- Next Action：STOP，本轮计划完成。
- Goal Status：COMPLETED。

## 执行说明

回归修复：框架读取器会忽略根对象后的内容，现显式校验文档边界；超大格式版本号的溢出异常统一转换为 InvalidDataException。对应回归均通过。严格 UTF-8、BOM、转义文本、原目录隔离、兼容序列、重复记录、空数组、不可定位流、读取异常、只读数据及并发查询均有手写测试。

本次由主代理完成目录肤色接口和回归，luna_worker 独立检查 DocFX 工具可用性；未执行独立第三方代码审查。测试按 .agents/skills/test-project-boundary/refs/test-project-boundary.md 归属 Extra 专属项目，无跨项目迁移。
