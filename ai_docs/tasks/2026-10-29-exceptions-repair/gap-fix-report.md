# Exceptions 差距修复报告

日期：2026-10-29。对应用户要求修复的 F13、T01 和性能证据缺口 P01。原始冻结基线保持不变，本轮证据位于 [artifacts/gap-fix](artifacts/gap-fix/)。

## 修复内容

| 问题 | 本轮处理 | 行为证据 |
| --- | --- | --- |
| F13 不同节点预算误判重复引用 | 在遍历器接纳不同节点前检查预算；重复引用不占预算，只有下一个不同实例才产生节点截断；输出满后不再 MoveNext | 两个入口、两种边界输入，修复前 4/4 失败，修复后全部通过；预算外消息读取会抛错的夹具未被访问 |
| T01-03 解构覆盖不完整 | 成功与失败均比较二元/三元解构的值、状态和异常身份 | SuccessDeconstruction_PreservesValueAndClearsException；FailureDeconstruction_PreservesFailureWithoutThrowing |
| T01-14 缺真正执行中取消 | 四个入口均先启动未完成操作，再取消；断言底层任务 Canceled、原异常/token/cause 保留、外层 RanToCompletion | RunningCancellation_PreservesOriginalFailureAndCompletesOuterTask 两种重载组合 |
| T01-16 旧入口缺消费者调用 | 外部消费者编译并执行 InvokeAsync(Task<Action>)，检查 Action 只执行一次且成功 | 四种实际生产资产的消费者均通过 |
| T01-18 预算场景不足 | 两个旧入口默认截断长消息；宽聚合触发字符预算；真实递归调用生成的长堆栈按指定长度截断 | 明确断言长度、具体截断原因、堆栈字段；不是用未抛出异常模拟堆栈 |
| T01-19 诊断字段不足 | 详情断言明确的 Source、原始抛出方法与内部异常消息 | Details_PreserveSourceThrowingMethodAndInnerMessage |
| P01 性能与统计缺口 | 增加成功 Map、16 分支聚合默认入口和显式展开；截断原因分类与可见消息计数在采样后执行 | 冻结基线 22 场景、候选 27 场景；真实输出的五类原因检查通过 |

生产变更局限于内部格式化/遍历及选项注释，没有新增或修改公开 CLR 签名。补充测试属于主包 Exceptions。消费者和探针由 luna_worker 独立修改，主代理集成验证。

## 本轮验证

| 验证范围 | 结果 | 证据 |
| --- | --- | --- |
| Exceptions 五宿主 | net8.0、net7.0、net6.0、net5.0、netcoreapp3.1 各 304/304，无跳过 | [test-results.json](artifacts/gap-fix/test-results.json) |
| 四目标生产构建 | net8.0、net7.0、net6.0、netstandard2.0 全部成功 | [build-results.json](artifacts/gap-fix/build-results.json) |
| 四资产消费者 | 全部通过，核对复制资产及实际 TargetFramework | [consumers-four-assets.log](artifacts/gap-fix/consumers-four-assets.log) |
| 性能探针 | baseline/candidate 均完成，加载资产与指定输入一致 | [baseline-net8.json](artifacts/gap-fix/baseline-net8.json)、[candidate-net8.json](artifacts/gap-fix/candidate-net8.json)、[probe-assets.json](artifacts/gap-fix/probe-assets.json) |
| 截断分类 | 无截断、深度、节点、长度、多原因共 5 项通过 | [truncation-reasons.json](artifacts/gap-fix/truncation-reasons.json) |
| 独立复核后的增量断言 | 失败二元/三元解构比较已补，五宿主最终各 1/1 通过；netcoreapp3.1 首次 MSBuild 子节点退出，串行重试成功 | [deconstruction-test-results.json](artifacts/gap-fix/deconstruction-test-results.json) |

新回归共 14 个用例。测试数量仅说明执行范围，关闭依据是表中的具体行为断言。独立复核后的最后变化只增加现有失败解构用例的两项断言，因此仅定向验证该用例，不重复全套或性能。

## 性能观察

同一版探针、固定输入，Windows x64 / .NET 8.0.31 / Release，3 次预热、10 次采样取中位数。新表的格式化统计位于采样外，不将时间与旧版探针结果直接拼接。

| 场景 | 基线分配/次 | 候选分配/次 | 基线/候选输出字符数 | 解释 |
| --- | ---: | ---: | --- | --- |
| 成功 Map | 208 B | 208 B | 不适用 | 中位耗时约 0.063 / 0.053 μs，仅供观察 |
| 失败 Map | 296 B | 112 B | 不适用 | 保留原包装后的分配减少 |
| 格式化深度 1 | 952 B | 1480 B | 69 / 90 | 完整输出；字段与预算机制有成本 |
| 格式化深度 10 | 7664 B | 10040 B | 1059 / 1365 | 完整输出；双方字段不同 |
| 格式化深度 1000 | 22312840 B | 66104 B | 5056899 / 11173 | 候选按默认深度 64 截断，原因 depth_limit |
| 聚合 16 分支，默认入口 | 3992 B | 2464 B | 512 / 205 | 基线 AggregateException.Message 拼入所有分支消息；候选摘要后按默认单链只显示首分支，不能当作相同完整输出比较 |
| 聚合 16 分支，显式展开 | 不适用 | 11072 B | 不适用 / 1975 | 16 条分支消息全部可见，未截断；基线没有该 API |

本次完整短链深度 1/10 的耗时为 5.10→12.05 μs、12.05→16.35 μs；深度 1000 为 3670.30→71.80 μs。短链存在预算和额外字段开销，深链下降包含默认截断效果，不据此宣称同语义性能倍数或设置 CI 纳秒阈值。

ExceptionNodeCount 表示输入异常数；聚合 OutputCount 表示输出中完整可识别的分支消息数，不代表实际遍历节点数。原因码为 depth_limit、node_count_limit、output_length_limit，多原因用分号连接，无截断为 null。

## 证据范围与复核

复用未受影响的 Guard 11/11、独立上下文/调度器宿主和既有公开 API 兼容证据。本轮没有重跑主测试项目全量；[初次验收快照](initial-validation-report.md)中 7876 通过、2 项日期失败是此前快照，不能视为当前工作区的全仓结论。

探针首次运行因扫描全部导出类型而误触发 Newtonsoft.Json 解析，现已改为精确发现 Exceptions 类型，未增加依赖。失败日志保存在 baseline-discovery-failure.log；之后两份实测成功。

独立复核确认 F13 的准入顺序、重复引用与输出停止行为正确。复核提出的失败二元/三元解构一致性断言已补，复核者确认契约缺口已消除；五宿主最终定向验证均通过，F13、T01 与 P01 已关闭。

输出/遍历预算仍不约束运行时 StackTrace 或自定义属性 getter 的总分配。真实长堆栈的最终文本长度有确定性断言。未改框架、依赖和日期模块，未提交、推送或发布。
