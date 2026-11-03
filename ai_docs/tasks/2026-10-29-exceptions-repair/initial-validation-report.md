# Exceptions 最终验证报告

日期：2026-10-29
实现范围：Bing.Utils/Bing/Exceptions。已完成 F01—F14 与 T01/B01 的实施工作，详情见 [实施计划](plan.md)、[问题与场景映射](findings.md) 和 [使用与迁移指南](../../../docs/reference/exceptions-guide.md)。

## 交付结果

- 修复解构、扩展/Tap 调用歧义、LINQ 异常捕获和失败链保留、类型提取、操作结果身份与批量单项隔离。
- 新增四个真实异步入口与协作取消契约，旧同步桥接在受限上下文提前失败。
- 新增显式聚合查找、选项快照和有界格式化；修正诊断类型、HResult、记录时间。
- 回归测试从 88 项扩展为 290 项；包含高参数重载矩阵、真实堆栈/重抛、取消和枚举释放等行为。
- 新增 [消费者](../../../build/ExceptionsConsumer/Program.cs)、[局部探针](../../../benchmarks/ExceptionsProbe/Program.cs) 和 [PowerShell 验证入口](../../../build/Test-Exceptions.ps1)，未增加运行时依赖。

## 验证状态

| 项目 | 结果 | 原始证据 |
| --- | --- | --- |
| 生产四目标构建 | PASS：net8.0/net7.0/net6.0/netstandard2.0 | artifacts/build-results.json；build-*.log |
| 原测试项目五宿主 Exceptions | PASS：各 290/290，无跳过 | artifacts/final-tests-results.json；tests-final-*.log |
| 宽聚合增量验证 | PASS：原用例输入扩为 10001 个分支，2 个节点预算；五宿主各 1/1 定向通过 | artifacts/tests-wide-*.log |
| Guard 受影响用例 | PASS：11/11 | artifacts/tests-guard-net8.0.log |
| 外部消费者 | PASS：net8 宿主显式引用四种生产资产 | artifacts/consumers-four-assets-final.log |
| 同步上下文/调度器 | PASS：独立进程在 20 秒限时内完成，旧桥接未调用委托，新 API 完成真实操作 | artifacts/context-run.log；candidate-context/stdout.log |
| 公开 API 兼容 | PASS：13 个既有公共类型的 241 个方法/访问器/构造函数，无签名、默认参数或泛型约束删除/变化；这些类型新增 14 个成员，另有新增选项类型 | artifacts/api-compatibility.json |
| 主测试项目 net8 全量 | PARTIAL：7876 通过、2 失败、0 跳过，总计 7878 | artifacts/tests-net8.0-full-final.log |
| 文件检查 | 37 个源码/测试/指南/脚本文件 UTF-8 解码无异常；定向 git diff --check 通过 | 本次工具结果 |
| 外部发布 | NOT_REQUESTED；未提交、推送或发布 | 用户执行约束 |

消费者运行日志输出实际程序集路径与 TargetFramework，脚本同时检查复制 DLL 与显式输入相同，避免只在命令中指定 TFM 却加载另一个同名 DLL。覆盖率仅包含 Bing.Exceptions，最终行覆盖 1089/1427（76.31%），分支覆盖 576/954（60.37%）；原基线为 19.97% / 17.24%。覆盖率用于定位缺口，F01—F14 的关闭基于具体断言。

### 全量套件的范围外失败

最终一次完整测试仍有两项失败：

1. Bing.Date.NoRepeatTimeStampFactoryTest.TimeStampObjectMethods_ConsecutiveCalls_EnsureNoRepeat：期望 100 个不同 Unix 时间戳，实际为 1。
2. Bing.Date.DateTimeRangeTest.Test_Properties：期望 ThisMonth.StartTime.Day 为 1，实际为 30。

这些测试及其日期实现不属于本次 Exceptions 修改范围，本任务未修改它们。不得把本模块通过表述为整个仓库通过。

第一次全量运行曾为 7846 通过、21 项日期回归失败；冻结主包基线在隔离宿主复现完全相同的 21 项失败，名称集合差异为 0（artifacts/full-suite-baseline-comparison.json）。随后其他日期工作修改了源码与测试，故最后追加一次全量验证，得到上述当前结果。保留历史日志，不把旧 21 项失败与当前 2 项混为同一状态。

## 性能与资源观察

环境：Windows x64、.NET 8.0.31、Release；每场景 3 次预热、10 个正式样本取中位数。原始结果为 [基线 JSON](artifacts/baseline-exceptions-net8.json) 与 [候选 JSON](artifacts/candidate-exceptions-net8.json)。输入、运行时、探针逻辑一致；新异步入口在基线不存在，BEFORE=NOT_APPLICABLE。

| 场景 | 基线分配/次 | 候选分配/次 | 解释 |
| --- | ---: | ---: | --- |
| Try.Create 成功 | 24 B | 24 B | 包装成功值未增加分配 |
| Try.Create 失败 | 528 B | 528 B | 包含真实抛出与包装成本 |
| 失败 Map 短路 | 296 B | 112 B | 复用原包装，减少重复异常分配 |
| typed/untyped Unwrap 单链 | 0 B | 0 B | 默认查找不创建集合 |
| FormatMessage 深度 1 | 952 B | 1472 B | 完整短输出；新增预算和字段开销 |
| FormatMessage 深度 10 | 7664 B | 10032 B | 完整短输出；双方内容字段不同，不声称逐字相同 |
| InvokeRange 成功，128 项 | 4264 B | 5504 B | 单项隔离与新迭代器有固定/按项开销 |
| FormatMessage 深度 1000 | 22312840 B | 66096 B | 候选按默认深度截断，属于资源边界比较 |

完整深度 1/10 短链的中位时间分别为 30.95→24.05 μs、55.35→25.15 μs。深度 1000 的各自默认策略为 10188.60→167.75 μs，输出由 5,056,899 降为 11,173 个 UTF-16 字符。深链候选只输出预算内节点，因此不能据此宣称同样完整输出语义下的算法加速倍数。

新 CreateAsync/InvokeTaskAsync 的已完成任务场景分别观测约 0.39/0.88 μs；未完成场景包含 Task.Delay(1) 的实际完成延迟，约 14.6/14.8 ms。异步分配统计包含测量包装及进程级分配影响，不把这些值作为 API 独立成本或 CI 阈值。

固定输入观察未发现阻止本次修复采用的局部性能问题；短链格式化和 Range 分配增加已披露。硬验收为确定性长度/节点/深度边界与正确行为，没有预设生产容量或纳秒级门槛。

### 格式化预算的准确含义

默认最大输出 32768 个 UTF-16 字符，包括换行和截断标记；深度 64、不同异常节点 256、缩进 8 层。聚合子项按需读取，AggregateException.Message 不被用来一次拼接所有子消息。

Exception.StackTrace 由运行时提供完整字符串，读取时可能分配该字符串。输出预算不限制运行时或自定义 getter 的成本，也不是整个调用的总内存硬上限；本库只按剩余预算追加，未截断时保留原始运行时堆栈文本。HideStackTrace 不等于消息脱敏，独立的异常方法字段仍保留。

## 审查与收口

独立审查发现三处测试缺口和一处资源预算表述边界，已定向补正。之后只验证变化影响的部分；中文注释和文档变化复用既有行为、消费者及性能证据，不重跑性能探针。公开 API 既有 CLR 签名保持；已批准的文本输出、成功操作身份、旧桥接受限上下文和取消语义变化记录在迁移指南。

IMPLEMENTATION_STATUS=PASS
TEST_STATUS=PASS（Exceptions/Guard/消费者），PARTIAL（主项目全量）
PERFORMANCE_EVIDENCE_STATUS=OBSERVED
RESOURCE_EVIDENCE_STATUS=VERIFIED_BOUNDARY（输出/遍历预算）
EXTERNAL_GATE_STATUS=NOT_APPLICABLE
RELEASE_STATUS=NOT_REQUESTED
OPEN_ACTIONABLE=0
GOAL_STATUS=COMPLETE_LOCAL_SCOPE

范围外日期失败独立记录，不进入 Exceptions 自动修复循环。本次没有自动提交、推送、包发布或框架/依赖升级。
