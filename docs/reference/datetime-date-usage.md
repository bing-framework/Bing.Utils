# DateTime / Date 使用指南与本次验收

验收日期：2026-11-06。适用当前工作区源码；不表示这些修改已经发布到 NuGet。

本文记录基础功能验收。后续新增的显式时区、版本化年度日历、类型化节气及资源探针见[能力补齐指南](datetime-capabilities.md)，最新新增能力测试结果见[验收记录](../quality/datetime-capabilities-2026-11-06.md)。

本次使用隔离 SDK 8.0.425、Release/net8.0 重新构建并测试：独立日期包 691/691、主包日期与严格 JSON 457/457、IdUtils 消费者 39/39，共 1,187 项通过，0 失败、0 跳过。构建存在仓库其他模块的既有警告。本次没有修改生产代码。

TRX 位于 `C:/Users/jianx/AppData/Local/Temp/bing-datetime-acceptance-current/` 的 `datetime.trx`、`main-date.trx`、`id-consumers.trx`；临时文件可能被系统清理。五框架、三时区与覆盖率的历史结果见[后续修复记录](../quality/datetime-date-followup-2026-11-06.md)，不计入本次测试数。

功能回归验收通过。时间戳并发性能仍需稳定环境确认；GitHub Actions 工作流尚无外部执行结果，不能据此宣称发布门禁全部通过。

## 引用与选型

仓库内引用 `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj` 即可传递引用主包。只需要区间、时间戳工厂或 JSON 转换器时，可单独引用 `src/Bing.Utils/Bing.Utils.csproj`。

| 需求 | API | 使用要点 |
|---|---|---|
| 时间点范围 | `DateTimeRange`、`date.In(range)` | UTC 闭区间；显示时转换目标时区 |
| 年月周期、末 tick | `Years/Months/From`、`EndOfDay` | 年月是日历分量；日末为 23:59:59.9999999 |
| 节假日与补班 | `BusinessCalendar` | 调用方提供日期；补班优先；计数为半开区间 |
| 稳定的相对时间测试 | `FixedClock`、`ElapsedTime(clock)` | 显式固定时间；未传时钟的入口使用系统时钟 |
| 时区显示、夏令时 | `TimeZoneConversion` | 解读钟面值与转换时间点使用不同入口 |
| 同实例递增时间戳 | `NoRepeatTimeStampFactory` | 复用实例，优先 UTC；不是分布式 ID |
| 严格 JSON 输入 | 两个 `Strict*DateTimeJsonConverter` | 显式注册；错误输入抛 `JsonException` |
| 节气日期 | `ChineseSolarTermHelper` | 1900～2100 年，北京时间自然日，精度到日 |

## 可运行示例

以下代码可作为引用上述项目的 net8.0 控制台程序的 `Program.cs`。示例全部使用固定值，无需修改系统时区或访问网络。本次已将代码块原样提取到临时控制台项目，引用当前源码编译运行，17 项示例断言全部通过。

```csharp
using System;
using System.Globalization;
using System.Text.Json;
using Bing.Date;
using Bing.Date.Calendars;
using Bing.Date.Chinese;
using Bing.Date.Clock;
using Bing.Date.TimeZones;
using Bing.Serialization.SystemTextJson;

static void Check(bool condition, string scenario)
{
    if (!condition) throw new InvalidOperationException(scenario);
    Console.WriteLine($"PASS: {scenario}");
}

// 1. 不同偏移的端点先按 UTC 排序，实际时长为 23 小时。
var range = new DateTimeRange(
    new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.FromHours(14)),
    new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.FromHours(-10)));
Check(range.TotalHours == 23 && range.StartTime.Kind == DateTimeKind.Utc,
    "UTC range");
Check(range.EndTime.In(range) && !range.EndTime.In(range, RangeMode.CloseOpen),
    "Range boundaries");

// 2. 日历月遵循月末截断；末日保留最后一个 tick。
var monthEnd = 1.Months().From(new DateTime(2024, 1, 31));
Check(monthEnd == new DateTime(2024, 2, 29), "Calendar month");
Check(monthEnd.EndOfDay().Ticks == monthEnd.AddDays(1).Ticks - 1, "Last tick");

// 3. 演示自定义规则，不是内置法定节假日表。
var business = new BusinessCalendar(
    holidays: new[] { new DateTime(2024, 1, 1) },
    workingDayOverrides: new[] { new DateTime(2024, 1, 6) });
var deadline = business.AddBusinessDays(new DateTime(2023, 12, 29, 9, 0, 0), 1);
Check(deadline == new DateTime(2024, 1, 2, 9, 0, 0), "Business day offset");
Check(business.CountBusinessDays(new DateTime(2024, 1, 1),
    new DateTime(2024, 1, 8)) == 5, "Business day count");

// 4. 固定时钟可避免测试依赖 DateTime.Now。
var clock = new FixedClock(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
var created = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
Check(created.ElapsedTime(clock) == TimeSpan.FromHours(2), "Fixed clock");
Check(created.AddHours(3).IsInTheFuture(false, clock), "Future instant");

// 5. 自建固定偏移用于可移植演示；有夏令时的地区应传实际 TimeZoneInfo。
var utcPlus8 = TimeZoneInfo.CreateCustomTimeZone("Example-UTC+08",
    TimeSpan.FromHours(8), "Example UTC+08", "Example UTC+08");
var display = TimeZoneConversion.ToTimeZone(clock.UtcNow, utcPlus8);
Check(display.Hour == 20 && display.Offset == TimeSpan.FromHours(8)
    && display.UtcDateTime == clock.UtcNow.UtcDateTime, "Timezone display");
var resolved = TimeZoneConversion.ResolveLocalTime(
    new DateTime(2024, 1, 1, 20, 0, 0, DateTimeKind.Unspecified), utcPlus8);
Check(resolved == clock.UtcNow, "Resolve wall time");

// 6. 使用相同基准演示精确递增 1 tick；真实业务复用同一工厂实例。
var factory = new NoRepeatTimeStampFactory(incrementMs: 0.0001);
var first = factory.GetTimeStamp(created);
var second = factory.GetTimeStamp(created);
Check(second.Ticks - first.Ticks == 1 && second.Kind == DateTimeKind.Utc,
    "One tick increment");
var batch = factory.GetTimeStamps(3, useUtc: true);
Check(batch[0] < batch[1] && batch[1] < batch[2], "UTC batch");

// 7. 严格转换器用于显式选择拒绝非法输入的接口。
var jsonOptions = new JsonSerializerOptions();
jsonOptions.Converters.Add(new StrictDateTimeJsonConverter());
jsonOptions.Converters.Add(new StrictNullableDateTimeJsonConverter());
var parsed = JsonSerializer.Deserialize<DateTime>("\"2024-01-01T12:00:00Z\"", jsonOptions);
Check(parsed == clock.UtcNow.UtcDateTime && parsed.Kind == DateTimeKind.Utc,
    "Strict JSON");
Check(JsonSerializer.Deserialize<DateTime?>("null", jsonOptions) == null,
    "Nullable JSON");
bool rejected = false;
try { JsonSerializer.Deserialize<DateTime>("\"invalid\"", jsonOptions); }
catch (JsonException) { rejected = true; }
Check(rejected, "Reject invalid JSON date");

// 8. 节气读取年月日字段；先将真实时间点转换为北京时间再取日期。
var lunar = new ChineseLunisolarCalendar();
Check(ChineseSolarTermHelper.GetSolarTerm(lunar, new DateTime(2023, 6, 21))
    == "夏至", "Solar term");
Check(ChineseSolarTermHelper.GetSolarTerm(lunar, new DateTime(2023, 6, 20))
    == string.Empty, "Non solar-term day");
```

## 使用固定时钟测试有效期

业务可以将当前时间通过 `IClock` 传入判断逻辑，并在测试中使用 `FixedClock` 指定准确时刻。以下示例自行约定有效期为 `[created, expires)`：创建时刻有效，到期时刻失效。

```csharp
using System;
using Bing.Date;
using Bing.Date.Clock;

var created = new DateTime(2024, 5, 1, 4, 0, 0, DateTimeKind.Utc);
var expires = new DateTime(2024, 5, 1, 4, 30, 0, DateTimeKind.Utc);
IClock clock = new FixedClock(new DateTimeOffset(2024, 5, 1, 12, 29, 59,
    TimeSpan.FromHours(8)).AddTicks(9999999));

bool isValid = created.IsInThePast(includeBoundary: true, clock)
               && expires.IsInTheFuture(includeBoundary: false, clock);
// isValid 为 true；到期时刻及之后为 false。
```

这里的左闭右开规则仅属于示例业务约定；`DateTimeRange` 的端点仍是闭区间。固定时钟会将带偏移的输入归一化到同一 UTC 时间点，测试可以分别覆盖创建前、创建时、到期前一 tick、到期时及之后，而无需等待真实时间流逝。

## 关键契约

- `ToUtc()` 仅改 Kind 标签；真正的本机本地时间转 UTC 使用 `ToUniversalTime()`。指定地区的无偏移钟面值使用 `ResolveLocalTime(localTime, sourceTimeZone, resolution)`；已有 `DateTimeOffset` 时间点的目标显示使用 `ToTimeZone(instant, destinationTimeZone)`。
- `ResolveLocalTime` 要求 Unspecified；夏令时缺失时间拒绝，重复时间默认拒绝，可显式选 `AmbiguousTimeResolution.Earlier/Later`。`ToCst()` 保留“忽略输入、获取当前上海时间”的历史行为。
- 区间和相对时间 API 的 Unspecified 按本机本地解释；`ToEpochTimeSpan` 则按 UTC 解释。跨接口传递时间点时明确 Kind 或使用 DateTimeOffset。
- `ElapsedMilliseconds` 返回 int，整毫秒超出范围抛异常；长跨度使用 `ElapsedTime`。
- `BusinessCalendar` 不内置、也不自动更新法定节假日。计数忽略时间字段，采用 `[start, end)`，反向区间为负；长跨度目前逐日扫描。
- 时间戳唯一性限于单实例一次 Reset 生命周期；多个实例、多个进程不共享序列。Unix 秒级包装和 COMB 毫秒字段可能重复；Peek 不推进状态，也不预留下一项。
- DateTimeSpan 转固定时长时保留 365 天/年、30 天/月近似；需要真实日历结果时使用锚点日期。含年月的 NodaTime Period 不能直接转换成固定时长。
- 节气前后查询严格排除当天；成功的 out 日期为午夜 Unspecified。无结果时先判断空字符串或 null，不把 out 哨兵当成有效结果。农历自身范围以 ChineseLunisolarCalendar 为准。
- 严格 JSON 转换器不会改变旧转换器的行为；在已有配置中应替换同类型旧转换器，避免前面的转换器优先匹配。要保留输入原始偏移，使用 DateTimeOffset。

完整兼容性变化和纪元极值的历史饱和限制见[迁移说明](datetime-date-migration.md)。

## 验收余项

- [x] 本次 net8.0 模块、主包日期与消费者测试通过。
- [ ] 外部 GitHub Actions 三时区工作流实际运行并保留结果。
- [ ] 稳定机器上确认时间戳并发性能；业务方若需容量门槛，应先确定工作负载与 SLA。
- [x] 后续补充 ISO 周及 .NET 6 以上 DateOnly/TimeOnly 适配；使用方式见[日期模块扩展能力](datetime-date-extended.md)。
- [ ] 基础逐日工作日算法的进一步优化按实际工作负载评估；年度业务日历已有索引。
