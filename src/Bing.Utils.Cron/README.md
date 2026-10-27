# Bing.Utils.Cron

提供 Cron 表达式解析及按时区计算触发时间的能力，命名空间为 `Bing.Date.Cron`。实现基于 Cronos 0.13.0，不包含任务调度器。

## 基本使用

```csharp
using Bing.Date.Cron;

var schedule = CronSchedule.Parse("30 9 * * 1-5");
var nextUtc = schedule.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);

var boundedSchedule = CronSchedule.Parse("*/15 * * * *");
var end = DateTimeOffset.UtcNow.AddHours(1);
foreach (var occurrence in boundedSchedule.GetOccurrences(DateTimeOffset.UtcNow, end, TimeZoneInfo.Utc, maxCount: 4))
    Console.WriteLine(occurrence);
```

标准格式包含分、时、日、月、星期五段；`CronExpressionFormat.IncludeSeconds` 启用秒字段的六段格式。所有计算结果都是 UTC `DateTimeOffset`。区间枚举使用 `[from, to)`，包含起点匹配，最多返回 `maxCount` 项，并在枚举期间响应取消令牌。

月日和星期同时指定时按 AND 匹配。夏令时无效本地时间的调整、回拨期间间隔表达式和固定时刻表达式的处理遵循 Cronos 语义。

## 错误处理

| 情况 | 行为 |
|---|---|
| 表达式无效 | `Parse` 抛出带中文说明的 `FormatException`；`TryParse` 返回 `false`，错误重载提供中文说明 |
| 表达式使用 `H` 字段但未提供随机种子 | `Parse` 抛出带中文说明的 `FormatException`；`TryParse` 返回 `false` |
| 表达式为空 | `Parse` 抛出 `ArgumentException`；`TryParse` 返回 `false` |
| 格式枚举值无效 | 抛出 `ArgumentOutOfRangeException` |
| 时区为空、次数上限不为正数、区间倒置 | 在调用时抛出对应参数异常 |
| 搜索范围内没有匹配 | 下一次触发时间为 `null`，区间枚举为空 |
| 枚举期间取消 | 抛出 `OperationCanceledException` |

Cron 字段、特殊字符和夏令时行为参见 [Cronos 文档](https://github.com/HangfireIO/Cronos)。
