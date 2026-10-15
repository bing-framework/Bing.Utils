# TODO 执行记录

更新时间：2026-10-15；基于 `813bd12` 上的工作区。本轮完成覆盖计划 P2-3 的六类边界检查策略及补测模板接入；保留此前未提交的源码、测试及工作流改动。本轮仅修改文档，不修改生产代码、测试、依赖、目标框架、CI 或基线。

## 执行依据与本轮范围

- 仓库没有统一的跨模块总排期。按各既有清单及后续记录核对，不将上轮审计建议视为已批准设计。
- [覆盖计划](test-coverage-map.md) 的 P0、P1、P2-1 及 [质量报告](test-quality-report.md) P1-1 已完成；覆盖计划 P2-2 已实现并通过本地回归，仅外部证据待验证。质量报告 P1-2 的本地实现完成，仅实际 nightly 待验证。本轮承接覆盖计划 P2 原顺序第 3 项，不重开已完成编码项，不改变外部阻塞项范围。
- 核对 [质量报告](test-quality-report.md)、[HTTP](http-security-reliability-report.md)、[安全清单](../security-cryptography-todo.md)、[图像](image-processing-execution.md)、[Text 资源](text-resources-execution.md)、[Emoji](emoji-execution.md)、[转换](conv-current-status.md)、[反射](reflection-todo-status.md) 的最新状态，后续记录覆盖历史描述。
- 原计划 P2-3 要求引入横向边界清单策略；交付为 `null/empty/invalid/overflow/concurrent/perf` 统一清单、已有用例引用及模板接入，不扩为全 API 补测。
- 既有单文件和批量补测模板已经存在，但未统一六类检查记录，模板索引也未收录这些入口；本轮直接修订已有模板，不另建重复工具或流程。
- 按 `test-project-boundary/refs/test-project-boundary.md` 保留测试归属规则；主断言对象决定模块测试归属，真实跨模块协作进入 Integration。本轮无测试新增或迁移。
- 主代理负责清单、模板和状态集成；Luna 按用户级委派规则只读定位六类已有样例并复核文档一致性。
- 验收边界：六类均有适用性判断、契约与断言要求、状态和证据字段；性能入口只引用，不伪造测量或预算，不机械新增并发、基线和 CI。

## 已完成 TODO

| 原条目 | 状态 | 实现说明 |
| --- | --- | --- |
| 覆盖计划 P0-1 ImageSharp | CLOSED（此前） | `ImageSharpHelperTest` 已覆盖空参、透明度边界、克隆及 alpha |
| 覆盖计划 P0-2 SkiaSharp | CLOSED（此前） | helper 与格式扩展测试已覆盖输入边界和格式映射 |
| 覆盖计划 P0-3 验证码 | CLOSED（此前） | `CaptchaBuilderTest` 已断言非法长度、空码和尺寸 |
| 覆盖计划 P1-1 Collections | CLOSED（此前） | `DictsAndCollConvTest`、`ReadOnlyDictsContractTest`、`Colls*Test` 已覆盖集合边界 |
| 覆盖计划 P1-2 Text | CLOSED（此前） | 拒绝零/负长度；覆盖参数信息、空输入、重复键及空键值分隔符；同步旧异常断言 |
| 覆盖计划 P1-3 IdUtils | CLOSED（此前） | 受控时钟覆盖回拨、恢复、序列溢出和真实等待循环；越界参数复用 `SnowflakeGeneratorTest` |
| 质量报告 P0-1 SendBefore | CLOSED（此前） | 正确判断回调，`HttpClientServiceTest.Regression` 已有回归 |
| 质量报告 P0-2 异步覆盖写 | CLOSED（此前） | `FileHelper.WriteAsync(byte[],...)` 使用 `FileMode.Create` 并释放流；回归断言截断 |
| 质量报告 P1-1 上传集成用例 | CLOSED（此前） | 无目录文件名、不同字段下的同名文件、同时到达的独立上传请求均验证实际落盘字节；测试控制器隔离并清理临时目录 |
| 覆盖计划 P2-1 HTTP 失败链路 | CLOSED（此前） | 复用原超时/取消及 400/500 测试补强断言；新增受控异常中间件回归，验证完整错误体与回调一致性 |
| 覆盖计划 P2-3 横向边界清单 | CLOSED（本轮） | 六类适用性、契约、断言和证据清单；接入单文件/批量补测模板、贡献指南及索引 |
| HTTP 后续批准修复 | CLOSED（此前） | 8 类修复及 HTTP-FIX-01/02 完成，逐项见 HTTP 记录；本轮补测范围另列 |
| SEC-001 | CLOSED（此前） | 遗留密码入口弃用边界 |
| SEC-002 | CLOSED（此前） | 版本化请求签名迁移入口 |
| SEC-003 | CLOSED（此前） | 密钥副本及清理 |
| SEC-004 | CLOSED（此前） | 国密互操作约定 |
| SEC-005 | CLOSED（此前） | 国密流式 Hash/HMAC |
| SEC-006 | CLOSED（此前） | 文件路径跨平台比较 |
| SEC-007 | CLOSED（此前） | 依赖漏洞检查门禁 |
| SEC-008 | CLOSED（此前） | 密码对象及流生命周期 |
| SEC-009 | CLOSED（此前） | 参数、认证及验签失败语义 |
| SEC-010 | CLOSED（此前） | 迁移文档及可比较基准 |
| 图像批准范围 | CLOSED（此前） | 三后端静态 JPEG/PNG、元数据、安全保存，细项见图像记录 |
| Text 资源及拼音 | CLOSED（此前） | 词组拼音、外置目录、地区转换、可选 HMM、轻量包及消费者 |
| Emoji 精简与导入 | CLOSED（此前） | 默认精简、离线本地化、肤色目录及消费者 |
| Conv 既定能力 | CLOSED（此前） | 核心转换及验收完成，见 Conv 当前状态和转换修复记录 |
| 反射实施和目录迁移 | CLOSED（此前） | 逐个 M 条目、契约、宿主及批准诊断基线见反射当前记录，不新增探针 |

此前结果只引用原记录，不声明本轮重跑。专项汇总不替代原记录的细项。

## 此前证据复用

Text 的 `FixedLength(0)` 已改为构造时抛 `ArgumentOutOfRangeException("length")`，IdUtils 只增加测试友元与受控时钟测试，算法不变。上轮 Release 编译及验证：Text 定向 39/39、完整模块 322/322；IdUtils 定向 14/14；主包旧边界 4/4；旧 `SplitterUT` 50/50，均无失败、无跳过。定向 Text 是完整模块的子集，计数不相加。

证据位于 `TestResults/todo-p1-boundaries/`：`text-splitter-net8.trx`、`idutils-snowflake-net8.trx`、`core-splitter-net8.trx`、`legacy-splitter-net8.trx`、`text-full-net8.trx`。本轮不修改这些实现，不重跑其验证。

上传批次的 net6/net8 各 64/64 是历史证据。此后 P2-1 修改共享宿主并补强失败链路，当前集成证据为 net6/net8 各 65/65，定向 5 项是其子集，不叠加计数；Release 编译均通过。最终 TRX 位于 `TestResults/todo-http-failures/`：`http-failure-targeted-net8.trx`、`http-failure-full-net8-authorized.trx`、`http-failure-full-net6.trx`。

保留该批次沙箱诊断结果 `http-failure-full-net8.trx`（54 通过、11 失败，证书导入或文件替换拒绝访问）；同一 net8 产物经权限流程复核后 65/65 通过。本轮源码、测试、目标框架未变，仅复用此前已记录环境下的本地证据，不声称重新构建、重新测试或验证其他平台。nightly 实际 CI 仍待验证。

## 本轮影响与验证

- Task：todo-boundary-checklist-p2-3；Round：六类边界检查策略。
- ChangedFiles：见本页 8 个文档；ChangedProjects、ChangedPublicContracts、ChangedProviders、ChangedRuntimePaths：无。
- ChangedTFMs、ChangedBuildPackaging、ChangedBenchmarkHarness：无。ChangedDocsOnly：是。AffectedDependents：补测模板使用者和贡献者。RiskLevel：LOW。
- L0：`git diff --check` 通过；本轮 8 个文档严格 UTF-8 解码通过、50 个相对链接目标存在；六类样例文件/方法及断言含义经只读核对一致。Git 的已有换行转换提示不代表格式检查失败。
- L1—L5：文档不改变可执行产物，未重新构建或运行测试；复用上节已有编译及本地回归证据，不增加基线、资源探针或视觉校验。
- 独立复核：Luna 未发现清单、两个补测模板及模板索引的可行动问题；主代理另修正批量模板的旧示例路径，并确保既定 TODO 顺序不被模板的风险排序覆盖。
- 本轮 Implementation：1/1（策略文档及模板接入）；文档静态验证 PASS，编译/测试未重跑。Goal Status：COMPLETED，Next Action：STOP；仓库其余 TODO 与发布门禁见下节，不据此宣布全部仓库任务完成。

## 未完成与依赖

保持各原清单优先级及内部顺序；跨清单没有批准总顺序，未定级的审计建议不冒充原计划排序。

| 原来源／顺序 | 剩余条目 | 状态、具体上游及阻塞 |
| --- | --- | --- |
| 质量报告 P1-2 | Http 多框架持续验收 | PARTIAL / BLOCKED_EXTERNAL；测试目标及 nightly 入口已实现；上游 P1-1 已完成，尚待工作流实际运行并取得 CI 结果 |
| 跨平台验收 | 运行时、GDI 平台和字体/ICC 资源 | BLOCKED_APPROVAL / BLOCKED_EXTERNAL；依赖支持矩阵、可分发资源及 runner，不自行删目标或扩矩阵 |
| 覆盖计划 P2-2 | Reflection 契约回归 | 实现与本地回归已由反射专项完成；远程结果 BLOCKED_EXTERNAL，不重新打开已关闭代码项 |
| 质量报告 P2-1 | 图像性能基准与阈值 | BLOCKED_APPROVAL；依赖批准负载、机器和预算，后续图像记录未批准此性能任务 |
| 质量报告 P2-2 | Reflection/Text 异常模板 | OPEN_ACTIONABLE；无未完成编码上游。本轮只接入通用清单，尚需按原条目的 null/empty/invalid 范围核对并补齐参数化回归；已有测试可复用，不将全 API 覆盖设为额外前置条件 |
| 反射剩余项 | 默认性能预算和容量告警 | BLOCKED_APPROVAL；依赖业务批准数值及环境，已有可选策略，不重建基线 |
| 反射剩余项 | 外部发布、消费者及依赖诊断治理 | BLOCKED_EXTERNAL / 待确认；依赖真实 CI、消费者及责任人；不擅自清零或扩大批准诊断 |
| 模块文档，未定级 | FTP 配置接入 | OPEN_ACTIONABLE / 部分待确认；字段映射可独立推进，代理接入方式及 FTP 验收环境需确认 |
| 接口推断，未定级 | JSON 转换器读取 | BLOCKED_APPROVAL；上游是仅写/双向及表结构契约，不定义猜测的读取格式 |
| 模块文档，未定级 | DependencyInjection | BLOCKED_APPROVAL；上游是保留与公共 API 决策，本轮禁止新增依赖 |
| 模块文档，未定级 | CharGuard | BLOCKED_APPROVAL；上游是守护契约及兼容决策 |
| 下一主版本安全计划 | 遗留密码 API 移除 | DEFERRED；依赖下一主版本、完整弃用周期和迁移安排，当前保留兼容 |
| 上轮风险审计，非实施方案 | FTPS 证书、防重放、词库容量、正则超时 | 待确认；分别依赖信任策略、调用方协议、资源上限、超时契约，不能自动当作批准方案 |

其他边界：图像动画/新格式、Emoji 额外语言及旧 API、Conv 其他优化沿用原记录 DEFERRED 或 ACCEPTED_LIMITATION；旧运行时、Linux、DocFX 沿用各专项外部阻塞。反射旧运行时证据不扩大为其他模块已验证。

另发现同步 `FileHelper.Write(byte[],...)` 仍有未释放 `File.Create` 的相似问题，与已关闭的异步条目不同，待纳入明确后续修复范围；本轮不修改。

下一轮建议：按质量报告 P2 原顺序记录 P2-1 的预算阻塞，再处理可独立执行的 P2-2 Reflection/Text 异常模板回归，限原定 null/empty/invalid 范围。它无未完成编码依赖，可复用本轮清单与现有测试；不扩为全 API 扫描。质量报告 P1-2、覆盖计划 P2-2 保持待外部实测，不重复增加工作流或重跑无关用例。

## 本轮文件

- 新增 `docs/quality/boundary-test-checklist.md`：六类边界检查、记录模板及已有样例。
- 更新 `docs/quality/prompts/test-completion-for-file.prompt.md`：单文件补测接入清单与最小验证。
- 更新 `docs/quality/prompts/test-completion-batch.prompt.md`：批量补测逐项接入清单，不扩大条目。
- 更新 `docs/quality/prompts/README.md`：收录既有补测、风格模板及清单入口。
- 更新 `docs/CONTRIBUTING.md`：贡献流程与 PR 检查接入清单，按影响范围选择验证。
- 更新 `docs/README.md`：增加清单和模板索引入口。
- 更新 `docs/quality/test-coverage-map.md`：同步 P2-3 完成状态，保留原计划。
- 更新 `docs/quality/todo-execution.md`：本轮状态、证据复用、剩余项与依赖。

本轮没有独立于 TODO 的配置清理，也没有 CI、基线或构建配置变更。
