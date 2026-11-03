# DateTime / Date 修复、性能优化与测试补强方案

日期：2026-10-29
状态：已实施并完成功能验收及性能量测（2026-10-29），详见[实施记录](../../quality/datetime-date-repair-report.md)。
范围：Bing.Utils.DateTime 独立包，以及 Bing.Utils 主包的 Bing.Date、Time、UnixTime 相关能力。
推荐顺序：明确契约 → 回归测试与 P1 修复 → 时间轴和区间修复 → 农历与精度修复 → 性能优化 → 模块验收。

## 1. 目标与范围

本方案用于修复前一轮审查已经复现的计算错误、无界枚举、重复时间戳、输入溢出、历法错误和精度损失，并使测试能够验证业务契约，而不是固定错误的“当前行为”。

| 范围 | 生产项目 | 测试归属 |
| --- | --- | --- |
| 日期扩展、DateTimeSpan、NodaTime、工作日、时区、农历 | [Bing.Utils.DateTime](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj) | [Bing.Utils.DateTime.Tests](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj) |
| DateTimeRange、TimeStamp、NoRepeatTimeStampFactory、UnixTimeStamp、Time/UnixTime | [Bing.Utils](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing.Utils.csproj) | [Bing.Utils.Tests](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj) |

按[测试归属规则](D:/Bing_Framework/Bing.Utils/.agents/skills/test-project-boundary/refs/test-project-boundary.md)，以被测实现所在程序集及主要断言目标确定归属，不能仅按 Bing.Date 命名空间判断。两个包都有该命名空间，本次不安排整目录迁移。已有混合测试优先在原项目按方法补强；确需拆分时单独列出方法清单。

沿用现有目标框架：

- 生产包：net8.0、net7.0、net6.0、netstandard2.0。
- 单元测试：net8.0、net7.0、net6.0、net5.0、netcoreapp3.1。
- 复用现有 NodaTime 3.1.2、xUnit、Shouldly、coverlet 和 BenchmarkDotNet，不以升级依赖或框架作为本次修复前提。
- 本方案生成阶段只新增本文；实施时不覆盖仓库中其他任务的改动，不自动提交、推送或发布。

## 2. 已有证据与推荐路线

### 2.1 修复前证据

前一轮审查的本机环境为 Windows、.NET 8.0.31、China Standard Time：

| 验证范围 | 结果 | 覆盖率 |
| --- | --- | --- |
| 独立 DateTime 包 | 588 通过、0 失败、0 跳过 | 行 74.12%，分支 71.50% |
| 主包筛选测试 | 1365 通过，其中日期相关 393 项 | 日期/时间助手采集范围：行 70.40%，分支 63.68% |
| NoRepeatTimeStampFactory | 已有测试通过 | 行、分支均为 100%，仍遗漏零 tick 增量 |
| 最小复现 | 本文 F01～F24 对应的问题均已有运行时复现 | 无界枚举使用 Take(32) 受控观察，未运行无限收集 |

主包的 1365 项包括额外扩展测试，不能把该数字当成日期模块测试数。后续使用第 9 节的收窄筛选。

审查时 Git HEAD 为 `bdb8193618487edf14b5855b21ea94be141b1705`；工作区另有其他任务的未提交修改，HEAD 不能单独代表整个工作区候选版本。实施前记录相关源码状态、实际编译产物、运行时和命令，不能把旧结果当成修复后验收。

原始审查材料保存在临时目录：

- `C:/Users/jianx/AppData/Local/Temp/bing-date-audit-20261029/datetime/`：TRX、独立包覆盖率。
- `C:/Users/jianx/AppData/Local/Temp/bing-date-audit-20261029/main/`：TRX、主包筛选覆盖率。
- `C:/Users/jianx/AppData/Local/Temp/bing-date-audit-20261029/core-probe/Program.cs`：独立包复现和性能短探针。
- `C:/Users/jianx/AppData/Local/Temp/bing-date-audit-20261029/probe/Program.cs`：主包复现。

临时材料可能被清理。长期验收以迁入正式测试的固定输入和断言为准，不依赖临时绝对路径。

### 2.2 路线选择

| 路线 | 质量与剩余问题 | 成本与速度 | 工具覆盖 |
| --- | --- | --- | --- |
| A：先修 P1 | 可快速消除无界枚举、重复时间戳和核心计算错误；P2、时间轴和精度问题仍未完成 | 较低，适合第一批交付 | 现有模块测试、受控复现 |
| B：完成 F01～F24，并明确历史契约，推荐 | 关闭已确认缺陷，统一时间轴、周期转换和边界规则；行为变更有迁移说明 | 中等，分阶段执行与验收 | 模块测试、多 TFM、定向消费者验证、局部 Benchmark |
| C：在 B 上扩充 DateOnly/TimeOnly、ISO 周、更多时钟入口及精确节气能力 | 提升功能覆盖，但不能代替基础修复 | 较高，按业务需要后续安排 | 新 API 契约测试、平台与历法数据验证 |

已批准执行 B，并将 E04 的日期级节气表纳入本轮：1900 年使用紫金山天文台历表，1901～2100 年使用香港天文台日期表，支持完整的 1900～2100 自然年；out 日期为午夜 Unspecified。其余 C 项和 P03 延后。D01～D08 均已确认，D02 采用 UTC 端点，不采用备选兼容路线。

## 3. 缺陷登记与关闭条件

F01～F24 已全部 CLOSED；原始优先级 P1 为 MUST_FIX，P2 为 SHOULD_FIX。逐项回归、旧实现失败证据及框架结果见实施记录。

| ID | 优先级 | 位置与问题 | 最小验收条件 |
| --- | --- | --- | --- |
| F01 | P1 | [DayOfWeekCalc](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateUtils/DayOfWeekCalc.cs:90)：非法结束枚举使枚举无法结束 | -1、7、int 极值被确定性拒绝；合法星期枚举最多返回 7 项 |
| F02 | P1 | [NoRepeat 增量](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Date/TimeStamp.NoRepeatFactory.cs:61)：正增量量化成 0 tick | 0.00001ms 被拒绝；0.0001ms 在支持的 TFM 上为 1 tick；连续结果严格递增；诊断不除零 |
| F03 | P1 | [Years](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:144)：年数写入 Months | 2.Years().From(2024-01-15) = 2026-01-15 |
| F04 | P1 | [SubtractDateTimeSpan](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:25)：时间分量符号错误 | 10:00 减 2 小时为 08:00；非月末的混合年月时分量加减验证通过 |
| F05 | P1 | [NodaPeriodExtensions](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:32)：Ticks 分量被当作总时长 | 2 小时、2 天及混合固定单位完整转换；含年月时不能静默变零 |
| F06 | P1 | [DateTimeSpan 转换](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeSpan.cs:546)：与 Period/Duration 转换丢分量或结果不一致 | Period 双向保留年月和固定时长；DateTimeSpan 的显式扩展与隐式 Duration 转换一致 |
| F07 | P2 | [一元负号](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeSpan.cs:315)：年月被折成天数 | -1.Month() 保留 Months=-1；作用于 2024-03-31 得 2024-02-29 |
| F08 | P2 | [HasIntersect](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Date/DateTimeRange.cs:608)：遗漏反向包含 | 外含内、内含外、相等、端点相接均双向一致；Intersect/Union 对称 |
| F09 | P2 | [DateTimeRange DTO 构造](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Date/DateTimeRange.cs:106)：混用各自钟面时间 | +14/-10 样本得到正 23 小时；公开端点、UTC 端点、比较、复制与格式化遵循 D02 |
| F10 | P2 | [干支年](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseDateHelper.cs:164)：天干索引偏移 | 2024-02-10 为甲辰年、1984-02-02 为甲子年；覆盖完整 60 年周期 |
| F11 | P2 | [农历判断](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseDateHelper.cs:324)、[天数](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseDateInfo.cs:227)：公历分量传入农历接口 | 2020-06-01 正确识别闰月；2024-01-31 不抛错，农历年/月天数为 384/30 |
| F12 | P2 | [CreateFirstDayOfMonth](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeFactory.cs:113)：默认返回首个周一 | 2024-06 返回 06-01；带星期参数的重载仍返回首个指定星期 |
| F13 | P2 | [LastDayOfMonth/Quarter/Year](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:322)：向前查找偏移方向错误 | 2024-01 最后周一为 01-29；三个入口均覆盖全部 7 种星期 |
| F14 | P2 | [IsDateBetweenWithBoundary](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:303)：不含起点且 MaxValue 溢出 | 起点包含；结束日期后一刻排除；MaxValue、nullable、逆序边界符合 D04 |
| F15 | P2 | [Time.OfEpochSecond/Milli](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Helpers/Time.cs:359)：乘加回绕后接受无效值 | 1844674407371 秒、1844674407371000 毫秒、long 极值均拒绝；合法负时间戳仍有效 |
| F16 | P2 | [周数](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:186)、[季度/年份偏移](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateUtils/DateTimeCalc.cs:345)：单位乘法溢出 | 极大参数不回绕成小偏移；可表示的大时长使用正确宽度；不可表示值抛明确异常 |
| F17 | P2 | [ToEpochTimeSpan](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1101)：Local 未转 UTC | 同一时刻的 Utc/Local 得到相同跨度；Unspecified 按 D03 明确处理 |
| F18 | P2 | [NoRepeat Kind](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Date/TimeStamp.NoRepeatFactory.cs:114)：单值、批量与 Peek 丢失 Kind | Local/Utc/baseTime 入口返回正确 Kind；混合调用共享 UTC 序列；包装入口转换无时区二次偏移 |
| F19 | P2 | [节气](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTermHelper.cs:131)：相邻三天命中、跨年为空 | 相邻普通日期不误命中；元旦上一节气和年末下一节气正确；边界外明确返回约定结果 |
| F20 | P2 | [ToUtc](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1061)、[Set 方法](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:69)：非目标精度丢失 | ToUtc 只改 Kind；Set 日期/时分等只改指定分量并保留更低精度；季度导航保留 Kind |
| F21 | P2 | [EndOf 方法](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:139)、[范围工厂](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Date/DateTimeRange.cs:255)：末尾漏 9999 ticks | 日/秒/分/时/月等终点为最后一个可表示 tick；最大日期不先计算越界的下一周期 |
| F22 | P2 | [DateTimeOffset.IsSameMonth](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:111)：忽略年份 | 2023-01 与 2024-01 不同月；语义为各自日历年/月相同 |
| F23 | P2 | [FromUnixTimeMilliseconds](D:/Bing_Framework/Bing.Utils/src/Bing.Utils/Bing/Date/TimeStamp.UnixTimeStamp.cs:263)：负毫秒截断为 0 | -1/-999/-1000 均拒绝；0 为纪元；正数不足一秒仍按原契约截断 |
| F24 | P2 | [二十八星宿](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseConstellationHelper.cs:43)：负数取模偏一位 | 参考日之前的任意日期与相隔 28 天日期一致；忽略日内时间，不改变周期 |

## 4. 统一契约与兼容处理

下表是用户确认的路线 B 实现契约；实际验证结果见[实施记录](../../quality/datetime-date-repair-report.md)。

| 决策 | 推荐契约 | 兼容影响 |
| --- | --- | --- |
| D01：日历周期与固定时长 | DateTimeSpan 的 Years/Months 保持日历分量；TimeSpan 保持固定时长。已有 DateTimeSpan → TimeSpan 的 365 天/年、30 天/月近似约定保留并明确标注 | 不更换公开字段和返回类型；禁止把近似时长描述为日历加减结果 |
| D02：DateTimeRange 时间基准 | 统一为 UTC 闭区间；StartTime/EndTime 与 UtcStartTime/UtcEndTime 均表达规范化 UTC 端点，跨度为实际经过时间 | 属于明确的行为变更：非 UTC 输入的公开端点、默认文本、DST 跨度会改变，必须在单独阶段更新调用方和迁移说明 |
| D03：ToEpochTimeSpan | Utc 原样；Local 转 UTC；Unspecified 按 UTC 解释以保留该旧方法原来的数值结果 | Local 结果修正；Bing.Helpers.Time/UnixTime 已有的 Unspecified 本地解释不在本轮统一改写，文档须明确区别 |
| D04：日期范围旧入口 | 下界包含；上界仍为 max 原值加 1 天的排他边界；保留参数时间分量；nullable 分支一致，min>max 为 false | 修正漏起点和溢出；不暗中把原来带时间的边界都截成 Date。自然日调用传入边界的 Date |
| D05：时间精度 | 未被明确设置的分量保持到 100ns；EndOfX 表示最后一个 tick | 从 .9990000 提升到 .9999999 会改变精确相等与序列化结果，需更新相关测试和说明 |
| D06：NoRepeat 序列 | 内部用一条 UTC tick 序列；按入口转换显示 Kind；增量在所有 TFM 使用相同量化规则 | 混用 Local/Utc 的唯一性按 UTC 时间点判断；不承诺不同表示下的钟面 ticks 各不相同 |
| D07：历史 ToUtc/ToCst | ToUtc 继续只设置 Kind，但保留全部 ticks；ToCst 继续读取当前上海时间 | 不把 ToUtc 偷换为 ToUniversalTime，不把 ToCst 偷换为输入时间转换。明确推荐现有 TimeZoneConversion 处理真实转换 |
| D08：异常 | 参数越界使用对应公开参数名的 ArgumentOutOfRangeException；无对应输入参数的时间戳序列耗尽使用 ParamName=null；日历周期不可转固定时长使用 InvalidOperationException；数值运算溢出保留明确的 BCL 风格异常 | 不把异常吞掉后返回默认时间、零时长或合法小偏移 |

D02 的迁移方式：

1. 调用方读取 StartTime/EndTime 时按 UTC 使用；显示时间显式转换到目标时区。
2. 依赖旧 ToString 本地文本的调用方改为明确格式和显示时区；日志建议使用带 Kind 的 `O` 格式。
3. DST 跨越日按实际经过时间计算，可能是 23/25 小时；自然日数量不能再由 TotalDays 直接推断。
4. DateTime 输入：Utc 不改标记；Local 使用自身时区语义转 UTC；Unspecified 保留旧“按本地解释”的兼容规则。该隐含本地规则必须写入构造器注释；需要明确时区的调用方先用现有 ResolveLocalTime 解析为 DateTimeOffset。
5. 保留 MinValue/MaxValue 的范围哨兵用途，将其明确表示为 UTC 边界；普通值不能通过强制标记 Local 来转换。
6. 本轮已选择 UTC 端点路线；不实施备选的双表示 API。普通本地值转换越界时明确失败，不静默饱和成极值哨兵。

## 5. 分组实现设计

### 5.1 输入校验、无界枚举与数值运算（F01、F15、F16、F23）

- 星期相关公开入口先验证 DayOfWeek 的 0..6 范围；Try 入口按其契约返回 false，普通入口抛异常。GetDaysBetween 的参数校验放在返回迭代器前，避免错误延迟到消费过程中。
- 合法枚举按有界天数计算，includeBoundary=true 最多 7 项。start=end 继续遵守现有“单点或空集合”的规则。
- Time.OfEpochSecond/Milli 优先用 BCL 的 DateTimeOffset.FromUnixTimeSeconds/FromUnixTimeMilliseconds 完成合法范围校验，再返回本地 DateTime；测试确认异常参数与项目契约一致。继续支持纪元之前的合法负值。
- 周、季度、年和近似年月转天数的乘法先提升到 long 或使用 checked，再按目标类型范围校验；不要在 int 已溢出后才转换到 long。
- 分清“时长可表示”和“加到某日期可表示”：例如大周数可以是合法 TimeSpan，但加入 DateTime 后仍可能越界。
- UnixTimeStamp 的 MinUnixTimestamp 现为 0；负毫秒在除以 1000 前拒绝。不要因 F23 扩展整个类型到负 Unix 时间戳。

### 5.2 DateTimeSpan 与 NodaTime（F03～F07）

- Years 构造修正为 Years=参数、Months=0；Months/Quarters 的单位保持一致。
- SubtractDateTimeSpan 对年月和 TimeSpan 三部分均取负；维持已有“先月、再年、再固定时长”的应用顺序。
- 一元负号直接构造含负 Years、Months、TimeSpan 的 DateTimeSpan，不经返回 TimeSpan 的 Negate() 绕行。
- 保留现有 `Negate(): TimeSpan` 返回类型，它继续表示近似固定时长；不要为修复运算符而破坏二进制签名。
- Period → TimeSpan/Duration：使用 NodaTime 自身的 ToDuration，完整处理周、日、时、分、秒、毫秒、ticks 和纳秒；含非零年月时明确失败。
- Period → DateTimeSpan：读取并保留年月，通过 ToBuilder 清除年月后将剩余周期转固定时长。DateTimeSpan → Period 使用 PeriodBuilder 同时写 Years、Months 和 TimeSpan.Ticks。
- DateTimeSpan → Duration 统一采用已有 DateTimeSpan → TimeSpan 的近似规则，使隐式转换与 AsDuration 一致；该路线明确属于兼容近似，不用于精确月末日期计算。
- 纳秒到 TimeSpan 的不足 100ns 部分按 NodaTime 3.1.2 的 ToTimeSpan 约定向零截断，并测试正负值；溢出不得回绕。
- 月末加减存在截断，不能要求所有 AddMonths 加减都严格互逆。用固定日期验证月末规则，在不会截断的日期上验证组合往返。

### 5.3 NoRepeatTimeStampFactory（F02、F18）

- 将毫秒先换算为局部候选 ticks，验证有限、正数、可表示且至少 1 tick 后再更新配置。
- 不依赖不同运行时的 TimeSpan.FromMilliseconds 舍入差异。推荐显式按 `incrementMs * TimeSpan.TicksPerMillisecond` 向零截断并 checked 转 long，使 0.0001ms 始终为 1 tick。
- 配置更新和生成共享现有锁；非法 setter 不改变旧增量。先保留 lock，不为性能猜测改成 CAS 或自旋。
- 内部比较和递增使用 UTC ticks。GetTimeStamp 返回 Local；GetUtcTimeStamp 返回 Utc；自定义 baseTime 保留其 Kind，Unspecified 按本地解释后生成，再以 Unspecified 钟面时间返回。
- 同步覆盖 scalar、batch、Peek 及 TimeStamp/UnixTimeStamp 包装入口，不能只修一个 return。涉及 UTC→Local 的边界无法正确表示时明确拒绝，不能通过截断制造重复结果。
- 先计算候选值并检查日期上限，再发布状态。批量调用先核验整批范围；失败不留下不可表示的水位。Peek 不推进序列。
- GeneratedCount 保留现有近似 tick 距离统计含义，保证不会除零且读取一致；不顺手改成真实调用次数。
- 唯一性限于单实例、同一序列生命周期；Reset 显式开启新生命周期，不承诺跨 Reset 或跨进程唯一。
- UnixTimeStamp 按秒表示，亚秒增量会在包装后落入同一秒。文档不能把 tick 级唯一性扩大成秒级包装值唯一性。

### 5.4 区间与 UTC（F08、F09、F14、F17）

- 相交判定采用统一时间轴上的 `startA <= endB && startB <= endA`，保留闭区间端点相接即相交的契约。
- D02 必须同时覆盖所有构造器、复制构造、Contains、In、HasIntersect、Intersect、Union、TimeSpan/TotalX、Equals、GetHashCode 和 CompareTo。
- IDateTimeRange 只有 DateTime 端点，接收自定义实现时也按同一 Kind 规则规范化，不能假设只会传本类实例。
- CompareTo 使用开始端点、结束端点的确定顺序，保证反对称和传递性；相等与哈希均使用同一对规范化端点。
- GetDays/GetHours 等整数接口保留原来的舍入方式；本阶段改变的是时间基准，不附带改成截断。
- 修正 ToUtcSafe 对 Utc 强制改标 Local 的路径；UTC 输入不能再次减去本地偏移。
- IsDateBetweenWithBoundary 使用一个共同的上下界判断内核处理 nullable；若 max+1 天超过可表示范围，上界覆盖剩余可表示值，不直接执行溢出加法。
- ToEpochTimeSpan 先按 D03 解释 Kind，再减 UTC 纪元，保留 ticks，避免中途转整数秒损失精度。

### 5.5 导航、分量设置与终点（F12、F13、F20～F22）

- 无星期参数的月初直接构造年月 1 日；有星期参数的重载继续计算该月首个匹配星期。
- 月/季/年末向前查找使用 `(末日星期 - 目标星期 + 7) % 7`；先验证目标星期，再减去偏移。
- ToUtc/SetKind 使用 DateTime.SpecifyKind；日期和部分时间分量设置保留未修改的小数 ticks。DateTimeOffset 对应方法保留 Offset 和未修改精度。
- SetMillisecond 只替换毫秒分量、保留毫秒以下 ticks；AtMidnight/AtNoon 及 BeginningOfX 则明确清零被截断的低位分量。
- 季度首尾导航保留输入 Kind；不顺带改变其既有“返回边界日零点”规则。
- EndOfSecond/Minute/Hour/Day 及周/月/季/年终点采用最后一个 tick；DateTimeOffset 版本和主包 DateTimeRange 的日期范围工厂同步调整。
- 最大日期避免“先加到下一天/年，再减一 tick”的溢出；超出 DateTimeOffset 的 UTC 可表示边界时抛明确异常。
- IsSameMonth 比较各自 Year 与 Month，不偷偷先转 UTC；这与 IsSameDay 的日历分量语义一致。

### 5.6 农历、节气与星宿（F10、F11、F19、F24）

- 干支：把正确的 sexagenaryYear 传入 GetCelestialStem/GetTerrestrialBranch，再对各自返回的 1-based 索引减 1；不能先从 sexagenaryYear 减 1。
- 闰年、闰月、闰日和农历天数先由 Calendar.GetYear/GetMonth/GetDayOfMonth 获取农历分量，再调用历法接口。
- 相关入口以 ChineseLunisolarCalendar.MinSupportedDateTime/MaxSupportedDateTime 为可表示边界，不能只用宽泛的 1900～9999 年检查充当农历范围校验。
- 中文年份单独格式化的旧 Gregorian 语义与完整农历日期的年份需要明确区分；完整农历文本如与农历年月日混用，增加春节前固定向量并按农历年拼接。
- 节气的“当日、前一个、后一个”使用明确的私有查询模式；比较完整日期，移除“相差一天也命中”的容差和用 comparer(0,1) 推测遍历方向的做法。
- 上一个/下一个节气继续按严格前日/后日查找，不把当日纳入；跨年只检查必要的相邻年，且不越过声明支持范围。
- 保留现有空字符串/null/out 值的无结果约定，并在注释中写清；节气独立支持完整的 1900～2100 自然年，不受农历范围限制。
- E04 已纳入：使用紫金山天文台 1900 年及香港天文台 1901～2100 年官方日期表，固定 2026-10-29 数据版本，精度到日。输入按年月日匹配，out 返回午夜 Unspecified；逐年检查 24 项完整性，并保留独立固定日期断言。来源与再生成见[数据说明](../../reference/solar-term-data.md)。
- 星宿先取自然日差，再用 `((offset % 28) + 28) % 28` 计算索引。验证参考日之前、当天、之后及相隔 28 天的周期性质。

## 6. 测试补强与归属清单

### 6.1 独立包测试

优先修改以下现有测试，再按需要拆出专门回归类：

| 测试文件/目录（位于 Bing.Utils.DateTime.Tests） | 对应缺陷 | 必补断言 |
| --- | --- | --- |
| [DateTimeSpanCoreTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeSpanCoreTest.cs) | F03、F06、F07、F16 | Years 真正按年；负号保留分量；Period 双向转换；溢出与纳秒边界 |
| [DateTimeOffsetCoreExtensionsTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeOffsetCoreExtensionsTest.cs) | F04、F22 | 减去时长的正确符号；跨年同月为 false；固定 Offset 保持 |
| [NodaExtensionsTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/NodaTime/NodaExtensionsTest.cs) | F05、F06 | 各固定单位、混合单位、含年月拒绝、负时长、显式/隐式结果一致 |
| [DayOfWeekCalcTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateUtils/DayOfWeekCalcTest.cs) | F01 | 所有合法起止组合、两种边界选项、-1/7/int 极值 |
| [DateTimeFactoryTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeFactoryTest.cs)、[DateTimeNavigationGetTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeNavigationGetTest.cs) | F12、F13 | 月初落在不同星期；年/月/季末全部目标星期；跨年与闰二月 |
| [DateTimeExtensionsBusinessAndRoundTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeExtensionsBusinessAndRoundTest.cs)、[DateTimeExtensionsAdditionalContractTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeExtensionsAdditionalContractTest.cs) | F14、F17 | 起点、上界前一 tick、上界、MaxValue、nullable、Kind 解释 |
| [DateTimeUpdateTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeUpdateTest.cs)、[DateTimeNavigationTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/DateTimeNavigationTest.cs) | F20、F21 | 1234567 小数 ticks、分量设置不变性、终点后一 tick、Utc/Local/Unspecified |
| [Chinese 测试目录](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/Chinese) | F10、F11、F19、F24 | 甲年与 60 年周期、春节前后、闰月首末、农历范围边界、节气前后及跨年、星宿周期 |
| [BusinessCalendarTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.DateTime.Tests/Bing/Date/Calendar/BusinessCalendarTest.cs) | P03 优化回归 | 七种起点星期、正负/零偏移、假日与补班冲突优先级、区间计数 |

### 6.2 主包测试

| 测试文件 | 对应缺陷 | 必补断言 |
| --- | --- | --- |
| [NoRepeatTimeStampFactoryTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing/Date/NoRepeatTimeStampFactoryTest.cs) | F02、F18 | 零 tick、1 tick、NaN/Infinity、setter 失败保留旧配置、同基准严格递增、批量/Peek/包装 Kind、混合 UTC/Local、范围失败不污染状态 |
| [DateTimeRangeTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing/Date/DateTimeRangeTest.cs)、[DateTimeRangeRegressionTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing/Date/DateTimeRangeRegressionTest.cs) | F08、F09、F21 | 双向包含、交并集、+14/-10 的 23 小时、复制/自定义接口、UTC 端点、哈希/比较一致性、DST 与末 tick |
| [TimeAndUnixTimeBoundaryContractTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing/Helpers/TimeAndUnixTimeBoundaryContractTest.cs) | F15 | 已复现回绕输入、long 极值、最后合法秒/毫秒、合法负纪元、异常参数名 |
| [UnixTimeStampTest](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.Tests/Bing/Date/UnixTimeStampTest.cs) | F23 | -1/-999/-1000、0、1/999/1000、最大支持边界 |

归属分类：上述独立包与主包清单可直接实施；未发现必须迁移的测试；通用扩展 partial 测试按方法补充，保留其他模块断言。

### 6.3 测试方法要求

- 把 YearsPluralMapsToMonths、UnaryMinus_DropsYearsAndMonths、SubtractDateTimeSpan 的错误 CurrentBehavior 断言改为正确语义并重命名，不能通过删除用例掩盖差异。
- 每个 F 编号至少对应一个能使旧实现失败的回归测试；同根因涉及多个入口时使用参数化矩阵。
- 用固定向量、BCL/NodaTime 的独立路径和不变量作参照，不复制待修复公式来计算 expected。
- 合法星期枚举和假日计算用小规模穷举；涉及日期跨度时使用固定种子，失败输出完整输入。
- 无界枚举用非法值立即抛错和有界 Take 验证，不在单元测试中真正无限 ToArray。
- 并发测试使用同步起点和有限工作量，等待所有任务结束；不能用 Sleep 猜测执行完成。
- Culture 测试恢复原值；不在并行测试中修改宿主时区。显式时区路径使用可控 TimeZoneInfo；依赖本地时区的契约在隔离进程或 CI 环境验证。
- 对 Kind、Offset、Ticks 分别断言；DateTime 值相等本身不能证明 Kind 正确。
- 不设“全部覆盖率 100%”作为唯一门槛。要求缺陷回归映射完整、修改分支有正反例，覆盖率用于定位遗漏。

## 7. 性能方案与证据边界

前一轮短探针关闭了分层编译，使用预热后 3 次测量的中位数；不等同于正式 BenchmarkDotNet 基线：

| 路径 | 单次耗时约 | 分配 |
| --- | --- | --- |
| 无参 GetWeekOfYear | 513ns | 848B |
| 显式等价周规则 | 34ns | 0B |
| 查询节气 | 2.4μs | 816B |
| 普通 AddBusinessDays(100000) | 0.82ms | 0B |
| BusinessCalendar.AddBusinessDays(100000) | 3.31ms | 0B |

| 任务 | 方案 | 验收方式 |
| --- | --- | --- |
| P01 / SHOULD_FIX | GetWeekOfYear 默认参数使用原来的 FirstDay/Sunday 常量，仍使用当前文化的 Calendar；不重复 new DateTimeFormatInfo | 多文化结果不变；预热后的调用不再产生每次 848B 的格式对象分配 |
| P02 / SHOULD_FIX | 节气查询改固定下标循环，移除每次 Range/ToArray/排序；先不增加年份缓存 | F19 正确性通过；报告当前 0 B 查询分配和耗时，日期语义不同的旧公式不作为加速基线 |
| P03 / OPTIONAL | 普通周末算法使用“完整周 + 余数”；BusinessCalendar 无假日/补班时可共用按周规则的快速路径 | 全星期、正负偏移对照逐日参考结果；例外日历保留可靠扫描并报告 O(跨度) |
| P04 / OPTIONAL | NoRepeat 保留锁，按相同总操作数测单线程/多线程；同步配置后的成本纳入测量 | 报告吞吐、分配和争用；没有业务吞吐门槛时不凭一次 smoke 判定必须换算法 |

周末快速路径必须单独处理周末起点及正负方向，不能直接把 days/5 换成整周后忽略余数语义。补班优先级和零偏移原样返回保持不变。

正式测量复用现有 [Benchmark 项目](D:/Bing_Framework/Bing.Utils/benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj)，实施阶段增加对 DateTime 包的 ProjectReference，以及拟新增的 `D:/Bing_Framework/Bing.Utils/benchmarks/Bing.Utils.Benchmark/Benchmarks/DateTimeBenchmarks.cs`。可在同一文件中组织 Week、SolarTerm、BusinessDay、NoRepeat 子类，不新建第二套性能框架。

测量规则：

1. 先完成正确性修复，再冻结用于性能优化比较的版本；语义已改变或旧路径返回错误值的场景，标为不可直接比较，不能宣称虚假的加速。
2. 同一 TFM、运行时、输入、种子和配置；冻结一次基线，之后复用。记录实际候选产物和相关源码状态。
3. 工作日跨度使用 10/1000/100000，时区、节气日期和并发总操作数固定；不默认扩展到重型容量矩阵。
4. BenchmarkDotNet 的 Dry 只用于检查 harness；正式结果使用相同配置的 before/after，并记录 Mean、分配、GC；并发另报告总工作量。
5. 一次诊断、一次确认足够时停止。无关文档或测试命名变化不重跑基准。
6. 绝对吞吐/延迟 SLA、容量和回归百分比尚无用户要求，不自行设置发布阈值；量测完成与业务 SLA 通过分别记录。

## 8. 分阶段 TODO 与协作边界

### 阶段 0：基线与契约

- [x] 记录相关工作区状态、生产/测试 TFM 与现有证据，保留其他任务修改。
- [x] 将 D01～D08 作为实现依据，尤其明确 D02 的 UTC 端点迁移。
- [x] 建立 F01～F24 到测试方法的对应清单，记录每项状态。
- [x] 将临时复现转成对应模块正式回归测试；按批次加入，不要求一次让所有测试变红。

完成条件：每个已知缺陷有清楚的修复契约、源码范围和验收输入。

### 阶段 1：P1 与输入边界

- [x] 修复 F01～F07。
- [x] 同批修复直接相关的 F15、F16、F23，统一越界处理。
- [x] 修复错误 CurrentBehavior 测试，运行 L1；按受影响项目运行 L2。

完成条件：无界枚举、零增量重复、年/月混淆、减法符号和周期丢失均有失败前/通过后的证据。

### 阶段 2：时间序列、区间与 UTC

- [x] 实施 F18，并完成 F02 的混合入口和失败状态验证。
- [x] 实施 D02，修复 F08、F09；核对全部范围操作和 IDateTimeRange 消费路径。
- [x] 修复 F14、F17，保留各旧入口明确的 Unspecified 解释。
- [x] 更新受影响调用方、测试和迁移示例。

完成条件：混合 offset 样本为正 23 小时；区间代数一致；同一时刻的 Epoch 结果一致；时间戳各入口无错误时区重解释。

### 阶段 3：历法、导航与精度

- [x] 修复 F10～F13、F19～F22、F24。
- [x] 同步 DateTime、DateTimeOffset 与主包范围工厂的末 tick 行为。
- [x] 用完整 60 年周期、春节/闰月向量、跨年节气和星宿周期验证。

完成条件：合法日期不因混用历法分量而异常，导航和精度满足约定。

### 阶段 4：性能优化

- [x] 冻结正确性修复后的可比基线。
- [x] 完成 P01、P02 的局部优化。
- [x] P03 继续 DEFERRED；P04 已完成固定总工作量量测，保留锁实现。
- [x] 输出局部性能结果和适用环境，不把缺少 SLA 当成继续修改代码的理由。

完成条件：正确性保持；分配问题有受控对比；可选算法优化不扩大到不必要的缓存或并发重构。

### 阶段 5：文档与模块验收

- [x] 运行第 9 节的模块/TFM 验证；复用仍有效的证据。
- [x] 更新 [DateTime 模块文档](D:/Bing_Framework/Bing.Utils/docs/modules/Bing.Utils.DateTime.md)，补充主包日期说明和迁移示例。
- [x] 在 [API 参考](D:/Bing_Framework/Bing.Utils/docs/reference/api-reference.md) 中仅调整本次涉及的日期契约，保留其他任务的修改。
- [x] 中文 XML 注释明确 Kind、Offset、区间端点、精度、近似换算和异常。
- [x] 独立审查已修改代码与 F 清单，逐项标为 CLOSED 或有理由的其他状态。

协作分工：主代理负责 D02/D06、跨文件契约、集成和最终验证；luna_worker 可独立承担固定回归向量、农历索引修复、星期/边界小函数等明确文件范围。共享 DateTimeExtensions/DateTimeSpan 文件由单一负责人顺序修改。所有 worker 应知道代码库存在其他协作者，不能撤销他人改动。当前方案编写阶段已由 luna_worker 只读核对主包测试清单和兼容风险。

## 9. 验证命令与执行层级

以下命令是后续实施的操作清单，本次生成方案没有执行这些测试。命令在 Windows PowerShell 中运行，先设置 UTF-8：

```powershell
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath 'D:\Bing_Framework\Bing.Utils'
$dateRepairResults = Join-Path $env:TEMP 'bing-datetime-repair-results'
```

每批测试前记录：ChangedFiles、ChangedProjects、ChangedPublicContracts、ChangedRuntimePaths、ChangedTFMs、ChangedBuildPackaging、AffectedDependents、RiskLevel。纯文档批次只检查文档、编码和链接。

L1：仅覆盖当前修复的类。例如：

```powershell
dotnet test tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~DateTimeSpanCoreTest|FullyQualifiedName~NodaExtensionsTest|FullyQualifiedName~DateTimeOffsetCoreExtensionsTest' --nologo
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~NoRepeatTimeStampFactoryTest|FullyQualifiedName~DateTimeRangeTest|FullyQualifiedName~DateTimeRangeRegressionTest' --nologo
```

L2：独立包全部用例、主包日期定向用例；收口时直接在该次运行收集覆盖率，避免为了覆盖率重复执行同一集合：

```powershell
$mainDateFilter = @(
    'FullyQualifiedName~Bing.Date.'
    'FullyQualifiedName~Bing.Helpers.TimeTest'
    'FullyQualifiedName~Bing.Helpers.TimeRegressionTest'
    'FullyQualifiedName~Bing.Helpers.TimeAndUnixTimeBoundaryContractTest'
    'FullyQualifiedName~Bing.Helpers.UnixTimeHelperTest'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.SetDateTime_DateTime'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_ToDateTimeString'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_ToDateString'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_ToTimeString'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_ToMillisecondString'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_ToChineseDateString'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_ToChineseDateTimeString'
    'FullyQualifiedName~Bing.Utils.Tests.Extensions.ExtensionsTest.Test_Description_Span'
) -join '|'

dotnet test tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj -c Release -f net8.0 --nologo --logger 'trx;LogFileName=datetime-net8.trx' --results-directory "$dateRepairResults/datetime-net8" --collect 'XPlat Code Coverage' -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include='[Bing.Utils.DateTime]*'
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0 --nologo --filter $mainDateFilter --logger 'trx;LogFileName=main-date-net8.trx' --results-directory "$dateRepairResults/main-net8" --collect 'XPlat Code Coverage' -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include='[Bing.Utils]Bing.Date.*,[Bing.Utils]Bing.Helpers.Time,[Bing.Utils]Bing.Helpers.UnixTime'
```

重要收口时补齐其余既有测试 TFM；一次执行即可，不在每个小修复后循环全矩阵：

```powershell
foreach ($dateRepairTfm in @('net7.0', 'net6.0', 'net5.0', 'netcoreapp3.1')) {
    dotnet test tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj -c Release -f $dateRepairTfm --nologo --logger "trx;LogFileName=datetime-$dateRepairTfm.trx" --results-directory "$dateRepairResults/$dateRepairTfm"
    if ($LASTEXITCODE -ne 0) { throw "DateTime tests failed: $dateRepairTfm" }
    dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f $dateRepairTfm --nologo --filter $mainDateFilter --logger "trx;LogFileName=main-date-$dateRepairTfm.trx" --results-directory "$dateRepairResults/$dateRepairTfm"
    if ($LASTEXITCODE -ne 0) { throw "Main date tests failed: $dateRepairTfm" }
}
```

net5.0/netcoreapp3.1 测试通过项目引用消费兼容产物，须记录实际选中的 netstandard2.0 程序集。若前面的构建没有覆盖 netstandard2.0，补以下构建；已验证同一产物则复用证据：

```powershell
dotnet build src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj -c Release -f netstandard2.0 --nologo
```

L3/L4：D02/D06 涉及公开行为时，检索直接调用方并构建、测试实际受影响项目；必要时增加消费者用例。只有发现共享行为广泛影响时扩大到主包全量或 Solution，不因本文改动运行全仓测试。

时区与平台：现有显式 TimeZoneInfo/DST 单元测试继续保留；本地时区相关入口在 UTC、UTC+8 和一个 DST 时区的隔离环境验证。无法访问的平台记为外部证据缺口，不在本机修改系统时区，也不把未验证的平台写成通过。

L5：以下 Benchmark 命令在阶段 4 增加 DateTimeBenchmarks 类后才有效：

```powershell
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0 -- --filter '*DateTime*Benchmarks*' --job Dry
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -f net8.0 -- --filter '*DateTime*Benchmarks*' --job short --memory
```

## 10. 可选增强与保留边界

| ID | 状态 | 内容与后续条件 |
| --- | --- | --- |
| E01 | DEFERRED / OPTIONAL | DateOnly/TimeOnly：有真实调用需求时为 net6+ 提供窄接口，不在 netstandard2.0 上引入自造替代类型 |
| E02 | DEFERRED / OPTIONAL | ISO 周：显式输出周所属年份与周号，不能改变现有 GetWeekOfYear 默认 FirstDay/Sunday 规则 |
| E03 | DEFERRED / OPTIONAL | IClock 接入更多“当前时间”入口：优先增加独立包实例/重载入口；主包不能反向引用依赖它的 DateTime 包形成循环 |
| E04 | CLOSED | 1900～2100 年北京时间自然日静态表；来源和版本已记录，运行时不联网，不新增天文依赖 |
| E05 | DEFERRED / OPTIONAL | 将 GeneratedCount 改为真实调用次数，或改变 ChineseDateInfo.AddWorkDays 非正数默认加一的旧契约：分别设计迁移，不混入当前补丁 |

ToCst 的当前时钟语义、DateTimeSpan → TimeSpan 的近似换算、UnixTimeStamp 的非负秒级范围均应准确写入文档。文档修正不能替代 F 清单中的实现修复，也不能把未经用户接受的范围限制标成 ACCEPTED_LIMITATION。

## 11. 完成标准与停止条件

本轮修复完成需要：

- [x] F01～F24 均有对应实现、回归用例和状态说明；已选路线 B 的必需项关闭。
- [x] D01～D08 的行为与测试、XML 注释、模块文档一致；D02/D05/D06 的迁移影响可查。
- [x] 独立包完整测试、主包日期定向测试和实际受影响消费者通过。
- [x] 既有 TFM 的结果和实际产物有记录；缺失平台/运行时单独报告，不能伪造通过。
- [x] P01 有冻结基线对照，P02 有固定循环及当前分配证据（旧公式比较 NOT_APPLICABLE）；P03/P04 的取舍有说明，不设无依据的性能通过率。
- [x] 仅修改必要范围，没有升级包版本或覆盖其他任务工作。

执行遵循 GOAL_RULES：只有 OPEN_ACTIONABLE > 0 才允许继续自动修复；最多三轮自动 Review → Fix → Review；同一问题、候选、负载和环境最多一次诊断、一次确认。BLOCKED_EXTERNAL、BLOCKED_APPROVAL、NOT_APPLICABLE、DEFERRED 不能单独触发下一轮 Fix。无新证据的重复失败使用 No Progress Guard 停止。

每阶段分别报告 Implementation、Tests、Performance Evidence、External Evidence，避免用一个 PASS/FAIL 同时表示代码、性能 SLA 和发布状态。没有正式发布要求时不创建额外发布门禁。

当前交付状态：

- 已完成：F01～F24、官方节气日期表、P01/P02、回归测试、迁移文档和独立审查；复核追加的 F01/D08 公开入口、F19/F21 边界、F18 消费者五项工作均已关闭。
- 验收：五个框架每个均通过独立包 667、主包日期 435、IdUtils 消费者 39 项；netstandard2.0 构建已覆盖。UTC、上海、纽约隔离测试通过。
- 性能：默认周数从 848 B/次降为 0 B/次；节气固定循环 0 B/次；时间戳按相同固定工作量重新量测，保留锁。共享机器短样本波动明显，时间戳回归结论 NOT_VERIFIED，不据此宣称无性能回归或容量达标。
- Open Actionable / Blocked Approval / Blocked External：0；不再启动修复循环。
- 详见[实施与验收记录](../../quality/datetime-date-repair-report.md)和[迁移说明](../../reference/datetime-date-migration.md)。
