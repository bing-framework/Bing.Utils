# TODO 执行记录

更新时间：2026-10-15。本轮核对发现质量报告 P0-2 的生产修复虽已完成，但原计划明确要求的并发／重复写入回归尚未补齐；本轮范围仅补齐该项验收。已有未提交变更全部保留；下方旧批次的“本轮”仅指各自历史语境。

## 当前批次：质量报告 P0-2 回归补齐

依据 [质量报告](test-quality-report.md) 第 4 节 P0-2，补齐 `FileHelper.WriteAsync(byte[],...)` 重复写入、空内容截断、返回后句柄释放及独立文件并发写入验证。既有覆盖／追加回归保留；不同文件并发不意味着同路径写入被串行化。

影响：ChangedProjects 仅 `Bing.Utils.Tests`；ChangedPublicContracts、ChangedRuntimePaths、ChangedProviders、ChangedTFMs、ChangedBuildPackaging、ChangedBenchmarkHarness 均无变化。按测试归属规则，被测对象属于主包，沿用现有测试项目及回归文件，不迁移测试。RiskLevel LOW，不涉及生产代码、依赖、CI 或基线。

本轮文件：更新 `tests/Bing.Utils.Tests/Bing/IO/FileHelperWriteRegressionTest.cs`、本页及 `docs/quality/test-quality-report.md`。新增 2 项测试：同路径连续写入长／短／空／后续内容，每次返回后独占重开核对字节；协调 8 个不同文件的并发写入，核对各自完整内容。无混合测试需要拆分，无测试迁移或引用变更。

验证：net8.0 Release 测试项目及其既有引用编译通过；写入相关测试 5/5 通过、0 失败、0 跳过（含新增 2 项，不重复叠加计数）。L0/L1 满足本轮验收，生产源码未变，不扩展多目标矩阵、全量测试或性能测量。现有编译警告保留，不做无关清理。证据：`TestResults/todo-file-write/file-write-net8.trx`。

首次 `--no-restore` 未进入测试：既有 obj 写入被拒绝，且 Http 资产缺少 net8.0；经权限流程使用本机缓存还原既有依赖后，以相同过滤重试通过。未修改包版本、TFM、断言或构建配置。

```powershell
dotnet restore tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj --source C:/Users/jianx/.nuget/packages --nologo -m:1 -nr:false
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0 --no-restore --filter 'FullyQualifiedName~Bing.IO.FileHelperWriteRegressionTest|FullyQualifiedName~Bing.IO.FileHelperWriteAsyncTest' --results-directory TestResults/todo-file-write --logger 'trx;LogFileName=file-write-net8.trx' --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

P0-2 本轮缺失验收 2/2 CLOSED，OPEN_ACTIONABLE 0，No-Progress CHANGED，Next Action STOP；下方未完成条目保持各自原优先级和具体依赖，不因外部阻塞自动重测或扩大实现范围。本轮没有独立配置清理。

## 上一批次：DependencyInjection

保留四个生产 TFM，运行库仅添加 Abstractions 8.0.2，现有模块测试使用官方容器 8.0.1。继承的 SourceLink 仅在 DI 项目标记为私有构建依赖。无其他模块依赖、CI、性能基线或发行版本改动。影响分析、验收命令及本轮全部 19 个文件见 DI 执行记录；实现 4/4、批准本地门禁 3/3 PASS，OPEN_ACTIONABLE 0，No-Progress CHANGED，Next Action STOP。其他模块剩余项保持原有状态，不重复验证。

## 上一批次：反射四项预算

实现与证据入口：[反射预算校准记录](reflection-budget-execution.md)、[冻结策略](reflection-performance-policy.local-net8.json)。一次无预算三轮校准后，仅从基线 21 条冷启动和 33 条稳态记录按批准公式冻结四项上限；一次带策略三轮正式确认得到 132 条完整结果，冷启动两项及稳态分配通过，稳态第一轮 `method signature` 为 2744.2 ns > 2376 ns，正式退出码 1。校准及策略落地 CLOSED，性能验收 PARTIAL / BLOCKED_EXTERNAL，不能声明整项通过。

本轮影响：ChangedProjects 为现有 comparison 工具；ChangedBenchmarkHarness 仅执行脚本的限时及失败证据路径，探针负载不变；ChangedPublicContracts/RuntimePaths/Providers/TFMs/BuildPackaging 均无生产变更；AffectedDependents 为显式预算调用者；RiskLevel LOW。主代理负责集成/校准/验证，Luna 窄范围只读复核样本选择及脚本改动。

本轮验证：L0 PowerShell 语法、策略 schema、UTF-8、文档链接和 `git diff --check`；两阶段既有流程各编译基线、候选和探针并成功。L1/harness smoke 由首次无预算完整比较覆盖，两个阶段结果/缓存指标均 132/132、runtime header 60/60；L5 仅本次校准及一次正式确认。L2/L3/L4 不运行，复用源码未变的既有反射正确性证据，不重跑图像、trim/AOT、容量或全量测试。

实现 2/2 PASS，四项预算门禁 3/4；同侧源码清单、测量程序及记录环境一致，正式第一轮基线自身也超出冻结耗时上限，具体原因未定位。OPEN_ACTIONABLE 0，性能及外部门禁 BLOCKED_EXTERNAL；No-Progress CHANGED，Goal STOPPED_BLOCKED，Next Action STOP，不调整阈值或自动第三次测量。容量告警和扩展矩阵继续 DEFERRED；统一上限局限为 ACCEPTED_LIMITATION；本批无新增 BLOCKED_APPROVAL / NOT_APPLICABLE / VERIFIED_BOUNDARY。

本轮 8 文件：更新 `benchmarks/ReflectionComparisonProbe/Run-ReflectionComparison.ps1`；新增 `docs/quality/reflection-performance-policy.local-net8.json`、`docs/quality/reflection-budget-execution.md`；更新 `docs/README.md`、`docs/quality/reflection-performance-evidence.md`、`docs/quality/reflection-todo-status.md`、`docs/quality/performance-scope-decisions.md`、本页。没有独立配置清理、CI 或诊断基线变更，原始输出仍在既有忽略目录，不暂存或提交。

上一图像批次：质量 P2-1 的 8 场景负载、smoke、校准和预算检查已实现，分配量 8/8、耗时 4/8 达标；保持 PARTIAL / BLOCKED_EXTERNAL，见 [图像执行记录](image-performance-execution.md)。本轮未重跑。下文 FTP 的“本轮影响”“本轮文件”及决策历史均保留原批次语境，由本节覆盖当前状态。

## 后续决策（2026-10-15）

用户委托决策质量 P2-1 与反射剩余项，详细规则见 [图像性能与反射决策](performance-scope-decisions.md)。本次只更新决策文档，保留上轮 6 个未提交文件，不修改业务代码、测试、CI、基线或依赖。下文“本轮”仍指 FTP 实现批次。

- 决策时的 `OPEN_ACTIONABLE` 为质量 P2-1 与反射预算；现两者实现及校准均已完成，性能验收阻塞，当前没有已确认可执行的编码项。
- `DEFERRED`：生产容量/告警数值、更多插件和 root 矩阵、外部自建缓存扩展审计、默认 PR 绝对时间门禁。
- `BLOCKED_EXTERNAL`：远程 CI、实际消费者及发布证据；既有诊断严格门禁和接受范围不变。
- 执行顺序：先质量 P2-1，再反射性能落地；其他条目契约不变。决策批次未触发测试，当前实现批次的实际运行见上节。

## 执行依据与本轮范围

- 仓库没有统一的跨模块总排期。按各既有清单及后续记录核对，不将上轮审计建议视为已批准设计。
- [覆盖计划](test-coverage-map.md) 的 P0、P1、P2-1、P2-3 及 [质量报告](test-quality-report.md) P1-1、P2-2 已完成；覆盖计划 P2-2 的实现与本地回归完成，仅外部证据待验证。质量报告 P1-2 待实际 nightly；质量报告 P2-1 待性能负载、机器和预算批准，均不重复编码。本轮按总账的下一建议核对 FTP，不重排已定级清单。
- 核对 [质量报告](test-quality-report.md)、[HTTP](http-security-reliability-report.md)、[安全清单](../security-cryptography-todo.md)、[图像](image-processing-execution.md)、[Text 资源](text-resources-execution.md)、[Emoji](emoji-execution.md)、[转换](conv-current-status.md)、[反射](reflection-todo-status.md) 的最新状态，后续记录覆盖历史描述。
- FTP 没有单独的详细 plan，依据 [Net 模块待实现项](../modules/Bing.Utils.Net.md)、配置模型 XML 注释和固定依赖 FluentFTP 42.0.1 的本地 API 文档核对；不将模型中所有字段默认当作同义映射。
- 本轮明确映射：`Timeout` 同时用于控制/数据连接建立；`ReadWriteTimeout` 接入控制/数据通道读取超时；`UsePassive` 选择 `AutoPassive/AutoActive`。不新增参数、默认值或校验规则。
- 保留原条目未完成部分：代理、其他超时、KeepAlive、连接重试、ASCII 模式及默认目录的具体契约待确认，逐项原因见下节；不为推进而改写文件长度校验、连接生命周期或传输算法。
- 按 `test-project-boundary/refs/test-project-boundary.md`，配置映射断言归属 Net.Tests，不创建新测试项目；真实服务器协作沿用既有 Net.Tests.Integration，未执行外部 FTP 操作。
- 主代理负责生产实现、文档和验证；Luna 负责独立配置核对、新增单文件测试及对生产映射的只读复核。

## 已完成 TODO

| 原条目 | 状态 | 实现说明 |
| --- | --- | --- |
| DI-01 基础批量注册 | CLOSED（此前） | 显式接口、必传生命周期、原生去重三策略，先校验后注册 |
| DI-02 扫描与开放泛型 | CLOSED（此前） | 显式程序集去重与排序、可见类过滤、参数原序映射校验 |
| DI-03 复杂泛型提前闭合 | CLOSED（此前） | 有限服务结构推导、约束、未绑定参数及歧义拒绝 |
| DI-04 测试兼容与交付 | CLOSED（此前） | 四生产 TFM、net8 测试 38/38、本地包依赖通过，中文模块文档及索引齐全 |
| 覆盖计划 P0-1 ImageSharp | CLOSED（此前） | `ImageSharpHelperTest` 已覆盖空参、透明度边界、克隆及 alpha |
| 覆盖计划 P0-2 SkiaSharp | CLOSED（此前） | helper 与格式扩展测试已覆盖输入边界和格式映射 |
| 覆盖计划 P0-3 验证码 | CLOSED（此前） | `CaptchaBuilderTest` 已断言非法长度、空码和尺寸 |
| 覆盖计划 P1-1 Collections | CLOSED（此前） | `DictsAndCollConvTest`、`ReadOnlyDictsContractTest`、`Colls*Test` 已覆盖集合边界 |
| 覆盖计划 P1-2 Text | CLOSED（此前） | 拒绝零/负长度；覆盖参数信息、空输入、重复键及空键值分隔符；同步旧异常断言 |
| 覆盖计划 P1-3 IdUtils | CLOSED（此前） | 受控时钟覆盖回拨、恢复、序列溢出和真实等待循环；越界参数复用 `SnowflakeGeneratorTest` |
| 质量报告 P0-1 SendBefore | CLOSED（此前） | 正确判断回调，`HttpClientServiceTest.Regression` 已有回归 |
| 质量报告 P0-2 异步覆盖写 | CLOSED（本轮补齐验收） | 生产修复沿用；重复覆盖、空内容截断、句柄释放及不同文件并发回归通过，相关测试共 5/5 |
| 质量报告 P1-1 上传集成用例 | CLOSED（此前） | 无目录文件名、不同字段下的同名文件、同时到达的独立上传请求均验证实际落盘字节；测试控制器隔离并清理临时目录 |
| 覆盖计划 P2-1 HTTP 失败链路 | CLOSED（此前） | 复用原超时/取消及 400/500 测试补强断言；新增受控异常中间件回归，验证完整错误体与回调一致性 |
| 覆盖计划 P2-3 横向边界清单 | CLOSED（此前） | 六类适用性、契约、断言和证据清单；接入单文件/批量补测模板、贡献指南及索引 |
| 质量报告 P2-2 Reflection/Text 异常模板 | CLOSED（此前） | 新增 10 个参数化测试实例，补齐格式化空输入、非法正则和显式空选择器；复用原有非法签名回归 |
| 质量报告 P2-1 图像性能实现部分 | 此前完成部分（整项 PARTIAL） | 8 场景负载、smoke、固定基线校准及预算检查已实现；编译和 smoke 通过，候选耗时门禁未通过 |
| 反射四项预算校准及策略落地 | CLOSED（此前，实现部分） | schema 1 四项冻结值、限时执行和失败完整证据已落地；正式稳态耗时 1 条超限，性能验收仍阻塞 |
| FTP 配置接入的明确字段 | 此前完成部分（整体未关闭） | 连接/读取超时覆盖控制及数据通道，主动/被动模式生效；默认与自定义配置回归已通过 |
| HTTP 后续批准修复 | CLOSED（此前） | 8 类修复及 HTTP-FIX-01/02 完成，逐项见 HTTP 记录；本轮未修改或重跑 |
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

Text 的 `FixedLength(0)` 已改为构造时抛 `ArgumentOutOfRangeException("length")`，IdUtils 只增加测试友元与受控时钟测试，算法不变。此前 Release 编译及验证：Text 定向 39/39、当时完整模块 322/322；IdUtils 定向 14/14；主包旧边界 4/4；旧 `SplitterUT` 50/50，均无失败、无跳过。旧 Text 全量计数只表示当时测试资产。

证据位于 `TestResults/todo-p1-boundaries/`：`text-splitter-net8.trx`、`idutils-snowflake-net8.trx`、`core-splitter-net8.trx`、`legacy-splitter-net8.trx`、`text-full-net8.trx`。此后异常模板批次 net8 Release 编译及 Text 定向 72/72、Reflection 定向 67/67 通过，见 `TestResults/todo-exception-templates/text-exceptions-net8.trx`、`reflection-exceptions-net8.trx`。本轮未修改这些模块，不重跑其验证。

上传批次的 net6/net8 各 64/64 是历史证据。此后 P2-1 修改共享宿主并补强失败链路，当前集成证据为 net6/net8 各 65/65，定向 5 项是其子集，不叠加计数；Release 编译均通过。最终 TRX 位于 `TestResults/todo-http-failures/`：`http-failure-targeted-net8.trx`、`http-failure-full-net8-authorized.trx`、`http-failure-full-net6.trx`。

保留该批次沙箱诊断结果 `http-failure-full-net8.trx`（54 通过、11 失败，证书导入或文件替换拒绝访问）；同一 net8 产物经权限流程复核后 65/65 通过。本轮 HTTP 相关源码、测试及框架未变，复用其原本地证据，不声称重跑 HTTP 或验证其他平台。nightly 实际 CI 仍待验证。

## 本轮影响与验证

- Task：todo-ftp-config；Round：明确配置映射。
- ChangedFiles：见本页 6 个文件；ChangedProjects：Net、Net.Tests。ChangedPublicContracts：无签名变更，原先忽略的超时和连接模式开始生效；ChangedRuntimePaths：客户端构造时的控制/数据连接配置。ChangedProviders：仅 FluentFTP 适配配置。
- ChangedTFMs、ChangedBuildPackaging、ChangedBenchmarkHarness：无。ChangedDocsOnly：否。AffectedDependents：FTP 客户端调用方；原配置默认超时将覆盖 FluentFTP 默认值。RiskLevel：MEDIUM，已在模块文档标明升级影响。
- L0：net8.0 测试项目及引用项目 Release 编译通过；Net 的 netstandard2.0 Release 编译通过。`git diff --check` 通过，6 个变更文件严格 UTF-8 解码通过，3 份文档的 31 个本地链接有效；已有编译警告不做无关清理。
- L1/L2：一次运行 Net.Tests 完整单元项目 24/24 通过，无失败、无跳过，其中包含本轮新增的默认配置、自定义被动、自定义主动 3 例，不重复叠加计数。
- 证据：`TestResults/todo-ftp-config/net-config-net8.trx`；成功命令见下方。未使用外部 FTP 服务，未把无网络构造断言当作真实协商结果。
- 环境诊断：沙箱拒绝写入 Net 的既有 obj 缓存及 netstandard2.0 输出 DLL；相同测试/构建经权限流程通过。未修改配置、依赖、断言、过滤或忽略检查来规避失败。
- L3：真实 FTP 控制/数据通道、主动模式和超时触发验收 BLOCKED_EXTERNAL，需要隔离服务器与凭据；L4/L5 不运行，无共享核心、性能或夹具变化，不扩矩阵、不增加 CI、资源或视觉验证。
- 复核：Luna 未发现生产映射及模块文档的可行动问题；主代理核对测试的实际底层配置、默认值、两个模式及未连接状态。私有反射仅用于测试观察，不新增生产入口或友元。
- 本轮已确认可映射字段 3/3；本地编译/单元门禁 2/2 PASS。FTP 原条目整体 PARTIAL，未确认字段 BLOCKED_APPROVAL、服务器验收 BLOCKED_EXTERNAL。No-Progress：CHANGED。本轮无剩余已确认可编码项，Goal Status：STOPPED_BLOCKED，Next Action：STOP；不自动关闭原 FTP 条目。

```powershell
dotnet test tests/Bing.Utils.Net.Tests/Bing.Utils.Net.Tests.csproj -c Release -f net8.0 --no-restore --results-directory TestResults/todo-ftp-config --logger 'trx;LogFileName=net-config-net8.trx' --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet build src/Bing.Utils.Net/Bing.Utils.Net.csproj -c Release -f netstandard2.0 --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

### 本轮六类边界记录

契约依据：`src/Bing.Utils.Net/Bing/Net/FTP/FtpClientConfig.cs` 的毫秒单位及模式字段；FluentFTP 42.0.1 本地 XML 的 `FtpConfig.ConnectTimeout/ReadTimeout/DataConnectionConnectTimeout/DataConnectionReadTimeout/DataConnectionType`。目标是字段实际生效，不扩大为配置校验、连接重试或传输算法改造。

| 类别 | 本轮适用性、输入与预期 | 测试与证据 |
| --- | --- | --- |
| null | NOT_APPLICABLE 于新增数值/布尔字段映射；整个 config 为 null 的前置校验不是本轮变更 | 不新增 null 配置行为契约 |
| empty | NOT_APPLICABLE 于新增字段；主机、凭据和字符集处理未变 | 不将空主机解释为新连接规则 |
| invalid | 负值、无限超时哨兵及发送/断开超时的统一校验契约待确认，本轮不新增校验 | 已验证默认值、两组自定义有限正值及两种模式；真实超时触发仍待服务器 |
| overflow | NOT_APPLICABLE；无数值换算或容量算法，毫秒 int 原值传递 | 不制造长时间等待 |
| concurrent | NOT_APPLICABLE；未更改共享状态或声明同实例并发安全 | 每个测试独立构造并释放客户端 |
| perf | 未更改算法、热路径或 benchmark；配置超时不是吞吐阈值 | 不运行性能基线或资源矩阵 |

## 未完成与依赖

保持各原清单优先级及内部顺序；跨清单没有批准总顺序，未定级的审计建议不冒充原计划排序。

| 原来源／顺序 | 剩余条目 | 状态、具体上游及阻塞 |
| --- | --- | --- |
| 质量报告 P1-2 | Http 多框架持续验收 | PARTIAL / BLOCKED_EXTERNAL；测试目标及 nightly 入口已实现；上游 P1-1 已完成，尚待工作流实际运行并取得 CI 结果 |
| 跨平台验收 | 运行时、GDI 平台和字体/ICC 资源 | BLOCKED_APPROVAL / BLOCKED_EXTERNAL；依赖支持矩阵、可分发资源及 runner，不自行删目标或扩矩阵 |
| 覆盖计划 P2-2 | Reflection 契约回归 | 实现与本地回归已由反射专项完成；远程结果 BLOCKED_EXTERNAL，不重新打开已关闭代码项 |
| 质量报告 P2-1 | 图像性能基准与阈值 | PARTIAL / BLOCKED_EXTERNAL；实现、编译、smoke 及冻结校准已完成；同程序集两次测量有显著耗时差异，4/8 耗时未过预算；依赖稳定环境复核，无未完成代码上游，不自动重跑或放宽 |
| 反射剩余项 | 性能验收、默认门禁和容量告警 | 校准/策略实现 CLOSED；稳态耗时 1 条超限，性能验收 BLOCKED_EXTERNAL，依赖稳定环境，无未完成代码上游；默认 PR 绝对门禁及业务容量/告警 DEFERRED，结构门禁保留 |
| 反射剩余项 | 外部发布、消费者及依赖诊断治理 | BLOCKED_EXTERNAL / 待确认；依赖真实 CI、消费者及责任人；不擅自清零或扩大批准诊断 |
| 模块文档，未定级 | FTP 配置接入 | PARTIAL；连接/读取超时与主动/被动模式已接入。其余字段依赖下列具体契约，BLOCKED_APPROVAL；真实控制/数据通道及超时触发依赖服务器/凭据，BLOCKED_EXTERNAL |
| 接口推断，未定级 | JSON 转换器读取 | BLOCKED_APPROVAL；上游是仅写/双向及表结构契约，不定义猜测的读取格式 |
| 模块文档，未定级 | CharGuard | BLOCKED_APPROVAL；上游是守护契约及兼容决策 |
| 下一主版本安全计划 | 遗留密码 API 移除 | DEFERRED；依赖下一主版本、完整弃用周期和迁移安排，当前保留兼容 |
| 上轮风险审计，非实施方案 | FTPS 证书、防重放、词库容量、正则超时 | 待确认；分别依赖信任策略、调用方协议、资源上限、超时契约，不能自动当作批准方案 |

其他边界：图像动画/新格式、Emoji 额外语言及旧 API、Conv 其他优化沿用原记录 DEFERRED 或 ACCEPTED_LIMITATION；旧运行时、Linux、DocFX 沿用各专项外部阻塞。反射旧运行时证据不扩大为其他模块已验证。

另发现同步 `FileHelper.Write(byte[],...)` 仍有未释放 `File.Create` 的相似问题，与已关闭的异步条目不同，待纳入明确后续修复范围；本轮不修改。

FTP 未完成字段的具体依赖：

- 代理：需明确 HTTP/SOCKS/FTP 代理协议及凭据策略；现有 `WebProxy` 不能直接等同 FluentFTP 的具体代理客户端。
- `DisconnectTimeout/ReceiveTimeout/SendTimeout`：FluentFTP 42.0.1 的配置无同义字段；需要确定适配范围，不把读取超时冒充独立发送/断开超时。
- `ConnectCount`：需确定连接重试次数、失败范围和等待策略；`RetryAttempts` 是上传/下载校验失败重试，继续固定为 3。
- `KeepAlive`：需明确请求后连接生命周期或 TCP 保活，不能静默混用两种语义。
- `UseBinary`：ASCII 可改变换行及字节长度，需明确与现有严格长度校验的兼容及验收方式。
- `LocalDirectory/RemoteDirectory`：需明确默认路径相对规则及现有显式路径优先级，不自动改变工作目录。

下一轮建议：STOP，先取得稳定测量环境或真实 CI/消费者证据，再复核对应外部门禁；图像及反射性能均保持失败记录，不自动复测或套用其他数值。FTP 等条目待具体契约和隔离服务器，不跳过决策去实现未批准范围。

## 本轮文件

- 更新 `src/Bing.Utils.Net/Bing/Net/FTP/FtpClient.cs`：构造时应用三个明确配置项。
- 更新 `src/Bing.Utils.Net/Bing/Net/FTP/FtpClientConfig.cs`：说明连接通道、读取超时和模式语义。
- 新增 `tests/Bing.Utils.Net.Tests/FtpClientConfigurationTest.cs`：默认配置及自定义主/被动模式的无网络映射回归。
- 更新 `docs/modules/Bing.Utils.Net.md`：字段映射、默认值生效影响及未完成限制。
- 更新 `docs/00-documentation-audit.md`：同步已生效和仍待确认字段。
- 更新 `docs/quality/todo-execution.md`：部分完成状态、证据与具体依赖。

本轮没有独立于 TODO 的配置清理，也没有 CI、基线或构建配置变更。
