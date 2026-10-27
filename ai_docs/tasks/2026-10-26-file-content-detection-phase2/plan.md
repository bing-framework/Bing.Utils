# 文件内容识别：路线 A 第二批

状态：已完成计划内实现、专项验证及独立审查；完整回归有既有计时/命令测试失败，详见 execution.md。2026-10-26。

## 目标与设计

- 在第一批独立 API 上增加显式 `FileDetectionOptions.InspectContainers`，默认 false，保留原有文件头检测语义。
- 识别 ZIP 中 DOCX/XLSX/PPTX/DOCM/XLSM/PPTM；结合 `[Content_Types].xml`、包级 `_rels/.rels` 的 officeDocument 关系和实际存在的主部件，支持自定义主部件路径、Strict/Transitional 关系。宏类型按主部件 ContentType 判断，不宣称实际含宏或安全。
- 先有界读取尾部与中央目录，再只解压两个识别用 XML；不解压正文、不提取到磁盘。不通过 ZipArchive.Entries 先加载无限目录。采用小型 ZIP 目录读取器与 BCL DeflateStream，无新依赖。
- 默认预算：MaxContainerBytesRead=4 MiB（额外读取，含重复范围）、MaxZipDirectoryBytes=1 MiB、MaxZipEntries=4096、MaxMetadataBytes=1 MiB（两个 XML 解压累计）。预算均为正整数并在开始时快照；读取和分配前检查，XML 禁止 DTD/外部解析。
- ZIP32 单卷、Store/Deflate 元数据为本批范围；ZIP64、分卷、加密元数据及其他压缩方法明确返回 UnsupportedContainer。旧 Office/加密 Office 的 OLE 容器、EPUB/APK、文本识别留待后续。
- 成功子类型保留 ContainerFormat=zip，Evidence=ContainerStructure。损坏、预算耗尽、无法定位分别返回 Incomplete + InvalidContainer/ContainerLimitReached/SeekableSourceRequired，保留 ZIP 签名候选与容器证据。源 IO 错误、取消继续抛出。
- BytesRead 保持文件头前缀计数；新增 ContainerBytesRead 记录实际额外读取。字节数组无整份复制；Span 同步路径按片段检查，不复制整个正文。
- Detect 流入口恢复位置。Probe 可定位源检查容器后恢复至前缀之后，Content 继续完整重放；不可定位 ZIP 返回未完成并保留可重放正文，由调用方显式使用已有 SpilloverStream 有界转存。
- 扩展名比较保留浅检测 ZIP + Office 为 Indeterminate；结构识别后支持上述六种 Office 扩展名及不一致判断。

## TODO / 分工

- [x] P1 主代理：选项/结果模型、有限 ZIP 结构读取与 Office 元数据解析。
- [x] P2 主代理：数组、Span、同步/异步流、文件与 Probe 集成，保证资源契约。
- [x] P3 luna_worker：仅新增 FileContentDetectionContainerTests.cs，以生成的真实 ZIP 验证六种格式、非标准路径、预算、损坏、异步/重放和扩展名行为。
- [x] P4 主代理：补充所需边界、更新使用指南、执行测试；独立只读审查和按证据修复。

## 验收与验证

测试归属依照 `.agents/skills/test-project-boundary/refs/test-project-boundary.md`：主要断言 Bing.Utils/Bing.IO，新增到 Bing.Utils.Tests/Bing/IO；无测试迁移、无跨模块改造。

1. L0：`dotnet build src/Bing.Utils/Bing.Utils.csproj --no-restore`，覆盖全部生产 TFM；UTF-8/空白检查。
2. L1：`dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --filter FullyQualifiedName~FileContentDetection --no-restore`；net6.0/net7.0/netcoreapp3.1 同组，后者验证实际 netstandard2.0 产物。
3. 用真实生成 ZIP 检验普通/空 ZIP、六种 Office、Strict 关系、自定义主部件、错误命名空间/DTD/重复项/丢失主部件、ZIP64/加密/损坏、精确预算、超量解压、非零流起点、同步/异步、非定位 Probe、取消与异常恢复。
4. 收口 L2：主包完整 net8.0 套件；避免并行构建影响既有计时测试。其他模块只验证引用编译，不运行不相关全仓套件。
5. 性能 before 为 NOT_APPLICABLE；本次新增解析路径以确定性的字节/条目/解压预算测试作为资源证据，不设未经批准的 benchmark 门槛。

## Change Impact Analysis（测试前）

ChangedProjects: Bing.Utils / Bing.Utils.Tests。
ChangedFiles: FileContentDetector、FileDetectionOptions/Result、新增 ZIP Inspector 与容器测试、参考指南/任务记录。
ChangedPublicContracts: 新增可选检测模式、预算、原因/证据枚举值、容器读取计数；既有默认行为保留。
ChangedRuntimePaths: 仅显式启用的新 ZIP 元数据读取/解压/XML 解析及其流适配；既有 FileHelper 调用不改接。
ChangedProviders/TFMs/BuildPackaging/BenchmarkHarness: 无。ChangedDocsOnly: 否。
AffectedDependents: 主测试项目引用消费者编译。RiskLevel: MEDIUM（不可信 ZIP/XML、资源预算和流位置）。

## 执行规则

遵循 GOAL_RULES：仅 OPEN_ACTIONABLE 驱动修复，最多三轮，复用未失效证据。不暂存/提交/发布，不升级依赖。使用当前宿主手工计划与执行阶段，无运行时 stageRunId/inputRevision，不伪造 StageResult。已存在用户授权，计划后直接进入实现。
