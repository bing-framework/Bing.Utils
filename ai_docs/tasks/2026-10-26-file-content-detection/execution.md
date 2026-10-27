# 文件内容签名识别执行记录

Task: 2026-10-26-file-content-detection
Round: 1（实现、测试集成、独立审查、验证收口）
Implementation: 4/4 (100%)
Validation Gates: 5/5 完成（完整套件首次失败及局部复测见下文）
IMPLEMENTATION_STATUS: PASS
TEST_STATUS: PASS（新增能力，四个兼容环境）
FULL_SUITE_FIRST_RUN: 7373 passed / 5 failed
FAILED_CASES_ISOLATED_RECHECK: 5 passed / 0 failed
RELEASE_STATUS: NOT_APPLICABLE（本任务未请求发布）
Provider/Model: 当前 Codex 主代理；测试与独立只读审查委派 luna_worker。

## 已完成

- P1：15 类二进制格式及变体、偏移/掩码规则、不可变结果、候选合并、扩展名三态比较。
- P2：字节/Span、路径、可定位流同步/异步检测；Probe 同步/异步前缀重放和资源所有权。
- P3：51 项精准用例，覆盖 15 类格式及变体、完整小样本、短读/取消/位置/释放/预算和内部冲突；按主责模块放入 Bing.Utils.Tests。
- P4：中文 XML 注释、使用指南、主包文档入口、四框架生产构建、四环境精准测试、完整主包回归及独立审查。

## 验证证据

| Gate | 命令或依据 | 结果 |
|---|---|---|
| G1 / L0 | dotnet build src/Bing.Utils/Bing.Utils.csproj --no-restore -v minimal | net8.0/net7.0/net6.0/netstandard2.0 全部成功；0 错误、70 条旧源码警告 |
| G2 / L1 | dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --filter FullyQualifiedName~FileContentDetection --no-restore | 51/51 |
| G3 / 兼容性 | 同命令分别使用 net6.0、net7.0、netcoreapp3.1 | 各 51/51；netcoreapp3.1 消费主包 netstandard2.0 产物 |
| G4 / L2 回归 | dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --no-build --no-restore | 首次 7373 通过、5 失败，总数 7378；失败项停止并行构建后单独复测 5/5 通过 |
| G5 / 审查与静态检查 | 独立只读审查、git diff --check、新增文件 UTF-8/尾随空白检查 | 通过；OPEN_ACTIONABLE=0 |

精准测试包含 byte[]/Span、Detect/DetectAsync、DetectFile/DetectFileAsync、Probe/ProbeAsync、前缀流 byte[]/Span/Memory 重载。PNG 样本按块结构与 zlib/CRC32 生成，GIF 手工编码有效 LZW 清除/像素/结束码；ZIP/GZip 运行时由 .NET 生成。没有调用外部服务或安装依赖。

首次测试集成修正了测试文件的 using 和继承计数器 setter；替换了原 PNG 的错误块校验值。这些问题均已 CLOSED。生产代码自首次四框架构建通过后没有再改动。

### 完整套件首次失败的局部确认

首次完整套件与其他 TFM 的编译并行运行，以下 5 个未修改的测试失败；这些用例含启动时限或性能断言。随后停止并行编译、使用 --no-build，按测试名筛选同一批用例单独复测，全部通过。该证据支持失败与运行负载有关，但不把局部复测写成一次全新完整套件通过。

- CommandLineExecutorRegressionTest.Execute_WithoutOutputCondition_WaitsForSuccessfulExit
- CommandLineExecutorRegressionTest.Execute_WhenExitedParentLeavesInheritedPipes_DoesNotWaitForChild（false / true 两项）
- CommandLineExecutorRegressionTest.Execute_WhenTimedOut_StopsTheStartedProcess
- MapperHelperTest.Map_LargeNumberOfProperties_ShouldPerformWell

复测命令：

```powershell
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --no-build --no-restore --filter 'FullyQualifiedName~Execute_WithoutOutputCondition_WaitsForSuccessfulExit|FullyQualifiedName~Execute_WhenExitedParentLeavesInheritedPipes_DoesNotWaitForChild|FullyQualifiedName~Execute_WhenTimedOut_StopsTheStartedProcess|FullyQualifiedName~Map_LargeNumberOfProperties_ShouldPerformWell' --logger 'console;verbosity=normal'
```

本地日志（.log 由仓库忽略）：targeted-net8.log、targeted-net6.0.log、targeted-net7.0.log、targeted-netcoreapp3.1.log、full-net8.log、existing-failures-recheck.log。没有为了复测修改旧代码或放宽断言。

## 影响分析与证据复用

ChangedFiles: 五个新增生产文件、三份新增测试、指南/主包文档和任务报告。
ChangedProjects: Bing.Utils、Bing.Utils.Tests。
ChangedPublicContracts: 新增独立 API；既有签名与行为不变。
ChangedRuntimePaths: 签名匹配、有限前缀读取、前缀重放；无旧调用点改接。
ChangedProviders/TFMs/BuildPackaging/BenchmarkHarness: 无。
ChangedDocsOnly: 否。
AffectedDependents: 主包测试所引用的 Http、Drawing、Collections 等消费者已随测试构建成功。
RiskLevel: MEDIUM（流位置、短读、取消、所有权），均有精准用例。

L3: 文件路径和重放流消费由公开入口测试覆盖；主包测试所引用的消费者构建通过。
L4: 按计划以主包完整套件收口，不运行全仓无关模块或外部集成。
L5: NOT_APPLICABLE，无旧等价 API，不声称性能提升；使用无限源和小预算用例确认逻辑读取有界。
Reused Evidence: 生产代码未再修改，复用四 TFM 构建和独立审查；随后仅报告修订不重跑代码测试。

## 最终分类与停止

Completed: P1/P2/P3/P4。
Open Actionable: none。
Blocked Approval: none。
Blocked External: none。
Not Applicable: 历史 before/after Benchmark、发布和外部 CI。
Accepted Limitations: 签名不证明完整性或安全；固定内置目录；Probe 中途失败可能已消费源；完整套件首次有负载相关失败、局部复测通过。
Verified Boundaries: 检测器向源流请求的头部字节最多为 min(MaxHeaderBytes, 12)，可提前停止；不包含底层流内部缓冲/预读；预算处不会额外探测 EOF。
Deferred: Office/ZIP 容器细分、文本探测，属于第二批，不影响当前支持清单。
No-Progress Check: CHANGED（已完成）。
Next Action: STOP。
Goal Status: COMPLETED（任务记录，未创建 Goal 自动化）。

未暂存、提交、发布或修改依赖/版本。
