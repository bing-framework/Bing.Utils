# SFTP Docker Round 3 最终定向复核

- Task：2026-10-28-sftp-docker-validation；时间：2026-10-28 18:16 +08:00。
- Candidate：HEAD `de08e07e9d16195f5f0c65d51968fd9e2b3637da`，当前工作区实际文件。
- 范围：仅 D6 两处 cp 参数及其定向 probe；D1–D5 和旧主线 16/16 沿用独立审查结论，不重新展开。
- 沿用当前独立审查会话及用户指定组合，未启动代理；工具无额外模型身份回执。
- 遵守 AGENTS、GOAL_RULES、显式 UTF-8 及已有审查/测试归属规则；仅更新本 review.md。源码、脚本、测试和历史 artifacts 未修改；无暂存、提交、发布或系统操作。

## 收口结论

```text
IMPLEMENTATION_STATUS: PASS（脚本本地审查范围）
TARGETED_D6_STATUS: CLOSED
LOCAL_TARGETED_TEST_STATUS: PASS（两条正式 cp 命令，2/2）
FINDINGS: CLOSED 6/6（D1–D6）
OPEN_ACTIONABLE: 0
TEST_STATUS: PARTIAL（本地验证通过，SFTP 单测复用 8/8，真实 L3 未运行）
EXTERNAL_GATE_STATUS: BLOCKED_EXTERNAL
RELEASE_STATUS: BLOCKED
GOAL_STATUS: STOPPED_BLOCKED
NEXT_ACTION: STOP
```

这是第三修复轮的最终收口。没有剩余可修项，结束自动 Review/Fix 循环。脚本本地审查通过与真实容器验证分开：用户要求的创建容器及 SFTP 实测尚未完成，不能将本结论称为 SFTP 实测 PASS。

## D6：CLOSED

- Requirement：MUST_FIX。
- 原问题：状态对象替换后，两处使用大括号变量名形式访问 ContainerId，导致 cp 目标只剩 `:/...`。
- 修复位置：`tests/Bing.Utils.Sftp.Tests.Integration/scripts/Invoke-SftpDockerValidation.ps1:282`、:326 均使用 `"$($containerState.ContainerId):..."` 子表达式访问属性；整个目标仍在字符串数组中占一个参数。
- 定向 probe：现有 `Test-SftpDockerValidationRound2.ps1:107` 新增 CopyArgumentsOnly 分支，从正式 runner AST 定位两个实际 Invoke-Docker command（:117–127），执行原命令文本并捕获边界 argv（:129–157），随后通过同一 native helper 和 Standard 参数模式传入真实 Python echo，逐参数比对（:159–167）。它没有复制另一套正确的 cp 参数构造替代正式命令。
- 断言独立比较两个预期源路径和目标；源路径含空格，TSV 路径还包含中文；要求完整 argv 数量为 3（cp、源、目标）且逐项相等。
- 本轮实际执行 PowerShell 7.6.6 / Standard 的 CopyArgumentsOnly 文件入口，2/2 通过。两个目标分别含完整合成 64 位 ID，源路径和目标在 Python native argv 中均各占一个参数。
- 同形错误搜索范围限正式 runner，`\$\{containerState\.` 命中 0。
- 验收满足，无新直接回归发现。没有将合成 ID 当作真实容器 ID，没有复制文件到 Docker。

## 六项 Finding 当前状态

| ID | Status | 关闭依据 |
| --- | --- | --- |
| D1 | CLOSED | 复用 Round 1：先创建 .ssh 并设置专用用户所有者、0700，再安装 owner/0600 公钥 |
| D2 | CLOSED | 复用 Round 2：合法 ID 才发布到状态；无效输出按 name+label 发现，再核对 ID+label 删除；九个共用 runner-flow 场景及首错保护验证 |
| D3 | CLOSED | 复用 Round 1：PowerShell 7.3+ native stderr/退出码与 AllowFailure 处理 |
| D4 | CLOSED | 复用 Round 1：Standard 模式保留空参数、引号和完整模板，版本及 README 前提一致 |
| D5 | CLOSED | 复用 Round 1：finally 保存摘要前重新采样进程，早期快照另列 |
| D6 | CLOSED | 本轮两处实际 cp 参数与 native argv 2/2 通过 |

D1–D5 为 REUSED_UNCHANGED_SCOPE，本轮没有重复执行其测试，也未因 Docker 外部阻塞重开。

## 本轮实际验证与证据

影响分析：仅 runner 两处插值和既有 probe 的定向入口；无生产/API/项目图/TFM/依赖变更，直接风险 LOW。采用最小参数 probe 和 L0；不运行无变化的生命周期矩阵、.NET、全仓库或 pack。

执行命令：

```powershell
pwsh -NoLogo -NoProfile -File tests/Bing.Utils.Sftp.Tests.Integration/scripts/Test-SftpDockerValidationRound2.ps1 -CopyArgumentsOnly -EvidencePath D:\Bing_Framework\Bing.Utils\ai_docs\tasks\2026-10-28-sftp-docker-validation\review.md
```

EvidencePath 指向唯一获准写入的 review.md，临时参数 JSON 随后由本报告覆盖；没有覆盖历史 artifact 或创建其他文件。CopyArgumentsOnly 在完成两条参数验证后 return，未执行下方九个生命周期场景。

| 验证 | 结果 |
| --- | --- |
| 当前 execution Round 3、正式两行、probe 定向入口及 round3-copy-argv.json 核对 | 一致 |
| 正式 runner AST → 边界捕获 → Standard Python native argv | 2/2 PASS，退出码 0 |
| runner 同形错误搜索 | 0 |
| runner/probe AST、严格 UTF-8 | PASS |
| git diff --check | PASS |

历史证据 `artifacts/round3-copy-argv.json` 的 PowerShell 7.6.6 / Standard、两个 CapturedArguments/NativeArguments 与本轮结果一致。复用此前核查的 SFTP 单测 TRX 8 Passed、0 Failed、0 skip，不重跑。旧主线 16/16 关闭结论保持。测试归属无新增问题或迁移需求。

## 外部门禁不变

Docker endpoint 恢复阻塞沿用已读 Round 1 记录：精确对象同目录隔离被系统拒绝，源仍在、备份不存在，daemon 不可用。本轮未重复恢复、启动 Docker、查询或修改系统、运行 .NET 或 pack。

| 场景 | 当前状态 |
| --- | --- |
| Docker pull / run / 创建容器 | 未执行，无镜像 digest、真实容器 ID、端口或协商 host key |
| 密码模式 7 项集成 | NOT RUN / BLOCKED_EXTERNAL |
| 私钥模式 7 项集成 | NOT RUN / BLOCKED_EXTERNAL |
| 非空中文 TSV 联网样例 | NOT RUN / BLOCKED_EXTERNAL |
| rename 已提交但回复丢失 | 未验证，无真实注入/窗口证据 |
| 远端暂存删除自身失败 | 未验证，无真实注入/窗口证据 |

未执行不等于 skip 或 pass。两个特殊 SSH 故障窗口没有被 Docker 生命周期或 cp argv probe 覆盖，一键脚本正常通过也不会自动验证它们。原 B1 未解除；没有本轮凭据或密钥生成。

## Round 3 Summary / TODO

- Implementation：6/6 CLOSED（100%，本地执行器 Finding）。
- Release Gates：1/6（复用 L1）；三项正常 L3 和两项特殊故障窗口未通过。
- Changed Scope：本审查仅更新 review.md。
- Validation Run：L0 与两参数定向 probe；L1 复用；L2–L5 未运行。
- Completed：D6 关闭，六项全部收口。
- Open Actionable：none。
- Blocked Approval：none。
- Blocked External：Docker endpoint/daemon、两轮认证、联网样例及两个特殊故障窗口的真实证据。
- Not Applicable：本轮全仓库、benchmark/baseline、双 TFM build/pack。
- Accepted Limitations / Verified Boundaries / Deferred：无新增。
- No-Progress Check：CHANGED，唯一剩余 D6 已由实际参数验收关闭。
- Next Action：STOP；不得仅因外部门禁未通过再启动自动 Fix。
- Goal Status：STOPPED_BLOCKED；本地审查完成，用户创建容器实测目标未完成。
