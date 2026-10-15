# TODO 执行记录

更新时间：2026-10-15；本轮基于 `0684e29`，开始时工作区干净。承接质量报告 P2-2 的 Reflection/Text 异常输入模板化测试；不修改生产代码、依赖、目标框架、CI 或基线。

## 执行依据与本轮范围

- 仓库没有统一的跨模块总排期。按各既有清单及后续记录核对，不将上轮审计建议视为已批准设计。
- [覆盖计划](test-coverage-map.md) 的 P0、P1、P2-1、P2-3 及 [质量报告](test-quality-report.md) P1-1 已完成；覆盖计划 P2-2 已实现并通过本地回归，仅外部证据待验证。质量报告 P1-2 的本地实现完成，仅实际 nightly 待验证。质量报告 P2-1 的性能负载、机器和预算尚未批准，保持阻塞；P2-2 不依赖该预算，可独立推进。
- 核对 [质量报告](test-quality-report.md)、[HTTP](http-security-reliability-report.md)、[安全清单](../security-cryptography-todo.md)、[图像](image-processing-execution.md)、[Text 资源](text-resources-execution.md)、[Emoji](emoji-execution.md)、[转换](conv-current-status.md)、[反射](reflection-todo-status.md) 的最新状态，后续记录覆盖历史描述。
- 原计划 P2-2 要求 `null/empty/invalid` 异常输入模板化测试。本轮使用既有 [六类边界清单](boundary-test-checklist.md) 核对代表性入口，复用已覆盖场景，只补明确缺口，不扩为全 API 扫描。
- Text：旧聚合测试已覆盖 `CaseFormatter` 的 LowerUnderscore 空输入及非法枚举，`SplitterTest` 已覆盖非正长度。补其余六种格式化样式的 null/空串/空白，以及 `OnPattern` 的非法正则模式。
- Reflection：`TypeVisitAdvancedTests` 已覆盖方法名称、类型、签名的非法输入；补泛型 `GetProperties` 显式 null/空选择器数组的全量属性契约，避免把合法空数组错误归为异常。
- 按 `test-project-boundary/refs/test-project-boundary.md`，新增断言分别归属 Text.Tests 与 Reflection.Tests，不迁移旧聚合测试，不改测试共享设施。`TypeReflections` 主包其他入口不纳入本轮代表性模板范围。
- 主代理负责 Text、集成和验证；Luna 独立定位 Reflection 缺口，仅实现 `TypeVisitMembersTests` 中明确的选择器场景。

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
| 覆盖计划 P2-3 横向边界清单 | CLOSED（此前） | 六类适用性、契约、断言和证据清单；接入单文件/批量补测模板、贡献指南及索引 |
| 质量报告 P2-2 Reflection/Text 异常模板 | CLOSED（本轮） | 新增 10 个参数化测试实例，补齐格式化空输入、非法正则和显式空选择器；复用原有非法签名回归 |
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

Text 的 `FixedLength(0)` 已改为构造时抛 `ArgumentOutOfRangeException("length")`，IdUtils 只增加测试友元与受控时钟测试，算法不变。此前 Release 编译及验证：Text 定向 39/39、当时完整模块 322/322；IdUtils 定向 14/14；主包旧边界 4/4；旧 `SplitterUT` 50/50，均无失败、无跳过。旧 Text 计数仅为历史证据，本轮修改测试后不宣称当前完整模块仍为 322 项。

证据位于 `TestResults/todo-p1-boundaries/`：`text-splitter-net8.trx`、`idutils-snowflake-net8.trx`、`core-splitter-net8.trx`、`legacy-splitter-net8.trx`、`text-full-net8.trx`。本轮不修改这些生产实现；Text 受影响类使用下节新结果，其他模块不重跑。

上传批次的 net6/net8 各 64/64 是历史证据。此后 P2-1 修改共享宿主并补强失败链路，当前集成证据为 net6/net8 各 65/65，定向 5 项是其子集，不叠加计数；Release 编译均通过。最终 TRX 位于 `TestResults/todo-http-failures/`：`http-failure-targeted-net8.trx`、`http-failure-full-net8-authorized.trx`、`http-failure-full-net6.trx`。

保留该批次沙箱诊断结果 `http-failure-full-net8.trx`（54 通过、11 失败，证书导入或文件替换拒绝访问）；同一 net8 产物经权限流程复核后 65/65 通过。本轮 HTTP 相关源码、测试及框架未变，复用其原本地证据，不声称重跑 HTTP 或验证其他平台。nightly 实际 CI 仍待验证。

## 本轮影响与验证

- Task：todo-exception-templates-p2-2；Round：异常输入模板化回归。
- ChangedFiles：见本页 5 个文件；ChangedProjects：Text.Tests、Reflection.Tests。ChangedPublicContracts、ChangedProviders、ChangedRuntimePaths：无生产变更。
- ChangedTFMs、ChangedBuildPackaging、ChangedBenchmarkHarness：无。ChangedDocsOnly：否。AffectedDependents：仅两个模块测试项目，未修改共享测试设施。RiskLevel：LOW。
- L0：两个测试项目及实际引用目标的 Release 编译通过；`git diff --check`、本轮 5 个文件严格 UTF-8 解码、14 个相对链接检查通过。既有编译警告与换行提示不做无关清理。
- L1：net8.0 Text 的 `CaseFormatterTest|SplitterTest` 共 72/72；Reflection 的 `TypeVisitMembersTests|TypeVisitAdvancedTests|TypeVisitSelectorValidationTests` 共 67/67，通过且无失败、无跳过。新增测试实例为 Text 8 项、Reflection 2 项，包含在上述计数内。
- 成功命令见下方；结果保存在 `TestResults/todo-exception-templates/text-exceptions-net8.trx` 和 `reflection-exceptions-net8.trx`。
- 环境诊断：首次默认并行构建无输出，终止该命令产生的节点后改为单进程。旧资产缺少 netstandard2.0，通过现有项目 `dotnet restore --ignore-failed-sources -m:1 -nr:false` 修复；沙箱随后拒绝写入已有 AssemblyInfoInputs.cache，两个相同范围的测试经权限流程执行成功。未修改项目、包版本、断言或测试过滤来掩盖失败。
- L2—L5：仅增加测试，无共享设施或生产变化，不扩为完整模块、全解决方案、跨框架、性能或视觉验证。
- 复核：主代理核对 Reflection 属性全集及重载契约；Luna 只读复核 Text 新增测试。没有将合法 null/空数组改写为应抛异常。
- 本轮 Implementation：1/1；本地编译及定向验收 2/2 PASS。No-Progress：CHANGED。Goal Status：COMPLETED，Next Action：STOP；仓库外部验收仍按下节保留。

```powershell
dotnet test tests/Bing.Utils.Text.Tests/Bing.Utils.Text.Tests.csproj -c Release -f net8.0 --no-restore --filter 'FullyQualifiedName~CaseFormatterTest|FullyQualifiedName~SplitterTest' --results-directory TestResults/todo-exception-templates --logger 'trx;LogFileName=text-exceptions-net8.trx' --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet test tests/Bing.Utils.Reflection.Tests/Bing.Utils.Reflection.Tests.csproj -c Release -f net8.0 --no-restore --filter 'FullyQualifiedName~TypeVisitMembersTests|FullyQualifiedName~TypeVisitAdvancedTests|FullyQualifiedName~TypeVisitSelectorValidationTests' --results-directory TestResults/todo-exception-templates --logger 'trx;LogFileName=reflection-exceptions-net8.trx' --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

### 本轮六类边界记录

契约依据：`src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs` 的格式化分支、既有聚合 `CaseFormatterTest` 兼容断言；`Splitter.cs` 的 `InternalSplitToEnumerable/SplitPatternList`；`src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs` 对 null/空选择器的明确约定及 [反射失败契约](reflection-contract-migration.md)。

| 类别 | 本轮适用性、输入与预期 | 测试与证据 |
| --- | --- | --- |
| null | 适用；格式化 null 返回空串，显式 null 的 params 选择器返回完整可读属性集合 | 新增 `To_WithNullOrEmptySequence_ReturnsEmptyString`、`GetProperties_GenericNullOrEmptySelectors_ReturnsAllGetterProperties`；上述 TRX |
| empty | 适用；空串/空白格式化为空串，空选择器数组返回三项精确属性集合 | 同上；原 LowerUnderscore 兼容用例仅作契约参考，不声称本轮运行旧聚合项目 |
| invalid | 适用；非法正则在分割时抛参数异常；非法名称、签名等维持现有异常类型 | 新增 `OnPattern_WithInvalidPattern_ThrowsArgumentExceptionWhenSplit`；已有 `FindMethod_WithInvalidArguments_ThrowsExpectedException` 纳入本轮 Reflection 定向测试 |
| overflow | NOT_APPLICABLE；本轮冻结范围是空输入、正则语法及选择器分派，不涉及数值容量运算 | 不扩展既有 Snowflake 等其他模块测试 |
| concurrent | NOT_APPLICABLE；本轮未修改并发实现或声明新的线程安全契约 | 不新增并发测试或基础设施 |
| perf | 无生产或夹具变化；复用原记录中的未受影响性能证据边界 | 未运行 benchmark，不宣称新阈值或性能通过 |

## 未完成与依赖

保持各原清单优先级及内部顺序；跨清单没有批准总顺序，未定级的审计建议不冒充原计划排序。

| 原来源／顺序 | 剩余条目 | 状态、具体上游及阻塞 |
| --- | --- | --- |
| 质量报告 P1-2 | Http 多框架持续验收 | PARTIAL / BLOCKED_EXTERNAL；测试目标及 nightly 入口已实现；上游 P1-1 已完成，尚待工作流实际运行并取得 CI 结果 |
| 跨平台验收 | 运行时、GDI 平台和字体/ICC 资源 | BLOCKED_APPROVAL / BLOCKED_EXTERNAL；依赖支持矩阵、可分发资源及 runner，不自行删目标或扩矩阵 |
| 覆盖计划 P2-2 | Reflection 契约回归 | 实现与本地回归已由反射专项完成；远程结果 BLOCKED_EXTERNAL，不重新打开已关闭代码项 |
| 质量报告 P2-1 | 图像性能基准与阈值 | BLOCKED_APPROVAL；依赖批准负载、机器和预算，后续图像记录未批准此性能任务 |
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

下一轮建议：先核对未定级 FTP 配置接入的既有方案与实际字段映射，仅处理契约明确且不依赖代理选择的部分；代理接入和真实 FTP 环境继续保持待确认。已定级的剩余项均依赖预算或外部验收，不为推进而修改其范围。

## 本轮文件

- 更新 `tests/Bing.Utils.Text.Tests/Bing/Text/Formatting/CaseFormatterTest.cs`：六种样式的 null/空串/空白参数化回归。
- 更新 `tests/Bing.Utils.Text.Tests/Bing/Text/Splitters/SplitterTest.cs`：两种非法正则模式的分割异常。
- 更新 `tests/Bing.Utils.Reflection.Tests/Bing/Reflection/TypeVisitMembersTests.cs`：显式 null/空选择器的精确属性集合回归。
- 更新 `docs/quality/test-quality-report.md`：同步质量 P2-2 完成及本地证据。
- 更新 `docs/quality/todo-execution.md`：本轮范围、六类边界记录、证据与剩余依赖。

本轮没有独立于 TODO 的配置清理，也没有 CI、基线或构建配置变更。
