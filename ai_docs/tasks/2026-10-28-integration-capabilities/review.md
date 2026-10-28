# 集成能力主线独立复审

- Task：2026-10-28-integration-capabilities
- Round：2 修复后的定向独立复审
- 时间：2026-10-28 15:50 +08:00
- Candidate：当前未提交工作区；HEAD `de08e07e9d16195f5f0c65d51968fd9e2b3637da`。三个新包及测试仍包含未跟踪文件，已读取本轮相关实际源码和测试，不能只依据 tracked git diff。
- 范围：仅复核上一轮 F10、I4；其余 14 项沿用此前独立复审证据，无新证据不重新打开。
- 操作边界：源码和测试只读，仅更新本 review.md；不启动代理，不暂存、提交或发布。用户明确授权写本文件，优先于审查技能默认不写报告的限制。

## 当前结论

```text
IMPLEMENTATION_STATUS: PASS
TEST_STATUS: PASS（本轮编码检测 17/17、示例 build/self-test 通过）
EXTERNAL_GATE_STATUS: BLOCKED_EXTERNAL
OPEN_ACTIONABLE: 0
CLOSED: 16 / 16（F1–F11、I1–I5）
RELEASE_STATUS: BLOCKED（真实 OpenSSH 外部验证未取得）
GOAL_STATUS: STOPPED_BLOCKED
NEXT_ACTION: STOP
```

F10、I4 均已根据真实实现和本轮执行结果关闭。代码完成与真实 SSH 互操作验证分开：没有当前工作区可修问题，不继续 Fix 或重复执行外部跳过用例。

## Change Impact Analysis

| 项目 | 本轮评估 |
| --- | --- |
| ChangedFiles | EncodingDetector.cs、编码检测回归测试、编码文档、示例 Program.cs 和示例说明 |
| ChangedProjects | Bing.Utils.Text.EncodingDetection、其专属测试、IntegrationCapabilities |
| ChangedPublicContracts | MaxSampleBytes 统一要求至少 4；示例增加 --self-test，不改变引擎成员命名契约 |
| ChangedRuntimePaths | 采样前配置校验；正式示例与自检共享 TSV 解析、映射、模板工厂 |
| ChangedProviders | 没有新增 Provider；UTF.Unknown/Scriban 的既有实现未升级 |
| ChangedTFMs | 未变，编码包仍为 netstandard2.0;net8.0，示例 net8.0 |
| ChangedBuildPackaging | 未改变项目/依赖图；执行者已重新验证编码包双 TFM build/Release pack |
| ChangedBenchmarkHarness | 否 |
| ChangedDocsOnly | 否 |
| AffectedDependents | 使用编码检测采样预算的调用者及本地组合示例 |
| RiskLevel | MEDIUM |

## F10：短预算 UTF-32 BOM 误判

- Requirement：MUST_FIX。
- Status：CLOSED。
- 原触发：MaxSampleBytes=2/3 将 UTF-32 LE 的 FF FE 00 00 截断为 UTF-16 BOM，转码 A 产生 00-41-00。
- 实现：`src/Bing.Utils.Text.EncodingDetection/EncodingDetector.cs:39` 明确最小预算，`:54` 在 Snapshot 中统一拒绝小于 4 的值。byte[] 入口在 `:170` 校验后才复制样本；同步/异步流入口在 `:209`、`:231` 校验后才采样；同步/异步回放入口在 `:273`、`:297` 校验后才采样；转码在 `:335` 校验后才进入检测及 I/O。整数预算便捷重载委托到同一路径，默认预算也经过 Snapshot。
- 文件入口：`:191`、`:255` 可以先打开文件句柄，然后委托检测入口校验，但校验前不读取文件内容。当前验收要求“读取输入前拒绝”得到满足，不把“打开文件”和“消费输入”混为一谈。
- BOM 判别：`:499`、`:500` 保持 UTF-32 LE/BE 先于 UTF-16 的顺序；最小合法预算 4 足以提供完整 BOM。实际输入文件可以短于 4 字节，约束针对采样预算，不是要求所有输入至少 4 字节。
- 文档：属性与三个 sampleBytes 参数、包 README、模块文档同步说明至少 4 字节及读取前失败。

回归证据（均由本轮 17/17 测试运行覆盖）：

| 验收面 | 真实测试和断言 |
| --- | --- |
| 2/3 预算拒绝 | `tests/Bing.Utils.Text.EncodingDetection.Tests/TextEncodingDetectorTest.cs:354` 参数化覆盖 byte[]、同步/异步文件、seek/nonseek 检测、同步/异步回放和转码，均断言 ArgumentOutOfRangeException |
| 不消费输入 | 同一测试断言可定位流 Position 保持 0、不可定位流 ReadCalls=0、转码目标 Length=0；`:420` 的 NonSeekableStream 记录 Read、ReadByte、ReadAsync 三类读取 |
| 最小合法预算 | `:285` 对 UTF-32 LE/BE 分别覆盖 byte[]、同步/异步文件、seek/nonseek 同步/异步检测，`:411` 统一断言 CodePage=12000/12001、HasBom=true、BomLength=4、Confidence=1、Source=Bom |
| 检测不误移位置 | `:298`、`:303` 断言可定位流检测后 Position=0 |
| 回放完整 | `:313`、`:325` 使用不可定位输入，分别同步/异步读取 ReplayStream，逐字节等于完整原始输入；包含采样后额外探测的字节 |
| 无 NUL 的转码 | `:337` 对 LE/BE 的不可定位流执行最小预算转码，输出精确等于 Encoding.UTF8.GetBytes("A")，即单字节 41；排除了额外 NUL 和 BOM |

原缺陷已由预算 Fail Fast 和完整 BOM 测试共同闭环；未通过降低置信度或接受错误解码规避问题。

## I4：非空组合示例失败

- Requirement：MUST_FIX。
- Status：CLOSED。
- 原触发：ReportRow.Name/Amount 保留原名，但模板访问小写 row.name/row.amount，非空渲染抛成员缺失异常。
- 修复：`samples/IntegrationCapabilities/Program.cs:104` 使用 row.Name/row.Amount，匹配 `:120`、`:121` DTO，保持引擎既有命名契约。
- 正式路径：`:58` 调用 ReadRowsAsync，`:61` 调用 CreateReportTemplate。
- self-test：`:9` 在读取 SSH 环境变量前进入本地验证；`:110` 构造真实非空 TSV（姓名/金额表头，Alice/12.5 数据行），`:111` 调用相同 ReadRowsAsync，`:112` 调用相同 CreateReportTemplate，`:113` 断言报告包含 Alice 和 12.5。
- 共用实现：`:78` 的 CreateReportMap 配置真实 TabularMap<ReportRow>；`:85` 的 ReadRowsAsync 使用 CsvStream.ReadAsync 解析和 map.Map 映射；`:102` 是唯一报告模板工厂。self-test 没有复制一份“正确模板”或绕过解析直接构造正确 DTO。
- 本轮执行：示例 build 成功，0 警告/0 错误；--self-test 退出码 0，输出“本地组合验证通过：TSV 行已映射并渲染到报告。”
- 验收边界：该自检验证非空 TSV→CsvStream→TabularMap→同一 Scriban 模板，不声称执行 SFTP 下载或联网端到端验证；后者仍属 B1。

## 其余 14 项：REUSED_UNCHANGED_SCOPE

以下为上一轮独立复审已关闭、当前没有新证据失效的结论。本轮未重复运行 SFTP/Scriban 测试，也未展开无关源码审查。

| ID | Status | 复用证据 |
| --- | --- | --- |
| F1 | CLOSED | 主机指纹前缀/padding 规范化、错误指纹拒绝；SFTP 指纹参数化单测 |
| F2 | CLOSED | Connect/Open/Copy/Rename 的真实异步 token 路径，无 Task.Run 同步网络包装；真实网络验证分列 B1 |
| F3 | CLOSED | 首错保留、清理错误附加、Committed 结果保存、rename 后断链 Unknown 语义及首错辅助测试；真实故障运行分列 B1 |
| F4 | CLOSED | 独立 SequencedProgress 使用递增序号及复制最终事件/Commit 顺序；服务端执行分列 B1 |
| F5 | CLOSED | 流下载允许部分输出，Completed 与 Committed 区分，文件下载另行暂存发布，XML 与模块文档一致 |
| F6 | CLOSED | 结构化编译错误、模板名、1-based 行列，包含多行/emoji 的位置测试 |
| F7 | CLOSED | 泛型/只读字典、嵌套数据、中文键及宿主方法拒绝测试，保留实际属性名 |
| F8 | CLOSED | 统一运行错误类型/错误码、位置/诊断/InnerException，缺失根变量独立码 |
| F9 | CLOSED | 独立 context、异步渲染传入 token、ScriptAbortException 映射 OCE，长循环取消测试 |
| F11 | CLOSED | 持续 Decoder/Encoder、底层读写 token、严格/替换解码与 leaveOpen；本轮编码全项目测试同时再次覆盖既有跨块和取消用例 |
| I1 | CLOSED | 密码/私钥、强制主机指纹、目录/属性接口，批准依赖和 TFM；真实认证分列 B1 |
| I2 | CLOSED | 本地文件同目录 CreateNew 暂存，关闭/取消检查后不覆盖 File.Move，失败仅清理暂存 |
| I3 | CLOSED | 远端同目录唯一暂存、CreateNew、普通不覆盖 rename，无 overwrite 或先删旧目标 API；真实竞争执行分列 B1 |
| I5 | CLOSED | 环境驱动 OpenSSH 夹具有 7 项实际断言与启用说明，非永久 Skip 占位；外部运行分列 B1 |

I2/I3 沿用上一轮 review 的编号：I2=本地文件下载，I3=远端上传；plan/execution 的两者顺序相反。拒绝覆盖为批准契约，首轮破坏性覆盖 ACCEPTED_LIMITATION 分类的撤回继续有效，不新增原子覆盖功能要求。

上轮 SFTP 8/8、Scriban 12/12 的成功证据复用；当前编码 17/17 为新鲜证据。不会把两轮证据混写为本轮全部 37 个测试重新执行。解决方案/发布脚本/模块登记、测试边界、FileHelper.GetEncoding 未被接管等已确认结论保持有效。

## B1：真实 OpenSSH 验证

- Status：BLOCKED_EXTERNAL。
- 当前没有可用的受控 SSH 服务；7 个真实 SSH 用例在上一轮为动态跳过，本轮按用户指令不重复执行 skip，不安装或修改 sshd/Docker/账户。
- 尚未取得真实证据：密码与私钥登录、错误指纹/认证拒绝、目录/属性、传输往返、服务端 rename 竞争保护、网络中途取消、远端暂存清理。rename 请求后断连与清理失败的真实故障注入也未取得；现有静态分支和辅助单测不能替代服务端故障验证。
- 完整 SFTP→转码→映射→报告的联网示例未执行；本地 self-test 仅关闭 I4 的非空解析/映射/渲染缺陷。
- 这是外部验证门禁，不是本轮可修代码缺陷；不能因它保持实现 NEEDS_FIX，也不能宣称真实 OpenSSH 验证通过。

## 验证记录与复用范围

| 等级 | 本轮实际命令 | 结果 |
| --- | --- | --- |
| L0 | git diff --check | PASS |
| L1/L2 | dotnet test tests/Bing.Utils.Text.EncodingDetection.Tests/Bing.Utils.Text.EncodingDetection.Tests.csproj -c Debug --no-restore | 17/17 PASS，失败 0、跳过 0；构建编码包 net8.0 |
| L0/L3 | dotnet build samples/IntegrationCapabilities/IntegrationCapabilities.csproj -c Debug --no-restore | PASS，0 警告/0 错误 |
| L3 | dotnet run --project samples/IntegrationCapabilities/IntegrationCapabilities.csproj -c Debug --no-build --no-restore -- --self-test | PASS，退出码 0 |

执行者 execution.md 的 Round 2 记录编码包双 TFM build 与 Release pack 成功；本轮未重复 pack，也未重新核验历史 nupkg 内容。netstandard2.0 构建记录与上轮兼容性证据复用，本轮明确取得的是 net8.0 构建、17 个测试和共享示例 self-test。未运行无变化 SFTP/Scriban 测试、OpenSSH 跳过测试、全仓库测试或 benchmark。

## Round 2 Summary

- Implementation：16/16 CLOSED（100%），其中本轮新增关闭 F10、I4，其余 14 项复用。
- Release Gates：本地实现验证通过；真实 OpenSSH 外部门禁未通过，发布状态仍 BLOCKED。
- Changed Scope：采样预算及回归、共享示例路径及说明。
- Validation Run：L0/L1/L2/L3 如上；L4/L5 未运行。
- Reused Evidence：其余 14 项、既有 TFM/依赖/登记；执行者本轮双 TFM build/pack 记录注明来源。
- Completed：F10、I4。
- Open Actionable：none。
- Blocked Approval：none。
- Blocked External：B1。
- Not Applicable：本轮无 benchmark/baseline 要求。
- Accepted Limitations：无新增。
- Verified Boundaries：无新增容量边界。
- Deferred：none。
- No-Progress Check：CHANGED（两个问题有实际修复与新鲜验收证据）。
- Next Action：STOP。
- Goal Status：STOPPED_BLOCKED；当前只读复审完成，外部服务缺失不触发新 Fix。
