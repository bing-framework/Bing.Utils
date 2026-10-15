# 六类边界检查清单

对应 [覆盖计划](test-coverage-map.md) P2-3。新增或变更 API、按文件补测时，以本次明确的成员范围逐项检查；正常路径仍须保留。测试框架沿用 xUnit、Shouldly 和已有必要的 Moq。

## 使用方式

1. 先读取实现方案、源码契约和已有测试，记录本次成员、目标项目及不在范围内的行为。
2. 按下表核对六类边界，再填写后面的记录模板。已有有效用例直接引用，只有实际缺口才补测。
3. 按主要断言目标选择项目：模块逻辑归属专属 `*.Tests`，真实 HTTP、文件等组件协作归属 `*.Tests.Integration`。具体规则见 [测试归属规范](../../.agents/skills/test-project-boundary/refs/test-project-boundary.md)。
4. 预期行为须能追溯到既定方案、接口/XML 契约或确认过的兼容行为。无法确认时标注“待确认”，列明缺少的契约，不按当前实现猜测新规则。
5. 只运行能验证本次改动的最小测试集合；纯清单、模板或文档改动执行链接、编码和差异检查，复用未受影响的代码验证证据。

## 六类检查

| 类别 | 应核对的输入或行为 | 关键断言与注意事项 |
| --- | --- | --- |
| `null` | 参数、集合元素、回调或依赖为空 | 按契约验证异常类型和参数名，或合法空结果；不可把 null 一律视为非法 |
| `empty` | 空字符串、空白、空集合、空流 | 分别确认行为；必要时验证空输出、无副作用或合法空内容，不能默认空白与空串等价 |
| `invalid` | 非法格式、越界参数、重复键、不可读流或错误状态 | 验证精确错误语义，以及未发送请求、未覆盖原文件等相关副作用；避免只断言“抛任意异常” |
| `overflow` | 数值极值、长度/容量临界、序列回绕 | 按实际算法选边界及相邻值；验证拒绝、进位或等待策略。时间相关逻辑优先使用已有受控时钟，不制造巨量输入 |
| `concurrent` | 明确支持并发的共享状态、独立请求隔离 | 验证唯一性、内容隔离及资源清理；需要重叠执行时用屏障证明。已知不支持的同实例并发不能被写成支持契约 |
| `perf` | 热路径、分配、算法复杂度或性能夹具是否变化 | 无相关变化时复用已有证据或说明不适用；有变化时只测受影响负载。未批准预算不能自行设阈值，也不自动新增基线、探针或 CI 组合 |

## 可复制记录模板

每次补测输出六行，不能因为已有 happy path 就省略类别。占位符在执行时填写，不表示已有测试通过。

```markdown
- 原 TODO：<编号及计划路径>
- 本次成员：<源码路径、类型/函数>
- 目标测试项目：<现有项目路径>
- 契约依据：<方案、接口/XML 或兼容记录>

| 类别 | 适用性及依据 | 输入、预期与测试函数 | 状态及验证证据 |
| --- | --- | --- | --- |
| null | <适用/不适用/待确认及理由> | <输入、结果/异常、副作用> | <状态、命令/结果或阻塞原因> |
| empty | <同上> | <同上> | <同上> |
| invalid | <同上> | <同上> | <同上> |
| overflow | <同上> | <同上> | <同上> |
| concurrent | <同上> | <同上> | <同上> |
| perf | <同上> | <同上> | <同上> |
```

状态使用以下分类：

- `CLOSED`：实现及所需验证证据齐全；复用结果时说明未受影响的范围、框架及环境。
- `OPEN_ACTIONABLE`：契约和验收标准明确，当前范围内确有可补的缺口。
- `NOT_APPLICABLE`：本次成员不涉及该类别，并有具体依据；测试困难或尚未覆盖不是不适用理由。
- `BLOCKED_APPROVAL`：缺少明确的行为决策、性能预算或支持边界；说明“待确认”的具体问题。
- `BLOCKED_EXTERNAL`：需要当前不可用的 CI、运行时或外部环境；不能用重复本地执行代替。
- `ACCEPTED_LIMITATION`、`VERIFIED_BOUNDARY`、`DEFERRED`：只引用已有明确决定或受控证据，并记录影响及后续条件。

性能预算未定只阻塞相应性能验收，不自动阻塞独立的正确性补测。只确认 benchmark 入口存在，不能标为性能验证通过。

## 现有用例参考

以下仅展示具体场景，不表示相应模块已全量覆盖六类边界，也不要求复制现有测试。

| 类别 | 现有入口 | 实际覆盖内容 |
| --- | --- | --- |
| null | [SplitterTest](../../tests/Bing.Utils.Text.Tests/Bing/Text/Splitters/SplitterTest.cs) 的 `FixedLength_WithEmptyInput_ReturnsEmpty` | null 输入下三种分割结果均为空 |
| empty | 同一文件的 `On_WithEmptyOrWhitespaceInput_ReturnsEmptyList` | 空串、空白输入得到空列表 |
| invalid | 同一文件的 `FixedLength_WithNonPositiveLength_ThrowsArgumentOutOfRangeException` | 零/负长度，验证异常类型、`ParamName` 和 `ActualValue` |
| overflow | [SnowflakeProviderBoundaryTest](../../tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/SnowflakeProviderBoundaryTest.cs) 的 `NextId_SequenceOverflow_UsesNextMillisecondAndResetsSequence` | 受控时钟下序列 4095→0、毫秒进位及 ID 唯一性 |
| concurrent | [HttpUploadReliabilityTest](../../tests/Bing.Utils.Http.Tests.Integration/Http/HttpUploadReliabilityTest.cs) 的 `FileContent_ConcurrentRequestsWithSameFileName_KeepIndependentContents` | 屏障保证两个请求同时到达，核对各自落盘字节 |
| perf | [ReflectionBenchmarks](../../benchmarks/Bing.Utils.Benchmark/Benchmarks/ReflectionBenchmarks.cs)；[Program](../../benchmarks/Bing.Utils.Benchmark/Program.cs) 中的 `BenchmarkSwitcher` | 已有性能夹具入口；本清单不代表已执行或已有批准阈值 |

## 验证与收口

- 修改测试：先编译对应项目、运行本次类/函数定向测试；共享测试设施变化时再扩大到受影响套件。
- 改变公开契约、跨项目调用或组件协作时，按影响补相关消费者或集成验证，不默认全解决方案、全框架或全资源矩阵。
- 失败要区分本次问题与已有环境问题；保留真实结果，不跳过 hooks、测试或放宽断言来制造通过。
- 输出本次新增/复用的测试、实际命令和结果、未覆盖项及阻塞依赖；不能将待确认或外部阻塞标成完成。

现有 [单文件补测模板](prompts/test-completion-for-file.prompt.md) 和 [批量补测模板](prompts/test-completion-batch.prompt.md) 均使用本清单；开发与评审入口见 [贡献规范](../CONTRIBUTING.md)。
