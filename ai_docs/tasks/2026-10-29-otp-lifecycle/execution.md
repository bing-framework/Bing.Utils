# TOTP/HOTP 执行与验收记录

2026-10-29，按已批准计划完成 T1–T7，并补齐真实 Redis 断连后的 HTTP 验证及算法/重同步专项边界测试。实现与本轮相关验收 PASS；OPEN_ACTIONABLE=0；远端 CI 按用户指令不纳入本轮验收，未宣称通过。未提交 Git、未发布包、未改变发行版本。

## 变更影响分析

新增五个 OTP 包、七个测试项目、net8.0 Web 示例、独立 NuGet 消费者和验证脚本。核心仅新增 Security/DiagnosticSource 依赖；通用 HMAC/Base32 及现有验证码/OCR 源码保持原行为。

高影响边界包括算法、凭据生命周期、账号隔离事务、失败锁定、密钥保护、资源所有权与 HTTP 认证/CSRF。因此采用 RFC 向量、最小核心测试、真实组件并发/重启/故障验证，再执行多框架、打包和解决方案检查。

解决方案和公共文档索引合并已有验证码/OCR 内容；BuildScript.csproj 仅追加独立 OTP 消费者源码排除项。无既有性能承诺变更，不新建 benchmark/baseline/resource gate；按 GOAL_RULES 复用通过证据，不因外部状态启动无效 Fix。

## 完成交付

| 阶段 | 结果 |
| --- | --- |
| T1 | 保存计划；修订原方案重放、确认接口、SQLite/Redis 边界；五包和测试骨架 |
| T2 | HOTP/TOTP、严格 Base32、URI、RFC 向量、真实碰撞取最大计数器 |
| T3 | 绑定、确认、验证、列表、撤销、双码重同步、保护器和限流契约 |
| T4 | SQLite schema v1、immediate 事务、持久失败锁定、8 字节计数器、有界清理 |
| T5 | Redis 单键 Lua、明确内存选项、DI 配置快照/唯一角色/资源所有权 |
| T6 | HTTPS Web、开发身份、生产接入说明、CSRF/no-store、MFA 会话与固定指标 |
| T7 | Windows/Linux 测试、四框架 pack、独立消费者、审查、解决方案构建 |

## 最终测试证据

SDK 8.0.425；Windows 本机与隔离 Linux SDK 容器。测试代码未记录真实用户数据，密钥环和数据库使用独立临时目录。

| 测试 | 框架/环境 | 通过 |
| --- | --- | --- |
| OneTimePassword.Tests | net8/net7/net6 | 80 × 3 |
| DependencyInjection.Tests | net8/net7/net6 | 10 × 3 |
| Redis.Tests | net8/net7/net6 | 12 × 3 |
| Sqlite.Tests.Integration | net8 Windows / Linux | 13 / 13 |
| DataProtection.Tests.Integration | net8 Windows / Linux | 4 / 4 |
| OneTimePassword.Tests.Integration（Web） | net8 Windows / Linux | 11 / 11 |
| Redis.Tests.Integration | net8 Windows / Linux，真实 Redis 8.6 | 3 / 3 |

当前 Windows 矩阵共 337 次，Linux 已有集成证据 31 次，均无失败或跳过。原始首轮结果为 290 / 29；Redis 补验后为 292 / 31；本轮新增核心 15 项，在三个框架各执行 80 项，将 Windows 矩阵增加 45 次。精准复跑不重复计入总数。本轮另复核 Windows SQLite 13 项与 Web HOTP 生命周期 1 项；其余未改动路径复用原证据，Linux 数字来自此前运行。核心包含 RFC 4226 全部十个向量、RFC 6238 三算法六时间向量、输入/时间/计数器边界、前导零、最高因子匹配、URI/Base32、全生命周期、跨账号/用途/会话、并发与指标敏感字段检查。

SQLite 真实测试覆盖竞争单次消费、同事务多设备、并发失败累计、持久锁定、撤销竞争、重启防重放、回滚/取消、5 秒锁等待、损坏记录、ulong 最大值与密文交换。DP 覆盖真实文件 key ring、用途隔离、未知 key、轮换及提供器重建。Web 覆盖身份、Cookie 属性、CSRF、近期认证、重同步/撤销、错误映射、no-store 及生产禁用演示入口。

本机 Redis 使用专用测试容器，测试完成已停止并移除。最初 redis:7-alpine 拉取 TLS 超时，使用已缓存 Redis 8.6 完成全部真实验证，没有将环境缺失记为通过。

原始证据：

- [Windows 单测](../../../output/otp-validation/final-unit/results.json) 与该目录 TRX、commands.log。
- [Windows 集成](../../../output/otp-validation/final-integration/results.json) 与该目录 TRX、commands.log。
- [Linux 日志](../../../output/otp-validation/linux.log) 与 [Linux TRX 目录](../../../output/otp-validation/linux-work/evidence)。
- [打包与消费者](../../../output/otp-validation/final-pack/results.json)。
- [最终构建](../../../output/otp-validation/final-solution-recheck/results.json)。
- [真实 Redis 故障补验](../../../output/otp-validation/redis-fault/results.json)：精准测试 2 项、Windows Web 11 项、Windows Redis 3 项、Linux Web 11 项；对应 TRX 和日志保存在同目录。
- [专项边界测试](../../../output/otp-validation/boundary-tests/results.json)：新增精准测试 15 项、核心 80 × 3 项、SQLite 13 项、Web HOTP 1 项，以及核心四框架编译；对应 TRX 和日志保存在同目录。

output 为忽略目录，原始文件保留在当前工作区；本记录保留可提交的结论和复现入口。

## 真实 Redis 故障补验

用户确认可使用本地 Redis 容器，并将远端 CI 排除出本轮范围。本轮使用缓存的 `redis:8.6` 创建独立临时容器，Windows 连接 `127.0.0.1:49785`；Linux SDK 容器复核同一真实 Redis。两容器完成后均已移除，现有 Redis 服务保持运行。

新增 `OtpWebRedisFailureIntegrationTests` 和 `RedisFaultProxy`，扩展 `OtpWebTestHost` 的真实 Redis 模式。按 test-project-boundary 的真实组件协作规则归入 Web Integration 项目，无测试迁移或归属待定项。每例使用随机限流键前缀、临时 SQLite 与密钥环；绑定确认和事务创建经过真实 Redis Lua。随后关闭测试拥有的 TCP 转发器和已有套接字，使现有连接断开且重连被拒绝。

TOTP/HOTP 均断言：`503 otp_unavailable`、`no-store`、HOTP 计数器/TOTP 已接受时间步未变化、事务未消费、账号/凭据/事务失败计数未变化，受 MFA 保护接口仍为 403。Redis 并发限额、分区隔离、固定过期三项同时通过。本轮没有修改生产代码，也没有发现需要生产修复的问题。

变更影响分析：

```text
ChangedFiles: 3 个 Web 集成测试/夹具文件，README、Redis 模块说明、计划和本记录
ChangedProjects: Bing.Utils.OneTimePassword.Tests.Integration
ChangedPublicContracts: none
ChangedRuntimePaths: 测试宿主真实 Redis 分支和 TCP 故障注入；生产实现未修改
ChangedProviders: none；本轮验证真实 Redis、SQLite、Data Protection 协作
ChangedTFMs: none；受影响测试为 net8.0
ChangedBuildPackaging: none
ChangedBenchmarkHarness: none
ChangedDocsOnly: false
AffectedDependents: Web 集成测试
RiskLevel: LOW
```

执行层级：L0 编译、UTF-8/空白检查；L1 新增精准用例 2/2；L2/L3 Windows Web 11/11、Redis 3/3、Linux Web 11/11。主代理审查断连路径、隔离、后台任务释放和断言，未发现 OPEN_ACTIONABLE。算法、生命周期、其他适配器、打包及解决方案构建使用 `REUSED_UNCHANGED_SCOPE`，不重复运行 L4/L5。

本轮状态：Completed=真实 Redis 故障验证；Open Actionable=0；Blocked Approval=0；Blocked External=0（当前验收范围）；Not Applicable=性能/容量门禁；Accepted Limitations=故障测试使用单节点 Redis；Verified Boundaries=连接故障关闭验证入口且不消费 OTP；Deferred=远端 CI（用户排除，后续 CI 验收任务）；No-Progress Check=CHANGED；Next Action=STOP；Goal Status=COMPLETED（本轮本地补验）。

## 专项边界测试补齐

新增 15 项核心用例，按 test-project-boundary 归入 `Bing.Utils.OneTimePassword.Tests`，无需外部服务或真实时间等待：

- HOTP 重同步：计数器从 1 开始，偏移 100 的双码通过并推进至 103；偏移 101 拒绝，计数器和事务保持未消费，同一事务可继续使用有效双码。另覆盖双码结束于 `ulong.MaxValue` 后耗尽，以及禁止跨上限回绕。
- HOTP 歧义：三组受控匹配位置覆盖窗口头部、首尾及尾部，要求多组匹配返回空且完整扫描 101 个候选。为直接覆盖该罕见分支，将既有循环等价提取为内部 `FindHotpResynchronizationCounter`，只向测试程序集开放；公开服务仍通过真实 HMAC 检查两个验证码。歧义用例验证生产使用的候选判定函数，真实服务拒绝时的状态不消费由窗口外用例覆盖。
- TOTP：固定 RFC 测试密钥在时间步 153567、153569 同为 `468457`，要求选择较大时间步；最大可表示时间覆盖三种哈希、30/60 秒及 6/8 位，共六组独立预计算向量。
- Base32：固定 `AA`、`AAAA`、`AAAAA`、`AAAAAAA` 正向向量，并遍历四种长度余数的全部非零尾位；合计 52 个大小写反例断言。测试中不搜索碰撞、不依赖随机结果。

变更影响分析：ChangedFiles 为 OtpService.cs、assembly-config.cs、两份核心测试及计划/记录；ChangedProjects 为核心包与核心 Tests；ChangedPublicContracts=none；ChangedRuntimePaths 为 HOTP 重同步内部候选选择；ChangedProviders/TFMs/BuildPackaging/BenchmarkHarness=none；ChangedDocsOnly=false；AffectedDependents 为 HOTP 生命周期及 SQLite/Web 调用链；RiskLevel=MEDIUM。测试友元声明未改变包依赖、公开接口或发行版本。

L1 精准用例 15/15；L2 核心 net8/net7/net6 各 80/80；L3 SQLite 13/13、Web HOTP 1/1。L0 核心 net8/net7/net6/netstandard2.0 编译通过，0 警告、0 错误，UTF-8/空白检查通过。独立只读审查确认提取前后完整扫描、唯一匹配与上限处理等价。未重跑无关 L4/L5；既有打包、消费者、整体构建及其他适配器证据仍标明原运行范围。

本轮状态：Completed=15 项专项用例与相关回归；Open Actionable=0；Blocked Approval=0；Blocked External=0（本轮范围）；Not Applicable=性能/容量门禁；Accepted Limitations=双码歧义采用受控匹配输入；Verified Boundaries=窗口上限、歧义拒绝、时间/计数器上限及非零尾位；Deferred=远端 CI；No-Progress Check=CHANGED；Next Action=STOP；Goal Status=COMPLETED。

## 打包和独立消费（首轮证据）

五个新包均 pack 成功，每包包含 net8.0/net7.0/net6.0/netstandard2.0 共四组 DLL 资产。依赖的当前 Security 包一同打包，未发布。

消费者在独立缓存中按精确 package source mapping 只使用本轮 Bing 包，无仓库 ProjectReference。四个目标框架编译通过；net8/net7/net6 分别运行 RFC 向量、URI 解析、真实 SQLite + DP 绑定确认、验证及重放拒绝，均输出 PASS。netstandard2.0 只做契约编译。

完整解决方案 Release -m:1 最终成功，0 错误；git diff --check 通过。新增文件 UTF-8/尾随空白检查通过，Web JavaScript 语法检查通过。

固定 Redis 2.13.1 的传递 System.IO.Hashing 9.0.12 对 net6/net7 输出支持提示，未隐藏；多框架单测和消费者实际通过。整体解决方案另保留既有 ImageSharp 安全审计等告警，未在 OTP 任务中修改其他模块依赖。

## 审查与修复

独立只读审查 OtpService、SqliteOtpStore、Web Program，未发现 OPEN_ACTIONABLE。主代理审查算法/协议/Redis/DI/保护器与集成，完成以下可复现修复：

1. URI 解析：System.Uri 会规范化畸形百分号和宽松处理非法 UTF-8。使用有效共享密钥复现两个失败后，增加原始转义及严格 UTF-8 校验；拒绝路径、Unicode/百分号往返及核心三框架全部通过。
2. 构建集成：BuildScript.csproj 默认递归包含 OtpPackageConsumer/Program.cs，导致独立消费者被错误并入构建脚本。追加精确 Compile Remove，BuildScript 单独构建及完整解决方案复核通过。
3. 测试夹具：最小 Web 宿主配置提前用 UseSetting 注入，登录后重新取得绑定身份的防伪令牌，保证临时数据库隔离并验证真实流程。

公开契约补齐中文 XML 参数/返回语义。对注释补充前后的五个文件去除 XML 和空行比较，确认实现与签名未变。未因 PARTIAL/NOT_VERIFIED 开启额外修复循环。

## 复现

```powershell
pwsh -NoProfile -File build/Test-OneTimePassword.ps1 -Stage Unit
$env:BING_OTP_REDIS = '127.0.0.1:6379,abortConnect=true'
pwsh -NoProfile -File build/Test-OneTimePassword.ps1 -Stage Integration -RequireRedis
pwsh -NoProfile -File build/Test-OneTimePassword.ps1 -Stage PackConsumer
pwsh -NoProfile -File build/Test-OneTimePassword.ps1 -Stage Final
```

CI 定义在 .github/workflows/one-time-password.yml：Windows/Linux 模块与消费者验证，独立 Linux Redis service。用户本轮明确先不考虑远端 CI；未触发或观察远端流水线，不把配置存在视为 CI 已通过，也不将其作为本轮阻塞项。

## TODO 与边界

本轮 TODO 全部完成，OPEN_ACTIONABLE=0。恢复码、Passkey、云 KMS、真实账号系统和实际认证器设备兼容性矩阵按用户范围留待后续。生产宿主负责真实主认证、近期认证策略、可信代理、密钥环保护/备份及周期清理；SQLite 仅用本地磁盘，禁止多主机共享网络文件。
