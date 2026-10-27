# 第二批实施与验证记录

日期：2026-10-26。执行方式：当前 Codex 宿主；主代理负责方案、源码、集成与验证，luna_worker 负责独立测试和只读审查。用户已授权继续第二批；无 Git 暂存/提交/发布，无依赖或版本变更。

## 已完成

- P1/P2：新增显式容器选项与预算快照、ZIP32 有界目录/元数据读取、六种 Office 主部件类型识别、独立证据/原因和额外读取计数。
- 所有入口完成集成：byte[]/Span、同步/异步可定位源、文件路径、可定位及不可定位 Probe。默认路径保留第一批行为。
- P3：两个新测试类归属 Bing.Utils.Tests/Bing/IO（主要断言 Bing.Utils 主包，依照 test-project-boundary refs）；无测试迁移、混合类拆分或项目引用变化。
- 使用指南增加开启方式、预算/原因、格式表、明确转存示例与支持边界；模块文档同步更新。

## 验证证据

| 检查 | 结果 |
|---|---|
| 生产包全部 TFM 构建 | PASS；net8.0/net7.0/net6.0/netstandard2.0，0 错误、70 条既有警告 |
| net8.0 文件识别专项 | PASS：84/84，包含第一批 51 个回归用例与本批 33 个用例 |
| UTF-8 / 尾随空白 | PASS：17 个功能/测试/文档文件；git diff --check 通过 |
| net6.0 / net7.0 / netcoreapp3.1 文件识别专项 | PASS：各 84/84；netcoreapp3.1 加载实际 netstandard2.0 产物 |
| 主包 net8.0 完整套件 | PARTIAL：最终 7408/7411 通过，3 项既有测试失败；修复前 7410/7410 通过 |
| 独立只读审查 | PASS：1 项 MIME 大小写问题已修复并独立复核 CLOSED，OPEN_ACTIONABLE=0 |

专项验证了真实源累计读取不超过配置、精确读取预算成功/少一字节停止、512 KiB 正文只读不到 80 KiB、伪造解压长度不能绕过预算、目录条目数量声明核对、短读/纯异步源、取消/异常恢复、非定位重放，以及旧浅检测行为。

首次测试编译发现新增测试的条件表达式需要显式 Exception 基类类型，已修正；没有生产代码失败证据。源抛出的 InvalidDataException 与解压数据错误已分开处理，前者传播，后者返回 InvalidContainer。

审查修复：MainTypes 使用 OrdinalIgnoreCase 比较 MIME；新增混合大小写 ContentType 回归。修复前四运行时各 83/83、完整套件 7410/7410 通过；修复后重新执行本节证据，不沿用失效的专项结果。netcoreapp3.1 输出目录实际 Bing.Utils.dll 的 TargetFrameworkAttribute 已验证为 .NETStandard,Version=v2.0。

## 边界与停止状态

- ACCEPTED_LIMITATION：ZIP64、分卷、加密元数据、非 Store/Deflate 元数据、非 ASCII 且未标记 UTF-8 的部件名；不验证 CRC、正文、宏或完整 OPC 一致性。
- VERIFIED_BOUNDARY：不可定位 ZIP 返回 SeekableSourceRequired 并保留可重放正文；调用方显式使用 SpilloverStream 转存。
- DEFERRED：旧 Office/OLE、模板/EPUB/APK/文本识别；无新增性能 benchmark，新增路径 before 为 NOT_APPLICABLE，资源证据来自预算测试。
- DEFERRED：既有命令执行/映射性能测试在当前运行中的失败，详见下节；不修改本任务范围外实现或放宽其阈值。
- 已完成：P1–P4、四运行时专项、完整套件执行、一次独立 Review/Fix/复核与文档。计划内无未完成实现，无审批阻塞。
- Implementation=PASS；FeatureTests=PASS；FullSuite=PARTIAL；Review=PASS；OPEN_ACTIONABLE=0（本次功能范围）。停止自动修复，不因 PARTIAL 开始额外修复循环。


## 完整套件与独立复查详情

最终完整套件日志：`full-net8.log`，7411 项中 7408 通过，失败为：

1. CommandRunnerTest.RunAsync_WhenExitCodeIsNonZero_ReturnsResultUntilEnsureSuccessIsCalled。
2. MapperHelperTest.Map_LargeNumberOfProperties_ShouldPerformWell。
3. CommandLineExecutorRegressionTest.Execute_WhenExitedParentLeavesInheritedPipes_DoesNotWaitForChild(requireMatch: false)。

随后仅运行这三个方法（继承管道方法有两个参数化案例），日志 `unrelated-recheck.log`：4 项中 1 通过、3 失败。非零退出码测试通过；映射性能测试仍未满足 5 秒阈值，继承管道 true/false 两个变体均发生 PowerShell 命令超时。故不能把全量结果表述为通过，也不能断言所有失败都只是偶发噪声。上述实现和测试在本任务中没有修改；修复前完整套件曾全部通过。保留失败证据，后续若要解决应单独分析命令进程时限与映射性能。

本次文件识别验证最终日志为 `focused-net8.0.log`、`focused-net6.0.log`、`focused-net7.0.log`、`focused-netcoreapp3.1.log`，均 84/84 通过。33 个新增用例（原第一批 51 个）覆盖新增功能和资源契约。最终 MIME 修复已随这四组测试重新编译验证。首次 `focused-net8.log` 属于修复前 83 项记录，不作为最终专项证据。
