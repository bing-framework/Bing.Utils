# OPC 合法输入兼容性修复

用户授权：继续修复上一轮审查确认的问题。范围为两个兼容性缺陷及独立样本、ZIP 数据描述符测试；可选格式扩展和异步释放增强不在本次缺陷修复范围。

## Findings 与验收

- OPC-SPACE-01（OPEN_ACTIONABLE）：路径中的合法 %20 被当作控制字符拒绝。允许编码后的空格，继续拒绝原始空白、编码后的路径分隔符、控制字符、损坏转义及 unreserved 别名。包含 Override 与 Default 的有效包应识别 DOCX。
- OPC-EXT-02（OPEN_ACTIONABLE）：Default Extension 无条件禁止百分号与冒号。按单个 URI 扩展名校验合法字符和转义，保留禁止目录/点号、编码分隔符等边界。主部件或无关附加部件使用 a:b、a%2Bb 均不得误拒整个包。
- ZIP-DD-03（OPEN_ACTIONABLE，测试覆盖）：用不可定位输出流生成带 bit 3 / 数据描述符的 ZIP，确认真实覆盖该分支，并验证 Detect / DetectAsync / ProbeBufferedAsync 正常识别、内容保持和本地头不一致拒绝。

## 实施顺序

1. 使用本机已有 System.IO.Packaging 8.0.1 独立生成小型包夹具，记录生产方式；测试运行仅读取夹具，不新增包依赖或项目配置。补充必要合法/非法边界并先复现失败。
2. 最小修复 FileContentOfficePackage 中的路径和扩展名校验；主代理拥有生产代码、兼容性测试与夹具，luna_worker 仅负责独立的数据描述符测试文件。
3. 文件检测专项验证，再完成主测试项目 net8.0 全量以及其他生产 TFM 对应的运行时专项；独立 reviewer 确认 Findings 状态。最多三轮，只处理 OPEN_ACTIONABLE。

## Change Impact Analysis

ChangedFiles: FileContentOfficePackage.cs；FileContentDetectionReviewRegressionTests.cs 的必要边界；新增 OPC 兼容性和数据描述符测试、Resources/FileContentDetection 夹具、使用指南及本任务记录。
ChangedProjects: Bing.Utils / Bing.Utils.Tests。
ChangedPublicContracts: 无新增 API；修正既有合法输入兼容性。
ChangedRuntimePaths: OPC 路径与 Default 扩展名校验；ZIP 数据描述符路径仅补测试。
ChangedProviders: 无。ChangedTFMs: 无。ChangedBuildPackaging: 无，使用现有 Resources 自动复制规则。
ChangedBenchmarkHarness: 无。ChangedDocsOnly: 否。
AffectedDependents: 文件识别各入口及显式缓存协作。
RiskLevel: MEDIUM（严格验证必须同时保留非法路径拦截）。

按 test-project-boundary 的主断言归属规则，测试放在 Bing.Utils.Tests/Bing/IO，无测试迁移。先运行新增兼容性与数据描述符用例，再运行全部 FileContentDetection；阶段收口运行主项目全量。无其他模块、依赖、版本或 Git 提交操作。

当前宿主使用手工阶段，无 UAW 运行时 task/stageRunId/inputRevision，不伪造 StageResult。独立审查结论保留原文，不重写此前任务的 review.md。

执行状态：全部完成；独立复核与最终测试记录分别见 review.md 和 execution.md。
