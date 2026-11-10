# DateTime / Date 行为迁移说明

本次调整保留既有方法的运行时签名、目标框架和依赖版本，并新增可注入时钟的重载及严格 JSON 转换器。`Clone` 重复扩展的源码调用调整见下文。涉及 Bing.Utils 主包及 Bing.Utils.DateTime 独立包；原修复清单和验证证据见[实施记录](../quality/datetime-date-repair-report.md)。

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

`date.In(IDateTimeRange, mode)` 与 `DateTimeRange.Contains(date)` 采用相同的时间点规则。自定义区间的端点先规范化并排序，再按 `RangeMode` 比较；仅接受两个独立日期端点的 `In(start, end, mode)` 重载仍按传入日期字段比较。

## 时间戳生成

`NoRepeatTimeStampFactory` 的单值、批量和 Peek 使用同一条受锁保护的 UTC tick 序列。`GetTimeStamp()` 返回 Local，`GetUtcTimeStamp()` 返回 Utc；指定基准入口保留基准 Kind，Unspecified 按本地时间解释。混合入口的唯一性按 UTC 时间点判断；DST 回拨期间不要用本地钟面 Ticks 判断实际先后。如果候选时间点无法用 Unspecified 无损表示（例如重复钟面时间的第一次出现），工厂会在提交状态前拒绝，调用方可使用 UTC 或 Local 入口。

基准时间晚于上次结果时使用该基准；只有基准未前进时才使用配置增量，因此增量不是所有相邻结果的最小间隔。增量按 `incrementMs * 10000` 向零截断为 ticks。非有限数、非正数、少于 1 tick 或无法表示的值会抛 `ArgumentOutOfRangeException`；`0.0001ms` 在全部目标框架上均为 1 tick。参数失败、单值溢出或整批溢出不消耗序列状态，Peek 不推进序列。

`TimeStamp` 包装保持 tick 精度；`UnixTimeStamp` 包装按秒量化，亚秒增量可产生相同 Unix 秒值。单实例的唯一性仅覆盖一次 Reset 生命周期；`GeneratedCount` 继续是近似 tick 距离统计，不代表真实调用次数。性能基准保留锁并报告并发成本。COMB 的 Unix 时间字段量化到毫秒，同一毫秒内可以相同；消费者应按格式精度使用时间戳。

`TimeStamp` 与 `UnixTimeStamp` 的比较、哈希、`GetDifference`、`GetTimeSpan` 及减法统一使用 .NET ticks；Unix 秒值先换算为 UTC 纪元后的整秒 ticks。普通 `TimeStamp` 仍保留输入的原始 ticks，不按 Kind 换算时区：混合比较实际时间点时，应先将普通时间戳转换为 UTC。Unix 同一秒内的亚秒部分不参与比较。

`UnixTimeStamp` 的范围为 0～253402300799 秒，与本机时区无关；`DateTime` 构造入口也拒绝纪元前的输入，包括不足一秒的负偏移。Local 按本机时区转换，Unspecified 沿用 UTC 解释；无法表示的 Local 转 UTC 抛 `ArgumentOutOfRangeException`。算术单位换算或整数加法溢出抛 `OverflowException`，结果超出 Unix 范围抛 `ArgumentOutOfRangeException`。

整数构造器的 `ToDateTime()` 仍返回本地时间，接近最大日期时 BCL 可能将不可表示的本地值饱和到 `DateTime.MaxValue`；需要无损时间点时使用 `ToUtcDateTime()`。对饱和值执行 `Add(TimeSpan)` 或 `Subtract(TimeSpan)` 时，结果改用 UTC 表示；普通情况下继续沿用原来的钟面运算。

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

- `DateInfo(DateTime)` 保留输入日期与 Kind，并清除全部时间刻度，包括毫秒以下 ticks；若需要保留时刻，应保留原始 `DateTime`。
- `DateTimeSpan.Equals(object)` 只接受 `DateTimeSpan`，不再将装箱的 `TimeSpan` 判为相等；同类型相等和哈希行为不变。需要跨类型比较时可使用现有的 `==` 运算符或先显式转换。
- `2.Years()` 表示两年；一元负号保留年月分量。`DateTimeOffset.SubtractDateTimeSpan` 同时减去年、月和固定时长。月末截断仍遵循 BCL 的日历规则。
- `DateTimeSpan ↔ NodaTime.Period` 保留年月和固定时长。单独把含非零年月的 Period 转为 TimeSpan/Duration 会抛 `InvalidOperationException`；应先提供日历锚点，或选择明确的近似换算。
- 既有 DateTimeSpan → TimeSpan/Duration 继续按每年 365 天、每月 30 天近似。纳秒转 TimeSpan 不足 100ns 的部分向零截断。
- `Set*` 保留未修改部分的 ticks、Kind 或 Offset；`EndOf*` 和日期范围工厂返回周期最后一个 tick，即 `.9999999`，旧的 `.9990000` 精确相等断言需调整。最大日期不先构造下一周期。
- 默认 `CreateFirstDayOfMonth` 返回当月 1 日；带星期参数的重载仍返回第一个指定星期。最后一个指定星期的查找方向已修正，非法星期立即拒绝。
- `DateTimeOffset.IsSameMonth` 同时比较各自的年、月；导航保留原 Offset。
- `GetMonthDiff` 按年月日字段返回完整月数的绝对值，忽略时分秒和 Kind；较晚日期的日号更小时扣除一个月。`2023-01-15` 至 `2023-05-10` 为 3，同月相差一天为 0；月末不自动视为相同日号，1 月 31 日至 2 月 28 日为 0。依赖旧多算一个月结果的断言和业务计算需调整。
- 周、季度、年和纪元单位换算不会整数回绕。合法负纪元由 `Time.OfEpochSecond/Milli` 支持；UnixTimeStamp 的非负契约不变，负毫秒在除法前拒绝。
- `IsDateBetweenWithBoundary` 包含下界，上界为原 max 值加一天且不包含；保留参数的时分秒。逆序返回 false；超过最大日期的上界覆盖剩余可表示日期。

## Kind 与历史入口

`ToEpochTimeSpan`：Utc 原样，Local 转 UTC，Unspecified 解释为 UTC。Bing.Helpers 中其他日期入口的旧 Unspecified 约定没有统一改写，跨接口调用应明确 Kind。

`ToUtc` 仍只修改 Kind 标签，并保留全部 ticks。本机本地时间转 UTC 使用 `ToUniversalTime()`；指定地区的 Unspecified 钟面值使用 `TimeZoneConversion.ResolveLocalTime(localTime, sourceTimeZone, resolution)` 解析为明确时间点。已有 DateTimeOffset 时间点的目标时区显示使用 `TimeZoneConversion.ToTimeZone(instant, destinationTimeZone)`。`ToCst` 仍忽略输入并读取当前上海时间；它不是输入日期的时区转换方法。可运行示例见[使用指南](datetime-date-usage.md)。

`ElapsedTime`、`ElapsedMilliseconds`、`IsInTheFuture` 和 `IsInThePast` 现在按 UTC 时间点比较：Local 与 Unspecified 按本机时区解释。对应重载可接收 `Bing.Date.Clock.IClock`，未提供时使用系统时钟；历史的 `ToCst` 等入口仍有各自的时钟语义。本地输入转换为 UTC 超出可表示范围时抛 `ArgumentOutOfRangeException`。`ElapsedMilliseconds` 返回 `int`，整毫秒超出范围时抛 `OverflowException`；需要更大范围时使用 `ElapsedTime`。

`TimeZoneConversion.ToTimeZone` 成功时保留原 UTC 时间点并返回目标偏移；目标本地日期越界时抛 `ArgumentOutOfRangeException`，参数名为 `instant`。这替代了底层时区 API 在该边界静默返回极值的行为。

历史 `Time.OfEpochSecond/Milli` 继续遵循 BCL 的 `LocalDateTime` 转换：UTC 极值在某些本机时区无法表示为本地日期时，会饱和到本地 MinValue 或 MaxValue，不能保证转回原时间戳。相应的本地日期转 UTC 也遵循 BCL 边界行为。需要完整保留极值时间点时，使用 `DateTimeOffset.FromUnixTimeSeconds/FromUnixTimeMilliseconds`，并按 UTC 保存。

`CompareInterval` 的月／年计算继续按月份逐次推进并返回非负值，最大日期不会因额外推进而溢出。`ToJsGetTime()` 的毫秒分支使用整数 ticks 和与文化无关的数字格式，亚毫秒部分向零截断；传入 `false` 的历史秒分支仍保持四舍五入行为。

## 日期复制、设置与取整

`DateTimeOffset.LastDayOfQuarter()` 直接计算季度末年月日，保留原时间 ticks 和 Offset。合法结果不再因中间季度首日越界而失败，例如 `0001-02-01 00:00 +14:00` 可返回 `0001-03-31 00:00 +14:00`；最终本地或 UTC 时间确实越界时仍抛 `ArgumentOutOfRangeException`。

`DateTime.Clone()` 现在直接复制值，保留 Local 日期在夏令时回拨期间选择的实际时间点。各分量设置方法、`Round`、`RoundUp` 和 `RoundDown` 通过原值的钟面 tick 偏移保留同一标记；`SetKind` 在 Kind 未变时也保留原值。设置方法仍校验日期和时间分量，并保留未修改分量及毫秒以下 ticks。这些操作不执行时区转换，不会重新解析 DST 缺失或重复时间；需要显式消歧时使用 `TimeZoneConversion.ResolveLocalTime`。

`BeginningOfSecond/Minute/Hour` 截断低位 ticks 时也保留该标记。自然日及周、月、季度、年的起点导航，以及季度首尾日期，继续保留原值的 Kind 和重复时间选择。`AnnualBusinessCalendar.AddBusinessDays` 按目标日偏移原值，保留时间精度和同一标记；正反业务日偏移返回原钟面日期时，不会把回拨期间第一次出现改成第二次出现。以上均按钟面和日历日期运算，实际经过时长受 DST 影响。

同时导入 `System` 和 `Bing.Date` 后可以直接调用 `date.Clone()`。扩展方法统一由 `System.BingDateTimeExtensions.Clone` 提供；`Bing.Date.DateTimeExtensions.Clone(DateTime)` 保留为静态兼容方法，已有编译结果仍可绑定原方法。只使用 DateTime 类型别名且未导入 `System` 的源码应补上 `using System;`。

```csharp
using System;
using Bing.Date;

var original = new DateTime(2024, 11, 3, 5, 30, 0, DateTimeKind.Utc).ToLocalTime();
var copy = original.Clone();
// 在纽约本地时区，original 是回拨前第一次 01:30；复制和原值设置仍对应 05:30 UTC。
bool sameInstant = copy.ToUniversalTime() == original.ToUniversalTime();
bool sameAfterSetting = original.SetMinute(original.Minute).ToUniversalTime() == original.ToUniversalTime();

var minimum = new DateTimeOffset(1, 1, 1, 14, 0, 0, TimeSpan.FromHours(14));
var rounded = minimum.Round(RoundTo.Day); // 0001-01-02 00:00:00 +14:00
```

`DateTimeOffset.Round` 先完成钟面取整，再使用原 Offset 构造最终结果。输入及最终结果均合法时，不会因中间向下截断的值越过 UTC 边界而失败；最终钟面日期或 UTC 时间点确实越界时仍抛 `ArgumentOutOfRangeException`。秒、分、时、日的中点进位规则保持不变。

## 严格 JSON 日期解析

原 `DateTimeJsonConverter`、`UtcDateTimeJsonConverter` 及其可空版本保留现有行为，无法解析的字符串仍映射为 `DateTime.MinValue`。需要严格输入时，显式注册 `StrictDateTimeJsonConverter` 和 `StrictNullableDateTimeJsonConverter`：它们接受 System.Text.Json 支持的 ISO 8601 日期字符串，非法字符串或错误类型抛 `JsonException`，可空类型允许 JSON null。写出使用完整日期精度；带非零偏移的输入按 System.Text.Json 规则转换为运行机器的 Local 时间，跨机器需要保留原始偏移时应使用 `DateTimeOffset`。

## 农历和节气

新增农历日期值、DateTime 扩展、显示月与闰月标志的迁移方式见[农历日期指南](datetime-lunar-guide.md)。旧农历接口的月序号和公历月偏移行为保持不变。

`ChineseDateInfo.GetChineseYear` 和 `GetChineseDateWithYear` 改为输出农历年份，因此春节前可能与公历年份不同。需要公历中文年份时，使用 `ChineseDateHelper.GetChineseYear(DateTime)`。对象的农历年份格式化现在会验证其历法和支持范围。

`DateTimeLeapExtensions` 的无历法参数闰年、闰月和闰日方法现在使用共享农历历法，替代原先固定抛出的空历法异常；日期超出支持范围时抛 `ArgumentOutOfRangeException`。显式历法参数为 null 时仍抛 `ArgumentNullException`。同时引用公历和农历扩展时，可用 `DateTimeLeapExtensions.IsLeapYear(date)` 明确选择农历判断。

新增 `SolarTermCalendar` 的类型化入口和 `AnnualBusinessCalendar` 的版本化配置用法见[能力补齐指南](datetime-capabilities.md)。新入口的明确范围异常不改变下述旧节气接口的空字符串/null 契约。

干支年采用正确的一基天干/地支序号。闰年、闰月、闰日及年/月天数使用农历年/月/日；`ChineseMonth` 仍是 BCL 农历月序号（闰年可为 13），不等同于显示月份数字。农历转换受 ChineseLunisolarCalendar 自身范围约束。二十八星宿按自然日使用正模，参考日前后的 28 天周期一致。

节气使用[官方日期表](solar-term-data.md)，覆盖 **1900-01-01～2100-12-31**，按北京时间的日历日期查询，运行时不联网。输入只读取年月日字段；不会因输入 Kind 或本机时区改变所查日期。需要按时间点查询时，调用方先转换成北京时间日期。

当日查询只匹配当天，上一/下一节气严格排除当天并跨年查找。`out DateTime` 返回午夜、Kind=Unspecified，不再返回旧公式估算的交节时刻。范围外或无前后项时继续返回空字符串/null，out 保留 calendar.MinSupportedDateTime 哨兵。2023-06-21 为夏至，6 月 20 日和 22 日均不能命中。

数据以 2026-10-29 记录的官方表版本为准，精度为日；其来源、长期日期不确定性和再生成方法见数据说明。新增的时钟重载、ISO 周、DateOnly/TimeOnly 适配及农历周年计算见[日期模块扩展能力](datetime-date-extended.md)。基础逐日工作日算法的进一步优化仍需结合工作负载评估；年度业务日历已提供索引。
