# Exceptions 问题状态与验收映射

状态日期：2026-10-29。差距复核重新打开的 F13/T01 及新增 P01 已修复并验证；独立复核确认契约缺口消除。OPEN_ACTIONABLE=0。

## 原始 Findings

| ID | 状态 | 实现与关闭证据 |
| --- | --- | --- |
| F01 | CLOSED | Failure 三元解构返回默认值/false/原包装；TryContractRegressionTests.FailureDeconstruction_PreservesFailureWithoutThrowing。 |
| F02 | CLOSED | 泛型扩展只在 ExceptionHelperExtensions；四种产物消费者编译扩展及两个静态调用。 |
| F03 | CLOSED | 全部 CreateFromTask 重载调用前保护上下文；独立进程验证受限 SynchronizationContext 与 TaskScheduler，20 秒超时保护。 |
| F04 | CLOSED | 基类两个 Tap 转发重载；消费者覆盖基类/Success/Failure 的零参、单参、具名双回调及 typed null。 |
| F05 | CLOSED | Success.Bind 捕获组合异常/null Try；六类回调故障、Where 拒绝消息和恢复行为断言。 |
| F06 | CLOSED | Failure.Bind 转发原包装；Map/LINQ 后 wrapper、InnerException、Cause、Code、Flag 不变，回调次数为 0。 |
| F07 | CLOSED | typed Unwrap 接受 Exception；精确/派生/未命中/null/无递归深链测试。 |
| F08 | CLOSED | TryAction.ExceptionAs 沿链或显式聚合查找；原异常/包装/成功/未命中测试。 |
| F09 | CLOSED | SuccessAction 使用引用身份与 RuntimeHelpers.GetHashCode；同一委托执行两次不相等，HashSet 保留独立结果。 |
| F10 | CLOSED | 16 个 InvokeRange 重载按项保护工厂和操作；高参数矩阵、首中尾故障、Cause/顺序/延迟枚举/Dispose 测试。 |
| F11 | CLOSED | 四个真实异步入口；未完成任务、同步故障/异步故障/null、预取消/执行中取消/忽略取消验证。 |
| F12 | CLOSED | 显式聚合开关，根优先左到右 DFS、引用去重；第二/嵌套分支、顺序及默认单链测试。 |
| F13 | CLOSED | 遍历器在接纳不同节点前检查预算，重复引用不占节点数；输出满后不推进枚举器。两入口四边界用例修复前全失败、修复后通过；独立复核确认。详见 gap-fix-report.md。 |
| F14 | CLOSED | 类型与 HResult 分开、记录时间标签；真实 throw/catch 堆栈、隐藏与 EDI 重抛身份断言。 |
| T01 | CLOSED | 五宿主各 304/304；最后追加的失败二/三元解构一致性断言五宿主各 1/1。补齐成功解构、执行中取消、旧入口消费者、默认长度/宽聚合/真实长堆栈与诊断字段，按行为关闭。 |
| B01 | CLOSED | 原项目验证已恢复，四目标构建和五宿主正常；未修改构建配置来绕过历史 XML 阻断。 |
| P01 | CLOSED | 成功 Map、16 分支聚合默认/显式入口、采样外统计和原因码齐全；同版探针基线 22/候选 27 场景完成，五类实际输出分类通过。 |

## 独立审查与补正

| ID | 状态 | 处理结果 |
| --- | --- | --- |
| R01 | CLOSED | 明确运行时 StackTrace 读取可能生成完整字符串；MaxOutputLength 不约束 getter 的总分配。未截断时保留运行时原始堆栈文本；避免对异常/内部异常调用无界 ToString。审查者已确认预算说明边界。 |
| R02 | CLOSED | 补齐 2—16 参数操作在首/中/尾项失败后的继续执行与原异常/cause；同时保留参数工厂首/中/尾失败用例。 |
| R03 | CLOSED | 常量及三类 TryGetValue fallback 固定成功值、失败回退、调用次数、布尔结果及回调再次抛错时的默认值。 |
| R04 | CLOSED | Where(false) 断言 InvalidOperationException 及 Predicate not satisfied。 |
| X01 | NOT_APPLICABLE | 初次验收全量快照的 2 项日期模块失败属于本任务范围外，详见 initial-validation-report.md；本轮未重跑全量，本任务未改日期源码，不计入 Exceptions 自动修复循环。 |

## T01 的 23 类场景

| 场景 | 主要证据 |
| --- | --- |
| 01 泛型扩展及静态调用 | ExceptionsConsumer.RunConsumerChecksAsync |
| 02 Tap 各静态类型/typed null | ExceptionsConsumer.ExerciseTap 三个重载；TryContractRegressionTests |
| 03 成功/失败解构 | SuccessDeconstruction_PreservesValueAndClearsException；FailureDeconstruction_PreservesFailureWithoutThrowing；均核对二元/三元一致性 |
| 04 LINQ 回调抛错 | Composition_CapturesCallbackException 六项 Theory |
| 05 LINQ 参数/null Try/拒绝 | Composition_ValidatesArgumentsAndDoesNotCaptureTapOrMatch；SelectMany_NullResultBecomesFailure；Where_RejectionUsesExpectedFailureTypeAndMessage |
| 06 失败短路身份和原因 | FailureComposition_PreservesWrapperAndCauseWithoutCallingCallbacks |
| 07 类型查找 | ExceptionTraversalTests 与原 ExceptionHelperTests |
| 08 TryAction 原异常提取 | ExceptionTraversalTests 与原 TryActionTests |
| 09 成功操作相等性 | SuccessAction_UsesResultIdentity |
| 10 批量首中尾隔离 | TryOverloadTests 的 arity 矩阵及两类故障位置 Theory |
| 11 批量枚举与释放 | Range_ValidatesConfigurationBeforeEnumeration；Range_IsLazyAndDisposesTheSourceOnStopOrFailure |
| 12 真实异步成功时机 | TryAsyncTests 未完成 TaskCompletionSource 用例 |
| 13 真实异步故障/null | TryAsyncTests 同步/任务故障及四入口 null 用例 |
| 14 协作取消 | RunningCancellation_PreservesOriginalFailureAndCompletesOuterTask 覆盖四入口执行中取消；保留预取消与忽略取消用例 |
| 15 上下文与调度器 | 独立消费者 context 模式，20 秒进程树超时 |
| 16 旧 Task<Action> 契约 | TryAsyncTests 旧异步用例；ExceptionsConsumer.RunConsumerChecksAsync；四资产实际编译执行 |
| 17 聚合顺序/去重/关闭 | ExceptionTraversalTests；ExceptionFormattingTests |
| 18 输出预算 | 两旧入口默认深链/长消息；64/128/1024 长度；宽聚合字符预算；真实长堆栈；不同节点/重复引用；深度/缩进 |
| 19 真实诊断字段 | Details_PreserveSourceThrowingMethodAndInnerMessage；Details_ShowTypeAndHResultWithoutCallingExceptionToString |
| 20 真实堆栈/重抛 | StackVisibility_UsesARealThrownException；Rethrow_PreservesIdentityAndOriginalLocation |
| 21 高参数重载 | 四工厂家族 0—16 参数成功/故障矩阵及 Range 1—16 参数矩阵 |
| 22 异步取值/恢复/fallback | ValueAccessAndRecovery_PreserveFailureContract；原恢复测试；TryGetValue_FallbackOverloadsPreserveResultSemantics |
| 23 资产兼容 | 四 TFM 构建、四资产消费者、公开签名对比（241 个既有成员无移除/变更） |

本次自动审查补正仅处理上述可执行差距。尚无生产 SLA、外部发布未执行和范围外全量失败均不触发新的 Fix 轮。
