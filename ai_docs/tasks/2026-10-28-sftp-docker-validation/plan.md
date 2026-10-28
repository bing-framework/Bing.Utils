# SFTP Docker 真实验证计划

## 目标与边界

- 在任务独有的 atmoz/sftp 容器中运行现有 7 项真实 OpenSSH 集成测试，分别使用密码和私钥认证。
- 运行 SFTP 单测，并执行 IntegrationCapabilities 联网样例，使用非空中文 TSV 验证下载、编码检测/转码、映射、报告内容和 UTF-8 输出。
- 交付可复现 PowerShell 执行脚本及无凭证证据；只删除本任务创建的容器和本任务临时目录。
- 尝试在真实服务端窗口注入 rename 后断连及远端清理失败；无法可靠证明请求/故障时保留为未验证，不使用定时 sleep 推断。
- 不修改版本、暂存、提交、推送、发布，不改 Docker/WSL/宿主 SSH 配置，不清理其他容器或用户文件。

## 测试归属规则

已读取 `.agents/skills/test-project-boundary/refs/test-project-boundary.md`。SFTP API 和回归断言归 `Bing.Utils.Sftp.Tests`；依赖真实 OpenSSH、验证 SFTP/网络交互的测试归 `Bing.Utils.Sftp.Tests.Integration`。本任务的认证容器脚本放在该 Integration 项目的 `scripts` 目录。组合样例保持在 `samples/IntegrationCapabilities`，不迁移到测试项目。

## Change Impact Analysis

| 项目 | 评估 |
| --- | --- |
| ChangedFiles | 计划新增集成执行脚本及本任务计划/执行证据；只有受控故障注入需要时才改集成测试支持 |
| ChangedProjects | `Bing.Utils.Sftp.Tests`、`Bing.Utils.Sftp.Tests.Integration`、现有 net8.0 IntegrationCapabilities 样例 |
| ChangedPublicContracts | 否 |
| ChangedRuntimePaths | 受控 OpenSSH 下的认证、传输、冲突保护、取消清理、rename/Commit 顺序；样例 SFTP 下载和转码映射链路 |
| ChangedProviders | 仅任务容器中的 atmoz/sftp/OpenSSH |
| ChangedTFMs | 否；测试与样例为 net8.0，生产包未计划改动 |
| ChangedBuildPackaging | 否；若出现生产 SFTP 修改，则追加两个 TFM 的 build 和 Release pack |
| ChangedBenchmarkHarness | 否 |
| ChangedDocsOnly | 否；新增可复现脚本和任务证据 |
| AffectedDependents | SFTP 集成夹具及 IntegrationCapabilities 样例 |
| RiskLevel | MEDIUM |

## 验收与验证矩阵

| 等级 | 命令/场景 | 验收 |
| --- | --- | --- |
| L0 | UTF-8/路径/清理校验；`git diff --check` | 不含凭证，清理路径严格位于本任务目录 |
| L1 | `dotnet test tests/Bing.Utils.Sftp.Tests/Bing.Utils.Sftp.Tests.csproj -c Debug` | 8 项单测通过；如新增本地测试支持则更新精准结果 |
| L3 | Integration 项目完整测试，密码模式 | 7 项运行、无 skip；错指纹/认证拒绝、往返/属性/目录、目标冲突、取消清理、rename 竞争、进度/Commit 顺序均通过 |
| L3 | Integration 项目完整测试，私钥模式 | 同上；错误私钥认证被拒绝 |
| L3 | IntegrationCapabilities 联网执行 | 从 SFTP 下载非空中文非 UTF-8 TSV；检测并转码，报告含预期姓名/金额，输出是合法无 BOM UTF-8 |
| L3（尝试） | rename 请求已到服务端后丢弃响应；远端暂存清理失败 | 必须证明故障处于相应窗口；无法稳定证明则记录未验证及原因 |

不运行 Scriban/EncodingDetection 独立测试、全仓库测试、benchmark 或无关包 pack。生产 SFTP 源码不变时沿用之前的 8/8 单测证据并按本计划刷新 L1；生产 SFTP 源码发生变化时追加该包双 TFM build/pack。

## 停止规则

- 容器/daemon 环境失败属于 `BLOCKED_EXTERNAL`，不以 skip 计为通过，也不盲目进入 Fix 循环。
- 仅存在 `OPEN_ACTIONABLE` 时修复，并按影响范围重跑 L1/L3。
- rename 请求后未知提交未获得受控证据时，保留为未验证场景，不声称 B1 全部解除。

## Round 1 审查修复补充

- 根据独立审查 D1-D5 修正任务执行器：先创建并设置 integration 用户拥有的 `.ssh` 目录，再以 `0700/0600` 安装公钥；Docker 创建失败后用完整单一 Go 模板参数按唯一名称发现容器，并再次校验本任务 label；发现失败记录为清理状态未知；原生进程捕获 stderr 和退出码；最终摘要前重新采样 Docker Desktop 进程。
- Windows 原生参数传递明确要求 PowerShell 7.3+，固定 `Standard` 模式。用真实 Python 子进程验证空参数、内嵌双引号、空格、完整 Go 模板、stderr+非零退出、AllowFailure 和严格失败语义。
- endpoint 恢复仅允许在确认精确遗留对象、父目录和 Docker 进程状态后，将 `C:\Users\jianx\AppData\Local\Docker\run\dockerInference` 对象移动到同目录唯一备份。若系统拒绝，则只读核对源对象和备份状态并停止；不尝试其他移动方式、权限修改或 Docker/WSL 重置。
- endpoint 成功隔离后才进行一次普通 Docker 启动并运行两种认证的 7 项集成测试和真实中文 TSV 联网样例。rename 已请求后丢失响应及远端清理自身失败只在能证明注入窗口时报告覆盖，否则保留未验证。
- 本轮实现及本地语义检查已完成；精确 endpoint 移动被系统以“系统无法访问此文件”拒绝，源对象仍在、备份不存在，故停止环境恢复。L3 容器测试和联网样例保持 `NOT RUN / BLOCKED_EXTERNAL`，没有把 skip 记作通过。详见 [execution.md](execution.md)。
