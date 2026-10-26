# 流处理与资源管理实施记录

日期：2026-10-24。依据[本轮计划](stream-platform-plan.md)继续当前含未提交成果的工作区；未升级依赖、调整目标框架或发布版本，未暂存、提交、推送或发布。

## 交付范围

| 编号 | 状态 | 已实现能力 |
|---|---|---|
| S01 | DONE | 文件监控 StopAsync/DisposeAsync、错误通知、有界 debounce、缓冲/队列溢出快照重扫及停止屏障 |
| S02 | DONE | 临时文件列表、重开、调整额度、保留保护、显式删除、同文件系统原子提交、可取消清理 |
| S03 | DONE | 按内存阈值溢写的可定位流，总长度预算及释放删除 |
| S04 | DONE | 多下载全局/主机并发、有界队列、聚合进度、暂停恢复持久磁盘预算 |
| S05 | DONE | 明确完成/失败范围/恢复位置的结果，传输字节、限速/排队等待、检查点/合并诊断 |
| S06 | DONE | 通用 JSON 数组逐项读取，短读/不可定位、单值/总输入预算、格式错误位置 |
| S07 | DONE | CSV/TSV 逐行读写，多行引用、表头映射、类型转换、行列错误及字节预算 |
| S08 | DONE | 异步序列数量/时间分批、有界预取、提前释放与取消收尾 |
| S09 | DONE | SSE 解析/写入、HttpClient 消费、显式重连、Last-Event-ID、ASP.NET 响应写入 |
| S10 | DONE | 可选异步 FTP 目录接口、元数据/校验值、递归传输、移动/删除及冲突策略 |
| S11 | DONE | 数据库结构元数据、多结果顺序消费、二进制/文本大字段逐块复制 |
| S12 | DONE | 命令 stdin、实时 stdout/stderr、回调背压、有界捕获、失败终止及取消收尾 |
| S13 | DONE | UUIDv7、毫秒时间提取、正确 Guid 端序及现有框架兼容 |
| S14 | DONE | 基线诊断确认重复扫描成本，新增单次扫描/根锁内批量预留并完成正式对照 |
| S15 | DONE（本地） | 中文指南、消费者编译、实际 netstandard、完整回归及独立审查；全量失败与定向复验分别记录 |

调用方说明见[使用指南](../reference/stream-platform-guide.md)。旧 `IHttpRequest`、`IFtpAsyncClient` 未增加必需成员；新增 HTTP/FTP 能力通过扩展及可选接口接入。

## 固定基线与影响

证据根目录：`artifacts/stream-platform-2026-10-24/`。实施前保存了 208 个相关源码和产物文件；`baseline/manifest.json` 记录当前脏工作区身份、SDK 8.0.425、运行时、输入种子 20261024 和 SHA-256。没有用旧 HEAD 替代当前工作区作为优化前基线。最终身份在 `candidate.json`。

影响项目为主包、HTTP、Net、IdUtils 及对应测试；不迁移无关测试，不修改已有图像/反射性能门槛。C02 的正文复制和合并算法保持原策略，新增诊断和显式额度检查位于外围；已有[写盘与合并证据](c02-io-performance.md)只复用于未变化的复制路径。

## 测试证据

| 检查 | 结果与证据 |
|---|---|
| 主包新增路径定向 | 首轮 52 项通过；生命周期最终 17 项通过，`core-streams.log` / `lifecycle-final.log` |
| HTTP 新增路径定向 | 最终 11 项通过，`http-streams-final.log` |
| FTP 目录 / UUIDv7 / 真实 HTTP 额度恢复 | 5 / 8 / 1 项通过，分别见 `ftp-directory.log` / `uuid7.log` / `download-quota.log` |
| 主包 net8 完整 | 最终 7305 通过；首次 7304 通过、1 个既有 Mapper 耗时门槛失败，原始证据保留 |
| HTTP net8 完整 | 1922 通过，`Bing.Utils.Http.Tests-full.log` |
| HTTP net8 集成 | 122 通过，`Bing.Utils.Http.Tests.Integration-full.log` |
| Net / IdUtils net8 完整 | 74 / 349 通过，项目 `*-full.log` |
| 四框架消费者 | netstandard2.0/net6.0/net7.0/net8.0 编译通过，0 错误、39 警告；`consumer-build.log` |
| 实际 netstandard2.0 | .NET Core 3.1 下主包 44、HTTP 11、集成 1、Net 4、IdUtils 8 项通过；监控最后兼容调整的 7 项再次通过；`netstandard.log` / `netstandard/identity.json` |
| 解决方案 Release 构建 | 首次完整 0 错误、1505 警告；最后兼容调整后增量构建 0 错误、395 警告；`solution-build.log` / `solution-final-build.log` |
| 解决方案 net8 测试 | 23 个程序集、13,127 项：13,108 通过、1 失败、18 跳过；`solution-final-test.log` / `solution-final-tests/` / `test-summary.json` |
| 最后失败用例定向复验 | 取消触发时序修正后，net8.0 与 netcoreapp3.1 各 1 项通过；`retry-cancel-net8.0.log` / `retry-cancel-netcoreapp3.1.log`；未重跑全量 |
| 独立审查 | 异步缓冲、命令 IO、下载调度及临时存储/额度提交审查完成；发现的 pending 残留问题已修复并复核闭合 |

回归涉及旧数据恢复、配额及路径限制、取消令牌身份、停止后不再交付通知、超过 debounce 容量的重扫差异、半途失败保留原目标、分片暂停后占用额度、合并残留清理、不完整 JSON/SSE、CSV 跨块引号及换行、数据库多结果及字段预算、命令消费者失败与收尾、FTP 目录路径越界及策略映射、UUID 时间/版本/变体位。

首次主包全测中的 `Map_LargeNumberOfProperties_ShouldPerformWell` 为现有 10,000 次映射的固定 5 秒断言，当次 6,601 ms；单独诊断通过。本轮未修改 Mapper 实现、该测试或其门槛，原始失败日志及 TRX 保留。没有因为该耗时门禁改动无关代码。

首轮解决方案测试还出现一次 `IndependentNodesShareOneLeaseAndPublishedSnapshot` 失败，其依赖的缓存实现与固定基线一致；HTTP 完整测试及单独诊断均通过。最终解决方案按测试程序集串行执行，避免跨程序集并发争用；保留所有失败与诊断记录，不放宽断言或改动缓存实现。

最终解决方案运行中，以上 Mapper 和缓存用例均通过；唯一失败为既有 `Retry_CancelDuringBackoffStopsFurtherAttempts`：原用例在构造请求前启动 50 ms 取消计时，负载下首次请求尚未发出就已取消，实际请求次数为 0。测试现通过重试观察回调确认首次失败并准备退避后再取消，仍断言只执行一次请求；等待使用 `Task.WhenAny`，避免观察回调退化时永久等待。仅调整该测试，生产代码未改变。随后 net8.0 与实际 netstandard 所在的 netcoreapp3.1 定向通过；最终全量运行的失败记录和退出状态保持原样，没有将定向结果改写成全量通过。18 项跳过均为既有外部 FTP 集成测试。

## 性能与资源

探针：`benchmarks/StreamPlatformProbe`。运行时 .NET 8.0.31，Windows，同一 SDK；固定每个租约 16 字节，10 个租约预热。使用 `GC.GetTotalAllocatedBytes(true)` 统计进程各线程分配，耗时含持久化预留和关闭句柄，不含创建/删除测试根目录；两条路径都保留正文供下一次检查。正式确认串行执行，未同时进行本任务的构建或测试。

| 场景 | 优化前逐个预留 | 批量预留 | 证据 |
|---|---|---|---|
| 100 个租约，正式耗时 | 6,104.57 ms | 804.32 ms | `temp-baseline-confirm.json` / `temp-candidate-confirm.json` |
| 100 个租约，正式总分配 | 45,575,464 B | 295,856 B | 同上 |
| 1,000 个租约，诊断耗时 | 765,881.07 ms | 17,541.51 ms | `temp-baseline.json` / `temp-candidate.json` |
| 1,000 个租约，诊断总分配 | 4,563,359,752 B | 2,921,416 B | 同上 |

1,000 项旧实现诊断期间与构建存在重叠，耗时仅用于定位规模问题，不能作为稳定加速比。正式确认限于 100 项，每组一次诊断、一次正式确认；没有把耗时百分比加入门禁。批量接口减少重复目录扫描和标记读取，独立 `CreateFile` 不使用缓存、仍保留磁盘核对成本，因此不要将数据解释为旧入口自动变快。

其他新能力没有历史等价入口，`BEFORE = NOT_APPLICABLE`。结构资源边界：JSON/CSV 按值或行缓存；SSE 按事件块缓存；DB 字段固定 80 KiB 字节或 4 Ki 字符缓冲；实时命令每管道 4 Ki 字符；异步预取限制队列项数；下载额度保守覆盖分片和合并正文。文件监控重扫快照随目录规模增长，不是常量内存。限制原始字节不等于限制反序列化后对象图。

## 修复与未运行环境

主代理在集成时修复了：重扫差异重新入队导致的恢复事件丢失、删除监控目录后的待处理通知、下载释放/排队唤醒竞态、旧分片与合并残留的额度低估、SSE EOF 控制行处理、跨卷移动复制导致原子提交契约不成立、FTP 点/空格路径规范化边界及跨框架引用问题。独立审查发现额度扩容中断的 `reservation.pending` 会阻止更新及回收；已在根锁/租约锁内恢复清理，并新增重开扩容、显式删除和过期清理回归。

最后核对并保留了旧 `FileWatcher.Stop` 清空目录的契约，再次启动前需要重新 AddPath；net8 与实际 netstandard 用例均通过。

Windows 本地验证与外部 Linux/macOS CI 分开。本轮未触发外部 CI，也未连接真实 FTP 服务器；FTP 协议交互依赖现有 FluentFTP 后端，目录逻辑通过 fake backend 验证。POSIX 原子提交分支等待外部平台测试；Windows 正常提交、拒绝覆盖、失败后重试、跨卷拒绝有本地用例。

ImageSharp 等既有依赖发布风险沿用原状态，未升级依赖、修改白名单或重跑无关发布门禁。代码实现与发布验收分开；只对有直接证据的 OPEN_ACTIONABLE 继续修复。

## 最终状态

| 状态 | 结论 |
|---|---|
| IMPLEMENTATION_STATUS | PASS：S01—S15 本地交付完成 |
| TARGETED_TEST_STATUS | PASS：新增路径及最后修正用例通过；历史失败原始记录保留 |
| BUILD_STATUS / CONSUMER_STATUS | PASS：解决方案 Release 和四框架消费者编译成功，已有警告仍保留 |
| NETSTANDARD_STATUS | PASS：实际产物 68 项通过，最后监控兼容及取消用例另有定向复验 |
| FULL_SOLUTION_RUN | 13,108 passed / 1 failed / 18 skipped；唯一失败的测试时序已修正并定向通过，未重跑全量 |
| OPEN_ACTIONABLE | 0（本轮范围内，无已确认且尚未处理的可执行问题） |
| EXTERNAL_CI / REAL_FTP | NOT_RUN；POSIX 原子提交仍需外部平台执行 |
| RELEASE_STATUS | NOT_VERIFIED；既有 ImageSharp 风险未解决，本轮未发布 |

后续待办仅为外部平台与真实 FTP 验证，以及原有独立依赖迁移事项。本轮停止修复，不因外部未验证或历史发布门禁进入新一轮修改。机器可读证据为 `candidate.json`、`test-summary.json` 和 `netstandard/identity.json`。
