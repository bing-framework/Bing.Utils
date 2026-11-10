# 日期与时间戳稳定性指南

本指南对应当前工作区代码，现有 API 的兼容语义见[原能力指南](datetime-capabilities.md)。三类功能彼此独立：跨年业务日历、调用方提供数据的节气时刻表、PostgreSQL 逻辑时间戳协调器。

## 跨年业务日历

`BusinessCalendarSet` 将业务已加载的 `AnnualBusinessCalendar` 组合为不可变快照。构造时复制集合，要求至少一个年度、每年唯一、地区标识严格相同；各年的版本和规则可以不同。读取线程可共享实例，更新规则时构建新实例再替换引用。

```csharp
using System;
using Bing.Date.Calendars;

// 演示规则，实际节假日和补班由业务提供并审核。
var firstYear = new AnnualBusinessCalendar(2024, "example", "2024.v1", "business",
    holidays: new[] { new DateTime(2024, 12, 31) });
var nextYear = new AnnualBusinessCalendar(2025, "example", "2025.v1", "business",
    holidays: new[] { new DateTime(2025, 1, 1) });
var calendar = new BusinessCalendarSet(new[] { firstYear, nextYear });
var deadline = calendar.AddBusinessDays(new DateTime(2024, 12, 30, 9, 0, 0), 1);
// deadline == 2025-01-02 09:00:00
var count = calendar.CountBusinessDays(new DateTime(2024, 12, 30), new DateTime(2025, 1, 3));
// count == 2；区间为 [起点, 终点)，逆序返回负数。
```

日期仅按日历字段计算。偏移不计起点，保留时间 ticks、Kind 及本地 DST 标记；缺少年份抛 `KeyNotFoundException`，不会退回默认周末规则。空区间为 0，即使没有该年配置；计数的排除终点恰好为次年元旦时，不要求该终点年度。零偏移仍要求起点年度已加载。极值越界抛 `ArgumentOutOfRangeException`。

计数和偏移按涉及的年份遍历；年内使用既有前缀索引和二分查找，不逐日扫描，也没有运行时网络请求或按查询日期增长的缓存。

## 节气时刻数据

`SolarTermInstantCalendar` 接受调用方提供的 24 个 `SolarTermInstant`，记录年份、版本、来源及时间分辨率。它校验枚举完整且不重复、北京时间所属年份与月份、UTC 时间顺序及分辨率刻度。构造时复制数据；查询为只读，`GetTerm` 为 O(1)，前后查询为 O(log 24)。

以下仅演示接口，日期为**合成数据，不是实际节气**：

```csharp
using System;
using System.Linq;
using Bing.Date.Chinese;

var synthetic = Enumerable.Range(0, 24).Select(index => new SolarTermInstant(
    (ChineseSolarTerms)((index + 22) % 24),
    new DateTimeOffset(2026, index / 2 + 1, index % 2 == 0 ? 5 : 20,
        12, 0, 0, TimeSpan.FromHours(8)))).ToArray();
var schedule = new SolarTermInstantCalendar(2026, "demo-v1", "synthetic example",
    TimeSpan.FromMinutes(1), synthetic);
var sample = schedule.GetTerm(ChineseSolarTerms.SummerSolstice);
var justBefore = schedule.GetNext(sample.Instant.AddTicks(-1));
// justBefore?.Term == ChineseSolarTerms.SummerSolstice
```

`GetNext`/`GetPrevious` 按 UTC 时间点比较，默认严格排除相等时刻，可设 `includeBoundary: true`。数据只覆盖当前快照中的单年；边界外无结果返回 null，不联网、不自动查询相邻年。实例支持 1～9998 年，但不表示本项目提供这些年份的时刻数据。

### 跨年时刻查询

`SolarTermInstantCalendarSet` 将调用方提供的年度时刻表组合成不可变快照，允许各年度使用不同版本、来源和分辨率；可通过 `GetCalendar(year)` 读取当年的元数据。以下续用上面的 `schedule`，下一年度仍是合成数据：

```csharp
var nextTerms = Enumerable.Range(0, 24).Select(index => new SolarTermInstant(
    (ChineseSolarTerms)((index + 22) % 24),
    new DateTimeOffset(2027, index / 2 + 1, index % 2 == 0 ? 5 : 20,
        12, 0, 0, TimeSpan.FromHours(8)))).ToArray();
var nextSchedule = new SolarTermInstantCalendar(2027, "demo-v2", "synthetic example",
    TimeSpan.FromMinutes(1), nextTerms);
var schedules = new SolarTermInstantCalendarSet(new[] { schedule, nextSchedule });
var query = new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.FromHours(8));
var nextTerm = schedules.GetNext(query); // 合成的 2027 年小寒
```

集合以固定 UTC+8 确定查询时间点的年份，前后比较仍按 UTC 时间点进行；同一时刻无论输入 Offset 如何，结果一致。默认严格排除相同时刻，`includeBoundary: true` 包含相同时刻。查询年份未加载，或跨年需要的年份位于集合最小和最大年份之间但未加载时，抛出 `KeyNotFoundException`；不会跳过缺失年份。查询年份已加载且越过集合最早或最晚年度的节气时，返回 null，表示快照没有更多数据，不表示天文上不存在节气。超出支持的北京时间范围抛出 `ArgumentOutOfRangeException`。

构造时复制年度集合；查询只读且不缓存按请求增长的数据。通过年份索引定位年度，年内复用单年度表的二分查询。更新数据时应重新构造并替换集合，业务仍须验证各年度数据的授权与精度。

分辨率是输入数据的刻度，不是实际误差上界，类型不会验证来源权威性、使用许可或天文精度。调用方应使用获得授权且已验证的时刻数据，不能把原日期表的午夜当成交节时刻。测试使用合成全年数据和一个独立向量：[美国海军天文台公布的 2026 年夏至为 08:24 UTC](https://aa.usno.navy.mil/calculated/seasons?dst=false&submit=Get+Data&tz=0&tz_label=true&tz_sign=1&year=2026)；这不代表其余时刻或长期误差已获验证。

## PostgreSQL 协调时间戳

可选项目 `Bing.Utils.DateTime.PostgreSql` 依赖 `DbConnection`，不绑定或新增日期主包的 PostgreSQL 驱动。业务宿主需要引用自己的 PostgreSQL 驱动，例如 Npgsql，并传入**每次创建独立连接**的工厂；协调器负责释放工厂返回的连接。网络连接池由宿主配置和管理。

上线前由数据库管理员选择可信 schema、设为迁移连接的 `search_path` 并执行 [postgresql-v1.sql](../../src/Bing.Utils.DateTime.PostgreSql/Schema/postgresql-v1.sql)；NuGet 包内路径为 `schema/postgresql-v1.sql`。所有节点必须连接同一数据库、同一 schema、同一 scope；应用账号需 SELECT/INSERT/UPDATE 权限，避免授予删除、截断或重置水位的权限。schema 参数默认 `public`，必须为小写 ASCII 标识符，拒绝 PostgreSQL 系统和临时 schema；适配器以全限定表名访问，不随连接的 `search_path` 改变目标。

```csharp
using System;
using Bing.Date.PostgreSql;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("APP_POSTGRES")
    ?? throw new InvalidOperationException("Missing PostgreSQL connection string.");
var coordinator = new PostgresTimestampCoordinator(
    () => new NpgsqlConnection(connectionString), "orders.created", schema: "public");

// 生产中先持久化本业务请求的 ID；重试时必须使用同一个 ID。
var requestId = Guid.NewGuid();
DateTime timestamp = await coordinator.GetUtcTimestampAsync(requestId);
DateTime retry = await coordinator.GetUtcTimestampAsync(requestId);
// timestamp == retry，且 Kind == Utc。
```

同一协调器实例的请求先异步排队，每次最多占用一个数据库连接；跨实例仍由数据库行锁协调。请复用实例，并在宿主对待处理请求数量设置上限，避免无限增长的应用层等待队列。每次请求在事务中设置 `synchronous_commit=on`、锁定 scope 对应水位，检查幂等键，原子更新水位及持久化请求记录，提交成功后返回。同一 scope 中不同新 requestId 的首次提交值严格递增；幂等重试返回历史值，客户端接收响应的顺序也可能不同。结果是 **UTC 逻辑时间戳**，高并发或时钟回拨时可能领先真实时钟，不应当作测量到的事件发生时间。不同 scope 可以返回相同 ticks。

无 `observedTime` 的重载在取得数据库行锁后读取**应用节点的系统 UTC**，不把排队前的时钟值当作当前时间。显式 `observedTime` 重载只适用于受信任的时钟或经过校验的内部输入，不能直接传入外部客户端提供的日期。错误的未来值或节点系统时钟大幅前跳都会永久推进水位，极值会耗尽该 scope；库不设置时钟偏差阈值，部署方须监控并校验各节点时钟。一般调用使用无 `observedTime` 的重载。

事务提交时断线可能导致结果未知。调用方无论遇到超时、取消还是连接异常，都应保留相同 scope/requestId 重试；不能用新 ID 代替重试。提交前取消会回滚；一旦开始同步提交，取消不能保证阻止提交。由于 `netstandard2.0` 缺少通用异步提交接口，提交本身使用同步 `DbTransaction.Commit()`。

唯一性取决于单一权威 PostgreSQL 主库、事务持久化、严格隔离旧主，以及故障转移不能丢失已确认提交。事务内 `synchronous_commit=on` 不能代替服务器的持久化与副本配置；[PostgreSQL 文档](https://www.postgresql.org/docs/17/warm-standby.html)说明了同步复制及持久化级别。异步复制切主、双主写入、旧备份覆盖或人工重置水位都可能破坏这些前提；库不能代替数据库集群或租约系统保证这些条件。部署方必须制定备份恢复和防脑裂方案，并在真实拓扑上演练。

同一个 scope 的请求经同一行锁串行化。请根据业务对请求并发设置限流、池大小、数据库超时和有界退避重试；过量等待可能导致超时。幂等表随唯一 requestId 数量持续增长，不会自动删除；删除记录会终止对应 requestId 的幂等重试保证，删除或回退水位会破坏唯一性。仅在业务明确接受重试窗口后，设计、审核并运行独立归档/清理策略。

## 验证边界

真实数据库测试只在显式设置 `BING_DATETIME_POSTGRES` 时运行，且数据库名必须以 `bing_datetime_test` 开头。测试验证不同连接并发、请求重放、真实提交后模拟丢失响应、行锁等待取消、回拨、上限和事务回滚；只清理测试生成的随机 scope。

这些测试不证明多主、网络分区、异步副本切主或旧备份恢复场景。稳定环境和生产部署测试需单独验证；共享开发机结果和上轮资源探针不能代替 SLA 或生产吞吐结论。
