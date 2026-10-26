# JSON Schema 校验实施计划

日期：2026-10-25。设计依据：[design.md](design.md)。用户已明确授权按方案→计划→实现顺序继续，本计划在当前聊天直接执行；没有 UAW taskId/stageRunId，不伪造运行时 StageResult。

## 任务清单与验收

| 编号 | 工作项 | 验收 | 状态 |
|---|---|---|---|
| J01 | 冻结方案和契约 | 目标框架、依赖、错误策略、测试范围明确，设计先于实现保存 | 完成 |
| J02 | 新包和公开模型 | 独立 net8.0 项目，所有公开契约有中文 XML 注释，只读结果/配置快照 | 完成 |
| J03 | 引擎适配和引用 | Create 准备、Validate 求值、方言/format、分支诊断、预注册 refs 和失败策略 | 完成 |
| J04 | 专属测试和官方样例 | 封装边界及离线官方用例全部通过 | 完成 |
| J05 | 解决方案和用户文档 | sln 包含两个新项目，模块目录可导航，README 随包分发且披露依赖条款 | 完成 |
| J06 | 构建、打包、消费与审查 | Release 模块测试、打包及独立消费者通过，审查无 OPEN_ACTIONABLE | 完成 |

执行顺序：J01 → J02 → J03/J04 → J05 → J06。主代理负责设计、生产实现、集成和最终验证；luna_worker 负责明确契约后的独立测试/固定样例或限定源码核查。所有构建串行执行，避免共享输出争用。

## 修改边界

- 新增 src/Bing.Utils.JsonSchema/** 和 tests/Bing.Utils.JsonSchema.Tests/**。
- 新增 docs/modules/Bing.Utils.JsonSchema.md 与本任务 design/plan/execution 文档。
- 必要修改 Bing.Utils.sln、docs/modules/README.md、docs/00-capability-map.md。
- 不更新其他项目依赖或发行版本；不暂存、提交、推送或发布。

## Change Impact Analysis

ChangedFiles：上述新增文件与入口。
ChangedProjects：Bing.Utils.JsonSchema、Bing.Utils.JsonSchema.Tests。
ChangedPublicContracts：新增校验器、选项、方言、状态、结果、错误和配置异常。
ChangedRuntimePaths：Schema 准备与只读 JSON 求值。
ChangedProviders：新增 JsonSchema.Net 9.4.1 适配。
ChangedTFMs：新增包仅 net8.0。
ChangedBuildPackaging：新增可选 NuGet 包及 solution 成员。
ChangedBenchmarkHarness：无。ChangedDocsOnly：否。AffectedDependents：新测试及临时包消费者；无既有生产依赖者。
RiskLevel：MEDIUM（标准语义、引用解析和并发复用）。

## 验证命令与证据

1. dotnet restore tests/Bing.Utils.JsonSchema.Tests/Bing.Utils.JsonSchema.Tests.csproj
2. dotnet test tests/Bing.Utils.JsonSchema.Tests/Bing.Utils.JsonSchema.Tests.csproj -c Release --logger trx --results-directory artifacts/json-schema-validation/tests
3. dotnet pack src/Bing.Utils.JsonSchema/Bing.Utils.JsonSchema.csproj -c Release --no-build -o artifacts/json-schema-validation/packages
4. 在 artifacts/json-schema-validation/consumer 创建独立 net8.0 消费者，仅通过本地 NuGet 包引用新模块，执行成功、失败、引用和诊断冒烟。
5. dotnet sln Bing.Utils.sln list；git diff --check -- Bing.Utils.sln docs/modules/README.md docs/00-capability-map.md；对新增文本执行严格 UTF-8 和末尾空白检查。

L0：构建、差异、编码。L1/L2：新模块全部单元及官方用例。L3：打包和独立包消费者。由于生产变更隔离且没有旧依赖者，L4 不扩大到无关 solution 测试；不涉及性能提升承诺，L5/历史 before 为 NOT_APPLICABLE。

必要验收：类型/required/null/额外属性/数组/数字边界、根标量和布尔 Schema、JSON Pointer 转义、成功/失败组合、引用顺序/递归/未解析、未知方言、format 开关、Schema 与输入生命周期、配置变更隔离、并发重复调用，以及包无核心依赖且第三方许可说明完整。

## 审查和停止条件

审查仅覆盖本任务新代码与集成入口。只有 OPEN_ACTIONABLE 可进入修复；每轮定位根因并运行相关测试。最多三轮；同输入同候选的同类实验最多诊断/确认各一次。公共 GOAL_RULES.md 的分类、No Progress Guard 和证据复用规则适用。

当前 Completed：J01—J06。Open Actionable：0。Blocked Approval/Blocked External：无。Not Applicable：历史 before、性能和全量跨模块回归。Accepted Limitations：仅 net8.0；DOM 求值无硬资源隔离；宿主直接操作引擎全局内存注册表的行为不隔离。Verified Boundaries：449 项测试、14 项独立包消费检查及 F1—F6 独立复核通过，详见 [execution.md](execution.md)。Deferred：流式入口、DTO/ASP.NET Core、旧 TFM。
