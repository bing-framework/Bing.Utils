# DateTime 农历日期指南

`Bing.Utils.DateTime` 的 `Bing.Date.Chinese` 命名空间提供日期字段与农历日期之间的转换。计算使用 .NET `ChineseLunisolarCalendar`，不会联网或自动判断法定假期。方案及验收范围见[农历接入方案](../superpowers/plans/2026-11-06-datetime-lunar-integration-solution.md)。

## 公历日期转农历

```csharp
using System;
using Bing.Date.Chinese;

var lunar = new DateTime(2023, 3, 22).ToLunarDate();
Console.WriteLine($"{lunar.Year}, {lunar.Month}, {lunar.Day}, {lunar.IsLeapMonth}");
// 2023, 2, 1, True
Console.WriteLine(lunar.ToChineseString());
// 二零二三年闰二月初一
Console.WriteLine(lunar.ToChineseString(traditionalChineseCharacters: true));
// 貳零貳叁年閏貳月初壹
```

`DateTime` 入口只读取年月日，忽略时间和 Kind，不自动按本地时区或北京时间转换。`Month` 是 1～12 的显示月份，`IsLeapMonth` 区分同名普通月和闰月；`CalendarMonth` 是 BCL 使用的月份序号，闰年可能为 13。两个不同农历日期不会因为显示月份相同而被视为相等。

日期范围为 **1901-02-19～2101-01-28**，包括两端，按 BCL 历法的支持范围确定。范围外 `ToLunarDate` 抛 `ArgumentOutOfRangeException`；需要避免预期输入错误时使用 `TryToLunarDate`。

```csharp
if (new DateTime(1800, 1, 1).TryToLunarDate(out var result))
    Console.WriteLine(result.ToChineseString());
else
    Console.WriteLine("日期不在农历支持范围内");
```

`default(LunarDate)` 不是合法日期，其 `IsValid` 为 false，数值属性为零；转换、计算及格式化方法抛 `InvalidOperationException`。

## 时间点与自然日

`DateTimeOffset.ToLunarDate()` 先按**固定 UTC+08:00** 获取自然日，再计算农历，和服务器本地时区无关。这是固定偏移，不采用历史上海时区的夏令时规则。

```csharp
var instant = new DateTimeOffset(2024, 2, 9, 16, 0, 0, TimeSpan.Zero);
var newYear = instant.ToLunarDate();
// 北京时间 2024-02-10，农历 2024 年正月初一。
```

业务若需其他时区的自然日，先调用 `TimeZoneConversion.ToTimeZone(instant, zone)`，再对结果的 `.DateTime` 调用 `ToLunarDate()`。农历日期只保存自然日；如需保留事件时刻与 Offset，请另存原 `DateTimeOffset`。

## 农历日期转公历

```csharp
var ordinary = ChineseLunarCalendar.Create(2023, 2, 1);
var leap = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);

Console.WriteLine(ordinary.ToDateTime()); // 2023-02-20 00:00:00
Console.WriteLine(leap.ToDateTime());     // 2023-03-22 00:00:00
```

返回值是午夜、`Kind=Unspecified` 的公历自然日，不能还原原输入的时刻、Kind 或 Offset。无效年、月、日抛 `ArgumentOutOfRangeException`；指定该年不存在的闰月抛 `ArgumentException`。`TryCreate` 对这些无效输入返回 false，输出无效默认值。

## 日与月偏移

```csharp
var normalMonth = ChineseLunarCalendar.Create(2023, 2, 1);
var leapMonth = normalMonth.AddMonths(1);  // 闰二月初一
var thirdMonth = leapMonth.AddMonths(1);   // 三月初一
var tomorrow = leapMonth.AddDays(1);      // 闰二月初二
```

`AddDays` 移动自然日，不表示某个时间点经过了 24 小时。`AddMonths` 依实际农历月份顺序计算，闰月占一个月。目标月不存在原日号时，默认抛 `ArgumentOutOfRangeException`；显式指定 `LunarDayOverflow.ClampToLastDay` 可选择目标月最后一天。截断后反向移动不保证得到原日期。

按农历年份计算周年日期可使用 `InYear` 或 `AddYears`；闰月缺失及日号缺失的策略与示例见[日期模块扩展能力](datetime-date-extended.md#农历周年)。

## 旧接口迁移

旧 `ChineseDateInfo.ChineseMonth` 与 `OfLunar(year, month, day)` 继续使用 BCL 月序号；迁移时可读取新类型的 `CalendarMonth` 对照，业务输入则应采用 `Month` 与 `IsLeapMonth`。旧 `ChineseDateInfo.AddMonths` 仍按公历月计算，新 `LunarDate.AddMonths` 按农历月计算。

`ChineseDateInfo.GetChineseYear` 及 `GetChineseDateWithYear` 现在使用农历年份；例如 2024-02-09 的完整日期为“二零二三年腊月三十”。旧 `ChineseDateHelper.GetChineseYear(DateTime)` 仍格式化公历年份。主包 `ToChineseDateString` 输出公历中文日期，不等同于新 `ToLunarDateString`。

无历法参数的旧 `DateTimeLeapExtensions.IsLeapYear/IsLeapMonth/IsLeapDay` 现在使用内置农历历法，读取输入日期字段并遵循农历支持范围。农历闰年指包含闰月的年份，BCL 农历中的闰日是闰月中的日期；显式历法重载传入 null 仍抛 `ArgumentNullException`。同时使用 `Bing.Date` 和 `Bing.Date.Chinese` 时，公历与农历的无参 `IsLeapYear` 扩展可能产生重名，应使用静态调用明确历法：

```csharp
using System;
using Bing.Date;
using Bing.Date.Chinese;

var date = new DateTime(2023, 3, 22);
bool lunarLeapYear = DateTimeLeapExtensions.IsLeapYear(date); // true，农历闰年
bool lunarLeapMonth = date.IsLeapMonth();                     // true，农历闰二月
bool lunarLeapDay = date.IsLeapDay();                         // true
bool gregorianLeapYear = DateTime.IsLeapYear(date.Year);      // false，公历平年
```

## 性能与并发

新农历日期转换、农历运算和无历法参数的闰年/月/日判断使用单个内部 BCL 历法实例，并通过锁保护。数值结果是不可变值类型，线程可以共享；无需每次创建 `ChineseLunisolarCalendar`。多线程调用会竞争同一把锁，文本格式化会分配字符串。接口不缓存按日期增长的结果，具体吞吐和内存开销以[验收记录](../quality/datetime-lunar-integration-2026-11-06.md)中的测量条件为准。

显式传入的历法及 `ChineseDateInfo` 持有的历法不使用该共享锁。跨线程共享这些可变实例时，调用方应避免并发修改历法配置，并负责必要的同步。
