# DateTime / Date 行为迁移说明

本次调整保留现有公开签名、目标框架和依赖版本。涉及 Bing.Utils 主包及 Bing.Utils.DateTime 独立包；修复清单和验证证据见[实施记录](../quality/datetime-date-repair-report.md)。

## UTC 区间端点

`DateTimeRange` 采用闭区间，所有构造器和复制、包含、交并集、比较、相等性、哈希均使用 UTC 时间点。`StartTime/EndTime` 的 Kind 为 Utc，与 `UtcStartTime/UtcEndTime` 一致。Local 转 UTC；Unspecified 按本机本地时间解释。端点转换后排序，因此不同 Offset 的输入也不会产生负跨度。

```csharp
var start = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.FromHours(14));
var end = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.FromHours(-10));
var range = new DateTimeRange(start, end);
// StartTime: 2023-12-31T20:00:00Z；EndTime: 2024-01-01T19:00:00Z
// TotalHours == 23
var localDisplay = range.StartTime.ToLocalTime();
var logValue = range.StartTime.ToString("O");
```

旧代码若直接显示端点或调用默认 `ToString()`，现在显示 UTC 钟面值。显示时请显式转换到目标时区；自然日范围仍按本机日历创建，再存储为 UTC。跨 DST 的一天可能经过 23 或 25 小时，`TotalDays` 不能代替自然日计数。`GetDays/GetHours` 等整数方法继续保留原有舍入规则。

MinValue/MaxValue 保留为 UTC 全范围哨兵；其他本地值若转换后超出 UTC 可表示范围，将抛异常，不静默折叠成哨兵。持续时间构造器先规范化到 UTC，再加减实际经过时长。相接端点视为相交；`Union` 保留包围两个区间的既有行为。

## 时间戳生成

`NoRepeatTimeStampFactory` 的单值、批量和 Peek 使用同一条受锁保护的 UTC tick 序列。`GetTimeStamp()` 返回 Local，`GetUtcTimeStamp()` 返回 Utc；指定基准入口保留基准 Kind，Unspecified 按本地时间解释。混合入口的唯一性按 UTC 时间点判断；DST 回拨期间不要用本地钟面 Ticks 判断实际先后。如果候选时间点无法用 Unspecified 无损表示（例如重复钟面时间的第一次出现），工厂会在提交状态前拒绝，调用方可使用 UTC 或 Local 入口。

基准时间晚于上次结果时使用该基准；只有基准未前进时才使用配置增量，因此增量不是所有相邻结果的最小间隔。增量按 `incrementMs * 10000` 向零截断为 ticks。非有限数、非正数、少于 1 tick 或无法表示的值会抛 `ArgumentOutOfRangeException`；`0.0001ms` 在全部目标框架上均为 1 tick。参数失败、单值溢出或整批溢出不消耗序列状态，Peek 不推进序列。

`TimeStamp` 包装保持 tick 精度；`UnixTimeStamp` 包装按秒量化，亚秒增量可产生相同 Unix 秒值。单实例的唯一性仅覆盖一次 Reset 生命周期；`GeneratedCount` 继续是近似 tick 距离统计，不代表真实调用次数。性能基准保留锁并报告并发成本。COMB 的 Unix 时间字段量化到毫秒，同一毫秒内可以相同；消费者应按格式精度使用时间戳。

## 公开输入和异常参数

所有公开星期入口统一拒绝 0～6 以外的 `DayOfWeek` 值，包括日期工厂、上一/下一星期、月季年首尾定位、星期文本和星期枚举。普通入口抛 `ArgumentOutOfRangeException`；不会再把非法值当成星期日，`ToChinese` 也不再为非法值返回 null。`DateJudge.IsSameWeek` 在日期为空或跨年时仍先验证星期。

`TryOffsetByWeek` 的整数和枚举重载遇到非法年、月、月内星期序号或星期值时，均返回 false，并将输出设为 `DateTime.MinValue`。合法参数但当月没有第 5 个指定星期时继续返回同一哨兵。

月份、季度、年份偏移和星期工厂的范围异常使用调用入口的参数名（如 `months`、`quarters`、`years`、`occurrence`、`dayOfWeek`），不泄露内部转发参数；受检数值乘法仍可能抛 `OverflowException`。

时间戳异常继续使用 `ArgumentOutOfRangeException`，但 `ParamName` 按公开入口区分：

| 失败场景 | ParamName |
|---|---|
| 指定基准转换越界、生成超界或无法按基准 Kind 表示 | `baseTime` |
| 批量数量非法或整批超出 UTC 范围（含首项） | `count` |
| 批量或 Peek 的本地显示无法无损表示 | `useUtc` |
| 无时间参数的生成入口越界，或 Peek 的 UTC 序列耗尽 | null |

以上失败均不发布序列状态。无输入参数的失败不再使用 `referenceTicks` 或 `utcTicks` 等内部变量名。

## 周期、精度和数值边界

- `2.Years()` 表示两年；一元负号保留年月分量。`DateTimeOffset.SubtractDateTimeSpan` 同时减去年、月和固定时长。月末截断仍遵循 BCL 的日历规则。
- `DateTimeSpan ↔ NodaTime.Period` 保留年月和固定时长。单独把含非零年月的 Period 转为 TimeSpan/Duration 会抛 `InvalidOperationException`；应先提供日历锚点，或选择明确的近似换算。
- 既有 DateTimeSpan → TimeSpan/Duration 继续按每年 365 天、每月 30 天近似。纳秒转 TimeSpan 不足 100ns 的部分向零截断。
- `Set*` 保留未修改部分的 ticks、Kind 或 Offset；`EndOf*` 和日期范围工厂返回周期最后一个 tick，即 `.9999999`，旧的 `.9990000` 精确相等断言需调整。最大日期不先构造下一周期。
- 默认 `CreateFirstDayOfMonth` 返回当月 1 日；带星期参数的重载仍返回第一个指定星期。最后一个指定星期的查找方向已修正，非法星期立即拒绝。
- `DateTimeOffset.IsSameMonth` 同时比较各自的年、月；导航保留原 Offset。
- 周、季度、年和纪元单位换算不会整数回绕。合法负纪元由 `Time.OfEpochSecond/Milli` 支持；UnixTimeStamp 的非负契约不变，负毫秒在除法前拒绝。
- `IsDateBetweenWithBoundary` 包含下界，上界为原 max 值加一天且不包含；保留参数的时分秒。逆序返回 false；超过最大日期的上界覆盖剩余可表示日期。

## Kind 与历史入口

`ToEpochTimeSpan`：Utc 原样，Local 转 UTC，Unspecified 解释为 UTC。Bing.Helpers 中其他日期入口的旧 Unspecified 约定没有统一改写，跨接口调用应明确 Kind。

`ToUtc` 仍只修改 Kind 标签，并保留全部 ticks。真正的本地时间转 UTC 使用 `ToUniversalTime()` 或现有 `TimeZoneConversion`。`ToCst` 仍忽略输入并读取当前上海时间；它不是输入日期的时区转换方法。

## 农历和节气

干支年采用正确的一基天干/地支序号。闰年、闰月、闰日及年/月天数使用农历年/月/日；`ChineseMonth` 仍是 BCL 农历月序号（闰年可为 13），不等同于显示月份数字。农历转换受 ChineseLunisolarCalendar 自身范围约束。二十八星宿按自然日使用正模，参考日前后的 28 天周期一致。

节气使用[官方日期表](solar-term-data.md)，覆盖 **1900-01-01～2100-12-31**，按北京时间的日历日期查询，运行时不联网。输入只读取年月日字段；不会因输入 Kind 或本机时区改变所查日期。需要按时间点查询时，调用方先转换成北京时间日期。

当日查询只匹配当天，上一/下一节气严格排除当天并跨年查找。`out DateTime` 返回午夜、Kind=Unspecified，不再返回旧公式估算的交节时刻。范围外或无前后项时继续返回空字符串/null，out 保留 calendar.MinSupportedDateTime 哨兵。2023-06-21 为夏至，6 月 20 日和 22 日均不能命中。

数据以 2026-10-29 记录的官方表版本为准，精度为日；其来源、长期日期不确定性和再生成方法见数据说明。DateOnly/TimeOnly、ISO 周、新时钟接口和工作日快速算法留待后续需求。
