# 集成能力修复执行记录

## 当前状态

- Task：2026-10-28-integration-capabilities
- Round：2（独立复审后，仅修 F10 与 I4）
- Candidate：当前工作区 `feature/dev`；初始改动包含已批准的三个新包及其登记，未提交变更保留。
- Implementation：Round 2 定向修复已完成；独立复审确认 14/16 项 CLOSED，F10/I4 原为 OPEN_ACTIONABLE，本轮仅修复这两项。
- Release Gates：三个扩展包双目标框架 pack 均通过；SFTP 外部服务运行仍被环境阻塞。
- 本机环境：.NET SDK 8.0.425；SSH.NET 2026.0.0、Scriban 7.5.3、UTF.Unknown 2.5.0 已缓存。`ssh-keygen` 可用；Windows OpenSSH Server 未安装；Docker CLI 因 Docker Desktop Linux engine 未运行而不可用。

## 初始证据

- Git 初始状态：三个扩展包和对应测试为未跟踪文件；解决方案、发布脚本和 capability/module/API 文档已有未提交修改。执行中保留这些改动。
- 测试归属：技能指定的 `refs/test-project-boundary.md` 已读取；模块缺陷测试跟随模块，真实 OpenSSH 测试放 `.Tests.Integration`。
- 固定依赖核对：SFTP `SSH.NET 2026.0.0`；模板 `Scriban 7.5.3`；检测 `UTF.Unknown 2.5.0`。不调整这些版本或其他依赖。
- OpenSSH 探测：`sshd` 服务和 `sshd.exe` 均不存在；Docker CLI 可用但 Docker daemon 连接失败。环境驱动集成夹具和启用命令已补齐，当前无法运行真实服务器测试。

## Round 2 影响分析

| 项目 | 评估 |
| --- | --- |
| ChangedFiles | `EncodingDetector.cs`、编码检测专属测试、编码包 README/模块说明、`IntegrationCapabilities/Program.cs`、本执行记录 |
| ChangedProjects | EncodingDetection 包与测试、IntegrationCapabilities 样例 |
| ChangedPublicContracts | 是；所有编码检测选项入口拒绝小于 4 字节的 `MaxSampleBytes`，以保证可完整判别最长 BOM |
| ChangedRuntimePaths | 编码参数校验、UTF-32 BOM 检测/回放/转码；样例本地 TSV 解析和报告渲染 |
| ChangedProviders | UTF.Unknown；消费现有 Scriban 与 CsvStream/TabularMap API，不改其实现 |
| ChangedTFMs | 否；包仍为 `netstandard2.0;net8.0`，样例仍为 net8.0 |
| ChangedBuildPackaging | 无项目图或依赖变化；刷新 EncodingDetection 定向构建/pack |
| ChangedBenchmarkHarness | 否 |
| ChangedDocsOnly | 否 |
| AffectedDependents | EncodingDetection.Tests、组合样例；不触及 SFTP 或 Scriban 生产代码 |
| RiskLevel | MEDIUM |

- 仅处理 F10/I4；其余 14 项继续复用复审确认的 CLOSED 状态，B1 保持 BLOCKED_EXTERNAL。
- F10 选择统一拒绝 `< 4` 的预算；所有入口共用 `Snapshot()`，测试覆盖 byte[]、file、seek/nonseek stream、sync/async detect/replay 和转码，且验证拒绝前输入未消费。
- I4 使用样例的 `--self-test` 本地入口验证非空 TSV 到映射 DTO、同一报告模板及输出断言；测试不需要 SFTP 环境或新增项目。

## Change Impact Analysis

详见 [plan.md](plan.md) 中的变更影响表与验证矩阵。当前风险为 HIGH；只执行受影响的新包、测试项目、组合样例及 pack 验证，不运行全仓库测试。

## Round 1 逐项验收证据

| ID | 状态 | 验收证据 |
| --- | --- | --- |
| F1 | CLOSED | `SftpClient.cs` 统一规范 `SHA256:` 与末尾填充；`SftpConnectionOptionsTest.MatchesHostKey_WithPrefixAndOptionalPadding_ShouldAccept` 覆盖四种前缀/填充组合，错误指纹用例拒绝。 |
| F2 | CLOSED；真实传输验证 BLOCKED_EXTERNAL | 连接、远端流、复制和 rename 使用 SSH.NET 异步 API 并传入 token；OCE 保持取消语义。`Upload_WhenCanceledDuringCopy_ShouldStopAndCleanTemporaryFile` 和文件下载取消集成用例已编译，尚未连接 OpenSSH 执行。 |
| F3 | CLOSED；远端清理路径验证 BLOCKED_EXTERNAL | 首错保留并将清理异常加入 `Exception.Data`；`AttachCleanupError_ShouldPreservePrimaryException` 验证异常身份和附加错误。集成夹具包含远端冲突、rename 竞争与上传取消清理，尚未连接服务器执行。 |
| F4 | CLOSED；进度运行验证 BLOCKED_EXTERNAL | `SequencedProgress` 为每次通知递增赋序号；上传/文件下载在发布后报告 Commit。`AssertProgressSequence` 断言 Read/Write 最终事件及 Commit 顺序；该 SSH 集成用例已编译但跳过。 |
| F5 | CLOSED | `SftpClient` XML remarks 与 `docs/modules/Bing.Utils.Sftp.md` 明确区分流下载可能部分输出和文件下载本地同目录暂存发布。 |
| F6 | CLOSED | 编译诊断在 `ScribanTemplateEngine.Compile` 读取原始跨度并换算 1-based 行列；`Compile_WithMultilineUnicodeError_ShouldReportOneBasedLocation` 覆盖多行和 UTF-16 列。 |
| F7 | CLOSED | `CreateScriptObject` 支持 `IDictionary<string,T>` / `IReadOnlyDictionary<string,T>`、中文键和嵌套模型；`RenderAsync_WithTypedAndNestedDictionaries_ShouldPreserveKeys` 与宿主方法拒绝用例通过。 |
| F8 | CLOSED | 编译/渲染使用 `ScribanTemplateException`、稳定码、中文诊断和 InnerException；缺失变量有专属错误码及位置，见 `RenderAsync_WhenVariableIsMissing_ShouldReturnStructuredError`。 |
| F9 | CLOSED | 独立 `TemplateContext` 和 `Template.RenderAsync(context)` 接收 token，ScriptAbortException 映射为 OCE；`RenderAsync_WhenCanceledDuringLongLoop_ShouldThrowOperationCanceled` 通过。 |
| F10 | CLOSED | `DetectBom` 先识别 UTF-32 LE/BE，再处理 UTF-8/UTF-16；UTF-32 测试验证 CodePage、HasBom、置信度 1 和 BOM 来源，文件异步检测测试覆盖有界采样。 |
| F11 | CLOSED | 增量 Decoder/Encoder 跨读写块运行，token 传入底层异步读写，leaveOpen=false 的清理错误不覆盖首错；阻塞读/写取消、替换/严格解码和字符块边界测试通过。 |

## Round 1 组合能力验收

| ID | 状态 | 验收证据 |
| --- | --- | --- |
| I1 | CLOSED | SFTP 提供密码/私钥、必需主机指纹、目录项/属性及流/文件接口；三个项目保持 `netstandard2.0;net8.0`，依赖固定为 SSH.NET 2026.0.0、Scriban 7.5.3、UTF.Unknown 2.5.0。 |
| I2 | CLOSED；远端执行 BLOCKED_EXTERNAL | 上传在同目录唯一远端暂存文件完成后 rename，上传 API 不提供 overwrite；已存在目标与 rename 竞争用例保留目标并清理暂存，集成运行待 OpenSSH 环境。 |
| I3 | CLOSED；远端执行 BLOCKED_EXTERNAL | 文件下载使用本地同目录临时文件并以不覆盖方式发布；集成用例覆盖成功下载、已有目标保留和取消清理。流下载部分输出语义见 XML 文档。 |
| I4 | CLOSED | `samples/IntegrationCapabilities` 编译验证通过；实现 SFTP 文件下载 -> 有界检测/增量 UTF-8 转码 -> `CsvStream`/`TabularMap<T>` -> Scriban 报告，并登记于模块/API 文档入口。 |
| I5 | CLOSED；真实服务器执行 BLOCKED_EXTERNAL | 环境驱动 OpenSSH 夹具包含 7 个事实：错误指纹/认证、往返传输/属性/目录、上传目标冲突、下载目标冲突、上传取消清理、文件下载取消清理、rename 竞争。README 给出 atmoz/sftp 启动和测试命令；本机因 sshd 与 Docker daemon 不可用而动态跳过 7 项。 |

## Round 1 验证结果

- `dotnet test tests/Bing.Utils.Sftp.Tests/Bing.Utils.Sftp.Tests.csproj -c Debug --no-restore`：8/8 通过。
- `dotnet test tests/Bing.Utils.Templates.Scriban.Tests/Bing.Utils.Templates.Scriban.Tests.csproj -c Debug --no-restore`：12/12 通过。
- `dotnet test tests/Bing.Utils.Text.EncodingDetection.Tests/Bing.Utils.Text.EncodingDetection.Tests.csproj -c Debug --no-restore`：13/13 通过。
- SFTP 集成项目构建成功；7 个服务依赖用例按环境变量动态跳过，状态为 `BLOCKED_EXTERNAL`，不是永久 Skip 占位。
- `dotnet build samples/IntegrationCapabilities/IntegrationCapabilities.csproj -c Debug --no-restore`：通过。
- 三个包分别以 Release 配置成功 `dotnet pack`，产物包含 `netstandard2.0` 和 `net8.0`，并打包各自 README；没有修改版本文件。
- `git diff --check` 通过；新增文本文件未发现行尾空白。未运行全仓库测试。
- 首次完整 pack 编译核心 `Bing.Utils` 时报告既有过时 API/XML 文档警告；三个扩展包没有编译错误。NuGet README 警告已通过包元数据和显式打包 README 修复。

## Round 1 复审前状态

- `OPEN_ACTIONABLE`：0。F1–F11 和 I1–I5 的代码/测试/文档工作已完成。
- `BLOCKED_EXTERNAL`：7 个真实 OpenSSH SFTP 集成事实无法在当前主机执行；运行命令见 `tests/Bing.Utils.Sftp.Tests.Integration/README.md`。
- 尚未进行独立复审；当前实现交回主代理 Codex gpt-6.1-sol/high 复核，不代表独立审查通过。

## Round 2 执行结果

### F10：采样预算与 UTF-32 BOM

- 契约选择：`MaxSampleBytes` 统一要求至少 4 字节；`EncodingDetectionOptions.Snapshot()` 在 byte[]、文件、流、回放和转码入口读取输入前校验。三个 `sampleBytes` XML 参数说明、包 README 和模块文档已同步。
- 回归覆盖：2、3 字节预算在 byte[]、同步/异步文件、可定位/不可定位流、同步/异步回放及转码入口抛 `ArgumentOutOfRangeException`；验证可定位流位置未移动、不可定位流读取次数为 0，转码目标长度为 0。
- 最小预算覆盖：预算 4 对 UTF-32LE/BE 输入确认编码 CodePage、`HasBom`、4 字节 BOM、置信度 1 和 BOM 来源；覆盖 byte[]、同步/异步文件、可定位/不可定位同步/异步检测、同步/异步回放与 UTF-8 转码。回放逐字节相同，转码结果恰为 UTF-8 `A` 且不含 NUL。

### I4：组合样例非空验证

- 修复 `samples/IntegrationCapabilities/Program.cs` 模板成员访问为 `row.Name`/`row.Amount`，保持引擎默认保留成员原名的契约。
- 新增 `--self-test` 本地入口，使用非空 TSV，调用样例共用的 `CsvStream.ReadAsync`、`TabularMap<ReportRow>` 映射函数和同一模板工厂，并断言输出包含 `Alice` 与 `12.5`；无需 SFTP 服务，不新增测试项目或依赖。
- 文档增加可执行入口：`dotnet run --project samples/IntegrationCapabilities/IntegrationCapabilities.csproj -- --self-test`。

### Round 2 验证命令与结果

- `dotnet test tests/Bing.Utils.Text.EncodingDetection.Tests/Bing.Utils.Text.EncodingDetection.Tests.csproj -c Debug --no-restore`：通过 17/17，失败 0、跳过 0。
- `dotnet build samples/IntegrationCapabilities/IntegrationCapabilities.csproj -c Debug --no-restore`：通过，0 警告、0 错误。
- `dotnet run --project samples/IntegrationCapabilities/IntegrationCapabilities.csproj -c Debug --no-build --no-restore -- --self-test`：通过，输出“本地组合验证通过：TSV 行已映射并渲染到报告。”
- `dotnet build src/Bing.Utils.Text.EncodingDetection/Bing.Utils.Text.EncodingDetection.csproj -c Debug --no-restore`：`netstandard2.0` 与 `net8.0` 均通过，0 警告、0 错误。
- `dotnet pack src/Bing.Utils.Text.EncodingDetection/Bing.Utils.Text.EncodingDetection.csproj -c Release --no-restore -o output/pack/integration-capabilities`：成功生成两个目标框架的 `.nupkg` 与 `.snupkg`；项目版本沿用原值 `1.5.0`，未改发行版本。
- 未重复运行 SFTP/Scriban 测试或 pack：本轮没有修改相应生产代码；复用独立复审确认的 14 项 CLOSED 证据。没有运行全仓库测试或 benchmark。
- `git diff --check`：通过；针对本轮文件的 UTF-8 行尾空白搜索无匹配。工作区状态已核对，变更保持未暂存，未提交。

## 当前剩余状态

- `OPEN_ACTIONABLE`：0（Round 2 范围内 F10、I4 已完成；其余 14 项保持独立复审 CLOSED，不重开）。
- `BLOCKED_EXTERNAL`：7 个真实 OpenSSH SFTP 集成事实仍受主机环境阻塞；本轮没有重跑跳过用例，也没有修改系统级 SSH 服务配置。
- 当前结论仅为定向实现与验证完成；尚待主任务安排 Codex gpt-6.1-sol/high 再次独立复核，不代表独立审查通过。
