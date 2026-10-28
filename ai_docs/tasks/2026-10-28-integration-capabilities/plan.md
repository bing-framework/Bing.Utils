# 集成能力修复计划

## 范围

处理 `Bing.Utils.Sftp`、`Bing.Utils.Templates.Scriban`、`Bing.Utils.Text.EncodingDetection` 三个新扩展包、专属测试、组合示例和必要文档登记。保持三个包的 `netstandard2.0;net8.0` 目标框架及已批准依赖版本；不修改发行版本、旧 `TemplateRenderer` 或无关生产代码。

测试按模块专属断言归入 `tests/Bing.Utils.<Module>.Tests`；需要真实 SSH.NET/OpenSSH 协作的测试归入 `tests/Bing.Utils.Sftp.Tests.Integration`。

## 变更影响分析

| 项目 | 评估 |
| --- | --- |
| ChangedFiles | 三个新包源码、对应测试、组合样例、能力文档与登记、当前执行证据 |
| ChangedProjects | 三个新包、四个专属测试项目、集成样例项目 |
| ChangedPublicContracts | 是；补全安全传输、模板限制、编码策略 API，并移除破坏性 overwrite 选项 |
| ChangedRuntimePaths | SFTP 连接/复制/发布，Scriban 编译/渲染，编码检测/流式转码 |
| ChangedProviders | SSH.NET、Scriban、UTF.Unknown |
| ChangedTFMs | 否；保持 netstandard2.0;net8.0 |
| ChangedBuildPackaging | 加入组合样例构建；包依赖版本不变，执行定向 pack |
| ChangedBenchmarkHarness | 否 |
| ChangedDocsOnly | 否 |
| AffectedDependents | 新组合样例引用三包及现有 CsvStream/TabularMap；不改既有依赖方 |
| RiskLevel | HIGH |

## Finding 验收映射

| ID | 验收条件 |
| --- | --- |
| F1 | SHA256 主机指纹支持前缀及有/无填充；错误指纹拒绝，单测覆盖 |
| F2 | 使用 SSH.NET 异步可取消连接、异步远端流复制和异步 rename；不以 Task.Run 包装同步网络 I/O；OCE 保持取消语义 |
| F3 | 临时文件/流/客户端清理失败不覆盖首错；已提交结果保持已提交；rename 请求后连接异常标记结果不确定 |
| F4 | 每次传输事件序号从 0 严格递增；读写阶段结束和发布提交分别准确报告 |
| F5 | 文档区分流下载的部分输出与文件下载的同目录暂存发布 |
| F6 | 编译异常提供模板名、1-based 行列及 Scriban 原始诊断；覆盖多行位置与 UTF-16 列 |
| F7 | 支持泛型/只读字典、中文键、嵌套模型；仅暴露数据，不暴露宿主方法 |
| F8 | 编译/渲染统一异常类型、稳定码、中文说明、模板名/位置及 InnerException；缺失变量有独立码 |
| F9 | 令牌传入独立 TemplateContext 和异步渲染；ScriptAbortException 转为 OperationCanceledException；循环中途可取消 |
| F10 | UTF-32 LE/BE BOM 优先于 UTF-16；HasBom、置信度 1 和来源证据正确 |
| F11 | 检测/回放/转码有界并传递取消；增量 Decoder/Encoder 跨块；leaveOpen 清理准确；阻塞流回归覆盖 |
| I1 | SFTP 密码/私钥及必需主机指纹策略、目录项/文件信息、流与文件传输均有实现 |
| I2 | 上传使用同目录唯一暂存后 rename，默认拒绝已存在目标；不提供先删旧目标的覆盖选项 |
| I3 | 文件下载使用本地同目录临时文件发布；流下载明确允许失败时出现部分输出 |
| I4 | 新增可编译的 SFTP 下载 -> UTF-8 转码 -> CsvStream/TabularMap -> Scriban 报告组合示例并登记 |
| I5 | 按实际风险补充单测；集成夹具可通过环境变量运行，覆盖 host key/认证/传输/冲突/取消/清理/rename |

## 验证矩阵

- L0：三个包 netstandard2.0 与 net8.0 构建、示例构建、`git diff --check`。
- L1/L2：新增包的定向回归测试与完整专属单元测试。
- L3：环境驱动 OpenSSH 集成测试；当前先检查本地服务和容器运行时是否可用。
- Release：三个包分别定向 `dotnet pack`，不运行全仓库测试、不升级依赖、不改版本。

## Finding 分类

固定的初始 Finding 为 F1–F11，全部按 `OPEN_ACTIONABLE` 开始。环境缺少真实 SSH 服务时，仅把真实集成执行归为 `BLOCKED_EXTERNAL`；可在工作区内完成的代码和测试不得因此降级。
