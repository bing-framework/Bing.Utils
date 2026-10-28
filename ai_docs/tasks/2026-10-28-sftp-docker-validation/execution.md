# SFTP Docker 真实验证执行记录

## Round 3 D6 定向修复

```text
IMPLEMENTATION_STATUS: PASS（两处 docker cp 属性插值已修正）
TARGETED_TEST_STATUS: PASS（runner AST 提取的两个参数均通过边界捕获和 Standard native argv 回显）
REVIEW_STATUS: READY_FOR_REVIEW
REVIEW_FINDINGS: D1-D5 CLOSED（沿用原 reviewer）；D6 修复证据待原 reviewer 确认关闭
BLOCKED_EXTERNAL: Docker daemon / 真实 SFTP L3（状态未变）
```

### 变更影响

只改 runner 的公钥与样例文件两个 `docker cp` 目标插值，并在既有 Round 2 本地 probe 增加独立 `-CopyArgumentsOnly` 路径。无生产源码、公开契约、项目引用、TFM 或依赖变化；不启动 Docker，不运行 .NET 或 pack。

正式 runner 两处目标改用 `"$($containerState.ContainerId):..."` 子表达式属性插值，整个目标仍为 `Invoke-Docker` 的一个参数。probe 从 runner AST 找到 `docker-copy-public-key` 和 `docker-seed-sample` 两条实际命令，在本地 `Invoke-Docker` 边界捕获参数；随后把捕获的 argv 经 PowerShell `Standard` 模式传给 Python native echo，逐参数比对回显。合成 ID 为 64 位；宿主公钥源路径和 TSV 源路径均含空格。

验证命令：

```powershell
pwsh -NoLogo -NoProfile -File tests/Bing.Utils.Sftp.Tests.Integration/scripts/Test-SftpDockerValidationRound2.ps1 -CopyArgumentsOnly -EvidencePath ai_docs/tasks/2026-10-28-sftp-docker-validation/artifacts/round3-copy-argv.json
```

结果：两个目标参数均为 PASS、0 FAIL、0 SKIP。目标分别精确为 `<合成ID>:/tmp/sftp-validation-client.pub` 与 `<合成ID>:/home/integration/upload/integration-capabilities-round3-probe.tsv`；两个源路径和两个目标在边界捕获与 Python argv 回显中均各占一个参数。证据见 [round3-copy-argv.json](artifacts/round3-copy-argv.json)。该开关只执行 D6 参数回归，没有重跑已关闭 D2 的九个生命周期场景；Round 1/Round 2 artifact 未覆盖。

runner 与 probe AST、严格 UTF-8、尾随空白和 `git diff --check` 检查通过；只检查 runner 中 `${containerState.` 同形插值，结果为 0 处。Docker、SFTP 密码/私钥集成、中文 TSV 联网样例均未运行；Round 1 endpoint 外部阻塞不变。没有生成凭证或密钥，`review.md` 未修改。D6 实现已交原 reviewer 收口，不代表独立审查通过。

## Round 2 D2 定向修复

```text
IMPLEMENTATION_STATUS: PASS（run 输出校验与 finally 清理共用控制流已修复）
TARGETED_TEST_STATUS: PASS（Round 2 本地 native/runner-flow probe 9/9 场景）
REVIEW_STATUS: READY_FOR_REVIEW（review.md 保持原样；D2 等原 reviewer 确认）
DOCKER_STATUS: NOT STARTED（按本轮指令；Round 1 endpoint 外部阻塞仍在）
L3_STATUS: NOT RUN / BLOCKED_EXTERNAL
```

### Change Impact Analysis

| 项目 | Round 2 影响 |
| --- | --- |
| ChangedFiles | SFTP Docker runner、其共用 helper、新增 Round 2 本地 probe、本执行记录及新 artifact |
| ChangedProjects | 仅 `Bing.Utils.Sftp.Tests.Integration` 任务级 PowerShell 执行支持 |
| ChangedPublicContracts | 无 |
| ChangedRuntimePaths | 容器 run 输出验证；finally 名称发现、唯一 label/ID 身份复核及清理状态 |
| ChangedTFMs / BuildPackaging | 无；没有生产源码变更，不运行 .NET 或 pack |
| Environment | 不启动 Docker/Desktop；不改 endpoint、服务、WSL 或其他系统状态 |
| RiskLevel | LOW；任务级脚本与本地验证支持 |

### 修复与共用控制流

- runner 将本次资源生命周期集中在状态对象中。`Invoke-RunContainerAttempt` 先标记 run 已尝试，再把合并输出放入局部候选值；只有完整输出严格匹配 64 位容器 ID 时才写入状态 ID。stdout ID 加 stderr 警告或 malformed 输出均保持状态 ID 为空并触发失败，随后 finally 必须尝试按本次唯一名称发现。
- runner 的实际 finally 和 Round 2 probe 共用 `Complete-RunContainerCleanup`。它按名称使用完整单参数 inspect 模板发现；只有 ID 和本次唯一 label 匹配后才调用删除。身份不符不删除。发现异常/失败及删除异常都会写入 cleanup error，且不会标为 `REMOVED`。
- D1、D3、D4、D5 未修改，继续按独立审查中已关闭状态处理。本轮不改 `review.md`；该文件中的 D2 状态仍等待原 reviewer 根据本轮实现和证据确认。

### 定向验证

命令：

```powershell
pwsh -NoLogo -NoProfile -File tests/Bing.Utils.Sftp.Tests.Integration/scripts/Test-SftpDockerValidationRound2.ps1
```

PowerShell 7.6.6 / `Standard` 参数模式下，实际 Python native 进程分别产生 exit 0 + 64 位 ID stdout + 非空 stderr warning，以及单独 malformed 输出。两者都经过 runner 使用的 `Invoke-RunContainerAttempt` 和 `Complete-RunContainerCleanup`，不启动 Docker。九个 runner-flow 场景均通过断言：

| 场景 | 断言结果 |
| --- | --- |
| ID stdout + stderr warning | 输出拒绝赋 ID；名称发现、本任务 label/ID 复核、删除；`REMOVED` |
| malformed 输出 | 失败后名称发现、自有身份复核并删除；`REMOVED` |
| 名称发现返回错误 label | 删除调用为 0；`UNKNOWN` 且有 cleanup error |
| 名称发现非零 / 抛异常 | `UNKNOWN`、有 cleanup error |
| 明确 `No such object` | `NOT_FOUND`、无 cleanup error、无删除调用 |
| 删除返回非零 / 抛异常 | 分别为 `FAILED` / `UNKNOWN`；均未标 `REMOVED`，有 cleanup error |
| ID 身份复核失败 | `UNKNOWN`、不删除、有 cleanup error |

结果保存在 [round2-native-semantics.json](artifacts/round2-native-semantics.json)。artifact 只包含合成容器 ID、场景状态和本地进程证据，不含凭证或密钥。runner、helper、probe 均通过 PowerShell AST 和严格 UTF-8 检查；Round 2 文本文件通过 UTF-8/尾随空白检查，`git diff --check` 通过。没有重跑 .NET、pack 或 Round 1 probe。

Round 2 没有 Docker 容器、镜像、端口或 host-key 运行证据。密码认证 7 项、私钥认证 7 项、中文 TSV 联网样例仍为 `NOT RUN / BLOCKED_EXTERNAL`，不是 pass 或 skip。rename 已提交但响应丢失、远端临时文件删除失败两个故障窗口仍未验证。没有生成本轮临时凭证/密钥；误路径产生的 sibling artifact 与本次新建空目录已按确切路径清理。

## Round 1 当前结论

```text
IMPLEMENTATION_STATUS: PASS（D1-D5 执行器修复完成；native/清理/进程快照语义 probe 通过）
TEST_STATUS: PARTIAL（L0 PASS；L1 复用未变范围 8/8；容器 L3 未运行）
EXTERNAL_GATE_STATUS: BLOCKED_EXTERNAL（runtime endpoint 不能安全隔离；daemon 仍不可用）
OPEN_ACTIONABLE: 0（本轮执行器修复范围；等待主任务 reviewer 复核）
RELEASE_STATUS: BLOCKED
GOAL_STATUS: STOPPED_BLOCKED
NEXT_ACTION: 解决 dockerInference endpoint 的系统访问阻塞后再运行容器门禁
```

Round 1 保留本文件后续 Round 0 记录与既有 TRX，不改独立审查文件。D1 已修正执行器中的 SSH 目录/密钥权限，但新容器内权限及双认证的运行验收仍为 `BLOCKED_EXTERNAL`。D2–D5 的本地语义验证详见下文。没有宣称独立审查通过。

## Round 0 结论

```text
IMPLEMENTATION_STATUS: PASS（执行脚本与任务证据已交付；没有发现可修复的 SFTP 生产缺陷）
TEST_STATUS: PARTIAL（SFTP 单测通过；真实 OpenSSH 与联网样例未启动）
EXTERNAL_GATE_STATUS: BLOCKED_EXTERNAL
OPEN_ACTIONABLE: 0
BLOCKED_EXTERNAL: 5（密码集成、私钥集成、联网样例、rename 响应丢弃、远端清理失败）
RELEASE_STATUS: BLOCKED
GOAL_STATUS: STOPPED_BLOCKED
NEXT_ACTION: Docker daemon 可用后重跑一键脚本
```

Candidate HEAD：`de08e07e9d16195f5f0c65d51968fd9e2b3637da`；执行时未改生产 SFTP 源码、包版本、项目图或 TFM。此前集成能力主线报告中的 16/16 复审结论未被本轮重开；本记录不构成新的独立审查结论。

## Round 1 修复与验证

### Change Impact Analysis

| 项目 | Round 1 影响 |
| --- | --- |
| ChangedFiles | Docker runner、共享 PowerShell helper、native 语义 probe、SFTP Integration README、本执行记录及本轮 artifacts |
| ChangedProjects | 仅 SFTP Integration 执行支持；未改 SFTP 生产项目或测试项目引用 |
| ChangedPublicContracts | 无 |
| ChangedRuntimePaths | 原生命令 argv/stderr/exit code、容器发现/清理、SSH 公钥目录权限、最终 Docker 进程状态采样 |
| ChangedProviders | 未运行容器；没有新的 OpenSSH provider 证据 |
| ChangedTFMs | 无；集成项目仍为 net8.0 |
| ChangedBuildPackaging | 无；生产 SFTP 未改，不运行双 TFM build/pack |
| ChangedBenchmarkHarness | 无 |
| AffectedDependents | 本地 SFTP Docker 验证执行器调用者 |
| RiskLevel | MEDIUM；仅任务级 PowerShell 执行器和文档 |

### D1-D5 状态

| Finding | Round 1 变更与证据 | 当前分类 |
| --- | --- | --- |
| D1 | 在容器中先 `mkdir -p /home/integration/.ssh`，再 `chown integration`、目录 `0700`，以 `install -o integration -m 600` 创建 `authorized_keys`；不再对可能不存在且 root-owned 的目录直接 chmod。真实容器权限及密码/私钥认证未运行。 | 实现已改；runtime 验收 `BLOCKED_EXTERNAL` |
| D2 | Go inspect 模板由 helper 构造成单一字符串 argv。失败后按唯一容器名发现，再核对 task label；标签不符不删除。native/local probe 覆盖部分创建后找回并删除、label mismatch 不删、inspect 不可用返回 `UNKNOWN` 并追加 cleanup error。 | 本地实现/语义 PASS；真实 Docker 路径未运行 |
| D3 | runner 明确改为 `#requires -Version 7.3`；`Invoke-NativeProcess` 统一合并捕获 stderr 并读取 `$LASTEXITCODE`，native 错误不由 `$ErrorActionPreference=Stop` 提前中断。 | 本地 native probe PASS |
| D4 | runner 固定 `$PSNativeCommandArgumentPassing = 'Standard'`，并设置 `$PSNativeCommandUseErrorActionPreference = $false`。Python 原生 argv probe 实收空参数、内嵌引号、空格和完整带引号 Go 模板；同一 helper 验证 stderr、exit 23、AllowFailure 与严格失败。README 命令改用 `pwsh`。 | 本地 native probe PASS |
| D5 | `run-summary.json` 前重新调用真实进程查询；daemon probe 时的状态单独记录为快照。受控 query stub 验证进程状态从 false 到 true 时两次采样反映变化。旧 Round 0 artifact 未覆盖。 | 本地快照 probe PASS |

语义 probe 命令：

```powershell
pwsh -NoLogo -NoProfile -File tests/Bing.Utils.Sftp.Tests.Integration/scripts/Test-SftpDockerValidationSemantics.ps1
```

PowerShell 7.6.6 / Standard 模式结果保存在 [round1-native-semantics.json](artifacts/round1-native-semantics.json)：

- 实际 native argv 精确包含 `""`、`embedded"quote`、`two words` 和单个完整 Go 模板参数。
- native stderr marker 与退出码 23 同时捕获；AllowFailure 继续，严格模式抛出预期 `InvalidOperationException`。
- 部分创建模拟通过名称+本任务 label 找到 ID 并调用删除；错误 label 的删除调用数为 0。
- inspect 失败产生 `UNKNOWN` 与清理错误，不被解释为无容器。
- 进程快照 stub 的前后结果为 false/true，结束状态读取调用实际进程查询 helper。

### Docker endpoint 恢复尝试

按审查日志确认的唯一目标 `C:\Users\jianx\AppData\Local\Docker\run\dockerInference` 做了操作前检查：它是 0 字节 `Archive, ReparsePoint`，mtime 为 2026-10-23；backend 日志有 6 条相同 listener/remove 访问失败；Docker Desktop/backend/proxy 进程为 0，service 为 `Stopped/Manual`；父目录不是 reparse point，当前身份在父目录有 FullControl；目标 ACL 查询返回 Win32 1920。目标 socket 连接未建立，选定的同目录唯一备份名操作前不存在。

只尝试了一次 PowerShell `Move-Item -LiteralPath` 到同目录唯一备份，返回“系统无法访问此文件。”。操作后源 endpoint 仍在且元数据未变，备份不存在；未改用其他移动方式、提权、ACL 修改、service/context/WSL 操作，也没有再次启动 Docker。操作前后证据分别见 [endpoint-recovery-20261028T094054Z-12b5120d.md](artifacts/endpoint-recovery-20261028T094054Z-12b5120d.md) 与 [endpoint-recovery-postcheck.md](artifacts/endpoint-recovery-postcheck.md)。因此 daemon 仍不可用，停止环境修复尝试。

### 测试结果

| 等级/项目 | 结果 | 说明 |
| --- | --- | --- |
| L0 PowerShell AST/UTF-8 | PASS | runner、helper、semantic probe 三份文件均通过 AST 与严格 UTF-8 解码 |
| L0 native/清理/快照语义 | PASS | 使用真实 Python native 进程，不是 PowerShell 函数 mock；D2/D5 状态用注入 callback 验证 |
| L0 `git diff --check` | PASS | Round 1 变更无 whitespace error |
| L1 SFTP 单测 | `REUSED_UNCHANGED_SCOPE` | SFTP 生产源码和测试未变；复用 Round 0 8/8、失败 0、跳过 0 TRX，不重复运行 |
| L3 密码 7 项 | NOT RUN / `BLOCKED_EXTERNAL` | endpoint 移动被系统拒绝，daemon 不可用；没有 skip 或 test invocation |
| L3 私钥 7 项 | NOT RUN / `BLOCKED_EXTERNAL` | 同上；没有生成临时密钥或密码 |
| L3 中文 TSV 联网样例 | NOT RUN / `BLOCKED_EXTERNAL` | 未创建容器，未下载、转码、映射或生成报告 |
| Docker image/container | NOT RUN | 无 pull、digest、容器 ID、端口或协商 host key |

特殊故障仍未验证：rename 已被服务端提交但响应丢弃、远端暂存文件删除自身失败。此次 helper 语义 probe 不模拟这两个 SSH 窗口；没有脆弱 sleep 或代理系统。

Round 1 没有重跑不变的 SFTP 单测、整仓库测试或 pack。没有凭证/私钥产生或进入日志；没有容器创建，因此无容器清理操作。Round 1 新增证据在 `artifacts/round1-native-semantics.json` 和 endpoint 恢复两份记录。

## Round 0 变更影响分析

| 项目 | 实际影响 |
| --- | --- |
| ChangedFiles | 新增本执行记录、Docker 验证脚本、daemon 探测证据；更新 SFTP Integration README 的一键入口 |
| ChangedProjects | SFTP 单测项目执行；SFTP Integration 项目脚本/说明；没有改生产项目 |
| ChangedPublicContracts | 否 |
| ChangedRuntimePaths | 本轮仅执行 SFTP 单测；真实 SSH 和联网样例因 daemon 不可用未启动 |
| ChangedProviders | 没有容器运行；未取得 atmoz/sftp 镜像身份 |
| ChangedTFMs | 否；单测 net8.0 |
| ChangedBuildPackaging | 否；未改生产 SFTP，不刷新双 TFM build/pack |
| ChangedBenchmarkHarness | 否 |
| ChangedDocsOnly | 否；增加可执行验证脚本及说明 |
| AffectedDependents | SFTP Integration 用户和本地组合样例使用者 |
| RiskLevel | LOW（执行器和文档；生产运行时无变更） |

## Round 0 环境与容器

- Docker CLI：`Docker version 29.4.0, build 9d7ad9f`。
- 当前 context：`desktop-linux`，endpoint `npipe:////./pipe/dockerDesktopLinuxEngine`；`default` context 未选中。
- `docker version` 服务端探测失败，退出码 1：`failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine; check if the path is correct and if the daemon is running: open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.`
- `C:\Program Files\Docker\Docker\Docker Desktop.exe` 存在。先隐藏启动并等待 45 秒，再由执行脚本隐藏启动/等待 180 秒；两次均未建立 daemon。结束后的只读检查显示没有 Docker Desktop/backend 进程，`com.docker.service` 为 `Stopped`、启动类型 `Manual`。
- 本轮没有启动或重置 Docker service、改 Docker context、运行 `wsl --shutdown`、更改 WSL 配置、改宿主 SSH 服务或清理其他容器。因为 daemon 不可达，没有执行 `docker pull`/`docker run`，没有容器、镜像 digest、端口或协商主机指纹可报告。
- `atmoz/sftp:latest`、专用用户名/密码、客户端密钥、容器标签和测试环境变量只在 daemon 就绪后创建。本轮在创建这些资源前退出，因此没有生成凭证或私钥。

daemon 探测原始结果见 [docker-probe.md](artifacts/docker-probe.md)。首次运行的 `run-summary.json` 将 `DockerDesktopLeftRunning` 记为 true，但终止后进程探测已确认没有相关进程。该摘要原样保留；执行脚本已修正为保存 daemon stderr、service/进程状态，并按结束时实际进程状态写值。修正后的脚本通过 PowerShell AST 解析和严格 UTF-8 检查，未再次重跑同一 180 秒外部等待。

## Round 0 L1：SFTP 单测

命令由一键脚本执行：

```powershell
dotnet test tests/Bing.Utils.Sftp.Tests/Bing.Utils.Sftp.Tests.csproj --configuration Debug --logger "trx;LogFileName=sftp-unit.trx" --results-directory ai_docs/tasks/2026-10-28-sftp-docker-validation/artifacts/20261028T091202Z-ba83c1bf
```

结果：8 通过、0 失败、0 跳过。TRX 与 console log 保存在该 run 目录的 `sftp-unit.trx`、`sftp-unit.log`。本单测候选未改源码，结果只覆盖该 net8.0 单测程序集。

## Round 0 L3：真实 SFTP 与样例矩阵

| 场景 | 命令/认证 | 通过 | 失败 | 跳过 | 结果 |
| --- | --- | ---: | ---: | ---: | --- |
| 密码认证下 7 项集成测试 | 一键脚本；`BING_SFTP_PASSWORD` | 0 | 0 | 0 | `BLOCKED_EXTERNAL`；dotnet test 未启动 |
| 私钥认证下 7 项集成测试 | 一键脚本；`BING_SFTP_PRIVATE_KEY_PATH` | 0 | 0 | 0 | `BLOCKED_EXTERNAL`；dotnet test 未启动 |
| 联网组合样例 | IntegrationCapabilities 私钥模式；UTF-16LE BOM 中文 TSV | 0 | 0 | 0 | `BLOCKED_EXTERNAL`；未下载/转码/映射/生成报告 |

这不是测试动态 skip：Integration 项目命令没有执行，因此没有测试结果文件，也不能把 14 项算通过或跳过。现有 7 项预期覆盖：错指纹/错误认证拒绝、流/文件上传下载往返、目录枚举与文件属性、远端与本地目标冲突保护、下载/上传取消及暂存清理、rename 竞争、递增进度和 Write 最终事件先于 Commit。

联网样例也未执行。之前的本地 `--self-test` 仅验证本地非空 TSV 解析/映射/模板，不作为本轮真实 SFTP 下载、编码转码或输出编码证据。

## Round 0 受控故障窗口

| 故障窗口 | 状态 | 原因 |
| --- | --- | --- |
| rename 请求已被服务端接受并提交，但响应丢弃/连接断开 | `BLOCKED_EXTERNAL` / 未验证 | OpenSSH 容器没有启动；没有办法证明 rename 请求已经到达服务端，未用 sleep、普通取消或坏参数冒充该窗口 |
| 上传失败后的远端暂存删除本身失败 | `BLOCKED_EXTERNAL` / 未验证 | OpenSSH 容器没有启动；没有执行权限变更或受控删除失败注入 |

未增加脆弱的定时测试或大型代理系统；没有声称旧 B1 全部解除。

## Round 0 脚本、清理与证据

- 一键脚本：[Invoke-SftpDockerValidation.ps1](../../../tests/Bing.Utils.Sftp.Tests.Integration/scripts/Invoke-SftpDockerValidation.ps1)。它会有界等待 Docker Desktop、记录实际镜像身份、将端口限制在 loopback、使用独有容器标签、分别运行 L1/密码/私钥验证和联网样例、脱敏日志，并在 `finally` 只删除身份匹配的自有容器与任务内临时目录、恢复进程环境变量。
- 本轮 run ID：`20261028T091202Z-ba83c1bf`。证据目录：[artifacts/20261028T091202Z-ba83c1bf](artifacts/20261028T091202Z-ba83c1bf)。其中有 `environment.txt`、`runner.log`、SFTP 单测 TRX/log 和 `run-summary.json`。
- `run-summary.json` 的 `ContainerCleaned=false` 表示没有创建过容器，不表示清理失败；`TemporaryDirectoryCleaned=true`、`EnvironmentRestored=true`，`CleanupErrors=[]`。任务临时目录 `.tmp-20261028T091202Z-ba83c1bf` 已确认不存在。
- 文本凭证/密钥没有被生成或写入日志。TRX 和日志保留于该目录。容器未创建，因此无需容器删除。
- L0：PowerShell AST parse PASS、严格 UTF-8 PASS、`git diff --check` PASS。脚本后续只修改了 daemon 诊断捕获及结束进程状态，故对未变的 SFTP 单测结果按 `REUSED_UNCHANGED_SCOPE` 复用；没有重跑同一测试或重复 180 秒等待。

## Round 0 轮次摘要

- Implementation：1/1（执行脚本与任务记录完成）；没有生产缺陷被证实。
- Release gates：1/6（L1 单测通过；密码集成、私钥集成、联网样例、rename 后响应丢弃、远端清理失败均未通过 daemon 门禁）。
- Completed：L1 8/8；可复现执行器；UTF-8/解析/差异格式检查。
- Open Actionable：none。
- Blocked Approval：none。
- Blocked External：Docker daemon/server service；其下的五个 L3/故障窗口验证。
- Not Applicable：Benchmark、全仓库测试、生产双 TFM build/pack（本轮未改生产代码）。
- Accepted Limitations：none。
- Verified Boundaries：本地临时目录限制清理并成功移除。
- Deferred：无代码工作延期；外部验证等待 Docker daemon 可用。
- No-Progress Check：UNCHANGED；第二次有界启动尝试重复确认相同 daemon pipe/service 阻塞，停止自动重试。
- Next Action：daemon 可用后运行 `Invoke-SftpDockerValidation.ps1`；当前状态 `STOPPED_BLOCKED`。
