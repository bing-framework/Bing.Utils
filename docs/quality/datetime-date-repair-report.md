# DateTime / Date 实施与验收记录

日期：2026-10-29。范围：F01～F24、P01/P02、E04 日期级节气表，以及复核确认的五项补充工作。Implementation、Tests 已完成；OPEN_ACTIONABLE=0。时间戳性能量测已完成，回归结论未定，详见性能证据。

## 基线与影响范围

- 基线 HEAD：`bdb8193618487edf14b5855b21ea94be141b1705`。初始工作区已有其他任务的修改；本次保留这些修改，不以 HEAD 冒充整个工作区候选版本。
- 首轮原始日志、TRX、覆盖率、基准和脚本：`C:/Users/jianx/AppData/Local/Temp/bing-datetime-repair-20261029/`。初始状态为 `initial-status.txt`，环境为 `environment.txt`。临时证据可被清理，长期回归和 Benchmark 已落在仓库。
- ChangedProjects：Bing.Utils、Bing.Utils.DateTime 及对应测试；现有 Benchmark 项目增加 DateTime ProjectReference 和日期基准。
- ChangedPublicContracts：UTC 区间端点、时间戳 Kind/增量、周期分量、末 tick、节气自然日；公开签名保持。详细行为见[迁移说明](../reference/datetime-date-migration.md)。
- ChangedRuntimePaths：日期计算、周期换算、时间戳生成、历法查询。ChangedProviders/TFMs：无；没有依赖、包版本或框架升级。
- AffectedDependents：ModelIdAccessor、Legacy Guid 中使用 NoRepeat 的 GUID/COMB 路径。RiskLevel：HIGH（跨入口时间契约）。
- 测试按实现程序集归属：独立包回归放在 Bing.Utils.DateTime.Tests，主包回归放在 Bing.Utils.Tests；未迁移无关混合测试。
- 主代理负责区间、时间戳、跨模块契约和集成；luna_worker 负责独立的历法文件、固定向量、精度测试，并进行限定范围的只读审查。

## F01～F24 关闭清单

下表方法均为正式测试，状态均为 CLOSED。完整源码位置可由测试名称直接定位。算术、精度和历法测试在独立包；Numeric、Range、NoRepeat 在主包。

| ID | 修复 | 关键回归 |
|---|---|---|
| F01 | 非法星期立即拒绝，枚举最多 7 项 | DateArithmeticRegressionTest.Weekdays_InvalidInput_FailsBeforeEnumeration；Weekdays_AllPairs_AreFiniteAndOrdered；DatePublicInputContractTest |
| F02 | 显式量化并拒绝零 tick/非有限增量 | DateNumericBoundaryRegressionTest.Increment_InvalidValue_DoesNotReplaceConfiguration；Increment_OneTick_IsIdenticalAcrossFrameworks |
| F03 | Years 写入年分量 | DateTimeSpanCoreTest.NumberExtensions_YearsPluralMapsToYears |
| F04 | DateTimeOffset 减法的固定时长同时取负 | DateTimeOffsetCoreExtensionsTest.AddDateTimeSpan_And_SubtractDateTimeSpan_RoundTripAwayFromMonthEnd |
| F05 | Period 使用完整固定时长，含年月时拒绝 | DateArithmeticRegressionTest.FixedPeriod_ConvertsWholeDurationAndTruncatesNanoseconds；FixedPeriod_WithCalendarUnits_RejectsAmbiguousDuration |
| F06 | DateTimeSpan/Period 保留分量，Duration 近似规则一致 | DateArithmeticRegressionTest.PeriodConversions_PreserveCalendarAndAllFixedComponents |
| F07 | 一元负号保留年月 | DateTimeSpanCoreTest.UnaryMinus_PreservesCalendarComponents |
| F08 | 双向包含和闭区间交并集一致 | DateTimeRangeUtcRegressionTest.IntervalAlgebra_HandlesContainmentTouchingAndCustomImplementations |
| F09 | 规范化 UTC 后排序，时长按实际经过时间 | DateTimeRangeUtcRegressionTest.MixedOffsets_AreSortedByInstantAndHavePositiveElapsedDuration；MixedKinds_RepresentEqualRangesAndHashCodes |
| F10 | 天干地支采用正确的一基序号 | ChineseDateHelperTest.GetSexagenaryYear_FullCycle_ReturnsOneBasedStemAndBranchNames |
| F11 | 闰年/月/日及天数使用农历分量 | ChineseDateInfoTest.GetDaysInYearAndMonth_UsesLunarCalendarParts；ChineseDateHelperTest/DateTimeLeapExtensionsTest 的闰月用例 |
| F12 | 默认月首为 1 日 | DatePrecisionNavigationRegressionTest.CreateFirstDayOfMonth_WithoutWeekday_ReturnsDayOne |
| F13 | 月/季/年末指定星期向前查找 | DatePrecisionNavigationRegressionTest.LastDayOfPeriods_AllWeekdaysStayWithinPeriodEnd |
| F14 | 区间包含下界，处理 nullable/逆序/MaxValue | DatePrecisionNavigationRegressionTest.IsDateBetweenWithBoundary_IncludesStartAndPreservesBoundaryTimes；IsDateBetweenWithBoundary_NullableReversedAndMaximumValues |
| F15 | 纪元乘加不回绕，保持负纪元 | DateNumericBoundaryRegressionTest.EpochSeconds_OverflowInput_IsRejected；EpochMilliseconds_OverflowInput_IsRejected |
| F16 | 周/季/年受检换算 | DateArithmeticRegressionTest.UnitMultiplication_DoesNotWrapToSmallOffsets；NodaWeeks_OutOfRangeCannotWrapIntoSmallDuration；NodaWeeks_LargeRepresentableDurationKeepsAllDays |
| F17 | Local 纪元跨度先转 UTC，Unspecified 视为 UTC | DatePrecisionNavigationRegressionTest.ToEpochTimeSpan_UsesUtcInstantAndTreatsUnspecifiedAsUtc |
| F18 | UTC 序列与入口 Kind 一致，失败不提交状态 | NoRepeatTimeStampRegressionTest.MixedEntrypoints_UseOneUtcSequenceAndPreserveKinds；BatchOverflow_DoesNotConsumeAnyTimestamp；ConcurrentCalls_HaveUniqueContiguousUtcTicks；DST 两项回归 |
| F19 | 当日精确匹配，严格前后查询且跨年 | ChineseSolarTermHelperTest.GetSolarTerm_SummerSolstice_UsesNaturalDateAndStrictNeighborSearch；GetLastAndNextSolarTerm_CrossYearUsesFixedDatesAndExcludesQueryDay；支持边界与全年完整性用例 |
| F20 | ToUtc/Set 保留未改 ticks，导航保留 Kind/Offset | DatePrecisionNavigationRegressionTest.DateTimeSetters_PreserveSubMillisecondTicksAndKind；DateTimeOffsetSetters_PreserveSubMillisecondTicksAndOffset；DateTimeOffsetNavigation_PreservesFixedOffsetAndSubMillisecondTicks |
| F21 | EndOf/范围工厂末 tick，不先构造越界下一周期 | DatePrecisionNavigationRegressionTest.DateTimeEndHelpers_ReturnLastTickIncludingMaxValue；DateTimeOffsetEndOfDay_PreservesOffsetAndLastTickAtLimits；LastDayOfWeek_MaximumYear_KeepsRepresentableDateAndPrecision；DateTimeRangeUtcRegressionTest.CalendarFactories_EndAtTheLastTick；DateTimeEndOfWeek_UsesConfiguredCultureAndCapsAtMaximum；DateTimeOffsetEndOfDay_ThrowsWhenLocalDayEndExceedsUtcMaximum |
| F22 | IsSameMonth 同时比较年、月 | DateTimeOffsetCoreExtensionsTest.IsSameMonth_DifferentYears_ReturnsFalse |
| F23 | Unix 负毫秒先拒绝再截断，保持非负秒级契约 | DateNumericBoundaryRegressionTest.UnixTimestamp_NegativeMilliseconds_AreRejected；正值截断用例 |
| F24 | 星宿按自然日和正模保持 28 天周期 | ChineseConstellationHelperTest.Get_PreReferenceDate_UsesPositiveModuloAndNaturalDate |

旧实现失败证据不是仅靠修改后的通过结果推断：

| 证据文件 | 旧实现失败 / 总数 | 主要覆盖 |
|---|---:|---|
| phase1-red.trx | 13 / 45 | F01、F03～F07、F16 |
| main-numeric-red.trx | 13 / 16 | F02、F15、F23 |
| timeline-red.trx | 8 / 9 | F08、F09、F18 |
| precision-red.trx | 11 / 12 | F12～F14、F17、F20、F21 |
| chinese-review-red/chinese-review-red.trx | 7 / 9 | F10、F11、F19、F24，以及 Noda 周数和最大日期审查补充 |
| same-month-red/same-month-red.trx | 1 / 1 | F22 |
| dst-unspecified-red/dst-unspecified-red.trx | 1 / 1 | F18：纽约回拨期间 Unspecified 丢失重复时间选择 |

后两批旧实现测试使用审查阶段保留的独立包 DLL，在临时目录运行当前回归测试，不回退共享工作区。DST 复现使用修复前的当前主包 DLL；最终三时区回归已通过。

## 补充核对与收口

复核发现的五项工作均已关闭；这是对原始缺陷修复之外的公开入口和回归覆盖补齐。

| 对应项 | 状态 | 当前实现和验收 |
|---|---|---|
| F01，MUST_FIX | CLOSED | 计算器、日期工厂、首尾/前后星期导航、星期文本、枚举扩展及 IsSameWeek 均拒绝非法星期；TryOffsetByWeek 的整数/枚举重载对无效日历参数返回 false 和 MinValue。DatePublicInputContractTest 覆盖 -1、7、int 极值及全部合法星期。 |
| D08，SHOULD_FIX | CLOSED | months/quarters/years/occurrence/dayOfWeek 使用公开参数名；NoRepeat 分别报告 baseTime/count/useUtc，无对应参数时为 null。补齐转换越界、输出越界、批量首项越界和失败状态断言。 |
| F19，SHOULD_FIX | CLOSED | 正式固定向量覆盖 2024 元旦前一年的冬至、2024 年末下一年的小寒、节气当天严格排除；同时断言日期、Ticks 和 Unspecified。 |
| F21，SHOULD_FIX | CLOSED | 受控 Culture 下验证 EndOfWeek 的普通周末和 MaxValue 末 tick，覆盖三种 Kind；验证合法 -14:00 上界输入的 EndOfDay 因 UTC 越界而拒绝。 |
| F18 消费者，SHOULD_FIX | CLOSED | ModelIdAccessor 补 Kind；Legacy Guid 直接拒绝 Guid.Empty，补实际 NoRepeatMode.On 及固定时间戳的编解码回归。 |

新增缺陷回归在修复前验证：`weekday-red/weekday-red.trx` 为 24/25 失败，`timestamp-red/timestamp-red.trx` 为 3/4 失败。修复前相关源码保存在本轮证据目录的 `before/`。额外的转发参数测试与显示边界测试随完整矩阵验收。

消费者测试发现 COMB 的既有 Unix 策略会舍弃毫秒以下精度；工厂的 UTC ticks 严格递增不意味着编码后的毫秒字段每次都不同。实际 On 接线正常，既有 COMB 编解码测试也明确了毫秒截断。因此保留编码精度契约，以固定基准验证精确递增，未增加毫秒水位或新时钟接口。

## 框架与消费者验收

全部为 Release，0 失败、0 跳过。以下是补充修复后的每框架测试数，不把重复框架执行当成不同用例。

| TFM | 独立包全部 | 主包日期筛选 | IdUtils 消费者 |
|---|---:|---:|---:|
| net8.0 | 667 | 435 | 39 |
| net7.0 | 667 | 435 | 39 |
| net6.0 | 667 | 435 | 39 |
| net5.0 | 667 | 435 | 39 |
| netcoreapp3.1 | 667 | 435 | 39 |

- 共 1,141 项筛选用例、5,705 次框架执行。消费者范围为 ModelIdAccessorTest 与 Bing.IdUtils.Legacy.GuidTest。
- 当前证据目录为 `C:/Users/jianx/AppData/Local/Temp/bing-datetime-followup-20261029/`；`final-verification.json` 显式记录各 TRX、16 个相关源码文件和测试程序集标识。
- 最终结果分别使用 `datetime-final-{TFM}`、`main-date-{TFM}`、`id-consumers-final-{TFM}`。初次矩阵中的旧参数名断言和过强的 COMB 毫秒严格递增断言失败记录保留，但不计入最终通过证据。
- net5.0/netcoreapp3.1 项目引用使用本轮构建的 netstandard2.0 生产程序集。旧框架没有改用新版本 API 或依赖。
- ModelIdAccessor 在快速刷新时同时验证 Local Kind 和严格递增；PostgreSQL 实际 On 入口验证 UTC、毫秒对齐与非递减。独立工厂的固定基准用例进一步精确验证 `.1234567Z → .123Z`、加 4ms 后 `.1274567Z → .127Z`，不依赖 Sleep 或共享状态重置。

net8.0 覆盖率：

| 采集范围 | 行 | 分支 |
|---|---:|---:|
| Bing.Utils.DateTime 程序集 | 83.95% | 78.59% |
| 主包 Bing.Date.*、Bing.Helpers.Time/UnixTime | 71.50% | 65.67% |
| 其中 NoRepeatTimeStampFactory | 100% | 100% |

独立包的原审查值为 74.12% / 71.50%，主包同一筛选范围原值为 70.40% / 63.68%。覆盖率是补充证据；日期范围工厂依赖当前系统时钟，最大年路径通过实现检查和相邻导航边界测试验证，没有为了测试新增时钟接口。Windows 覆盖率不合并 Linux DST 运行；覆盖率不能替代公开契约和边界断言。

## 隔离时区与数据验收

使用现有 Debian .NET 8 SDK 容器，`--pull=never --network none`，只读挂载源码和已编译测试，结果写入本轮证据目录。宿主机时区未修改。

| Linux 时区 | 独立包全部 | 主包时间轴/区间/纪元/时间戳定向 | IdUtils 消费者 |
|---|---:|---:|---:|
| UTC | 667 / 667 | 142 / 142 | 39 / 39 |
| Asia/Shanghai | 667 / 667 | 142 / 142 | 39 / 39 |
| America/New_York | 667 / 667 | 142 / 142 | 39 / 39 |

合计 2,544 次隔离执行。上海覆盖正偏移下 UTC 上界无法显示的拒绝路径；UTC/纽约覆盖可显示分支，纽约同时覆盖 DST 回拨的 Unspecified 无损表示检查。时间戳失败后状态不变，并可继续取出剩余 UTC 值。

镜像为 `mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim`；本轮原始证据使用 `linux-{UTC|Asia-Shanghai|America-New_York}-{datetime|main|id-consumers}`，命令保存在 `run-timezones.ps1`。没有本计划要求的环境缺口。

节气表共 **201 年 × 24 项 = 4,824 个日期**，来源、版本及再生成方法见[节气数据说明](../reference/solar-term-data.md)。逐年枚举所有自然日检查恰有 24 个不同节气；固定向量覆盖完整 1900 年、2023-06-21 夏至及前后一天、跨年和 1900/2100 支持边界。农历覆盖完整干支周期、春节、闰月及 BCL 月序号；星宿覆盖参考日前后的 28 天循环。

## 性能证据

复用现有 BenchmarkDotNet 0.13.11、MemoryDiagnoser 和固定总工作量场景。P01/P02 的相关运行路径和 Harness 本轮未改，复用首轮证据；时间戳核心新增公开异常上下文，因此重新量测其正常路径。

| 复用场景 | Mean | StdDev | 分配/次 |
|---|---:|---:|---:|
| 默认周数，优化前 | 197.2 ns | 10.65 ns | 848 B |
| 默认周数，优化后 | 8.747 ns | 0.666 ns | 0 B |
| 节气：年末查下一项 | 154.98 ns | 2.473 ns | 0 B |
| 节气：查询夏至当天 | 38.10 ns | 3.475 ns | 0 B |

P01 的基线保存在首轮证据目录的 `week-before-source/`；BaselineSource 为 `c35e8e9d70999f9602718fd4b2bb106cc3191716ecbeb5ba804d0da20655ddea`，Harness 为 `a3d2cc341d4476cfebf645c66de633261be9283ad51ab28e2329b2c5ccf90dc0`。默认规则 FirstDay/Sunday 和 CurrentCulture.Calendar 的契约保持。P02 固定下标循环不分配，官方日期表与旧近似公式语义不同，旧公式加速比较仍为 NOT_APPLICABLE。

本轮仓库原有时间戳 Harness 每次固定生成 40,000 项，ShortRun（3 次预热、3 次采样、1 次启动）结果为：1 worker 66.45 ns/次，StdDev 2.713 ns；4 workers 446.55 ns/次，StdDev 79.501 ns。原始总分配分别为 12,736 B / 10,240,000 项、79,576 B / 1,280,000 项（约 0.0012/0.0622 B/项），Gen0/1/2 均为 0；并发包含 Parallel 调度，汇总表的“—”是取整显示。

上述耗时高于首轮记录，因此使用本轮冻结的修复前 NoRepeat 源码与当前程序集做一次诊断、一次正式确认。基线仅调整类型名以便同时测量，固定工作量和生成逻辑保持一致；没有覆盖原始基线。诊断临时项目使用了 SDK 10，其数据不用于正式前后结论。确认项目复制仓库 global.json，双方使用 SDK 8.0.425、Runtime 8.0.31、X64 RyuJIT，Windows 11 / Intel Core Ultra 9 185H，共享开发环境。

| 正式确认 | Mean | StdDev | 99.9% CI 半宽 | Current/Before |
|---|---:|---:|---:|---:|
| 修复前，1 worker | 91.76 ns | 5.982 ns | 109.13 ns | 1.00 |
| 当前，1 worker | 148.44 ns | 10.137 ns | 184.94 ns | 1.62 |
| 修复前，4 workers | 833.05 ns | 19.833 ns | 361.83 ns | 1.00 |
| 当前，4 workers | 371.57 ns | 3.810 ns | 69.51 ns | 0.45 |

**时间戳的性能回归结论未定。** 短样本跨次波动明显，单线程和并发结果方向不一致，不能据此声称无回归、性能提升或业务容量达标。量测和复现材料已完成；未给定业务 SLA，不设置自造门槛，也不因不确定结果继续修改代码或重复基准。

当前仓库基准可用以下命令复现：

```powershell
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0 -- --filter '*DateTimeTimestampBenchmarks*' --job Short
```

本轮原始输出在 `benchmark-timestamp/`、`timestamp-comparison-final/BenchmarkDotNet.Artifacts/`，正式确认源码和 global.json 在 `timestamp-comparison-final/`；日志为 `benchmark-timestamp.log`、`timestamp-comparison-final.log`。时间戳锁和并发模型保持。

## 审查与最终状态

本轮影响范围为 7 个生产文件、9 个测试文件及计划/验收/迁移文档；生产项目为 Bing.Utils.DateTime 和 Bing.Utils，消费者断言归入 Bing.Utils.IdUtils.Tests。公开签名、TFM、包依赖、打包结构和仓库 Benchmark Harness 未变。风险为 MEDIUM：变化集中于非法输入及异常诊断，正常路径通过完整模块和消费者矩阵验证。

主代理负责公共入口、异常契约与集成；luna_worker 负责限定文件的三类回归，并独立只读审查主代理的生产改动。审查发现的 occurrence 和绝对年份偏移参数转发问题已经修正；最终限定范围审查没有 OPEN_ACTIONABLE。

- Implementation：本轮 5/5 关闭；原始 F01～F24、D08 补充契约、E04、P01/P02 已落实。
- Tests：PASS。L0 为范围内 diff/UTF-8 检查与构建；L1 为缺陷先失败再通过；L2/L3 为五框架模块和消费者、三时区验证。没有运行无关全 Solution 测试。
- Performance Evidence：P01/P02 复用；时间戳量测完成，性能回归与容量结论 NOT_VERIFIED，原因和原始结果如上。
- Completed：公共星期输入、公开异常参数、跨年节气、EndOfWeek/Offset 上界、IdUtils 消费者回归、迁移说明与验收。
- Open Actionable：0；Blocked Approval：0；Blocked External：0；Verified Boundaries：无新增容量边界。
- Not Applicable：旧错误节气公式与官方表的加速比较。
- Accepted Limitations：365/30 近似周期、Unix 秒级及 COMB 毫秒级量化、Reset 新生命周期、ToUtc/ToCst 历史语义、节气日期级精度；共享机器短基准不提供稳定容量结论。
- Deferred：DateOnly/TimeOnly、ISO 周、新时钟接口、工作日快速算法。
- No-Progress Check：CHANGED。Next Action：STOP；没有新增可执行代码问题，不因性能结论未定进入自动修复循环。

未执行提交、推送、依赖升级或发布。保留其他任务的工作区修改。
