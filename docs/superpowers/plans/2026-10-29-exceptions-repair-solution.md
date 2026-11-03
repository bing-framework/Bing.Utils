# Exceptions 修复与测试补强方案

日期：2026-10-29
用途：将本轮 Exceptions 审查结果转为后续可执行的实现任务、兼容性约定和验收标准。
状态：方案已实施；最终契约、问题状态和验证结果见 [实施报告](../../../ai_docs/tasks/2026-10-29-exceptions-repair/final-report.md)。下文的审查基线与阻断描述保留为历史证据，不代表当前状态；未执行发布。
推荐实施顺序：确认构建与基线 → 修复公开调用及失败传播 → 补齐异步与异常遍历 → 限制格式化资源 → 完成消费者、测试与性能验收。

## 1. 目标与范围

目标是修复已复现的编译歧义、失败解构异常、同步等待死锁、失败信息丢失和异常逃逸；补充真正的异步操作、多分支聚合异常查找与有限输出能力；用测试固定公开契约。

生产范围为主包的 [Exceptions 目录](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Exceptions)。当前包含 17 个源码文件：异常辅助与格式化、Try 工厂、`Try<T>` 成功/失败结果、TryAction、LINQ 扩展及两种包装异常。

测试仍归属 [Bing.Utils.Tests](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj)。按照 [测试项目归属规则](D:/Bing_Framework/Bing.Utils/.agents/skills/test-project-boundary/refs/test-project-boundary.md)，这些 API 属于主包，现有测试归属正确；不为此新拆 NuGet 包或迁移测试。

沿用现有框架：

- 生产包：net8.0、net7.0、net6.0、netstandard2.0。
- 单元测试宿主：net8.0、net7.0、net6.0、net5.0、netcoreapp3.1。
- SDK：以仓库 global.json 为准，目前为 8.0.425。
- 所有文件读取、写入与日志导出显式使用 UTF-8。

本次方案不要求升级框架、增加外部依赖、引入全局异常处理服务或日志平台。Tap、Match 的用户副作用回调、已有 TryGetValue 带 fallback 的成功判定，不因本轮修复顺带重定义。

## 2. 修复前证据

### 2.1 正确性与测试证据

本轮已完成全部 17 个文件的审查，以及消费者编译和行为复现：

- 现有 3 个测试文件共 88 个 Fact：ExceptionHelperTests 27 个、TryTests 38 个、TryActionTests 23 个；无 Theory。
- 88 个测试全部通过，但 Exceptions 局部行覆盖率为 177/886，即 19.97%；分支覆盖率为 79/458，即 17.24%。
- CreateFromTask、InvokeAsync、InvokeRange 未被这些测试覆盖。
- 泛型扩展 `Unwrap<TException>`()、Tap()、Tap(successAction: ...) 的消费者样例均出现 CS0121。
- 失败三元解构直接抛出 TryCreatingValueException。
- Select、SelectMany、Where 的回调异常逃逸；Map 的对应异常可以包装为失败。
- Failure 经 Map 后自定义 Cause 被原始消息替换，直接 InnerException 多出一层 TryCreatingValueException。
- Unwrap 的 Exception 基类匹配、TryAction 的原异常类型提取均不符合公开注释。
- 两个不同静态 Action 在本轮运行中产生相同哈希，被 SuccessAction 错判为相等。
- InvokeRange 的第二项参数工厂抛错后，第三项未执行。
- 带单线程同步上下文的 CreateFromTask 被同步等待阻塞；取消等待后才能继续执行排队的 continuation。
- `Task<Action>` 包装 async void 时，在操作未完成前返回成功，后续异常逃逸。
- AggregateException 第二分支的目标异常查找返回 null。

常规测试项目命令曾被 Bing.Tests.Samples 复制缺失的 output/release/netstandard2.0/Bing.Utils.xml 阻断。88 个测试的通过结果来自临时测试项目：链接仓库原测试文件，引用本轮主包 net8.0 产物。它证明这些测试在该产物上的结果，不代替原测试项目、多目标或发布验收。

审查时的临时证据目录：

~~~text
C:/Users/jianx/AppData/Local/Temp/bing-exceptions-review-20261029-sgntisi0/
  Probe/Program.cs
  ExistingTests/ExistingTests.csproj
  TestResults/ee87129c-8734-4b1d-a8f8-f2d569251db7/coverage.cobertura.xml
~~~

临时目录可能被清理，因此后续实现应将关键复现转为仓库内回归测试。本文已保存结论、输入形态和关闭条件，不将临时文件存在作为实现前提。

### 2.2 性能观察

环境：Windows x64、.NET 8.0.31、Release；每项 3 次测量，取中位数。短路径进行了预热；格式化使用消息很短、没有实际抛出的异常链。这是局部探针，不是生产容量或正式性能阈值。

| 场景 | 单次耗时 | 单次分配 |
| --- | ---: | ---: |
| Try.Create 成功 | 约 12 ns | 24 B |
| Try.Create 抛错并包装 | 约 6.3 μs | 528 B |
| Try.Invoke 成功 | 约 18 ns | 24 B |
| 格式化 10 层异常 | 约 3.9 μs | 4.9 KB |
| 格式化 100 层异常 | 约 126 μs | 245 KB |
| 格式化 1,000 层异常 | 约 5.6 ms | 22.3 MB |

1,000 层异常生成约 504 万字符。重复输出随深度增长的缩进使结果大小呈平方级增长。即使不包含真实堆栈，资源放大已经成立。

## 3. 问题登记与关闭条件

下列条目初始状态均为 OPEN_ACTIONABLE。P1 对应 MUST_FIX；P2 对应 SHOULD_FIX。这里只登记本轮已发现的问题及必要验证，不把可选扩展自动列为缺陷。

| ID | 级别 | 问题与主要位置 | 关闭条件 |
| --- | --- | --- | --- |
| F01 | P1 | `Failure<T>`.Deconstruct 三元重载读取会抛错的 Value；``Try`1.Failure.cs`` | 返回 default(T)、false 和原包装异常，不抛错；二元/三元一致 |
| F02 | P1 | ExceptionHelper 与 ExceptionHelperExtensions 重复声明泛型扩展 | 扩展调用及两个类的已有静态调用均可编译 |
| F03 | P1 | CreateFromTask 在受限上下文同步 Wait | 新异步入口不阻塞；旧入口在不支持的上下文调用前明确失败，不挂起线程 |
| F04 | P2 | `Try<T>`.Tap 两个可选参数重载歧义 | 空调用、仅成功回调、具名失败回调在抽象与具体结果类型上均可编译 |
| F05 | P2 | Select/SelectMany/Where 回调异常逃逸 | 有效委托执行异常转换为 Failure，原异常可提取，后续 Recover 可执行 |
| F06 | P2 | Failure.Bind 重复包装并丢失 Cause | Map/Select/SelectMany/Where 的失败短路保留同一包装异常和 Cause，且不执行回调 |
| F07 | P2 | Unwrap 拒绝 typeof(Exception) | 基类、派生类允许/禁止、精确匹配和未命中均符合约定 |
| F08 | P2 | TryAction.ExceptionAs 只转换外层包装 | 沿异常链返回目标异常实例，成功或未命中返回 null |
| F09 | P2 | SuccessAction 以哈希值作为相等条件 | 根据明确的身份契约比较；哈希碰撞不产生错误相等 |
| F10 | P2 | InvokeRange 参数工厂在保护范围外执行 | 单项工厂失败形成该项 FailureAction，后续项继续，顺序和索引正确 |
| F11 | P2 | 缺少真正的异步创建/操作包装入口 | 可等待未完成 Task，覆盖故障、取消、null Task 与同步抛错；保留旧签名 |
| F12 | P2 | 缺少 AggregateException 多分支查找 | 提供显式启用多分支的入口，遍历次序确定；旧无参解包语义保持 |
| F13 | P2 | FormatMessage 深层输出平方增长，详情输出无预算 | 两种格式化入口有深度/节点/输出预算，截断可辨，普通短异常仍有完整诊断字段 |
| F14 | P2 | GetExceptionDetail 将 HResult 标为异常类型，测试断言空洞 | 类型与 HResult 分开输出，测试断言真实字段及真实堆栈 |

另登记两项交付任务：

| ID | 状态 | 内容 | 完成条件 |
| --- | --- | --- | --- |
| T01 | OPEN_ACTIONABLE | 测试与消费者覆盖补强 | 第 7 节行为矩阵和消费者编译用例完成；已复现缺陷均有对应回归测试 |
| B01 | OPEN_ACTIONABLE | 恢复原测试项目验证入口 | 查清缺失 XML 的生成/复制原因并完成原项目验证；涉及其他模块的修复单独记录实际范围 |

B01 是本地构建待处理项，不预先归类为外部阻塞，也不归因于 Exceptions 的生产实现。

## 4. 实施路线与兼容性选择

推荐路线 B：在主包内完成缺陷修复和受控的能力补齐，保留既有公开方法的 CLR 签名，明确记录行为变化。

| 路线 | 质量与范围 | 成本、速度 | 工具及证据 |
| --- | --- | --- | --- |
| A：局部补丁 | 先修解构、歧义、提取和传播；异步与资源问题仍未关闭 | 工作量较小，交付较快 | 当前 xUnit 与消费者编译 |
| B：完整修复，推荐 | 完成 F01—F14、T01，并对旧入口提供迁移约定 | 中等工作量，按阶段交付 | xUnit、消费者、受控子进程、局部性能探针 |
| C：重做 Result/Try 体系 | 可能统一更多设计，但扩大 API 与迁移面 | 成本最高，回归范围最大 | 另立设计及版本迁移任务；不作为当前修复前提 |

本方案采用以下推荐决策；它们是后续实施基准，不是已经发生的变更：

| 决策 | 推荐契约 | 兼容性影响 |
| --- | --- | --- |
| D01：泛型 Unwrap | 保留两个静态方法的签名，仅 ExceptionHelperExtensions 保留 this 扩展标记 | 普通 namespace 扩展调用恢复；扩展元数据有变化，需要消费者验证 |
| D02：Tap | 保留两个现有双参数签名和默认值，补明确的零参数、单成功回调转发入口 | 新增重载，不能只验证 `Try<T>` 而遗漏 `Success<T>`/`Failure<T>` |
| D03：失败传播 | 失败变换直接复用既有 TryCreatingValueException | Cause 与异常身份得到保留；不再增加无意义包装层 |
| D04：异步命名 | 新增 CreateAsync 和 InvokeTaskAsync；保留旧 InvokeAsync(`Task<Action>`) | 不向旧 InvokeAsync 添加同名 `Func<Task>` 重载，避免旧 InvokeAsync(null) 新增歧义 |
| D05：取消 | 新 Try 异步入口将取消表达为失败结果，保存 OperationCanceledException | 外层 Task 正常完成并返回 Failure，不伪装为 Task 的 Canceled 状态 |
| D06：旧同步等待 | 非默认同步上下文/任务调度器中，CreateFromTask 在调用委托前返回明确失败 | 原来可能成功的同步完成委托也会被保守拒绝；调用方迁移到 CreateAsync |
| D07：成功操作相等性 | SuccessAction 按结果实例身份比较；同一个结果等于自身 | 同一委托执行两次得到不同结果，不再相等；不保留委托或其闭包来实现相等 |
| D08：聚合异常 | 多分支行为通过新增显式参数启用，已有链式入口维持原语义 | 新能力可选择使用，不静默更换原来命中的分支 |
| D09：格式化 | 旧入口也使用有限默认预算，新重载可配置有限预算 | 深链/长消息会截断，文本不再适合作为稳定机器协议；保留常见字段标签 |
| D10：批量输入错误 | coll、操作及参数工厂为 null 视为调用配置错误，显式拒绝；运行时单项异常形成结果 | null 操作不再生成一串无意义失败，需在迁移说明中标注 |

若实施时确需选择其他语义，先更新对应决策和验收用例，不能在实现与测试中各自采用不同约定。本文不授权自动升级版本、修改发布基线或执行发布。

## 5. 具体设计

### 5.1 公开调用、解构和失败传播：F01、F02、F04—F06

**泛型 Unwrap：**

- `ExceptionHelper.Unwrap<TException>`(Exception) 继续作为静态辅助入口，去掉 this。
- `ExceptionHelperExtensions.Unwrap<TException>`(this Exception) 是唯一扩展入口。
- 增加只引用主包的消费者，验证普通 namespace 导入、显式静态调用和泛型约束。

**失败解构：**

- 三元解构的 value 使用 default(T)，tryResult 为 false，exception 直接返回当前 Exception 属性。
- 不经过 Value、GetValue 或重抛辅助方法。
- 二元、三元解构均保持异常引用身份。

**Tap 重载：**

- 添加明确的 Tap() 和 Tap(`Action<T>` successAction)，转发时用明确类型的失败回调 null 选择既有重载。
- Tap() 无副作用并返回当前实例；单成功回调只在成功结果上执行。
- 在 `Try<T>`、`Success<T>`、`Failure<T>` 三种静态类型下验证成员查找。如具体类型的重载隐藏导致调用仍有歧义，在具体类补相同转发入口。
- 具名 failureAction 的单参数/双参数委托仍走原有契约。
- 两个不同委托类型都能接收的裸 null，如 Tap(action, null)，继续要求显式类型；不通过改变失败回调含义来猜测调用意图。

**LINQ 与失败短路：**

- source、selector、convert、predicate 等必需参数校验放在调用边界，继续抛 ArgumentNullException。
- 对合法委托的执行异常，在成功分支的内部 Bind 中统一转换成 Failure；避免每个 LINQ 算子各写一套不一致的捕获逻辑。
- SelectMany 的 selector/convert 返回 null Try 时，返回包含 InvalidOperationException 的失败结果，不能让 null 沿链传播。
- Where 返回 false 时继续返回失败结果；固定 InvalidOperationException 与可理解消息。
- Map 保持现有异常捕获效果；不因统一 Bind 而重复包装其已经返回的 Failure。
- `Failure<T>` 增加内部“接受既有 TryCreatingValueException”构造或工厂。`Bind<TResult>` 创建目标泛型结果时复用同一包装异常，保留 Cause、InnerException、Code、Flag。
- 该内部工厂不改变公开 LiftException(exception, cause) 对显式构造新失败的现有入口语义。
- Tap、Match 的回调异常仍按原契约向调用方传播；本轮不把所有回调都改为捕获。

### 5.2 真正的异步入口及旧同步入口：F03、F11

建议新增以下最小签名。无需为新异步入口立即复制 0—16 参数重载；调用方用 lambda 传递业务参数，现有重载仍保留。

~~~csharp
public static Task<Try<T>> CreateAsync<T>(
    Func<Task<T>> createFunctionAsync,
    string cause = null);

public static Task<Try<T>> CreateAsync<T>(
    Func<CancellationToken, Task<T>> createFunctionAsync,
    CancellationToken cancellationToken,
    string cause = null);

public static Task<TryAction> InvokeTaskAsync(
    Func<Task> invokeAsyncAction,
    string cause = null);

public static Task<TryAction> InvokeTaskAsync(
    Func<CancellationToken, Task> invokeAsyncAction,
    CancellationToken cancellationToken,
    string cause = null);
~~~

具体执行契约：

1. 参数为 null、委托同步抛错、委托返回 null Task、Task 故障，均返回对应失败包装；保留原异常及 cause。null 委托/Task 的内层异常为 ArgumentNullException。
2. 使用 await 等待实际操作完成；库内部 await 使用 ConfigureAwait(false)。不使用 Wait、Result，也不把普通异步 I/O 默认塞进 Task.Run。
3. 新入口可以同步执行委托在首次 await 之前的部分，不承诺自动在线程池启动委托；需要 UI 线程的操作仍由调用者控制上下文。
4. 对显式 CancellationToken 重载，预取消时不执行用户委托，返回包含该 token 的 OperationCanceledException 失败结果。
5. 执行中把同一个 token 传给用户委托，等待委托任务真正结束。若委托忽略 token，不假装已取消，也不提前返回并遗留无人处理的任务。
6. await 抛出 OperationCanceledException 时保留该实例及其 token，返回 Failure。任务成功完成后，不仅因 token 随后变为已取消而撤销成功。
7. 普通 `Func<Task>` 重载不提供“只能取消包装等待”的 token 参数，避免误认为底层操作已停止。
8. 暂不增加独立重试、超时策略、ValueTask 重载或异步 LINQ 算子；它们不是本轮缺陷的必要修复。

旧入口约定：

- CreateFromTask 继续是同步桥接，签名和返回类型不变。
- 若 SynchronizationContext.Current 是非默认实现，或 TaskScheduler.Current 不是默认调度器，在执行委托前返回包装了 InvalidOperationException 的失败结果，消息引导使用 CreateAsync。
- 默认上下文下保留同步等待、原异常解包和 cancellationToken 仅取消等待的契约。它不会自动取消用户任务，文档必须明确这一点。
- 旧同步桥接没有通用无阻塞保证，不能把它用于高并发服务热路径。提供迁移例子，而不是用 ConfigureAwait(false).GetAwaiter().GetResult() 宣称消除了任意委托的上下文死锁。
- 旧 InvokeAsync(`Task<Action>`) 仍表示“等待取得同步 Action，再执行它”。不要改变为隐式 async void 包装器；中文 XML 注释明确 Action 必须是同步操作。
- 异步操作迁移到 InvokeTaskAsync。无需通过反射猜测 Action 是否为 async void，也不在基础库安装全局未处理异常处理器。

### 5.3 异常类型与聚合分支：F07、F08、F12

**已有链式 API：**

- targetType 合法性用 typeof(Exception).IsAssignableFrom(targetType)，包含 Exception 本身。
- mayDerivedClass=true 时以可赋值关系判断；false 时只允许实际类型完全相同。
- 从外到内返回第一个匹配异常，未命中返回 null；exception/targetType 为 null 的约定保持。
- 将递归查找改为迭代，避免每层重复类型校验与递归栈消耗。
- `TryAction.ExceptionAs<TException>`() 与 `Try<T>` 对齐，从包装异常开始沿链查找，返回原实例。

**显式聚合查找：**

建议给 typed Unwrap 增加一个无可选参数的四参数重载：

~~~csharp
public static Exception Unwrap(
    Exception exception,
    Type targetType,
    bool mayDerivedClass,
    bool includeAggregateExceptions);
~~~

扩展层提供对应入口；泛型扩展可添加 `Unwrap<TException>`(bool includeAggregateExceptions)。`Try<T>`/TryAction 的 ExceptionAs 可提供同名 bool 重载，以复用相同遍历契约。原有零参数签名保留。

- includeAggregateExceptions=false：与已有 InnerException 链语义一致。
- true：根节点优先、深度优先、AggregateException.InnerExceptions 按原列表顺序从左到右；返回第一个匹配。
- 聚合节点只枚举 InnerExceptions，不再额外枚举指向第一分支的 InnerException。
- 使用显式栈与引用身份去重，不能依赖异常类型重写的 Equals/GetHashCode；netstandard2.0 下使用内部引用比较器。
- 不先调用 Flatten() 丢掉中间聚合节点，也不构造所有分支的字符串。
- 参数为空时即时校验；异常查找保持有限输入上的迭代算法，格式化的遍历预算由第 5.5 节另行控制。

原有 Unwrap()、ToUnwrappedString() 继续表示单链最内层异常及消息，不将“一个异常”静默替换为所有分支拼接结果。

### 5.4 相等语义与批量隔离：F09、F10

**SuccessAction：**

采用 D07 的结果实例身份语义：

- 同一实例与自身相等；两个不同结果实例不相等，即使来自同一委托。
- Equals 与 ==/!= 一致，GetHashCode 使用稳定的对象身份哈希，例如 RuntimeHelpers.GetHashCode(this)。不同对象哈希相同仍不表示相等。
- 移除 _hashOfAction 作为相等条件，成功构造改为内部无参构造，更新同步/旧异步工厂。
- 不缓存成功结果单例，否则会破坏新定义的结果身份。
- 不持有用户委托来判等，避免结果寿命无意延长闭包和业务对象寿命。
- `Success<T>` 的值相等语义及 FailureAction 现有异常身份语义不随本项一起重写。

若未来必须恢复“相同委托生成的成功结果相等”，应使用真实 Delegate.Equals 并明确闭包引用寿命，作为单独的兼容选择；不能退回只比较哈希。

**InvokeRange：**

- 公共入口校验 coll、invokeAction 和必需参数工厂，随后返回私有延迟枚举核心。
- 每个元素的参数工厂求值与操作执行处于同一个单项保护范围，按声明顺序求值；前一个工厂抛错后不继续求值该项后续工厂。
- 工厂或操作抛错，生成该索引的一项 FailureAction，并继续处理下一项；不重试、不回滚已经执行的其他项。
- 无论该项成功或失败，下一项索引都加一，输出顺序与输入一致。
- C# 中不要在带 catch 的 try 内 yield return。先计算单项 TryAction，再在保护范围外 yield return。
- 不预先 ToArray 全量物化输入，不默认并行执行，保留延迟执行与提前停止能力。
- 源枚举器的 GetEnumerator/MoveNext/Current 抛错不属于可恢复的某项操作错误，原异常向调用方传播并释放枚举器，不能伪造一个元素后继续未知状态。
- 将 IEnumerable 的重复枚举会重复执行操作写入注释，不在本轮引入隐藏缓存。

### 5.5 有限格式化与诊断字段：F13、F14

增加 ExceptionFormatOptions，并给 FormatMessage/GetExceptionDetail 增加接受 options 的重载；既有签名保留并转入同一实现。

建议默认值如下。这些是推荐产品边界，不是已经批准的生产性能 SLA：

| 选项 | 推荐默认值 | 定义 |
| --- | ---: | --- |
| MaxDepth | 64 | 根节点深度为 1，限制沿路径展开的层数 |
| MaxExceptionCount | 256 | 一次格式化最多访问的不同异常节点数 |
| MaxOutputLength | 32768 | UTF-16 字符数上限，包含分隔符、换行与截断标记 |
| MaxIndentLevel | 8 | 每层两个空格，最多 16 个缩进空格 |
| IncludeAggregateExceptions | false | 旧入口沿单链；新 options 可启用所有聚合分支 |
| HideStackTrace | false | 控制堆栈输出；旧 bool 参数映射到此属性 |

实现约束：

- 一次调用开始时读取、校验并快照 options，避免外部修改 options 改变进行中的预算。
- 深度/节点数必须大于 0，MaxIndentLevel 不小于 0，MaxOutputLength 至少容纳固定截断标记；非法 options 抛 ArgumentOutOfRangeException。
- 在追加内容和访问下一节点前检查预算；预留截断标记空间，保证最终字符串长度不超过 MaxOutputLength。
- 格式化聚合分支时按需推进子节点枚举器，不先把全部 InnerExceptions 压栈或复制为数组；否则节点预算虽限制输出，额外内存仍可能随输入宽度无界增长。
- 使用有限缩进与 StringBuilder，按剩余预算追加字符串片段；避免先拼接整行、完整内部异常或超长堆栈后才截断。
- 不通过 Append(innerException)、AppendFormat 的对象参数或异常 ToString() 绕过预算。
- 在启用聚合分支时，AggregateException 的摘要使用明确的聚合类型/数量信息，再逐项受限输出；避免直接读取会拼接全部子异常消息的 AggregateException.Message。自定义聚合说明的保留方式必须在测试与文档中说明，不能靠私有字段反射提取。
- 对常规 Exception.Message/StackTrace 属性，库能控制追加与遍历量；不能声称能限制任意自定义虚属性内部的分配或执行时间。
- 以引用身份去重；重复节点标识为已出现，截断标记区分深度、节点数或长度预算。
- 输出类型使用 GetType().FullName，HResult 单独输出，不再把错误码标为类型。
- HideStackTrace=true 不输出任何层的堆栈；不会自动隐藏消息中的路径、SQL、token 等内容，不命名为“安全响应”。
- GetExceptionDetail 的时间字段应描述格式化/记录时间，不声称知道异常实际发生时刻。
- FormatMessage 对 null 保留现有空字符串行为；GetExceptionDetail 对 null 明确抛 ArgumentNullException，并为该契约补测试。
- 旧 FormatMessage 常见字段标签保持；GetExceptionDetail 保留可识别分隔符。深层缩进、截断、类型字段修正等文本变化记录在迁移说明中，不承诺整个字符串逐字不变。

基础包提供诊断文本，不实现通用敏感信息识别或 HTTP 错误响应。面向公开接口时由应用把异常映射为明确的用户消息；本轮没有发现此模块已经连接公开错误响应的证据。

## 6. 文件与责任分工

下表为建议修改/新增位置，实施前检查是否与其他工作区变更冲突。

| 范围 | 文件 | 责任 |
| --- | --- | --- |
| 解包与类型契约 | src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs | F02、F07、F12，保留转发与静态调用 |
| 格式化 | ExceptionExtensions.cs、ExceptionHelper.cs；新增 ExceptionFormatOptions.cs 及必要内部格式化文件 | F13、F14 |
| 结果与 LINQ | ``Try`1.cs``、``Try`1.Success.cs``、``Try`1.Failure.cs``、TryExtensions.cs | F01、F04—F06，必要的 ExceptionAs 聚合重载 |
| 操作结果 | TryAction.cs、TryAction.Success.cs、Try.Invoke.cs、Try.InvokeAsync.cs | F08、F09，旧异步入口文档 |
| 同步桥接与新异步 | Try.CreateAsync.cs；新增 Try.Async.cs 或按现有 partial 命名拆分 | F03、F11 |
| 批量调用 | Try.Invoke.Range.cs | F10，覆盖现有 1—16 参数入口 |
| 现有单元测试 | tests/Bing.Utils.Tests/Bing/Exceptions/*.cs | 修正空洞断言，补充局部回归 |
| 新测试夹具 | 同目录的 Async/Traversal/Formatting/Range/Contract 测试类 | 避免继续把所有场景堆入一个测试类 |
| 消费者与死锁宿主 | 新增 build/ExceptionsConsumer/ | 公开调用编译、真实资产加载、受控上下文验证 |
| 性能 | 新增 benchmarks/ExceptionsProbe/ | 使用 BCL Stopwatch 与分配计数即可，不为本轮引入新依赖 |
| 验证入口 | 新增 build/Test-Exceptions.ps1 | 组织消费者、受控子进程和可选性能探针 |
| 文档 | docs/modules/Bing.Utils.md；按实际改动更新 API reference | 新 API、行为变化、取消、聚合、预算与迁移示例 |

主代理负责契约决策、共享文件修改、集成与最终验证。边界清晰的测试矩阵、消费者样例、探针和文档核对可交给 luna_worker；不能让多个工作者同时修改共享的 ExceptionHelper.cs/``Try`1.cs``。复杂阶段完成后安排独立只读 Review，不在审查阶段夹带实现。

## 7. 回归测试矩阵

测试的主要断言目标必须是用户可见结果或契约，不是逐行复制实现。消费者编译与真实包/资产加载不能只用同一程序集内的静态调用代替。

| 编号 | 场景 | 必要输入与预期 | 验证层级 |
| --- | --- | --- | --- |
| T01-01 | 扩展调用 | `ex.Unwrap<T>`()、`ExceptionHelper.Unwrap<T>`()、`ExceptionHelperExtensions.Unwrap<T>`() 均可编译 | 消费者编译 |
| T01-02 | Tap 重载 | `Try<T>`/`Success<T>`/`Failure<T>` 下零参、单成功回调、两种具名失败回调、显式 typed null | 消费者编译 + 单元测试 |
| T01-03 | 三元解构 | 成功、失败；值/false/异常身份与二元结果一致 | 单元测试 |
| T01-04 | LINQ 抛错 | Select、两种 SelectMany、Where 各自抛同一异常；结果失败且 ExceptionAs 返回同一实例 | 单元测试 |
| T01-05 | LINQ 边界 | null source/委托显式拒绝；null Try 返回值变失败；Where false 的异常类型和消息正确 | 单元测试 |
| T01-06 | 失败传播 | 自定义 Cause 的失败经过多个算子；Cause、包装、InnerException 身份不变，回调计数为零 | 单元测试 |
| T01-07 | 类型查找 | Exception 基类、精确/派生匹配、未命中、null 参数、较深链 | 单元测试 |
| T01-08 | TryAction 提取 | 原异常、包装异常、派生类型、成功结果及未命中 | 单元测试 |
| T01-09 | 成功操作判等 | 同一实例相等；同/不同委托的两次执行都产生不同结果；HashSet 保留两个实例 | 单元测试 |
| T01-10 | 批量隔离 | 第一/中间/最后参数工厂或操作抛错；数量、顺序、索引、后续执行和 Cause 正确 | 单元测试 |
| T01-11 | 批量枚举契约 | 空/null 输入、null 工厂、延迟执行、提前停止、源枚举抛错及 Dispose | 单元测试 |
| T01-12 | 真异步成功 | TCS 尚未完成时入口已返回 Task；完成 TCS 后得到正确结果 | async 单元测试 |
| T01-13 | 真异步失败 | 同步抛错、faulted Task、null Task、null 委托；原异常与 Cause 可提取 | async 单元测试 |
| T01-14 | 取消 | 预取消不执行委托；执行中透传 token；取消成为 Failure；忽略 token 的任务仍被等待 | async 单元测试 |
| T01-15 | 上下文边界 | 新 API 在受控单线程上下文可完成；旧桥接在调用委托前明确失败；非默认调度器同样覆盖 | 受控子进程 |
| T01-16 | 旧异步能力边界 | `Task<Action>` 同步操作成功/失败/取消/空操作；新的 InvokeTaskAsync 等待真实操作并捕获故障 | 单元测试 + 消费者 |
| T01-17 | 聚合异常 | 显式 includeAggregateExceptions=true 测第二分支、嵌套聚合、稳定次序和重复实例；false 与旧入口保持默认单链结果 | 单元测试 |
| T01-18 | 格式化预算 | 旧签名按默认预算截断，options 重载按指定预算截断；覆盖深链、宽聚合、长消息与真实堆栈，长度均不超上限且原因明确 | 单元测试 |
| T01-19 | 诊断字段 | 真实类型、独立 HResult、源/方法/内部消息；null 输入行为明确 | 单元测试 |
| T01-20 | 堆栈与重抛 | 实际抛出后捕获；显示/隐藏堆栈；PrepareForRethrow/ReThrow 保留实例与原抛出方法 | 单元测试 |
| T01-21 | 现有高参数重载 | Create/Invoke/CreateFromTask/InvokeAsync/InvokeRange 各家族的参数顺序、单次执行和失败包装 | 数据化单元测试 |
| T01-22 | 异步取值与恢复 | GetValueAsync 成功/失败，GetSafeValueAsync fallback 执行次数、异常传播，Recover/RecoverWith 成功与再次失败 | 单元测试 |
| T01-23 | 资产兼容 | 新旧消费者调用形状；四个生产 TFM 构建；netstandard2.0 产物由运行时宿主真实加载 | 消费者与构建 |

补充要求：

- 重抛测试必须先真正 throw/catch，断言异常引用相同且堆栈包含已知原始抛出方法；避免内联影响时对 fixture 方法显式禁用内联。
- 堆栈测试不能使用只有 new Exception 而从未抛出的实例，也不能断言固定机器路径或完整行号。
- 类型字段测试断言具体类型名与独立 HResult；不能用“非空”替代。
- 不要求运行时恰好出现某个随机哈希碰撞；新身份契约的确定性用例已经覆盖根因。
- 用 TCS、事件、明确握手控制异步时序；不通过大段 Thread.Sleep 期待任务恰好完成。
- 有潜在挂起风险的探针放在单独子进程中，预设 20 秒进程预算；超时终止进程树并记录诊断，不把未完成当通过，也不让后台工作遗留在 xUnit 进程中。
- 高参数重载先参数化验证 0/1/2/16（Range 为 1/2/16）边界；对被批量改写的家族覆盖所有现有参数个数及转发顺序，不生成无意义的输入笛卡尔积。
- 覆盖率用于定位未测分支，不以一个百分比替代 F01—F14 的验收。原先零覆盖的异步/批量能力必须拥有成功、失败和边界测试。

## 8. 性能与资源验收

### 8.1 有限的测量范围

复用第 2 节的诊断结论，不因只修改测试名称或文档重跑性能。正式实施在第一次修改前冻结一次可执行基线；之后保持同一 SDK、运行时、构建配置、输入、预热和重复次数。

探针只包含：

- Try.Create / Try.Invoke 成功与失败。
- `Try<T>` 成功 Map 和已失败结果的链式传播。
- 链深 1/10/100/1,000 的 typed/untyped Unwrap。
- 同样深度的普通格式化；另有有限宽度的 AggregateException。
- InvokeRange 的正常路径、少量失败路径与延迟枚举。
- 新异步入口的已完成 Task 和真正挂起后完成的 Task。

记录耗时、每次调用分配、输出字符数、输入规模、实际异常节点数和截断原因。不要将异常实际抛出成本归咎于包装类本身，也不要把未完成任务的启动时间当成完整异步耗时。

### 8.2 验收解释

- 短成功路径不得引入全局锁、全局缓存或不必要的任务调度。
- 失败短路不应为每个算子增加异常包装层；操作成功后的结果对象分配由原有 API 语义决定。
- typed Unwrap 采用迭代、单次参数校验；大链输入不依赖递归栈。
- 旧版本与新版本均完整输出的短链，可比较耗时/分配。
- 新版本深链被截断时，只能比较“各自默认策略的资源上界”，不能声称相同完整输出语义下获得相同倍数的算法加速。
- F13 的硬验收是输出/节点/深度上界及截断语义，不是某台开发机上的固定毫秒值。
- 不把第 2 节的纳秒数直接转为 CI 门槛。没有已确定的生产性能预算时，报告观测结果与差异；正式阈值单列为待定，不影响已完成正确性修复的关闭。
- 不执行 OOM 压力或不断提高层数寻找崩溃点；上述固定规模足以验证已发现的增长问题。

每个相同候选、输入和假设，最多一次诊断加一次正式确认。timeout 可以作为边界证据，但不能为超时运行编造吞吐量或内存统计。

## 9. 分阶段 TODO

### 阶段 0：确认契约、原始产物与构建入口

- [ ] 阅读当前 AGENTS.md、GOAL_RULES.md、本方案及测试归属规则。
- [ ] 确认 D01—D10 作为本次实施契约，记录任何与本方案不同的选择。
- [ ] 保留当前无关工作区变更，仅记录 Exceptions 相关源码、基线构建产物与运行环境。
- [ ] 诊断 B01，恢复原测试项目的最小可运行路径；不通过删除无关项目绕过。
- [ ] 在修改前保存一次基线探针与消费者样例；临时目录已消失时按本文输入重新建立。
- [ ] 记录变更影响分析：预期文件、公开调用形状、运行时路径、TFM、消费者与测试范围。

完成标志：基线可识别，原测试入口状态明确，契约没有互相矛盾的预期。

### 阶段 1：局部公开契约与失败处理

- [ ] F01、F02、F04：先让失败解构和常见公开调用正确。
- [ ] F05、F06：统一成功 Bind 捕获，保留失败身份与原因。
- [ ] F07、F08：修正类型校验和链式提取。
- [ ] F09、F10：按决策修正身份比较和逐项隔离。
- [ ] 为各修复补对应回归测试，执行 L0/L1。
- [ ] 消费者编译验证抽象与具体类型，不能仅靠静态辅助调用绕过歧义。

完成标志：对应 Finding 逐项关闭，原测试仍通过，新的行为差异已记录。

### 阶段 2：异步与聚合能力

- [ ] 实现 CreateAsync、InvokeTaskAsync 与明确的取消语义。
- [ ] 旧 CreateFromTask 的上下文保护和迁移说明完成。
- [ ] 保留 `Task<Action>` 的同步操作契约。
- [ ] 新增显式聚合遍历，保持原链式入口的结果选择。
- [ ] 完成 T01-12—T01-17 及消费者验证。
- [ ] 独立 Review 只检查本阶段与阶段 1 的交叉点；只修复新的 OPEN_ACTIONABLE。

完成标志：F03、F11、F12 关闭，上下文探针正常结束，没有后台任务残留。

### 阶段 3：格式化与诊断测试

- [ ] 新增并校验格式化 options 与预算。
- [ ] 两个旧入口接入有限格式化，修复类型/HResult 与时间标签。
- [ ] 修正原有空洞断言，补真实堆栈、重抛与预算测试。
- [ ] 执行同输入的局部性能探针；区分等价输出比较与截断边界证据。

完成标志：F13、F14 关闭，资源边界有确定性测试与测量支持。

### 阶段 4：模块验收与交付

- [ ] 完成四个生产 TFM 构建和五个测试宿主的 Exceptions 用例。
- [ ] 执行主测试项目 net8.0 完整单元测试及 Guard 受影响用例。
- [ ] 完成消费者编译、netstandard2.0 实际资产加载及受控子进程。
- [ ] 核对公开 API 差异与旧消费者兼容；不得自动把当前实现写回既有 API baseline。
- [ ] 更新模块文档和迁移示例，记录字段、取消、相等、同步桥接和截断变化。
- [ ] 独立最终 Review，逐项记录 Findings、测试、性能与构建状态。
- [ ] 全 Solution/外部 CI 仅在实际跨模块影响或项目发布要求下运行，不能把局部证据写成全仓已通过。

完成标志：F01—F14、T01 满足关闭条件；B01 有真实原项目验证结论；未完成的外部或发布事项单列。

## 10. 验证命令

以下命令从仓库根目录执行，实际运行前按变更范围选择。仅编写本方案时不运行这些代码测试。

PowerShell 编码设置：

~~~powershell
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
~~~

局部构建和 L1：

~~~powershell
dotnet build src/Bing.Utils/Bing.Utils.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0 --filter "FullyQualifiedName~Bing.Utils.Tests.Bing.Exceptions"
~~~

B01 诊断首先检查 XML 的实际生成与路径，必要时定向构建该资产，然后重跑原测试命令：

~~~powershell
dotnet build src/Bing.Utils/Bing.Utils.csproj -c Release -f netstandard2.0 -p:GenerateDocumentationFile=true
~~~

该命令是诊断/生成步骤，不保证自动解决所有依赖复制问题。若仍失败，记录真实错误并检查生成/复制配置，不以空 XML 文件或禁用检查掩盖原因。

阶段 4 多目标验证：

~~~powershell
foreach ($exceptionsBuildTfm in @('net8.0', 'net7.0', 'net6.0', 'netstandard2.0')) {
    dotnet build src/Bing.Utils/Bing.Utils.csproj -c Release -f $exceptionsBuildTfm
    if ($LASTEXITCODE -ne 0) { throw "Exceptions build failed: $exceptionsBuildTfm" }
}

foreach ($exceptionsTestTfm in @('net8.0', 'net7.0', 'net6.0', 'net5.0', 'netcoreapp3.1')) {
    dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f $exceptionsTestTfm --filter "FullyQualifiedName~Bing.Utils.Tests.Bing.Exceptions"
    if ($LASTEXITCODE -ne 0) { throw "Exceptions tests failed: $exceptionsTestTfm" }
}

dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Guard.Tests/Bing.Utils.Guard.Tests.csproj -c Release -f net8.0 --filter "FullyQualifiedName~ValidationExceptionHelperTest"
~~~

局部覆盖率：

~~~powershell
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0 --filter "FullyQualifiedName~Bing.Utils.Tests.Bing.Exceptions" --collect:"XPlat Code Coverage" --results-directory artifacts/exceptions/coverage -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include="[Bing.Utils]Bing.Exceptions.*"
~~~

以下入口在实施时创建，目前尚不存在；创建后应支持这些命令：

~~~powershell
dotnet build build/ExceptionsConsumer/ExceptionsConsumer.csproj -c Release
dotnet run --project benchmarks/ExceptionsProbe/ExceptionsProbe.csproj -c Release
./build/Test-Exceptions.ps1 -Configuration Release -Mode Consumer
./build/Test-Exceptions.ps1 -Configuration Release -Mode Context
~~~

验证脚本的最小职责：

- Consumer 模式编译真实公开调用；除常规 ProjectReference 外，用明确 HintPath/资产选择验证 netstandard2.0 产物，避免名为 netstandard 测试实际仍使用 net8.0 实现。
- 若验证旧消费者二进制兼容，先用修复前程序集编译，再替换为候选程序集运行；不能用重新编译的源码消费者冒充该证据。
- Context 模式启动独立宿主并监督退出；默认 20 秒，超时终止进程树、保存标准输出/错误和退出码。
- 所有日志使用显式 UTF-8；非零退出码立即报告失败，不继续把后续成功覆盖为整体通过。
- 性能探针独立运行，避免与覆盖率插桩、调试器或并行压力任务混合。

## 11. 完成标准与自动执行停止条件

后续执行前必须遵循用户级 GOAL_RULES.md。实现循环只由 OPEN_ACTIONABLE 驱动，不能因为 PARTIAL、NOT_VERIFIED、TIMEOUT 或 Release 未通过而自动重复修复。

每轮记录：

~~~text
Changed Scope:
Validation: L0 / L1 / L2 / L3 / 必要时 L4 / 定向性能探针
Completed:
Open Actionable:
Blocked Approval:
Blocked External:
Accepted Limitations / Verified Boundaries / Not Applicable / Deferred:
Next Action:
~~~

必须满足的交付条件：

- F01—F14 每项都有对应代码行为、测试或消费者证据，不能只凭总测试数关闭。
- 新异步取消、旧同步桥接限制、结果相等、聚合匹配顺序和格式化预算均有中文 XML 契约。
- T01 的必需矩阵完成，原 88 个测试之外增加能抓住真实缺陷的回归。
- B01 恢复后的原项目验证可复现；若受外部环境阻断，写明条件与影响，不能以临时宿主结果替代。
- 性能报告区分修复前诊断、候选测量和资源边界；不发明正式阈值。
- 保留不相关工作区变更；不自动 git add/commit/push、创建 PR、升级版本或发布。

没有新的 OPEN_ACTIONABLE 时停止修复。相同问题、候选和证据连续两轮没有进展时停止重复尝试；自动 Review → Fix → Review 最多按公共规则进行 3 轮。外部环境、发布审批或尚未确定的性能 SLA 单独报告，不使已经闭环的正确性 Finding 重新打开。

## 12. 后续实现交接

- 当前已完成：审查结论归档、方案与兼容性决策、回归矩阵、实施顺序和验证命令。
- 当前未执行：上述生产实现、新测试、消费者/探针脚本以及修复后验收。
- 当前下一步：从阶段 0 开始确认原项目构建与基线，随后执行阶段 1。
- 本文为后续实现的主方案；执行日志与原始测量可放在 ai_docs/tasks/2026-10-29-exceptions-repair/，不要反复向本文追加每轮日志。
