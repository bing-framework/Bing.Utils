# 工具能力收敛执行记录（2026-10-23）

后续 C02 写盘与合并耗时修复已完成，最新实现、验证及未改善场景见 [C02 I/O 报告](c02-io-performance.md)。以下保留前轮执行时的状态与证据。

本轮继续实施 [收敛计划](capability-refinement-plan.md)。起点为提交 `9258eab` 上包含既有修复的未提交工作区，固定基线保存于 `artifacts/capability-refinement-2026-10-23/baseline`；没有以旧 HEAD 替换该工作区基线。保留既有 F01—F10、CI、资源限额及此前扩展能力。

## 实现状态

| 工作包 | 已交付内容 | 状态 |
|---|---|---|
| R01 | 默认 64 KiB、空流不租页、几何页表；小页、租用失败、溢出、重复释放回归 | 完成 |
| R02 | 固定循环内执行分片重试、直接状态序列化、单次读取校验合并；保留检查点独立快照和持久化时点 | 实现完成，性能结果单独记录 |
| R03 | 显式 Complete 构造、配置快照、SSL 回退、六类代理、目录基准、ASCII、连接重试、复合操作断开 | 本地完成，真实服务端验证未运行 |
| R04 | v2 结构先行、异步逐行读写、独立行记录、预算和取消/资源契约 | 完成 |
| R05 | 四种表内行状态、Original/Current/Proposed、错误与约束恢复、后续接受/拒绝/取消编辑 | 完成 |
| R06 | 中文文档、消费者、跨框架运行、独立审查、最终集成与性能证据 | 检查完成；全量运行有四项时序失败 |

完整 FTP 模式明确拒绝后端无法实现的独立 SendTimeout/同步 DisconnectTimeout，没有伪造超时中止能力。旧构造保留 Legacy。C02 未静默改为批量检查点或改变公开快照格式，尚未引入增量日志；当前持久化场景无法分离刷盘/替换与完整 JSON 序列化的耗时，未满足计划中确认主要瓶颈的前提。

## 本地验证证据

原始日志、TRX、跨框架阶段退出码统一位于 `artifacts/capability-refinement-2026-10-23`。

| 范围 | 结果 | 证据 |
|---|---|---|
| 主包首轮全测 net8 | 7,229 通过，5 项失败 | `main-full.trx` |
| 上述失败独立复核 | 24/24 通过；限制测试并行度，未修改测试阈值或相关代码 | `main-failures-recheck.trx` |
| 审查修复后的 v2 定向 | 52/52 通过 | `review-fixes.trx` |
| HTTP 全量 net8 | 1,897/1,897 通过 | `http-full.trx` |
| HTTP 集成 net8 | 120/120 通过 | `http-integration.trx` |
| Net 全量 net8 | 69/69 通过 | `net-full.trx` |
| Core 3.1 主包定向 | 126/126 通过 | `core31-main.trx` |
| Core 3.1 HTTP 分片 | 48/48 通过 | `core31-http.trx` |
| Core 3.1 Net 全量 | 69/69 通过 | `core31-net.trx` |
| 消费者编译 | netstandard2.0/net6/net7/net8 全部成功 | `consumer-build.log` |
| 实际 netstandard 产物 | 14/14 通过，包含 v2 编辑状态与默认小页 | `netstandard-artifact.log` |
| 解决方案多目标构建 | 成功，0 错误、1,518 警告；未扩大历史警告清理范围 | `solution-build.log` |
| 最终解决方案 net8 | 23 个测试项目，12,997 通过、18 跳过、4 失败 | `solution-test-summary.json`、`solution-tests/*.trx` |
| HTTP 两项失败独立复核 | 2/2 通过，未修改缓存代码或测试 | `http-failures-recheck.trx` |
| 性能正式确认 | 前后各一次，原始结果保留，耗时未全面收敛 | `before-confirm.json`、`after-confirm.json` |

主包首轮失败是四个 PowerShell 子进程启动/输出超时和一个旧映射性能阈值；单独复核全部通过。本轮未修改这些无关路径，也未调宽它们的阈值。新增能力回归全部通过。

最终解决方案的四项失败为 `Execute_WhenExitedParentLeavesInheritedPipes_DoesNotWaitForChild` 的两种参数（耗时断言）、`CleanupFailure_IsReportedAndDoesNotStopLoop`（三秒等待超时）和 `IndependentNodesShareOneLeaseAndPublishedSnapshot`（两秒 TTL 场景进入返回 null 的回源委托）。命令两例已包含在此前 24 项独立复核中；HTTP 两例在最终运行后独立复核通过。相关代码、测试与本轮固定基线一致。证据支持测试对调度和时间敏感，尚未将这些失败定位为本轮功能回归；保留原始失败，不将单独通过替换为全量通过。

当前状态：实现与独立审查完成，确认的新代码缺陷 `OPEN_ACTIONABLE = 0`；定向、跨框架和消费者验证 PASS，整套本地测试 PARTIAL，远程 CI NOT_VERIFIED，发布验收未通过。C02 分配改善已验证，检查点及合并耗时仍作为后续专项，不继续反复跑分。

## 审查与修复

主代理负责表格/调度设计、流生命周期、集成和 FTP 复核；独立 luna_worker 完成小页和 FTP 范围实施，并只读审查主代理的表格与分片代码。未同时运行共享输出构建。

| Finding | 处理 | 最终状态 |
|---|---|---|
| Proposed 在 ItemArray 恢复时被提交 | 改为 BeginEdit 后逐列赋值；完整状态及 Accept/Reject/Cancel/End 对照 | CLOSED |
| 流 Dispose 异常覆盖处理或取消异常 | 已有主异常时清理不覆盖；成功时清理失败仍传播 | CLOSED |
| v2 流读取约束异常类型与文本不同 | 统一为 JsonSerializationException，取消保留 | CLOSED |
| 单元格预算对转义 JSON 的语义不明确 | 明确紧凑重新编码大小；原始行预算另行生效，增加转义回归 | CLOSED（契约澄清） |
| FTP 无参目录查询忽略基准、目录传输忽略失败状态 | 完整模式返回配置基准并检查传输结果；旧模式保持 | CLOSED |

独立复审确认前述表格异常修复闭合，未发现其引入的新问题。对分片超长输入的疑似回归已查明不成立：StreamTransfer 达到限额后探测一个额外字节并拒绝超限，撤回该 finding。

## 边界

Windows 本地实现与回归证据不等同于远程 CI 或发布验收。Linux 文件提交行为、真实 FTP/FTPS/代理及服务端 ASCII 行尾转换尚未运行。ImageSharp 既有高危依赖仍按前轮记录保留，未升级依赖、扩充白名单或宣布发布门禁通过。

表格完整编辑状态限定为当前类型/结构白名单内的表内行版本与错误，不包含事件、DataView、脱离行、任意 CLR 类型、表达式或内部自增计数。逐行接口与完整对象接口的内存差异、预算解释和所有权见 [调用指南](../reference/capability-refinement-guide.md)。

未提交、暂存、推送或发布代码。性能前后数据单独见 [性能报告](capability-refinement-performance.md)，不将正确性通过解释为所有耗时改善。
