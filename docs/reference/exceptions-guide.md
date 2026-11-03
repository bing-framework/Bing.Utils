# Exceptions 使用与迁移指南

Exceptions 位于主包 Bing.Utils，支持 net8.0、net7.0、net6.0 和 netstandard2.0。以下示例使用 `using Bing.Exceptions;`。

## 失败结果与组合

`Try<T>` 的失败三元解构返回 `default(T)`、`false` 和原 `TryCreatingValueException`；直接读取失败结果的 `Value` 仍会抛出包装异常。失败经过 Map、Select、SelectMany、Where 后，包装异常、InnerException 和 Cause 保持原引用或内容，用户回调不执行。

~~~csharp
var original = new InvalidOperationException("库存不足");
var failed = Try.LiftException<int>(original, "创建订单失败");
var mapped = failed.Map(value => value.ToString());
var (value, success, error) = mapped;
// value == null，success == false；error 与 failed.Exception 为同一实例。
~~~

成功分支的 LINQ/Map 回调抛错会转成失败结果；组合函数返回空 Try 时，失败包含 InvalidOperationException。空参数等调用校验仍直接抛出。Tap、Match 是调用方副作用，回调抛错继续向外传播。

`Tap()`、`Tap(Action<T>)` 可以从 Try<T>、Success<T>、Failure<T> 三种静态类型调用。两个旧双回调重载保持不变；传入失败回调时用明确参数类型选择对应重载。

SuccessAction 使用结果实例身份相等：同一 Action 执行两次得到的两个成功结果不相等。同一结果与自身相等；结果对象不保存 Action 委托。

## 真实异步操作

新增两个值工厂和两个无返回值操作入口：

~~~csharp
Task<Try<T>> Try.CreateAsync<T>(Func<Task<T>> createFunctionAsync, string cause = null);
Task<Try<T>> Try.CreateAsync<T>(Func<CancellationToken, Task<T>> createFunctionAsync,
    CancellationToken cancellationToken, string cause = null);
Task<TryAction> Try.InvokeTaskAsync(Func<Task> invokeAsyncAction, string cause = null);
Task<TryAction> Try.InvokeTaskAsync(Func<CancellationToken, Task> invokeAsyncAction,
    CancellationToken cancellationToken, string cause = null);
~~~

~~~csharp
var result = await Try.CreateAsync(async token =>
{
    await Task.Delay(10, token);
    return 42;
}, cancellationToken, "读取失败");

if (result.IsFailure)
{
    var cancellation = result.ExceptionAs<OperationCanceledException>();
    // 需要由调用方决定业务上的取消响应。
}
~~~

四个入口都等待实际任务完成，内部 await 使用 ConfigureAwait(false)。空委托、空任务、同步抛错、任务故障和取消均保存为失败；外层任务正常完成，不能用外层 IsCanceled 判断业务取消。原 OperationCanceledException（包括令牌）保持引用。预取消不调用委托；执行中取消由操作协作处理，委托忽略令牌时仍等到它完成。

旧 CreateFromTask 是同步桥接。非默认 SynchronizationContext 或非默认 TaskScheduler 下，在调用委托前返回包含 InvalidOperationException 的失败结果，避免同步阻塞形成死锁。其旧 cancellationToken 仅取消等待，不能终止已启动的操作。UI、请求处理和异步流程应迁移到 await Try.CreateAsync。

旧 InvokeAsync(Task<Action>) 仍等待取得一个同步 Action，然后执行它。异步操作应迁移到 InvokeTaskAsync(Func<Task>)；不要将 async lambda 转成 Action/async void。

## 异常类型与聚合分支

`exception.Unwrap()` 保持沿 InnerException 单链返回最内层异常。typed/generic Unwrap 与结果的 ExceptionAs 默认沿单链从根查找第一个匹配；允许 Exception 基类。

~~~csharp
var original = new InvalidOperationException("目标");
var aggregate = new AggregateException(new ArgumentException("第一分支"), original);
var match = aggregate.Unwrap<InvalidOperationException>(includeAggregateExceptions: true);
var exact = ExceptionHelper.Unwrap(aggregate, typeof(InvalidOperationException),
    mayDerivedClass: false, includeAggregateExceptions: true);
var action = Try.Invoke((Action)(() => throw aggregate));
var cause = action.ExceptionAs<InvalidOperationException>(includeAggregateExceptions: true);
~~~

显式打开聚合遍历后采用根优先、左到右深度优先顺序，以引用去重；不调用异常重写的 Equals/GetHashCode。泛型扩展只在 ExceptionHelperExtensions 上声明；ExceptionHelper.Unwrap<TException>(exception) 的静态签名仍可调用。

## 批量隔离与枚举

InvokeRange 保持延迟执行，每次枚举都会重新执行。空集合引用、空操作和空参数工厂立即抛出 ArgumentNullException。每项的参数工厂和操作位于同一保护范围，失败产生带原异常和 cause 的 FailureAction，随后继续下一项；工厂索引从 0 开始。

源枚举器的 GetEnumerator、MoveNext、Current 故障向外传播，不伪装成单项失败。正常结束、提前停止及枚举故障时使用 foreach 的释放语义。调用方提前停止时也应释放结果枚举器。

## 格式化预算

FormatMessage() 和 ExceptionHelper.GetExceptionDetail(exception) 均使用下表默认值。旧入口也会截断超限文本；需要更多诊断时显式提供 ExceptionFormatOptions。

| 选项 | 默认值 | 有效范围 |
| --- | ---: | --- |
| MaxDepth | 64 | ≥ 1，根为 1 |
| MaxExceptionCount | 256 | ≥ 1，不同异常实例数 |
| MaxOutputLength | 32768 | ≥ 64，UTF-16 字符数，包含标记与换行 |
| MaxIndentLevel | 8 | ≥ 0，每层两个空格 |
| IncludeAggregateExceptions | false | 显式展开聚合分支 |
| HideStackTrace | false | 隐藏所有节点的堆栈字段 |

~~~csharp
var text = exception.FormatMessage(new ExceptionFormatOptions
{
    MaxDepth = 16,
    MaxExceptionCount = 64,
    MaxOutputLength = 8192,
    MaxIndentLevel = 4,
    IncludeAggregateExceptions = true,
    HideStackTrace = true
});
~~~

配置在调用开始时读取快照；后续修改不会改变本次格式化。调用方应避免并发修改同一配置对象，快照不提供多字段事务语义。超限时出现深度、节点数或输出长度标记；重复引用输出重复标记，不消耗不同节点预算；即使节点预算恰好用满，也只在遇到下一个不同实例时显示节点数截断。输出字符预算耗尽后不再推进分支遍历。聚合摘要使用类型及子异常数量，不读取会拼接全部子消息的 AggregateException.Message。只逐项读取当前所需分支，不一次性物化全部子节点。

详情将异常类型与 HResult 分开显示；时间字段表示记录时间，不是异常发生时间。隐藏堆栈仍保留异常方法等独立字段，也不会自动脱敏消息。格式化控制本模块追加和遍历的资源。Exception.StackTrace 由运行时提供完整字符串，读取时可能先生成它；本库按剩余预算追加，不再拼接第二份完整堆栈；未截断时保留运行时提供的原始堆栈文本，不承诺不同运行时生成完全相同的文本。MaxOutputLength 是输出字符上限，不是整个调用的总分配上限。异常属性 getter（包括运行时堆栈生成和自定义属性）的执行时间、分配和异常不在预算保护内。

FormatMessage 对 null 异常返回空串；GetExceptionDetail 对 null 异常抛 ArgumentNullException。配置不可为 null，数值非法时抛 ArgumentOutOfRangeException。PrepareForRethrow/ReThrow 立即重抛原异常，并保留已有抛出位置。

## 验证入口

~~~powershell
# 先构建四个生产目标，再编译 net8 消费者并逐一加载四种资产。
dotnet build src/Bing.Utils/Bing.Utils.csproj -c Release
pwsh -NoProfile -File build/Test-Exceptions.ps1 -Mode Consumer

# 独立子进程，默认 20 秒超时并终止进程树。
pwsh -NoProfile -File build/Test-Exceptions.ps1 -Mode Context

# 局部探针，默认使用 output/release/net8.0/Bing.Utils.dll。
pwsh -NoProfile -File build/Test-Exceptions.ps1 -Mode Probe -Label candidate
~~~

测试归属 tests/Bing.Utils.Tests/Bing/Exceptions。实现方案、问题状态与实测证据见 [本次实施报告](../../ai_docs/tasks/2026-10-29-exceptions-repair/final-report.md)。
