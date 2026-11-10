# 日期模块扩展能力

本文对应当前工作区中的时钟注入、ISO 周、`DateOnly`/`TimeOnly` 适配和农历周年计算；不代表 NuGet 已发布。

## 时钟与业务自然日

```csharp
using System;
using Bing.Date;
using Bing.Date.Clock;

var zone = TimeZoneInfo.CreateCustomTimeZone("Example+08", TimeSpan.FromHours(8), "Example", "Example");
IClock clock = new FixedClock(new DateTimeOffset(2024, 2, 9, 16, 0, 0, TimeSpan.Zero));
var today = DateTimeFactory.Today(clock, zone); // 2024-02-10，Kind=Unspecified
bool matches = new DateTime(2024, 2, 10).IsToday(clock, zone); // true
var utc = DateTimeFactory.UtcNow(clock); // Kind=Utc
var tomorrowLocal = 1.Days().FromNow(clock); // 按本机本地时间偏移
```

`DateTime.IsToday(clock, zone)` 只比较输入年月日，不根据 Kind 把输入转换为时间点；`DateTimeOffset.IsToday(clock, zone)` 将输入时间点和时钟分别投影到业务时区。两者的可空重载对 null 返回 false，并仍验证时钟和时区。指定时区中的当前时间无法表示时抛出 `ArgumentOutOfRangeException`。

`Now(clock)`、`Before(clock)`、`FromNow(clock)` 和对应 Offset 方法沿用本机本地时间语义。Offset 方法计算时使用取得时的固定偏移，跨 DST 后不会重新解析目标时区；需要按地区日历规则计算时应显式解析目标本地时间。`Now(clock)` 的极值本地转换沿用 BCL 饱和行为。原无参方法的行为不变。

## ISO 周

```csharp
using System;
using Bing.Date;

var week = IsoWeekCalendar.GetIsoWeek(new DateTime(2021, 1, 1)); // (2020, 53)
var monday = IsoWeekCalendar.GetDate(week.WeekYear, week.WeekNumber, DayOfWeek.Monday); // 2020-12-28
var weeksInYear = IsoWeekCalendar.GetWeeksInYear(2020); // 53
```

输入按公历年月日字段读取，不转换时区；反向结果是午夜、`Kind=Unspecified`。年份和周号须合法，反向结果超出 `DateTime` 范围时抛异常。这里的 ISO 周所属年份可能与公历年份不同；旧 `GetWeekOfYear` 的文化和周起始规则不变。

## DateOnly 和 TimeOnly

以下适配只存在于 `net6.0`、`net7.0`、`net8.0` 目标程序集；`netstandard2.0` 没有这些类型或对应 API。

```csharp
using System;
using Bing.Date;
using Bing.Date.Chinese;
using Bing.Date.TimeZones;

var date = new DateOnly(2023, 3, 22);
var lunar = date.ToLunarDate(); // 农历闰二月初一
var original = lunar.ToDateOnly();
var isoWeek = date.GetIsoWeek();
var zone = TimeZoneInfo.CreateCustomTimeZone("Example+08", TimeSpan.FromHours(8), "Example", "Example");
var instant = date.ResolveLocalTime(new TimeOnly(12, 30), zone);
```

`DateOnlyTimeOnlyExtensions.GetDateFromIsoWeek` 可从 ISO 年、周和星期恢复 `DateOnly`。年度及跨年业务日历提供 `DateOnly` 的 `IsBusinessDay`、`CountBusinessDays` 和 `AddBusinessDays` 重载；计数区间依旧是左闭右开。`ResolveLocalTime` 保留时间 ticks，夏令时跳时拒绝，重复时间默认拒绝，可显式指定 `AmbiguousTimeResolution.Earlier` 或 `Later`。

## 农历周年

```csharp
using Bing.Date.Chinese;

var leapBirthday = ChineseLunarCalendar.Create(2023, 2, 1, isLeapMonth: true);
var anniversary = leapBirthday.InYear(2024, LunarLeapMonthOverflow.UseRegularMonth); // 2024 普通二月初一
var next = leapBirthday.AddYears(19); // 2042 闰二月初一
var lastDay = ChineseLunarCalendar.Create(2023, 2, 30)
    .InYear(2025, missingDay: LunarDayOverflow.ClampToLastDay); // 2025 二月廿九
```

`InYear` 以农历年为目标，`AddYears` 按农历年偏移；两者默认拒绝目标年缺少同名闰月或目标月缺少原日号。可以分别选择 `UseRegularMonth` 和 `ClampToLastDay`，两种策略可组合；截断或替代后往返不保证还原原日期。支持范围仍由 `ChineseLunisolarCalendar` 决定。

## 资源与验证边界

ISO 周使用现有 NodaTime 周规则；农历周年计算复用内部历法和锁，不创建按日期增长的缓存。以上是实现方式，不是吞吐或零分配承诺。稳定机器并发性能、资源容量和外部 CI 仍需单独验收。
