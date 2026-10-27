# 文件内容签名识别：路线 A 第一批

状态：已按计划实现并完成验证；用户已授权先计划再实现。2026-10-26。

## 目标与边界

在 Bing.Utils / Bing.IO 新增轻量、无新增识别依赖的文件头检测能力。支持 JPEG、PNG、GIF、BMP、TIFF、ICO、WebP、PDF、ZIP、GZip、7z、RAR、WAV、AVI、FLAC 共 15 类格式（RAR 4/5、GIF 87a/89a、TIFF 大小端等作为规则变体）。只确认签名，不保证结构完整或安全。Office、文本识别、其他容器解析推迟到第二批，不修改已有 FileHelper.GetContentType / IsImageFile 行为。

## 设计决策

- 静态 FileContentDetector：Detect(byte[] / ReadOnlySpan<byte>)、Detect(Stream)、DetectAsync(Stream)、DetectFile / DetectFileAsync、Probe / ProbeAsync。
- FileDetectionOptions.MaxHeaderBytes 默认 65536，必须正数；实际仅按内置规则所需前缀读取，预算不自动增大。规则集合内部不可变，不引入公开插件框架。
- FileContentFormat 包含稳定 Id、MimeType、Extensions、IsContainer；结果包含 Status、Format、Candidates、ContainerFormat、Evidence、ReasonCode、BytesRead。
- Status 为 Matched / Ambiguous / Unknown / Empty / Incomplete；候选是完整命中的格式，尚有未排除的不完整规则时返回 Incomplete。部分签名遇 EOF 为 Truncated，预算耗尽为 HeaderLimitReached。无匹配为 NoSignatureMatch。
- 多变体同格式去重；不同格式命中返回 Ambiguous，不按注册顺序选胜者。通过内部规则测试验证冲突路径。
- 所有输入均把当前位置作为内容起点。Detect(Stream) 要求可定位并在 finally 恢复原位置，始终保留源。非定位源必须使用 Probe。
- Probe 成功消费前缀并返回 FileContentProbe（IDisposable）：Result + Content。Content 为只读、不可定位的前缀重放流；不支持并发读写。释放 probe 等价于释放 Content。leaveOpen 默认 true；false 在参数验证和初始取消检查后接管源，包括中途失败。中途失败或取消不保证 Probe 的源位置恢复。
- 同步与异步入口分别调用同步/异步 IO，循环处理短读；异步令牌传至源；初始取消不消费/释放源，已开始后按所有权清理。
- CompareExtension(result, extension) 接受 jpg 或 .jpg、忽略大小写；唯一确定且已知的内置扩展名进行一致/不一致判断；未知声明、未知/歧义/未完成结果返回 Indeterminate。ZIP 与 DOCX 等未支持子类型返回 Indeterminate。
- XML 中文注释说明默认值、限制、资源所有权和错误。无百分比置信度、IsSafe、完整格式校验或自动解压。

## TODO / 责任

- [x] P1：主代理实现不可变结果模型、签名目录、片段/偏移/掩码匹配、扩展名比较。
- [x] P2：主代理实现流入口、文件入口、前缀重放与取消/资源生命周期。
- [x] P3：luna_worker 编写主包测试（只拥有新增 FileContentDetectionTests.cs）；主要断言目标属于 Bing.Utils，按 test-project-boundary 放入 Bing.Utils.Tests/Bing/IO。主代理集成并补充必要用例。
- [x] P4：主代理完成参考指南/目录链接、构建和测试；独立代理只读审查后仅修复 OPEN_ACTIONABLE。

## 验收和验证命令

1. 覆盖所有格式及变体、固定偏移反例、随机未知/空/截断、预算、歧义与同格式合并、扩展名别名及容器不确定。
2. 覆盖可定位流非零起点、成功/异常/取消后的恢复；不可定位短读、前缀重放一致、零长度读、取消、释放及 leaveOpen；路径入口释放文件；大/无穷源实际读取有界。
3. 至少用真实完整 PNG/GIF 和运行时生成 ZIP/GZip 通过公开入口检验；其余为独立格式规范签名样本，记录来源与识别边界。
4. L0: dotnet build src/Bing.Utils/Bing.Utils.csproj --no-restore（全部生产 TFM）；git diff --check；UTF-8 检查。
5. L1: dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --filter FullyQualifiedName~FileContentDetection --no-restore。再在 net6.0 / net7.0 / netcoreapp3.1（实际 netstandard2.0 产物）运行同组测试。
6. L2 / 收口 Full Suite: dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0 --no-restore，主包对应完整测试项目，同时编译其 Http/Drawing 等消费者。没有改动其他模块运行路径，不运行全仓所有模块及外部集成套件。
7. 新能力没有同构旧 API，性能 before 为 NOT_APPLICABLE；以确定性读字节上限测试验证资源契约，不新增 Benchmark 项目或自定性能门槛。

## 影响分析（测试前更新）

ChangedFiles: 新增 Bing/IO/FileContent*、FileDetection*、内部规则/重放流、新增测试、指南和计划。
ChangedProjects: Bing.Utils、Bing.Utils.Tests。
ChangedPublicContracts: 新增独立 API；既有签名与行为不变。
ChangedRuntimePaths: 新增文件头读取、匹配、前缀重放；无既有调用点改接。
ChangedProviders: 无。ChangedTFMs: 无。ChangedBuildPackaging: 无。ChangedBenchmarkHarness: 无。
ChangedDocsOnly: 否。AffectedDependents: 测试项目引用的 Http / Drawing 等消费者进行编译验证。
RiskLevel: MEDIUM（流位置、资源归属、短读和取消）。

## 执行及停止规则

主代理负责设计/实现/集成，luna_worker 负责边界清晰测试工作；最后独立只读审查。使用 GOAL_RULES：最多三轮 Review/Fix，仅 OPEN_ACTIONABLE 驱动修复，证据范围未变时复用。不提交、不暂存、不发布、不变更版本或依赖。执行报告记录当前状态、测试证据、未完成及后续范围。

工作流采用当前宿主手工阶段：resolver 未提供 stageRunId/inputRevision，且外部 codex CLI 不可用；不启动外部 UAW 执行，也不构造伪造的运行时 StageResult。用户本轮已同时授权计划及实现，因此计划落盘后连续进入实现。
