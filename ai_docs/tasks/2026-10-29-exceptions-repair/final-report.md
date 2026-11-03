# Exceptions 最终验证报告

日期：2026-10-29。完整修复方案和本次差距补正均已落实，F01—F14、T01/B01 与新增性能证据项 P01 已关闭，OPEN_ACTIONABLE=0。

本轮修复了达到不同异常节点预算时误判重复引用的问题，并补齐此前缺失的回归测试、旧异步入口消费者调用与性能场景。实现、逐项行为断言和性能比较见 [差距修复报告](gap-fix-report.md)，状态见 [findings.md](findings.md)，执行记录见 [execution-log.md](execution-log.md)。

## 本轮交付

- 遍历器接纳不同异常实例前执行预算限制；重复引用只输出标记，输出满后停止推进枚举器。
- 成功/失败二元与三元解构一致性，四个异步入口的真实执行中取消，两个旧格式化入口的默认长度上限，宽聚合和真实长堆栈的长度截断，完整诊断字段。
- 四资产消费者实际编译并执行旧 InvokeAsync(Task<Action>)。
- 成功 Map、有限宽度聚合的性能记录及明确截断原因；统计工作移到采样之外。
- 更新中文注释、[使用指南](../../../docs/reference/exceptions-guide.md)、[实施计划](plan.md)和验证报告。

## 验证结果

| 范围 | 结果 | 证据 |
| --- | --- | --- |
| Exceptions 五个宿主 | PASS：net8.0/net7.0/net6.0/net5.0/netcoreapp3.1 各 304/304，无跳过 | [test-results.json](artifacts/gap-fix/test-results.json) |
| 复核后追加的解构断言 | PASS：五宿主各 1/1；netcoreapp3.1 首次构建 MSB4166，单节点重试通过 | [deconstruction-test-results.json](artifacts/gap-fix/deconstruction-test-results.json) |
| 生产四目标构建 | PASS：net8.0/net7.0/net6.0/netstandard2.0 | [build-results.json](artifacts/gap-fix/build-results.json) |
| 四种实际资产消费者 | PASS：编译、执行、复制资产与实际目标框架均核对 | [consumers-four-assets.log](artifacts/gap-fix/consumers-four-assets.log) |
| 性能记录 | OBSERVED：冻结基线 22 场景，候选 27 场景 | [baseline-net8.json](artifacts/gap-fix/baseline-net8.json)、[candidate-net8.json](artifacts/gap-fix/candidate-net8.json) |
| 截断原因分类 | PASS：实际输出的无截断/深度/节点/长度/多原因五项 | [truncation-reasons.json](artifacts/gap-fix/truncation-reasons.json) |
| 独立复核 | 已确认 F13 修复及 T01 契约缺口关闭；harness 由主代理独立复核 | [review-summary.md](artifacts/gap-fix/review-summary.md) |

所有新增回归均在 Exceptions 所属主包测试项目中。末轮只增加已有用例的断言，按影响范围重跑该用例，复用其他有效证据；没有以数量或覆盖率替代行为验收。

## 证据复用及全量状态

此次未更改异步生产逻辑、同步上下文保护、Guard 单链调用或公开签名，因此复用上一阶段的 Guard 11/11、20 秒受控上下文/调度器宿主及 241 个既有成员兼容性证据。相关原始日志和 API JSON 保留在 artifacts 中。

本轮未重跑主测试项目全量。上一阶段完整测试是 7876 通过、2 项日期模块失败；它只是历史快照，不代表当前并行工作区的全仓状态。原始结果、日期失败详情及旧覆盖率保存于 [初次验收报告](initial-validation-report.md)。本任务未修改日期模块。

## 性能与资源结论

成功 Map 分配为 208→208 B/次，失败 Map 为 296→112 B/次。候选显式展开 16 个聚合分支，16 条消息完整输出，共 1975 个 UTF-16 字符，分配 11072 B/次。

完整短链格式化仍有预算机制和额外字段开销：深度 1/10 分配为 952→1480 B、7664→10040 B。深度 1000 的默认输出为 5056899→11173 字符、分配 22312840→66104 B；候选已按默认深度截断，这一差值不能解释为相同完整输出语义下的加速。时间、完整输入及输出统计见差距修复报告，没有将单机耗时设为 CI 阈值。

资源验收约束输出字符、不同节点、深度和缩进。Exception.StackTrace 及自定义 getter 仍可能先生成完整数据，预算不是整个调用的总内存上限；真实长堆栈的输出长度已通过断言验证。

IMPLEMENTATION_STATUS=PASS
TEST_STATUS=PASS（本轮定向范围）；NOT_RUN（本轮主项目全量）
PERFORMANCE_EVIDENCE_STATUS=OBSERVED
RESOURCE_EVIDENCE_STATUS=VERIFIED_BOUNDARY
RELEASE_STATUS=NOT_REQUESTED
OPEN_ACTIONABLE=0

未提交、推送或发布；未升级框架或依赖。
