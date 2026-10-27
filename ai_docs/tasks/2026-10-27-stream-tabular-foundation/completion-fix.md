# 审查项补全记录

依据用户“补全整块的内容”的授权，完成审查项和本轮回归暴露的测试问题。原始 review.md 与原始日志保留；当前结论如下。

## Finding 与验证

| ID | 状态 | 实现与验证 |
|---|---|---|
| F01 | CLOSED | 按 ordinal 缺列诊断保留 SourceOrdinal，CSV 一基列号越界时保留行位置；映射与预检回归五框架通过 |
| F02 | CLOSED | 补齐枚举、Guid、日期时间、TimeSpan、URI、布尔、字符、溢出、文化及可空非空值转换；覆盖读取/取消与 Dispose 同时失败、多行 CSV 异常位置 |
| F03 | CLOSED | 更换能在各框架触发回溯的正则测试模式，并先直接断言框架 Regex 超时；工具 API 异常断言保持 |
| F04 | CLOSED | 完整构建成功；核心和 HTTP 项目完整复验通过，未改变范围复用全量通过/跳过证据；汇总包含整组跳过项 |
| F05 | CLOSED | 竞速测试用 TaskCompletionSource 协调启动、胜者放行和真实取消观察，消除固定 Delay 对调度先后的猜测 |
| F06 | CLOSED | 隔离 SaveFile 回归纳入已有 TimingSensitive 集合，保留独立进程、60 秒上限及内容断言；五框架通过 |
| F07 | CLOSED | HTTP 过期使用受控时间且验证持久化 TTL 范围、CorruptEntries=0；锁等待观察实际计数/进程握手；维护轮次使用 TCS；SSE 保留累计时长超过 idle timeout 的心跳行为并隔离调度 |
| F08 | CLOSED | 既有 MapperHelper 性能测试在定向和核心完整复验中通过，原 5000ms 阈值未改 |

`completion_review` 独立只读复核确认代码和断言意图；并核对最终日志。其指出的 TTL 覆盖不足已补强并通过五框架测试，其指出的 54 个整组跳过漏计也已修正。没有以自报实现完成替代验证或独立复核。

## 正则样例的可复现原因

临时无项目依赖的控制台直接调用 `new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(15)).IsMatch(new string('a', 10000) + "!")`：

| 运行时 | 原模式 `^(a+)+$` | 新模式 `^(a|aa)+$` |
|---|---|---|
| .NET 5.0.17 | 约 1ms 返回 false，未超时 | RegexMatchTimeoutException |
| .NET 8.0.31 | RegexMatchTimeoutException | RegexMatchTimeoutException |

原样例在 .NET 5 上不满足“会触发超时”的前提。修复只更换测试样例并补框架对照，没有改动生产正则实现或超时策略。

## 关键证据

- [completion-targeted.log](completion-targeted.log)：五框架各 58 通过、3 失败；三项失败来自新测试用空 CsvRow 违反已有最少一字段约定。该夹具已修正。
- [completion-ordinal-regression.log](completion-ordinal-regression.log)：修正夹具后五框架各 3 通过。核心完整复验也覆盖最终版本。
- [completion-timing-core-targeted.log](completion-timing-core-targeted.log)：线程、隔离文件写入和原性能测试，五框架各 9 通过。
- [completion-timing-http-targeted.log](completion-timing-http-targeted.log)：五个 HTTP 修复用例，五框架各 5 通过。
- [completion-http-expiry-regression.log](completion-http-expiry-regression.log)：最终 TTL 范围和损坏计数断言，五框架各 1 通过。
- [completion-core-full.log](completion-core-full.log)、[completion-http-full.log](completion-http-full.log)：两个受影响项目最终完整复验，均 exit 0。
- [completion-final-build.log](completion-final-build.log)：最终完整 Release 构建 exit 0，0 错误、26 警告。
- [completion-test-summary.json](completion-test-summary.json)：保留原全量失败事实，按未改变范围复用证据，当前汇总 100 个组合、66782 通过、90 既有跳过、0 未解决失败。

当前汇总包含三个网络集成项目的整组跳过：net8.0/net7.0/net6.0 各 18 项，共 54 项。它们没有被排除或计作通过。

## 影响分析

生产增量仅为 TabularMap 缺列诊断位置及 DataIssue 注释，公共签名不变。其余补全和稳定性修复位于 Bing.Utils.Tests、Bing.Utils.Http.Tests；无 Provider、TFM、依赖、打包或 Benchmark 修改。生产源码自本轮完整测试开始后未改变。

测试遵循测试项目归属规则：核心 API 归主包测试，HTTP 逻辑归 HTTP 测试。先运行精准验证，再完整复验受影响项目；其他未改变范围按 GOAL_RULES 复用本轮全量证据。计时阈值和原有跳过策略均保持。

## 当前分类

Completed：F01～F08 与交付文档。Open Actionable：无。Blocked Approval / Blocked External：无本任务阻塞。Not Applicable：性能提升 Benchmark 与发布。Accepted Limitations：原计划的合作取消与阶段最终事件语义。Verified Boundaries：缺列定位、原异常保真、测试真实同步、原性能阈值。Deferred：仅原计划排除项。

本轮停止修复循环；后续报告调整不重跑已经通过且源码未变的测试。
