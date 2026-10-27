# 文件内容检测审查修复执行结果

Task: 2026-10-26-file-content-review-fixes
Round: 2（首轮实现与独立审查；次轮关闭新发现的名称等价及快速路径边界）
Implementation: 6/6
Required local gates: 7/7
Implementation status: PASS
Test status: PASS
Goal status: COMPLETED
Open actionable: 0
Next action: STOP

## 已完成

1. Office 主部件增加真实本地记录验证：签名、标志、方法、名称、尺寸一致性和物理范围；清单/关系部件不能冒充主文档。
2. PartName 和 Target 分开校验；合法路径冒号、相对中间点段和 ASCII 大小写继续支持；非法转义、目录目标、越根路径、伪造扩展名被拒绝。路径等价只折叠 ASCII，防止不同 Unicode 名称被合并。
3. ZIP 尾目录先读 22 字节，必要时扩展注释窗口；无效候选继续搜索，选择最近的完整有效目录。ZIP64 locator 的上下文在回退时重新核验。全部候选的源读取共用累计预算。
4. FileDetectionResult.LimitKind 细分 HeaderBytes、ContainerBytesRead、ZipDirectoryBytes、ZipEntries、MetadataBytes；既有原因与状态枚举值不变。
5. ProbeBuffered / ProbeBufferedAsync、FileContentBuffer 组合有界完整缓存与默认深检测。必填总长度上限，复用 TemporaryFileStore / SpilloverStream / StreamTransfer；缓存、源、取消和异常路径拥有明确的清理契约。
6. CompareFileName、FileContentFormats 共享实例与 SupportedFormats 只读目录；更新使用指南和模块入口。主代理完成解析器/诊断/目录/集成，luna_worker 完成独立缓存入口及 13 个生命周期测试。

## Change Impact Analysis

ChangedFiles: FileContent* / FileDetection* 生产文件，两个新增 FileContentDetection 测试文件，使用指南、模块索引及本任务记录。
ChangedProjects: Bing.Utils；Bing.Utils.Tests。
ChangedPublicContracts: 仅增量 API、结果预算属性、共享格式目录；已有入口默认浅检测和 Probe 重放契约保留。
ChangedRuntimePaths: ZIP32 目录定位、OPC 识别、显式有界缓存。
ChangedProviders: 无。
ChangedTFMs: 无；验证 net8.0、net7.0、net6.0、netstandard2.0。
ChangedBuildPackaging: 无。
ChangedBenchmarkHarness: 无。
ChangedDocsOnly: 否。
AffectedDependents: 主测试项目中的真实公开 API 使用及缓存/临时存储协作。
RiskLevel: MEDIUM。

新增测试的主要断言目标属于主包，按 `.agents/skills/test-project-boundary/refs/test-project-boundary.md` 放入 Bing.Utils.Tests/Bing/IO；没有测试迁移、重复项目或依赖变更。

## 最终验证证据

| 门禁 | 结果 | 日志 |
| --- | --- | --- |
| net8.0 文件检测 | 148/148 | test-net8-final-targeted.log |
| net6.0 文件检测 | 148/148 | test-net6.0-final-targeted.log |
| net7.0 文件检测 | 148/148 | test-net7.0-final-targeted.log |
| netcoreapp3.1 文件检测 | 148/148 | test-netcoreapp3.1-final-targeted.log |
| net8.0 主测试项目全量 | 7475/7475，33 秒 | test-net8-full.log |
| 构建与静态检查 | 四个生产 TFM 编译成功；UTF-8 / 尾随空白 / git diff --check 通过 | 各 final-targeted 日志及静态检查输出 |
| 独立只读复核 | 无 OPEN_ACTIONABLE | review.md |

生产 TFM 的构建由以上测试命令完成，日志记录 output/debug/net8.0、net6.0、net7.0、netstandard2.0/Bing.Utils.dll。netstandard-consumer.log 记录旧运行时实际加载的 TargetFramework 为 `.NETStandard,Version=v2.0`。存在仓库既有编译警告，本次 FileContent/FileDetection 范围没有新增编译警告。

新增 64 个测试用例（原 84，现 148），涵盖恶意结构、路径兼容性、每项预算、字节/同步/异步入口一致性、取消/失败后的临时文件清理和 API 易用性。

计数流验证 512 KiB 主部件样本的实际读取小于 4 KiB，且读取计数等于 BytesRead + ContainerBytesRead；原实现对这种无注释大 ZIP 固定先读取 65557 字节尾窗口。这里只报告源读取量证据，不宣称耗时或内存基准收益。

L0: PASS，生产四 TFM 随运行时验证构建，静态/UTF-8 检查通过。
L1: PASS，四运行时各 148 项文件检测测试。
L2: PASS，Bing.Utils.Tests net8.0 全量 7475 项。
L3: PASS，有界缓存与 TemporaryFileStore / SpilloverStream 协作、旧运行时使用 netstandard2.0 产物。
L4: 不运行无关子包全 Solution；本次公开 API 收口由受影响主测试项目全量覆盖。
L5: NOT_APPLICABLE，无正式重基准需求；仅运行受影响的计数流 IO 断言。

首轮 test-net8-targeted、test-net6-targeted、test-net7-targeted、test-core31-targeted 的 142 项日志保留为过程证据，不代表最后候选；最终采用上表明确列出的 final-targeted / full 日志。没有把不同代码版本的结果拼成最终 PASS。

## 状态分类

Completed: 6 项计划实现及审查追加问题均关闭，全部本地门禁通过。
Open Actionable: 无。
Blocked Approval: 无。
Blocked External: 无，本任务未要求外部发布或 CI 门禁。
Not Applicable: 重型 Benchmark、其他模块/全 Solution 回归、发布验证。
Accepted Limitations: ZIP32 单卷；仅识别所需部件 Store/Deflate；不解压或验证主正文、CRC、宏行为与完整 OPC；多个有效目录最近者优先；调用方负责配置并拥有临时存储。
Verified Boundaries: 各类预算停止原因明确；缓存超限最多一个额外读取字节，失败/取消清理缓存；无注释大正文不触发完整尾窗口读取。
Deferred: 无新增延期项。
No-Progress Check: CHANGED，第二轮有明确修复和新增反例，随后停止。

下一步无需继续自动修复。新 API 及示例见 docs/reference/file-content-detection-guide.md。
