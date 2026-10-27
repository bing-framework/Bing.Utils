# OPC 合法输入兼容性修复结果

Task: 2026-10-26-file-content-opc-compatibility
Round: 1
Implementation: 3/3
Required local gates: 7/7
Implementation status: PASS
Test status: PASS
Goal status: COMPLETED
Open actionable: 0
Next action: STOP

## 实现与独立复核

| Finding | 结果 | 实现与验收 |
| --- | --- | --- |
| OPC-SPACE-01 | CLOSED（独立 reviewer） | 允许合法 %20，保留原始空白、编码控制字符及编码分隔符拒绝。Default 与 Override 两种包均正确识别。 |
| OPC-EXT-02 | CLOSED（独立 reviewer） | Extension 不再无条件禁止 % 与 :，仍校验单段 URI 字符和合法转义；a:b / a%2Bb 用于主部件或无关附件均不误拒整包。 |
| ZIP-DD-03 | CLOSED（独立 reviewer） | 不可定位 ZIP 输出实际生成 bit 3 / Deflate / 数据描述符，显式断言三处字段；检测、位置恢复、缓存回读清理及错误字段拒绝均覆盖。 |

生产改动仅 FileContentOfficePackage.cs 中的两个私有校验规则及说明注释。公共 API、预算和项目依赖无变动。新增 34 个测试用例：29 个兼容性/独立夹具用例，5 个数据描述符用例。既有回归未删减或放宽。

七个固定夹具使用本机已有 System.IO.Packaging 8.0.1 独立生成并重新读取确认；总计 7598 字节。生成脚本与 README 同置于 tests/Bing.Utils.Tests/Resources/FileContentDetection。测试仅用 BCL 读取固定样本，不依赖生成器程序集，复用项目现有 Resources 复制规则。测试归属按 test-project-boundary 的主断言规则放在 Bing.Utils.Tests/Bing/IO，没有迁移或新增测试项目。

主代理负责生产规则、OPC 夹具、兼容性测试和最终验证；luna_worker 负责独立的数据描述符测试；另一 luna_worker 独立复核。复核原文保存在 review.md，未修改此前任务的审查记录。使用当前 Codex 宿主手工阶段，没有 UAW 运行时 task/stageRunId/inputRevision，不生成伪造 StageResult。

## 验证证据

测试前影响分析已记录于 plan.md：主包私有 OPC 校验及主包测试资源，风险 MEDIUM；无公开契约、Provider、TFM、构建图或 Benchmark 变更。先精准测试，再扩大到文件检测专项和最终主项目全量。

| 验证 | 结果 | 日志 |
| --- | --- | --- |
| 修复前独立兼容性回归 | 7 个预期失败，22 通过；确认捕获原缺陷 | tests-before.log |
| 修复后新增兼容性 + 数据描述符 | 34/34 | tests-focused-after.log |
| net8.0 文件识别专项 | 182/182 | tests-net8.0.log |
| net6.0 文件识别专项 | 182/182 | tests-net6.0.log |
| net7.0 文件识别专项 | 182/182 | tests-net7.0.log |
| netcoreapp3.1 文件识别专项 | 182/182，使用 netstandard2.0 生产产物 | tests-netcoreapp3.1.log |
| net8.0 主测试项目全量 | 7509/7509，33 秒 | tests-net8-full.log |

所有正式验证发生在相同的最终生产/测试代码上。四个生产 TFM 的编译随上述测试构建完成；net8.0 最后两次验证复用精准测试时编译的同一产物。没有并发执行构建与全量测试。现有仓库编译警告保留，本次文件检测范围没有新增警告。UTF-8、尾随空白及 git diff --check 检查通过；七个 ZIP 均未被 Git ignore 排除。

L0: PASS（四 TFM 编译、静态/编码检查）。
L1: PASS（修复前复现，修复后新增 34 项及四运行时 182 项）。
L2: PASS（主项目 net8.0 全量 7509 项）。
L3: PASS（显式缓存、临时存储、数据描述符协作；netstandard2.0 消费路径）。
L4: 不运行无关子包全 Solution；主包全量作为本阶段收口证据。
L5: NOT_APPLICABLE，未修改性能热点算法或资源预算，不重复 Benchmark。

## 状态分类

Completed: 三项均经独立复核闭环，指南与夹具说明已更新。
Open Actionable: 无。
Blocked Approval: 无。
Blocked External: 无。
Not Applicable: 新格式扩展、API 架构扩展、重型性能测试及外部发布门禁。
Accepted Limitations: 既有 ZIP32 / 有界结构识别边界；独立夹具是最小 OPC 识别包，不代表 Office 应用完整文档验收。
Verified Boundaries: 合法输入放行与非法转义拒绝同时验证；数据描述符包可完整缓存回读，释放后临时租约为零。
Deferred: 无本轮承诺事项延期；此前建议的其他格式及异步释放便利性不属于本次两处缺陷修复。
Reused Evidence: 对未改动的检测器入口实现沿用此前设计/审查结论；本次运行结果均为最终候选的新证据。
No-Progress Check: CHANGED，7 个已复现失败修复后通过，独立复核确认无新问题。
Next Action: STOP。
