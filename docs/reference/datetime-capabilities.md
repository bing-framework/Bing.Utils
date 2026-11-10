# 时间模块能力补齐与使用指南

本轮方案见[能力补齐方案](../superpowers/plans/2026-11-06-datetime-capability-extension.md)。以下内容对应当前工作区源码，不表示已发布 NuGet。原有行为迁移见[迁移说明](datetime-date-migration.md)。

后续新增的跨年日历、业务提供数据的节气时刻表和可选 PostgreSQL 事务协调器，见[日期与时间戳稳定性指南](datetime-distributed-stability.md)。

## 1. UTC 存储与目标时区显示

`DateTimeRange` 继续以 UTC 保存端点。新增 `TimeZoneConversion.ToTimeZone(range, zone)` 返回 `(DateTimeOffset StartTime, DateTimeOffset EndTime)`，两个端点分别计算偏移，跨 DST 可为不同 Offset。原范围和实际经过时长不变。

新增 `TimeZoneConversion.ToUtc(localTime, sourceTimeZone, resolution)` 接受 Unspecified 钟面时间并按显式来源时区转换。它不同于历史扩展 `date.ToUtc()`（只改 Kind）。夏令时缺失时间抛 ArgumentException；重复时间默认拒绝，可选择 Earlier/Later。显式结果超出可表示范围抛异常。

资源：使用值类型返回，无全局缓存；DateTimeRange 实例投影不新建范围对象，自定义 IDateTimeRange 会创建规范化副本。时区对象由调用方复用，不在每次查询中加载时区数据库。

## 2. 由业务维护的版本化年度日历

`AnnualBusinessCalendar` 是不可变年度快照，保存 Region、Year、Version、Source 和前缀计数。没有内置国家/地区假期，没有后台联网刷新或隐式“最新版本”。新配置加载成功后，业务可用 `Interlocked.Exchange(ref currentCalendar, replacement)` 原子替换引用；一次业务操作先获取当前引用并使用同一快照。

配置字段名大小写敏感，未知字段、重复字段和结构版本会被校验。日期必须为 `yyyy-MM-dd` 且属于该年度；补班优先于节假日和周末。省略 restDays 默认为周六/周日，传空数组表示没有固定周休息日。省略日期数组为空，显式 null 非法。以下仅为**演示业务规则**，不是官方节假日表：

```json
{
  "schemaVersion": 1,
  "region": "demo-office",
  "year": 2024,
  "version": "2024.v1",
  "source": "业务部门审核的示例规则",
  "restDays": [0, 6],
  "holidays": ["2024-01-01"],
  "workingDayOverrides": ["2024-01-06"]
}
```

加载：`BusinessCalendarConfiguration.FromJson(json)` 或 `LoadFile(path)`。文件必须为 UTF-8，可有 BOM；最大 65,536 字符，每组例外最多 366 项。格式/规则错误统一抛 JsonException；字符串过长抛 ArgumentException，文件超限抛 IOException，错误编码抛 DecoderFallbackException。加载失败不会修改既有日历。

`IsBusinessDay` 为 O(1)；`CountBusinessDays` 用前缀差为 O(1)；`AddBusinessDays` 二分定位为 O(log 366)。构造阶段为 O(年日数 + 输入规模)，暂时复用已有 BusinessCalendar 计算规则，之后只保留最多 367 个 int（数组数据区 1,468 B，另加对象和元数据），没有按查询日期增长的缓存。加载解析有短期分配，建议每个业务版本加载一次并复用。

日期按字段计算，不自动转时区。Add 不计算起点，保留起点时间 ticks/Kind；目标超年明确拒绝，不能自动借用下一年的未知安排。Count 是半开区间，允许次年 1 月 1 日作为边界，反向为负。9999 年无次年哨兵，全年数量使用 TotalBusinessDays。跨年度业务由调用方先确保所有年度配置存在，再按年度拆分统计；缺少年度时返回业务配置缺失错误，不退回“只有周末”的算法。

## 3. 时间戳与分布式唯一标识

这部分与节气查询无关。

### 当前可使用的方案

- 单实例 UTC tick 递增：复用 `NoRepeatTimeStampFactory`，不在每个请求中新建或 Reset。多线程共享同一实例。Unix 秒级包装不能保留 tick 唯一性。
- 跨节点标识：引用 `Bing.Utils.IdUtils`，复用 `SnowflakeGenerator.Create(workerId: 1, dataCenterId: 1)` 的返回实例，再调用 `NextId()`。Twitter 版本的两个节点号各为 0～31；单参数 Seata 版本 workerId 为 0～1023。节点号需要部署系统保证唯一；同一节点号同时运行多个独立实例可能冲突。
- ID 与业务发生时间分别存储，例如 `long Id` 和 `DateTimeOffset OccurredAt`。不将 Snowflake ID 强转 DateTime ticks，也不从顺序 ID 推导精确物理事件时间。

Twitter 实现遇到物理时钟回拨会拒绝发号，序列耗尽等待下一毫秒有超时。Seata 是另一种逻辑计数布局，其顺序和时钟行为不同；本轮不改变两种算法。节点租约、重复配置检测、滚动重启期间的实例隔离和历史发号水位恢复不由这些本地类提供。新增测试验证不同节点位及并发结果集合隔离，不能替代真实部署中的租约验收。

### 如果业务必须要求跨节点唯一的“时间戳值”

建议独立协调服务实现，不能靠修改本地工厂的锁来完成。服务在具备线性一致性的事务中读取持久化水位，并原子提交 `next = max(observedUtcTicks, lastCommittedTicks + 1)`；只返回已提交值，检查 DateTime 上界。为重试增加请求幂等键，故障转移使用 fencing/租约隔离旧主节点，重启恢复已提交水位，禁止 Reset 既有命名空间。

这种结果属于**逻辑时间戳**，高并发时可能领先真实物理时钟。若还要求返回值必须等于真实时钟，则多个同时发生的事件无法都取得不同物理时间，需要另加序号。逐项协调可实现统一分配顺序，但有网络/持久化成本；批量租用号段降低协调次数，但只能保证唯一，不能保证所有节点的实际返回顺序严格递增。

现已提供可选的 PostgreSQL 事务协调适配器，使用范围和运行前提见[稳定性指南](datetime-distributed-stability.md)。节点租约与高可用数据库部署仍由宿主负责，不能用本地集成测试声称网络分区和多主场景已通过。

## 4. 节气日期能力

新增 `SolarTermCalendar`，使用现有官方日期表，公开 MinDate、MaxDate、DataVersion。返回 `SolarTermDate?`，含 Term 枚举和 Date（午夜 Unspecified）。没有匹配项返回 null，表外输入抛 ArgumentOutOfRangeException，避免将“未支持”误当作“当天不是节气”。旧 ChineseSolarTermHelper 的空字符串/哨兵契约不变。

日期入口 `GetTerm/GetPrevious/GetNext(DateTime)` 只读年月日；时间点入口接受 DateTimeOffset，先按固定 UTC+8 换算自然日，再查表。上一/下一节气排除当天整个自然日，不根据当天的时刻改变结果。结果不是精确交节时刻。

仍支持 1900-01-01～2100-12-31，版本为 2026-10-29；来源见[节气数据说明](solar-term-data.md)。查询最多检查 24 项，不联网、不创建日历对象、不为结果分配引用对象，内嵌日期数据区为 4,824 字节。

若需要范围外年份或精确交节时刻，应单独确定新的数据源、授权与版本，选择具有误差定义的天文历表/算法，并分别处理 UTC、TT 和北京时间输出；以独立权威时刻向量、最大误差、跨日边界验收。不能将现有日期午夜包装成真实交节时刻，也不能外推旧近似公式作为已验证数据。本轮交付日期级能力，不承诺该扩展精度。

## 5. 可运行示例

引用当前 `Bing.Utils.DateTime` 与 `Bing.Utils.IdUtils` 项目，以下代码可作为 net8.0 控制台程序的 Program.cs。本轮已原样提取编译运行，11 项断言全部通过。

```csharp
using System;
using Bing.Date;
using Bing.Date.Calendars;
using Bing.Date.Chinese;
using Bing.Date.TimeZones;
using Bing.IdUtils;

static void Check(bool value, string name)
{
    if (!value) throw new InvalidOperationException(name);
    Console.WriteLine($"PASS: {name}");
}

// 显式来源时区转换；示例采用固定 +08，实际有 DST 的地区传对应 TimeZoneInfo。
var zone = TimeZoneInfo.CreateCustomTimeZone("demo+08", TimeSpan.FromHours(8), "demo+08", "demo+08");
var utc = TimeZoneConversion.ToUtc(new DateTime(2024, 1, 1, 12, 0, 0), zone);
Check(utc.Hour == 4 && utc.Kind == DateTimeKind.Utc, "UTC conversion");
var range = new DateTimeRange(utc, utc.AddHours(2));
var display = TimeZoneConversion.ToTimeZone(range, zone);
Check(display.StartTime.Hour == 12 && display.EndTime.Hour == 14, "Range projection");
Check(range.StartTime == utc, "UTC range unchanged");

// 演示配置，不代表法定节假日。
var json = """
    {"schemaVersion":1,"region":"demo-office","year":2024,
     "version":"2024.v1","source":"business-config",
     "holidays":["2024-01-01"],"workingDayOverrides":["2024-01-06"]}
    """;
var calendar = BusinessCalendarConfiguration.FromJson(json);
Check(calendar.CountBusinessDays(new DateTime(2024, 1, 1), new DateTime(2024, 1, 8)) == 5, "Configured business days");
var friday = new DateTime(2024, 1, 5, 9, 0, 0, DateTimeKind.Utc);
Check(calendar.AddBusinessDays(friday, 1) == friday.AddDays(1), "Working override");
// 文件加载：var calendar = BusinessCalendarConfiguration.LoadFile("calendar.json");

// 单实例时间戳递增；跨节点标识单独配置。
var timestamps = new NoRepeatTimeStampFactory(0.0001);
var t1 = timestamps.GetTimeStamp(utc);
var t2 = timestamps.GetTimeStamp(utc);
Check(t2.Ticks == t1.Ticks + 1, "Local timestamp sequence");
var node = SnowflakeGenerator.Create(workerId: 1, dataCenterId: 1);
var id1 = node.NextId();
var id2 = node.NextId();
Check(id2 > id1, "Node ID sequence");

// UTC 的 6 月 20 日 16:00 已是北京时间 6 月 21 日。
var instant = new DateTimeOffset(2023, 6, 20, 16, 0, 0, TimeSpan.Zero);
var term = SolarTermCalendar.GetTerm(instant);
Check(term.HasValue && term.Value.Term == ChineseSolarTerms.SummerSolstice, "Solar date at UTC+8");
Check(term.Value.Date == new DateTime(2023, 6, 21)
    && term.Value.Date.Kind == DateTimeKind.Unspecified, "Solar date precision");
Check(!SolarTermCalendar.GetTerm(instant.AddTicks(-1)).HasValue, "Before Beijing midnight");
Check(SolarTermCalendar.GetNext(instant).Value.Term == ChineseSolarTerms.SlightHeat, "Strict next solar term");
```

## 6. 性能、资源与稳定环境验收

年度日历基准复用 BenchmarkDotNet；对比同输入、同规则的通用逐日计算和年度索引。结果相等在 GlobalSetup 验证，7/180/366 天分开报告。不把新能力对比冒充历史版本提升。

```powershell
dotnet build benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0
dotnet benchmarks/Bing.Utils.Benchmark/bin/Release/net8.0/Bing.Utils.Benchmark.dll --filter '*AnnualBusinessCalendarBenchmarks*' --job short --exporters json

dotnet build benchmarks/DateTimeResourceProbe/DateTimeResourceProbe.csproj -c Release
dotnet benchmarks/DateTimeResourceProbe/bin/Release/net8.0/DateTimeResourceProbe.dll 4 200000 7 single
dotnet benchmarks/DateTimeResourceProbe/bin/Release/net8.0/DateTimeResourceProbe.dll 4 200000 7 batch
```

资源探针参数依次为 workers、每次总生成量、重复次数、single/batch。每个组合单独进程，固定总量比较 1/4/8 workers。single 使用固定 UTC 基准，batch 使用批量 API（每批最多 1000），两者的时钟读取和返回对象不同，不直接据此宣称 API 加速比。

指标含 wall time、进程 CPU 时间、进程总托管分配、GC 0/1/2、峰值和结束工作集，以及最大重叠 worker 数。wall time 包含线程启动/调度/同步与结果检查；CPU 百分比按一个核计，短样本可能受系统计时粒度影响；峰值工作集是整个进程生命周期高水位。工厂仍是一把锁，重叠 worker 数不是锁内真正并行度。默认 environmentClass=UNATTESTED，不自动声称稳定硬件。

稳定环境正式确认步骤：

1. 固定候选 commit/工作区范围、程序集 SHA-256、SDK/runtime、CPU/核心数、系统版本、电源策略、GC 模式；保留 `dotnet --info` 和任务管理器/监控负载记录。
2. 使用专用空闲机器，避免并发构建/测试，采用相同预热与样本数，分开测 1/4/8 workers 和单值/批量，保留原始 JSON。先确定测试窗口和负载，预期均为 EXPECTED_SUCCESS。
3. 正式时戳微基准沿用 `DateTimeTimestampBenchmarks`；用资源探针补充进程级 CPU/分配/峰值，不能把二者的绝对耗时混为同一指标。
4. 只有业务提供目标吞吐、延迟和内存预算后才能作 SLA 验收。当前没有批准的性能阈值；报告分布与波动，不自设通过线。
5. 如需前后回归，双方必须为同语义入口、同 harness、同负载和同环境；只改变文档不重建基线。当前工厂核心未改，本机诊断不是对旧性能问题的再次修复。

## 7. 当前交付边界

已提供可调用的显式转换、年度配置快照、节气类型化查询，新增测试和可复现性能工具。后续事务协调适配器和可注入时刻表的能力边界见[稳定性指南](datetime-distributed-stability.md)；稳定硬件及生产拓扑仍需独立验收。本轮原结果见[验收记录](../quality/datetime-capabilities-2026-11-06.md)。
