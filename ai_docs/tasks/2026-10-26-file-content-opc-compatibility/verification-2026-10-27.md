# 2026-10-27 测试结果复验

结论：当前代码重新验证通过，未发现测试失败、跳过、超时或中止；六次测试进程退出码均为 0。本轮没有修改生产代码或测试代码。

| 范围 | 框架 | 通过/总数 | 失败 | 跳过 | 退出码 |
| --- | --- | --- | --- | --- | --- |
| 文件内容识别专项 | net8.0 | 182/182 | 0 | 0 | 0 |
| 文件内容识别专项 | net7.0 | 182/182 | 0 | 0 | 0 |
| 文件内容识别专项 | net6.0 | 182/182 | 0 | 0 | 0 |
| 文件内容识别专项 | net5.0 | 182/182 | 0 | 0 | 0 |
| 文件内容识别专项 | netcoreapp3.1 | 182/182 | 0 | 0 | 0 |
| Bing.Utils.Tests 全量 | net8.0 | 7509/7509 | 0 | 0 | 0 |

五条专项路径均使用当前源码进行构建检查（--no-restore），其中 net5.0 是本轮补充验证。net8.0 全量复用本轮已构建的产物（--no-build --no-restore），耗时 40 秒。每次运行的完整命令、退出码和 TRX 计数保存于下列证据文件。

- [专项测试命令与计数](artifacts/revalidation-2026-10-27/focused-summary.json)
- [主项目全量命令与计数](artifacts/revalidation-2026-10-27/full-summary.json)
- [主项目全量日志](artifacts/revalidation-2026-10-27/net8-full.log)

独立 luna_worker 核对上一轮 execution.md 与其中明确列出的测试日志，统计一致。tests-before.log 的 7 项失败属于修复前故障复现；修复后的 34 项新增回归、四条专项路径和主项目全量均为零失败、零跳过。本轮另行保存实际进程退出码与 TRX 结果。

net5.0 编译保留现有源码警告（过时 API、未使用成员、空引用注解上下文等），没有编译错误。git diff --check 退出码为 0，仅提示现有文件 CRLF/LF 转换。

测试前影响分析：ChangedFiles/ChangedProjects/ChangedPublicContracts/ChangedRuntimePaths/ChangedProviders/ChangedTFMs/ChangedBuildPackaging/ChangedBenchmarkHarness/ChangedDocsOnly 均无代码变更；AffectedDependents 为 Bing.Utils.Tests；RiskLevel 为 LOW。按用户再次验证的要求，先运行专项，再执行主测试项目全量检查点。

Task: 2026-10-26-file-content-opc-compatibility / revalidation-2026-10-27
Round: 1（结果核验）
Implementation: 3/3（沿用上一轮已关闭问题）
Required local validation gates: 6/6
L0: PASS（构建与 diff 检查）；L1/L3: PASS（五条专项运行路径）；L2: PASS（主项目全量）；L4/L5: NOT_APPLICABLE。
Reused Evidence: 上一轮独立代码审查；本轮测试结果为新证据。
Completed: 旧日志核对、五框架专项、主项目全量、退出码与 TRX 结果核对。
Open Actionable: 无。
Blocked Approval: 无。
Blocked External: 无。
Not Applicable: 本轮未涉及发布与重型性能验证。
Accepted Limitations: 既有文件识别支持边界不变。
Verified Boundaries: 原有非法输入与资源预算断言随专项测试通过。
Deferred: 无本轮事项延期。
No-Progress Check: CHANGED（补充 net5.0 与新鲜测试证据；源码未变）。
Next Action: STOP。
Goal Status: COMPLETED。
