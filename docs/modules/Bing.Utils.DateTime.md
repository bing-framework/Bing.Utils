# Bing.Utils.DateTime

当前能力与可运行示例见 [DateTime / Date 使用指南与本次验收](../reference/datetime-date-usage.md)，涵盖区间、业务日历、固定时钟、时区、时间戳、严格 JSON 和节气查询。

新增的显式 UTC/区间时区投影、版本化年度日历加载、类型化节气查询及性能资源方案见[能力补齐指南](../reference/datetime-capabilities.md)。年度日历由业务维护地区配置，查询使用不可变前缀索引。

## 1. 包职责（Scope）
- 解决的问题
- 提供日期时间计算、导航、比较、业务日运算与 NodaTime 转换扩展。[证据] `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:3` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:72` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1118`
- 提供 `DateTimeSpan` 流式 API（`Days/Weeks/Before/FromNow`）。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:23` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:83` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:208`
- 不解决的问题（Out of Scope）
- 不提供定时任务调度器、时区数据库管理服务（仅扩展方法/工具层）。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1079`

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `DateTimeExtensions.ToUtc()/ToCst()/ToEpochTimeSpan()/ToLocalDateTime()`。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1061` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1079` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1101` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1118`
- `DateTimeExtensions.AddBusinessDays()/IsBefore()`。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:72` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:542`
- `DateTimeSpanExtensions`：`Days()/Weeks()/Before()/FromNow()`。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:23` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:186` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:208`
- `IClock`、`SystemClock`、`FixedClock`；相对当前时间的计算支持显式传入时钟。`BusinessCalendar` 支持节假日和补班规则，`TimeZoneConversion` 支持显式时区和歧义处理。
- 输入输出约定
- `ToUtc` 只改 `Kind`，不做时区换算（保留全部 ticks）。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1050` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1061`
- `ToCst` 忽略传入参数，直接基于当前时钟返回上海时区时间。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1066` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1081`
- 边界行为（null、空集合、非法参数）
- `DateTime` 是值类型，大多数 API 无 null 分支；`DateTime?` 扩展方法会做空值防御后返回 false。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:566` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:568`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
var utcTime = localTime.ToUtc();
utcTime.Kind.ShouldBe(DateTimeKind.Utc);
```
[证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:26` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:29` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:32`
- 示例2：进阶用法
```csharp
var testDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var span = testDate.ToEpochTimeSpan();
span.TotalDays.ShouldBeGreaterThan(0);
```
[证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:112` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:116`
- 示例3：常见错误与修正
```csharp
var input = DateTime.UtcNow;
var cst = input.ToCst(); // 注意：实现按当前时钟取值，不使用 input
// 基于输入时间换算请使用现有 TimeZoneConversion API。
```
[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1066` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1081` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:95`

## 4. 性能与线程安全说明
- 是否分配敏感
- 多数 API 为值类型运算，分配压力低；NodaTime 转换会创建结构/对象实例。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1118`
- 是否线程安全
- 扩展方法大多无共享状态，可并发调用。
- 是否可并发调用
- 可并发调用；注意调用方若依赖“当前时间”语义，结果受系统时钟影响。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1081`

## 5. 异常与日志策略
- 抛出哪些异常
- 当前展示的核心扩展多为纯计算，不主动抛业务异常（除基础框架异常）。
- 什么时候返回默认值而不是抛异常
- `DateTime?` 比较扩展在空值场景返回 `false`，不抛异常。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:568`

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils` + `NodaTime`。[证据] `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:11` [证据] `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:15`
- 被哪些包复用
- 当前 `src` 层未发现其他子包直接引用 `Bing.Utils.DateTime`（主要由测试/业务侧消费）。[证据] `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:11`

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 项目：`tests/Bing.Utils.DateTime.Tests`。[证据] `tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj:11`
- 类：`DateTimeExtensionsToTest`，覆盖 `ToUtc/ToCst/ToEpochTimeSpan/ToLocalDateTime`。[证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:10` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:23` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:84`
- `DateTimeSpan` 的核心计算由 `tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeSpanCoreTest.cs` 覆盖。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 依赖 `NodaTime 3.1.2`，升级时需回归时区与边界日期行为。[证据] `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:15`


## 9. 本次修复的契约与迁移

完整变化见 [DateTime / Date 迁移说明](../reference/datetime-date-migration.md)。主包 DateTimeRange 统一为 UTC 闭区间，显示端点需明确时区；NoRepeatTimeStampFactory 使用 UTC tick 序列并保留入口 Kind。

独立包保留 DateTimeSpan 的年月分量与固定时长，修正 NodaTime 转换；含年月的 Period 不可直接转固定时长。Set 方法保留未修改精度，EndOf 系列使用最后一个 tick。ToEpochTimeSpan 的 Unspecified 按 UTC 解释，ToUtc 只改标签的历史契约保留。

节气按[官方自然日数据](../reference/solar-term-data.md)覆盖 1900～2100 年，运行时不联网，输出午夜 Unspecified；农历和节气各自使用适用范围。默认周数仍用当前文化的 Calendar 与 FirstDay/Sunday 规则，去除每次调用的格式对象分配。

农历可通过 `DateTime.ToLunarDate()` 和 `ChineseLunarCalendar` 双向转换，显示月份与闰月标志分开表示。日期字段和时间点入口的时区契约、旧接口迁移及范围见[农历日期指南](../reference/datetime-lunar-guide.md)。

新回归入口包括 DateArithmeticRegressionTest、DatePrecisionNavigationRegressionTest 和 Chinese 目录的历法测试；主包对应 DateNumericBoundaryRegressionTest、DateTimeRangeUtcRegressionTest、NoRepeatTimeStampRegressionTest。框架、覆盖率和性能证据见[实施记录](../quality/datetime-date-repair-report.md)。
