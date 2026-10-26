# 功能变更隔离与验证

日期：2026-10-26。基线：`aa311b4d366323b5eef9be109c1e477db4ada10a`。

本次处理剩余工作区变更的交付隔离，以及 JSON Schema 官方样例的两处行尾空格。功能代码沿用已有实现；通过显式文件清单、CI 差异块和可独立应用的补丁验证每个批次的依赖闭包。原工作区及暂存区保持原有归属，不提交或发布。

## 变更影响与验收

- ChangedFiles：两份 JSON Schema fixture；HTTP 下载与响应输出测试拆分；本说明及文档入口。原有待交付变更作为补丁输入，逐文件列入 manifest。
- ChangedProjects：JsonSchema.Tests 的样例格式、Http.Tests 的测试归属；各功能项目在隔离检出中用于验证。
- ChangedPublicContracts / ChangedRuntimePaths / ChangedProviders / ChangedTFMs：本次均不变。
- ChangedBuildPackaging：按依赖拆分交付批次；CI 的运行时配置与功能验证步骤分开。
- ChangedBenchmarkHarness：测量代码不变，仅确定交付依赖，复用历史性能证据。
- ChangedDocsOnly：否。AffectedDependents：JSON Schema 官方用例及补丁消费者。RiskLevel：LOW。
- L0：UTF-8、空白、补丁可应用性及全部文件重建一致性。
- L1/L3：每批只在 HEAD 加显式前置批次的干净检出上构建、执行该批相关测试；JSON Schema 执行完整模块测试；消费者编译四个既有目标框架。
- L4：最终组合的构建与受影响主包、HTTP 测试作为集成检查点。
- L5：生产算法及测量程序未变，不重跑历史性能矩阵。远程 CI 单独记为外部未验证。

## 批次边界

| 批次 | 内容 | 前置依赖 |
|---|---|---|
| 01 | JSON Schema、官方样例、模块文档与 solution 入口 | 无 |
| 02 | 既有依赖修复与对应漏洞基线收缩 | 无 |
| 03 | 内存流、有界复制、原子写入、严格读取与限速 | 无 |
| 04 | 临时存储、溢写流与文件监控 | 03 |
| 05 | 压缩、ZIP 预算与安全回归 | 03 |
| 06 | 异步集合与调度 | 无 |
| 07 | 命令执行与实时输入输出 | 无 |
| 08 | 正则缓存与超时 | 无 |
| 09 | JSON 默认选项、数组及 NDJSON 流 | 无 |
| 10 | 表格 JSON、行状态与数据库流适配 | 09 |
| 11 | CSV/TSV | 无 |
| 12 | HTTP 响应及下载预算、分片调度与生命周期 | 03、04 |
| 13 | HTTP 流式响应与 SSE | 09 |
| 14 | HTTP 诊断 | 无 |
| 15 | CI 运行时安装、检查与 SDK 固定 | 无 |
| 16 | 跨功能消费者、混合回归、实际 netstandard 验证与 CI 接入 | 02—15；集成候选包含依赖修复 |
| 17 | 性能探针 | 对应流、压缩、集合、正则、序列化与 HTTP 批次 |
| 18 | 跨功能指南、质量记录与汇总入口 | 前述全部批次 |

HTTP 下载批次保留响应读取、续传和分片下载的统一预算语义，避免中间候选仅部分入口执行限额。混合消费者与测试在第 16 批显式集成，不作为独立功能零依赖的证据。`.github/workflows/dotnet.yml` 由第 15、16 批先后修改：前者不引用后者才引入的脚本和消费者。

测试拆分遵循 `.agents/skills/test-project-boundary/refs/test-project-boundary.md`：按主要断言目标归属，混合类按方法拆分并保持语义。`DownloadCapabilityExpansionTest.ResponseAdaptersWriteRecordsAndHonorAbort` 移至同项目的 `HttpResponseRecordStreamTest`，归入第 13 批；原下载测试保留在第 12 批。不迁移项目、不新增依赖，不保留重复用例。`StreamReviewRegressionTest`、`UtilityBudgetCapabilityTest` 与实际 netstandard 消费测试属于明确的组合验证，保留在第 16 批。

## 交付物

完整文件归属、依赖及补丁位于 `artifacts/feature-isolation-2026-10-26/manifest.json` 和同目录 `patches/`。补丁均相对于上述 HEAD 及其声明前置批次，可按 manifest 顺序使用 `git apply`，不需要写入原暂存区。原始验证日志与 TRX 位于同目录 `validation/`；`validation.json` 记录实际命令、闭包和结果。

`export_batches.py` 重新导出当前候选，`validate_batches.py` 在专用检出中重建指定批次。两者是本轮交付辅助文件，不进入产品或 CI。历史性能基线未覆盖。

## 当前执行状态

Round：1。本地实施与隔离验收完成，5/5 项关闭。

| 项目 | 结果 |
|---|---|
| JSON Schema 格式阻塞 | `properties.json:233`、`required.json:160` 各删除一个行尾空格；JSON 语义不变，CLOSED |
| 功能批次与依赖 | 18 个可应用补丁覆盖 232 个文件；独立编译发现并补齐 05→03 的共享流依赖，CLOSED |
| HTTP 测试隔离 | 下载类中的响应输出方法拆至独立类，方法仅存在一次，原断言保留，CLOSED |
| CI、消费者与性能工具 | CI 基础配置和功能验证分开；四框架消费者、实际 netstandard 回归、六个探针编译通过，CLOSED |
| 汇总资料 | 补齐批次及证据入口，更正同步文件写入已修复却仍列为待办的过期状态，CLOSED |

## 验证结果

所有隔离测试均使用 `D:/Bing_Framework/bu-verify-1026` 的基线检出，加上 manifest 声明的依赖闭包；构建串行执行。该目录仅用于本次验证，交付依据是补丁、命令日志和 TRX。

| 层级 / 范围 | 结果 |
|---|---|
| L0 编码、补丁与文件重建 | 18 批严格空白检查及应用通过；232 个文件按仓库 LF 规则比较文本一致；3 个 PowerShell 脚本语法检查通过 |
| JSON Schema 独立模块 | 449/449 |
| 共享流 / 文件生命周期 / 压缩 | 30/30、22/22、51/51 |
| 异步集合 / 命令 / 正则 | 28/28、26/26、237/237 |
| JSON 流 / 表格 / CSV | 14/14、102/102、11/11 |
| HTTP 下载 | 单元 181/181；集成 33/33 |
| HTTP 流式响应 / 诊断 | 23/23、3/3 |
| 跨功能回归 / 实际 netstandard 产物回归 | 34/34、30/30 |
| 消费者 | netstandard2.0、net6.0、net7.0、net8.0 四框架编译通过；BuildScript 编译通过 |
| 性能工具 | 六个探针以候选模式编译通过；测量代码与历史结果保持不变 |
| L4 最终组合：主包完整测试 | 7327/7327 |
| L4 最终组合：HTTP 完整单元测试 | 1930/1930 |
| L4 最终组合：HTTP 完整集成测试 | 122/122 |

上表的定向、组合与完整回归存在覆盖重叠，不累加为独立测试总数。HTTP 下载集成的 33 项在仅拆分另一个测试项目的方法后复用；其生产代码、集成测试和依赖未变。汇总收口仅修改文档，复用已通过的代码测试。L5 不运行，既有性能证据按未变测量范围复用。

首次验证暴露并解决了三处具体问题：验证命令全局覆盖 TargetFrameworks 使固定 netstandard2.0 的样例项目还原不完整，已移除该覆盖；ZIP 批次缺少共享流前置，已补 manifest；HTTP 下载类混入响应输出用例，已按方法拆分。诊断失败日志与通过后的日志均保留，不覆盖原性能基线。

独立核对确认文件归属无重复或遗漏，CI 引用的脚本随相应批次交付，序列化、HTTP 与性能工具的新增类型依赖均有前置归属。没有扩大到原有生产实现的无关重构。

Completed：上述 5/5。Open Actionable：0。Blocked Approval：无。Blocked External：远程 CI 未运行，本地结果不替代跨平台结果。Not Applicable：本次新性能测量、发布。Accepted Limitations：组合消费者与混合回归作为第 16 批交付，需要全部声明前置。Verified Boundaries：每批只依赖基线及声明闭包，完整补丁可重建候选。Deferred：无新增。No-Progress：CHANGED。Next Action：STOP。Implementation / Local Tests：PASS。External Gate：NOT_RUN。Release：NOT_REQUESTED。Goal Status：STOPPED_BLOCKED（本地请求已完成，远程 CI 另行验证）。
