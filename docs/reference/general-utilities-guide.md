# 通用工具主线使用指南

| 能力 | 类型与包 | 生命周期和边界 |
| --- | --- | --- |
| 按键互斥 | `Bing.Threading.KeyedAsyncLock<TKey>`，`Bing.Utils` | 同实例同 key 互斥；租约释放，空闲条目回收 |
| 重叠请求合并 | `Bing.Threading.SingleFlight<TKey,TValue>`，`Bing.Utils` | 同 key 共享进行中的任务；完成不缓存 |
| Cron 时间计算 | `Bing.Date.Cron.CronSchedule`，`Bing.Utils.Cron` | 显式时区，返回 UTC；不运行任务 |
| 大文本行比较 | `Bing.Text.Diff.TextDiff`，`Bing.Utils.Diff` | 同步、可取消、有计算预算；只读结果 |

核心包保持既有目标框架与依赖。两个新包支持 `netstandard2.0;net8.0`，.NET 6/7 使用 netstandard2.0 产物。没有新增分布式锁、调度器、缓存 TTL、字符级 Diff 或三方合并。

## 按键互斥

把实例保存在服务生命周期内，让需要互斥的调用共享它。每次操作临时创建实例无法形成跨调用互斥。

```csharp
using Bing.Threading;

var locks = new KeyedAsyncLock<string>(StringComparer.Ordinal);
using (await locks.LockAsync("order:42", cancellationToken))
{
    await SaveOrderAsync();
}

using (var lease = await locks.TryLockAsync("order:42", TimeSpan.FromSeconds(2), cancellationToken))
{
    if (lease == null) return; // 等待超时
    await SaveOrderAsync();
}
```

同 key 非重入，不承诺严格 FIFO；不同 key 可以并行。等待取消抛 `OperationCanceledException`，不会取消已经进入临界区的业务代码。键不得为 null，使用期间哈希与相等性必须稳定。租约是引用类型，重复或并发释放幂等；必须用 `using` 或 `finally` 释放。组件不提供销毁正在使用锁的接口，持有者和等待者全部退出后条目自动回收。超时可为零、`Timeout.InfiniteTimeSpan` 或不超过 `Int32.MaxValue` 毫秒的非负时长。

## 合并进行中的请求

```csharp
using Bing.Threading;

using var serviceLifetime = new CancellationTokenSource();
var requests = new SingleFlight<string, string>(
    (key, ownerToken) => LoadAsync(key, ownerToken),
    StringComparer.Ordinal,
    serviceLifetime.Token);

var value = await requests.RunAsync("config:main", callerCancellationToken);
```

构造时固定工厂、键比较器与生命周期令牌。重叠调用共享一次执行，工厂在登记锁外启动；其首个 `await` 前的同步代码仍在发起调用的线程运行。调用方令牌只取消自己的等待，工厂只收到组件生命周期令牌；执行超时由工厂自行配置。所有等待者退出后工厂仍可继续，失败也会被观察并回收。

预取消不会创建执行；成功、失败和取消都会移除登记，后续调用重新执行。生命周期已取消时不再接受新调用；工厂需要协作响应令牌。工厂同步异常或任务异常传给调用者，返回 null 任务产生 `InvalidOperationException`。故障任务保持故障状态，取消任务保持取消状态。引用类型结果由等待者共享，建议返回不可变对象。工厂不能等待同实例同 key 的 `RunAsync`，以免形成循环等待。

## Cron 时间计算

```csharp
using Bing.Date.Cron;

var schedule = CronSchedule.Parse("0 9 * * 1-5");
var now = DateTimeOffset.UtcNow;
var next = schedule.GetNextOccurrence(now, businessTimeZone);
var occurrences = schedule.GetOccurrences(now, now.AddDays(7), businessTimeZone,
    maxCount: 20, cancellationToken: cancellationToken);

if (!CronSchedule.TryParse(expression, out var parsed, out var error))
    Console.WriteLine(error);
```

默认五段，六段需要显式指定 `CronExpressionFormat.IncludeSeconds`。起点默认不包含；`GetNextOccurrence(..., inclusive: true)` 可包含起点，无下一次匹配返回 null。枚举必须给出有限起止点及正数量上限，区间是 `[from,to)`，达到数量上限正常停止；参数立即校验，取消在枚举时检查。

使用固定依赖 Cronos **0.13.0**。月日与星期同时指定采用 **AND**，沿用其夏令时跳时和回拨规则：跳时调整到有效时刻；回拨期间按上游的间隔/固定时间语义处理。返回值统一为 UTC `DateTimeOffset`。只支持五段和六段，不声明 Quartz 七段兼容；本接口不提供 `H` 字段随机种子。解析错误给中文说明，保留上游异常作为内层异常，不编造字符位置。

## 可取消的大文本 Diff

```csharp
using Bing.Text.Diff;

var result = TextDiff.Compare(oldText, newText, new TextDiffOptions
{
    MaxLinesPerInput = 200_000,
    MaxCharsPerInput = 16_000_000,
    MaxDuration = TimeSpan.FromSeconds(30),
    IgnoreCase = false,
    IgnoreLeadingTrailingWhitespace = false
}, cancellationToken);

foreach (var block in result.Blocks)
{
    Console.WriteLine($"{block.Kind}: 旧 {block.OldStartLine}+{block.OldLineCount}，新 {block.NewStartLine}+{block.NewLineCount}");
}
```

上述值即默认配置。字符数按 UTF-16 代码单元计数，emoji 可能占两个单元。每份输入分别检查字符数与逻辑行数；所有上限必须为正值。计时从扫描前开始，以单调时钟覆盖扫描、哈希、计算和结果汇总。字符扫描最多每 4096 个代码单元检查、算法循环最多每 1024 步检查，并在阶段/子问题边界额外检查；预算属于协作限制，不承诺硬实时中断。

| 失败 | 异常 | 稳定代码 |
| --- | --- | --- |
| 调用方取消 | `OperationCanceledException` | 使用令牌判断 |
| 输入字符超限 | `TextDiffLimitExceededException` | `diff.input_characters` |
| 输入逻辑行超限 | `TextDiffLimitExceededException` | `diff.input_lines` |
| 计算预算耗尽 | `TextDiffLimitExceededException` | `diff.duration` |

失败不返回部分结果。内部算法使用显式工作栈和复用的线性数组，不构造行数乘积规模的矩阵；高差异输入仍可能耗尽预算。

`OldLines`、`NewLines` 为只读原始行集合，`Blocks` 为只读差异块集合。索引从 0 开始，插入时旧侧长度为零，删除时新侧长度为零，替换时两侧长度都非零。不为未变化行逐项创建结果。CRLF、LF、CR 都作为分隔符；末尾空行保留，空文本是零行。换行风格差别本身不产生变化。

默认大小写、空白敏感。可忽略序号大小写及行首尾 Unicode 空白，原始行仍完整保留；此时变化块表示比较语义上的差异，不能恢复被忽略的原文差异，完整目标行应读取 `NewLines`。

入口是同步方法。需要后台执行时由调用方安排执行上下文，并把令牌传入 `Compare`；仅取消外围等待不能停止算法。结果保有行字符串引用，不需要 Dispose，使用结束后释放对象引用即可。

算法来源为 DiffPlex `DiffPlex-1.7.0`，改动范围及 Apache-2.0 许可随包保存在 `ThirdParty/DiffPlex/`。正确性、规模、耗时和分配证据见 [实施记录](../quality/general-utilities-execution.md)。
