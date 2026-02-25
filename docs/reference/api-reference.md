# API Reference

> 统计范围: `src/Bing.Utils.Collections` / `src/Bing.Utils.DateTime` / `src/Bing.Utils.Drawing*` / `src/Bing.Utils.Http` / `src/Bing.Utils.IdUtils` / `src/Bing.Utils.Reflection` / `src/Bing.Utils.Text`.
> 高频 API 依据: 测试代码命中频次 + 入口类/扩展入口推断 (same-name methods may be merged).

## 命名空间索引

| 命名空间 | 分类 | Public 类型数 | 扩展方法族数 |
|---|---|---:|---:|
| `Bing` | Id | 1 | 1 |
| `Bing.Collections` | Collections / Text | 25 | 14 |
| `Bing.Conversions` | DateTime / Drawing | 5 | 2 |
| `Bing.Date` | DateTime | 12 | 179 |
| `Bing.Date.Chinese` | DateTime | 8 | 5 |
| `Bing.Date.DateUtils` | DateTime | 3 | 0 |
| `Bing.Drawing` | Drawing | 13 | 9 |
| `Bing.Dynamic` | Reflection | 1 | 0 |
| `Bing.Extensions` | Drawing | 2 | 22 |
| `Bing.Helpers` | Http / Id | 6 | 0 |
| `Bing.Http` | Http | 3 | 11 |
| `Bing.Http.Clients` | Http | 3 | 0 |
| `Bing.Http.Clients.Parameters` | Http | 2 | 0 |
| `Bing.Http.Extensions` | Http | 1 | 0 |
| `Bing.IdUtils` | Id | 17 | 0 |
| `Bing.IdUtils.GuidImplements` | Id | 1 | 0 |
| `Bing.IdUtils.GuidImplements.Internals` | Id | 1 | 0 |
| `Bing.Net` | Http | 2 | 0 |
| `Bing.Net.IPv4` | Http | 5 | 0 |
| `Bing.Net.IPv6` | Http | 13 | 0 |
| `Bing.Net.Mac` | Http | 2 | 0 |
| `Bing.Net.NetworkInformation` | Http | 10 | 0 |
| `Bing.Parameters` | Http | 1 | 2 |
| `Bing.Reflection` | Reflection | 5 | 21 |
| `Bing.Text` | Text | 7 | 6 |
| `Bing.Text.Joiners` | Text | 6 | 0 |
| `Bing.Text.Similarity` | Text | 2 | 0 |
| `Bing.Text.Splitters` | Text | 4 | 0 |
| `Bing.Text.Truncation` | Text | 2 | 0 |
| `NodaTime` | DateTime | 2 | 20 |
| `System` | DateTime | 1 | 1 |

## 分类索引

### Collections
- 公开类型: `24`, 扩展方法族: `13`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.Collections.ArrayJudge` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArrayJudge.cs:6` |
| `Bing.Collections.Arrays` | class | 高频入口 API，用于组合常用功能 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/Arrays.Copy.cs:22` |
| `Bing.Collections.ArraysExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysExtensions.Copy.cs:6` |
| `Bing.Collections.ArraysShortcutExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.Copy.cs:6` |
| `Bing.Collections.CollConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollConv.cs:8` |
| `Bing.Collections.CollConvExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollConvExtensions.cs:8` |
| `Bing.Collections.CollConvShortcutExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollConv.cs:160` |
| `Bing.Collections.CollJudge` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollJudge.cs:8` |
| `Bing.Collections.CollJudgeExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollJudge.cs:105` |
| `Bing.Collections.Colls` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/Colls.cs:64` |
| `Bing.Collections.CollsExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollsExtensions.cs:8` |
| `Bing.Collections.DictConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/DictConv.cs:9` |
| `Bing.Collections.DictConvExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/DictConvExtensions.cs:8` |
| `Bing.Collections.Dicts` | class | 高频入口 API，用于组合常用功能 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:6` |
| `Bing.Collections.DictsExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/DictsExtensions.cs:6` |
| `Bing.Collections.EnumerableProxy` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/EnumerableProxy.cs:9` |
| `Bing.Collections.ReadOnlyCollConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyCollConv.cs:9` |
| `Bing.Collections.ReadOnlyCollConvExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyCollConv.cs:28` |
| `Bing.Collections.ReadOnlyColls` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs:42` |
| `Bing.Collections.ReadOnlyCollsExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs:105` |
| `Bing.Collections.ReadOnlyDictConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyDictConv.cs:8` |
| `Bing.Collections.ReadOnlyDictConvExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyDictConvExtensions.cs:6` |
| `Bing.Collections.ReadOnlyDicts` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/ReadOnlyDicts.cs:28` |
| `Bing.Collections.ArrayCopyOptions` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Collections/Bing/Collections/Arrays.Copy.cs:6` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.Collections.ArraysShortcutExtensions.BinarySearch()` | 提供通用基础能力 | `this Array array, object value (+3 重载)` | `int` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:17` |
| `Bing.Collections.ArraysShortcutExtensions.BlockCopy()` | 提供通用基础能力 | `this Array src, int srcOffset, Array dst, int dstOffset, int count` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.Copy.cs:68` |
| `Bing.Collections.ArraysShortcutExtensions.ByteLength()` | 提供通用基础能力 | `this Array array` | `int` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:244` |
| `Bing.Collections.ArraysShortcutExtensions.Clear()` | 提供通用基础能力 | `this Array array, int index, int length (+1 重载)` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:60` |
| `Bing.Collections.ArraysShortcutExtensions.ConstrainedCopy()` | 提供通用基础能力 | `this Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.Copy.cs:57` |
| `Bing.Collections.ArraysShortcutExtensions.Copy()` | 提供通用基础能力 | `this Array sourceArray, Array destinationArray, int length (+3 重载)` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.Copy.cs:15` |
| `Bing.Collections.ArraysShortcutExtensions.GetByte()` | 用于获取目标值 | `this Array array, int index` | `byte` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:252` |
| `Bing.Collections.ArraysShortcutExtensions.IndexOf()` | 提供通用基础能力 | `this Array array, object value (+2 重载)` | `int` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:87` |
| `Bing.Collections.ArraysShortcutExtensions.LastIndexOf()` | 提供通用基础能力 | `this Array array, object value (+2 重载)` | `int` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:117` |
| `Bing.Collections.ArraysShortcutExtensions.Reverse()` | 提供通用基础能力 | `this Array array (+1 重载)` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:146` |
| `Bing.Collections.ArraysShortcutExtensions.SetByte()` | 用于设置目标值或更新实例 | `this Array array, int index, byte value` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:262` |
| `Bing.Collections.ArraysShortcutExtensions.Sort()` | 提供通用基础能力 | `this Array array (+7 重载)` | `void` | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:164` |
| `Bing.Collections.CollsExtensions.RemoveDuplicatesIgnoreCase()` | 用于移除指定元素 | `this IList<string> source` | `IEnumerable<string>` | `[证据] src/Bing.Utils.Collections/Bing/Collections/CollsExtensions.Ops.cs:112` |

### DateTime
- 公开类型: `30`, 扩展方法族: `207`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.Conversions.DayOfWeekConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Conversions/DayOfWeekConv.cs:6` |
| `Bing.Conversions.DayOfWeekConvExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Conversions/DayOfWeekConv.cs:36` |
| `Bing.Conversions.TimeSpanConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Conversions/TimeSpanConv.cs:6` |
| `Bing.Conversions.TimeSpanConvExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Conversions/TimeSpanConv.cs:26` |
| `Bing.Date.DateInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateInfo.cs:8` |
| `Bing.Date.DateJudge` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateJudge.cs:8` |
| `Bing.Date.DateTimeExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:10` |
| `Bing.Date.DateTimeFactory` | class | 提供对象构建能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeFactory.cs:9` |
| `Bing.Date.DateTimeOffsetExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:6` |
| `Bing.Date.DateTimeSpanExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:8` |
| `Bing.Date.DayOfWeekExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DayOfWeekExtensions.cs:8` |
| `Bing.Date.DateOffsetStyles` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetStyles.cs:6` |
| `Bing.Date.DateTimeOffsetOptions` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetOptions.cs:6` |
| `Bing.Date.RoundTo` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/RoundTo.cs:6` |
| `Bing.Date.TimeOffsetStyles` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetStyles.cs:37` |
| `Bing.Date.DateTimeSpan` | struct | 表示轻量值对象 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpan.Parse.cs:8` |
| `Bing.Date.Chinese.ChineseAnimalHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseAnimalHelper.cs:6` |
| `Bing.Date.Chinese.ChineseConstellationHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseConstellationHelper.cs:6` |
| `Bing.Date.Chinese.ChineseDateHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseDateHelper.cs:8` |
| `Bing.Date.Chinese.ChineseDateInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseDateInfo.cs:8` |
| `Bing.Date.Chinese.ChineseSolarTermHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTermHelper.cs:8` |
| `Bing.Date.Chinese.ChineseSolarTermsExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTermsExtensions.cs:6` |
| `Bing.Date.Chinese.DateTimeLeapExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/DateTimeLeapExtensions.cs:8` |
| `Bing.Date.Chinese.ChineseSolarTerms` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTerms.cs:9` |
| `Bing.Date.DateUtils.ConstellationHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateUtils/ConstellationHelper.cs:6` |
| `Bing.Date.DateUtils.DateTimeCalc` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateUtils/DateTimeCalc.cs:68` |
| `Bing.Date.DateUtils.DayOfWeekCalc` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateUtils/DayOfWeekCalc.cs:10` |
| `NodaTime.NodaDurationExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:8` |
| `NodaTime.NodaPeriodExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:8` |
| `System.BingDateTimeExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.DateTime/System/Extensions.DateTime.cs:6` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.Conversions.DayOfWeekConvExtensions.CastToInt32()` | 提供通用基础能力 | `this DayOfWeek week (+1 重载)` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Conversions/DayOfWeekConv.cs:42` |
| `Bing.Conversions.TimeSpanConvExtensions.CastToDateTime()` | 提供通用基础能力 | `this TimeSpan time` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Conversions/TimeSpanConv.cs:33` |
| `Bing.Date.DateTimeExtensions.AddBusinessDays()` | 用于追加或增量计算 | `this DateTime dt, int days` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:72` |
| `Bing.Date.DateTimeExtensions.AddDuration()` | 用于追加或增量计算 | `this DateTime dt, Duration duration` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:55` |
| `Bing.Date.DateTimeExtensions.AddQuarters()` | 用于追加或增量计算 | `this DateTime dt, int quarters` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:40` |
| `Bing.Date.DateTimeExtensions.AddWeeks()` | 用于追加或增量计算 | `this DateTime dt, int weeks` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:26` |
| `Bing.Date.DateTimeExtensions.At()` | 提供通用基础能力 | `this DateTime dt, int hour, int minute (+2 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:14` |
| `Bing.Date.DateTimeExtensions.AtMidnight()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:39` |
| `Bing.Date.DateTimeExtensions.AtNoon()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:45` |
| `Bing.Date.DateTimeExtensions.BeginningOfDay()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:70` |
| `Bing.Date.DateTimeExtensions.BeginningOfHour()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:64` |
| `Bing.Date.DateTimeExtensions.BeginningOfMinute()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:58` |
| `Bing.Date.DateTimeExtensions.BeginningOfMonth()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:96` |
| `Bing.Date.DateTimeExtensions.BeginningOfQuarter()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:109` |
| `Bing.Date.DateTimeExtensions.BeginningOfSecond()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:52` |
| `Bing.Date.DateTimeExtensions.BeginningOfWeek()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:83` |
| `Bing.Date.DateTimeExtensions.BeginningOfYear()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:122` |
| `Bing.Date.DateTimeExtensions.Clone()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:147` |
| `Bing.Date.DateTimeExtensions.DaysInMonth()` | 提供通用基础能力 | `this DateTime dt` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:391` |
| `Bing.Date.DateTimeExtensions.DaysInYear()` | 提供通用基础能力 | `this DateTime dt` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:397` |
| `Bing.Date.DateTimeExtensions.ElapsedMilliseconds()` | 提供通用基础能力 | `this DateTime dt` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:251` |
| `Bing.Date.DateTimeExtensions.ElapsedTime()` | 提供通用基础能力 | `this DateTime dt` | `TimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:234` |
| `Bing.Date.DateTimeExtensions.EndOfDay()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:157` |
| `Bing.Date.DateTimeExtensions.EndOfHour()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:151` |
| `Bing.Date.DateTimeExtensions.EndOfMinute()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:145` |
| `Bing.Date.DateTimeExtensions.EndOfMonth()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:183` |
| `Bing.Date.DateTimeExtensions.EndOfQuarter()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:196` |
| `Bing.Date.DateTimeExtensions.EndOfSecond()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:139` |
| `Bing.Date.DateTimeExtensions.EndOfWeek()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:170` |
| `Bing.Date.DateTimeExtensions.EndOfYear()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:209` |
| `Bing.Date.DateTimeExtensions.FirstDayOfMonth()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:276` |
| `Bing.Date.DateTimeExtensions.FirstDayOfQuarter()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:252` |
| `Bing.Date.DateTimeExtensions.FirstDayOfWeek()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:295` |
| `Bing.Date.DateTimeExtensions.FirstDayOfYear()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:233` |
| `Bing.Date.DateTimeExtensions.GetMonthDiff()` | 用于获取目标值 | `this DateTime dt1, DateTime dt2` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:170` |
| `Bing.Date.DateTimeExtensions.GetQuarter()` | 用于获取目标值 | `this DateTime dt` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:404` |
| `Bing.Date.DateTimeExtensions.GetQuarterEnum()` | 用于获取目标值 | `this DateTime dt` | `Quarter` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:411` |
| `Bing.Date.DateTimeExtensions.GetTimeSpan()` | 用于获取目标值 | `this DateTime leftDt, DateTime rightDt` | `TimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:227` |
| `Bing.Date.DateTimeExtensions.GetTotalMonthDiff()` | 用于获取目标值 | `this DateTime dt1, DateTime dt2` | `double` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:196` |
| `Bing.Date.DateTimeExtensions.GetWeekOfYear()` | 用于获取目标值 | `this DateTime dt (+3 重载)` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:428` |
| `Bing.Date.DateTimeExtensions.IsAM()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:502` |
| `Bing.Date.DateTimeExtensions.IsAfter()` | 用于判断条件是否成立 | `this DateTime dt, DateTime toCompareWith, bool includeBoundary = false (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:597` |
| `Bing.Date.DateTimeExtensions.IsAfternoon()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:441` |
| `Bing.Date.DateTimeExtensions.IsBefore()` | 用于判断条件是否成立 | `this DateTime dt, DateTime toCompareWith, bool includeBoundary = false (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:542` |
| `Bing.Date.DateTimeExtensions.IsBetween()` | 用于判断条件是否成立 | `this DateTime dt, DateTime from, DateTime to, bool includeBoundary = true` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:277` |
| `Bing.Date.DateTimeExtensions.IsDateBetweenWithBoundary()` | 用于判断条件是否成立 | `this DateTime dt, DateTime min, DateTime max (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:303` |
| `Bing.Date.DateTimeExtensions.IsDateBetweenWithoutBoundary()` | 用于判断条件是否成立 | `this DateTime dt, DateTime min, DateTime max` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:355` |
| `Bing.Date.DateTimeExtensions.IsDateEqual()` | 用于判断条件是否成立 | `this DateTime dt, DateTime date` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:932` |
| `Bing.Date.DateTimeExtensions.IsDusk()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:461` |
| `Bing.Date.DateTimeExtensions.IsEarlyMorning()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:407` |
| `Bing.Date.DateTimeExtensions.IsEvening()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:484` |
| `Bing.Date.DateTimeExtensions.IsInTheFuture()` | 用于判断条件是否成立 | `this DateTime dt, bool includeBoundary = false (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:646` |
| `Bing.Date.DateTimeExtensions.IsInThePast()` | 用于判断条件是否成立 | `this DateTime dt, bool includeBoundary = false (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:692` |
| `Bing.Date.DateTimeExtensions.IsLeapYear()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:971` |
| `Bing.Date.DateTimeExtensions.IsMorning()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:424` |
| `Bing.Date.DateTimeExtensions.IsPM()` | 用于判断条件是否成立 | `this DateTime dt` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:516` |
| `Bing.Date.DateTimeExtensions.IsSameDay()` | 用于判断条件是否成立 | `this DateTime dt, DateTime date (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:810` |
| `Bing.Date.DateTimeExtensions.IsSameMonth()` | 用于判断条件是否成立 | `this DateTime dt, DateTime date (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:851` |
| `Bing.Date.DateTimeExtensions.IsSameYear()` | 用于判断条件是否成立 | `this DateTime dt, DateTime date (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:892` |
| `Bing.Date.DateTimeExtensions.IsTimeEqual()` | 用于判断条件是否成立 | `this DateTime dt, DateTime date` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:950` |
| `Bing.Date.DateTimeExtensions.IsToday()` | 用于判断条件是否成立 | `this DateTime dt (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:375` |
| `Bing.Date.DateTimeExtensions.IsWeekday()` | 用于判断条件是否成立 | `this DateTime dt (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:737` |
| `Bing.Date.DateTimeExtensions.IsWeekend()` | 用于判断条件是否成立 | `this DateTime dt (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:772` |
| `Bing.Date.DateTimeExtensions.LastDayOfMonth()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:359` |
| `Bing.Date.DateTimeExtensions.LastDayOfQuarter()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:334` |
| `Bing.Date.DateTimeExtensions.LastDayOfWeek()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:378` |
| `Bing.Date.DateTimeExtensions.LastDayOfYear()` | 提供通用基础能力 | `this DateTime dt (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:315` |
| `Bing.Date.DateTimeExtensions.NextDay()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:508` |
| `Bing.Date.DateTimeExtensions.NextDayOfWeek()` | 提供通用基础能力 | `this DateTime dt, DayOfWeek dayOfWeek` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:535` |
| `Bing.Date.DateTimeExtensions.NextMonth()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:484` |
| `Bing.Date.DateTimeExtensions.NextQuarter()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:472` |
| `Bing.Date.DateTimeExtensions.NextWeek()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:496` |
| `Bing.Date.DateTimeExtensions.NextYear()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:460` |
| `Bing.Date.DateTimeExtensions.OffsetBy()` | 提供通用基础能力 | `this DateTime dt, int offsetVal, DateOffsetStyles styles (+1 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:17` |
| `Bing.Date.DateTimeExtensions.On()` | 提供通用基础能力 | `this DateTime dt, int year, int month, int day` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:58` |
| `Bing.Date.DateTimeExtensions.PreviousDay()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:514` |
| `Bing.Date.DateTimeExtensions.PreviousDayOfWeek()` | 提供通用基础能力 | `this DateTime dt, DayOfWeek dayOfWeek` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:542` |
| `Bing.Date.DateTimeExtensions.PreviousMonth()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:490` |
| `Bing.Date.DateTimeExtensions.PreviousQuarter()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:478` |
| `Bing.Date.DateTimeExtensions.PreviousWeek()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:502` |
| `Bing.Date.DateTimeExtensions.PreviousYear()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:466` |
| `Bing.Date.DateTimeExtensions.Round()` | 提供通用基础能力 | `this DateTime dt, RoundTo rt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1001` |
| `Bing.Date.DateTimeExtensions.SetDate()` | 用于设置目标值或更新实例 | `this DateTime dt, int year (+2 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:131` |
| `Bing.Date.DateTimeExtensions.SetDay()` | 用于设置目标值或更新实例 | `this DateTime dt, int day` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:169` |
| `Bing.Date.DateTimeExtensions.SetHour()` | 用于设置目标值或更新实例 | `this DateTime dt, int hour` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:103` |
| `Bing.Date.DateTimeExtensions.SetKind()` | 用于设置目标值或更新实例 | `this DateTime dt, DateTimeKind kind` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:176` |
| `Bing.Date.DateTimeExtensions.SetMillisecond()` | 用于设置目标值或更新实例 | `this DateTime dt, int millisecond` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:124` |
| `Bing.Date.DateTimeExtensions.SetMinute()` | 用于设置目标值或更新实例 | `this DateTime dt, int minute` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:110` |
| `Bing.Date.DateTimeExtensions.SetMonth()` | 用于设置目标值或更新实例 | `this DateTime dt, int month` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:162` |
| `Bing.Date.DateTimeExtensions.SetSecond()` | 用于设置目标值或更新实例 | `this DateTime dt, int second` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:117` |
| `Bing.Date.DateTimeExtensions.SetTime()` | 用于设置目标值或更新实例 | `this DateTime dt, int hour (+3 重载)` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:69` |
| `Bing.Date.DateTimeExtensions.SetYear()` | 用于设置目标值或更新实例 | `this DateTime dt, int year` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:155` |
| `Bing.Date.DateTimeExtensions.ToBytes()` | 用于将输入转换为目标表示 | `this DateTime dt` | `byte[]` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1160` |
| `Bing.Date.DateTimeExtensions.ToCalculateAge()` | 用于将输入转换为目标表示 | `this DateTime birthday (+1 重载)` | `int` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:104` |
| `Bing.Date.DateTimeExtensions.ToCst()` | 用于将输入转换为目标表示 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1079` |
| `Bing.Date.DateTimeExtensions.ToEpochTimeSpan()` | 用于将输入转换为目标表示 | `this DateTime dt` | `TimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1101` |
| `Bing.Date.DateTimeExtensions.ToLocalDate()` | 用于将输入转换为目标表示 | `this DateTime dt` | `LocalDate` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1132` |
| `Bing.Date.DateTimeExtensions.ToLocalDateTime()` | 用于将输入转换为目标表示 | `this DateTime dt` | `LocalDateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1118` |
| `Bing.Date.DateTimeExtensions.ToNodaLocalTime()` | 用于将输入转换为目标表示 | `this DateTime dt` | `LocalTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1149` |
| `Bing.Date.DateTimeExtensions.ToUtc()` | 用于将输入转换为目标表示 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1061` |
| `Bing.Date.DateTimeExtensions.Tomorrow()` | 用于将输入转换为目标表示 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:521` |
| `Bing.Date.DateTimeExtensions.Yesterday()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:528` |
| `Bing.Date.DateTimeOffsetExtensions.AddBusinessDays()` | 用于追加或增量计算 | `this DateTimeOffset dto, int days` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:35` |
| `Bing.Date.DateTimeOffsetExtensions.AddDateTimeSpan()` | 用于追加或增量计算 | `this DateTimeOffset dto, DateTimeSpan timeSpan` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:15` |
| `Bing.Date.DateTimeOffsetExtensions.At()` | 提供通用基础能力 | `this DateTimeOffset dto, int hour, int minute (+2 重载)` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:14` |
| `Bing.Date.DateTimeOffsetExtensions.AtMidnight()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:39` |
| `Bing.Date.DateTimeOffsetExtensions.AtNoon()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:45` |
| `Bing.Date.DateTimeOffsetExtensions.BeginningOfDay()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:16` |
| `Bing.Date.DateTimeOffsetExtensions.EndOfDay()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:27` |
| `Bing.Date.DateTimeOffsetExtensions.FirstDayOfMonth()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:54` |
| `Bing.Date.DateTimeOffsetExtensions.FirstDayOfQuarter()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:43` |
| `Bing.Date.DateTimeOffsetExtensions.FirstDayOfWeek()` | 提供通用基础能力 | `this DateTimeOffset dto, CultureInfo cultureInfo = null` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:61` |
| `Bing.Date.DateTimeOffsetExtensions.FirstDayOfYear()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:37` |
| `Bing.Date.DateTimeOffsetExtensions.IsAfter()` | 用于判断条件是否成立 | `this DateTimeOffset current, DateTimeOffset toCompareWith` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:85` |
| `Bing.Date.DateTimeOffsetExtensions.IsBefore()` | 用于判断条件是否成立 | `this DateTimeOffset current, DateTimeOffset toCompareWith` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:78` |
| `Bing.Date.DateTimeOffsetExtensions.IsInTheFuture()` | 用于判断条件是否成立 | `this DateTimeOffset dto` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:91` |
| `Bing.Date.DateTimeOffsetExtensions.IsInThePast()` | 用于判断条件是否成立 | `this DateTimeOffset dto` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:97` |
| `Bing.Date.DateTimeOffsetExtensions.IsSameDay()` | 用于判断条件是否成立 | `this DateTimeOffset current, DateTimeOffset date` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:104` |
| `Bing.Date.DateTimeOffsetExtensions.IsSameMonth()` | 用于判断条件是否成立 | `this DateTimeOffset current, DateTimeOffset date` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:111` |
| `Bing.Date.DateTimeOffsetExtensions.IsSameYear()` | 用于判断条件是否成立 | `this DateTimeOffset current, DateTimeOffset date` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:118` |
| `Bing.Date.DateTimeOffsetExtensions.IsToday()` | 用于判断条件是否成立 | `this DateTimeOffset dto (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:65` |
| `Bing.Date.DateTimeOffsetExtensions.LastDayOfMonth()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:92` |
| `Bing.Date.DateTimeOffsetExtensions.LastDayOfQuarter()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:80` |
| `Bing.Date.DateTimeOffsetExtensions.LastDayOfWeek()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:98` |
| `Bing.Date.DateTimeOffsetExtensions.LastDayOfYear()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:74` |
| `Bing.Date.DateTimeOffsetExtensions.NextDay()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:168` |
| `Bing.Date.DateTimeOffsetExtensions.NextDayOfWeek()` | 提供通用基础能力 | `this DateTimeOffset dto, DayOfWeek dayOfWeek` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:193` |
| `Bing.Date.DateTimeOffsetExtensions.NextMonth()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:144` |
| `Bing.Date.DateTimeOffsetExtensions.NextQuarter()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:132` |
| `Bing.Date.DateTimeOffsetExtensions.NextWeek()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:156` |
| `Bing.Date.DateTimeOffsetExtensions.NextYear()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:120` |
| `Bing.Date.DateTimeOffsetExtensions.On()` | 提供通用基础能力 | `this DateTimeOffset dt, int year, int month, int day` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:58` |
| `Bing.Date.DateTimeOffsetExtensions.PreviousDay()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:174` |
| `Bing.Date.DateTimeOffsetExtensions.PreviousDayOfWeek()` | 提供通用基础能力 | `this DateTimeOffset dto, DayOfWeek dayOfWeek` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:200` |
| `Bing.Date.DateTimeOffsetExtensions.PreviousMonth()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:150` |
| `Bing.Date.DateTimeOffsetExtensions.PreviousQuarter()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:138` |
| `Bing.Date.DateTimeOffsetExtensions.PreviousWeek()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:162` |
| `Bing.Date.DateTimeOffsetExtensions.PreviousYear()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:126` |
| `Bing.Date.DateTimeOffsetExtensions.Round()` | 提供通用基础能力 | `this DateTimeOffset dto, RoundTo roundTo` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:130` |
| `Bing.Date.DateTimeOffsetExtensions.SetDate()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int year (+2 重载)` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:139` |
| `Bing.Date.DateTimeOffsetExtensions.SetDay()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int day` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:182` |
| `Bing.Date.DateTimeOffsetExtensions.SetHour()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int hour` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:107` |
| `Bing.Date.DateTimeOffsetExtensions.SetMillisecond()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int millisecond` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:131` |
| `Bing.Date.DateTimeOffsetExtensions.SetMinute()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int minute` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:115` |
| `Bing.Date.DateTimeOffsetExtensions.SetMonth()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int month` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:174` |
| `Bing.Date.DateTimeOffsetExtensions.SetSecond()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int second` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:123` |
| `Bing.Date.DateTimeOffsetExtensions.SetTime()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int hour (+3 重载)` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:69` |
| `Bing.Date.DateTimeOffsetExtensions.SetYear()` | 用于设置目标值或更新实例 | `this DateTimeOffset originalDate, int year` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:166` |
| `Bing.Date.DateTimeOffsetExtensions.SubtractBusinessDays()` | 提供通用基础能力 | `this DateTimeOffset dto, int days` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:55` |
| `Bing.Date.DateTimeOffsetExtensions.SubtractDateTimeSpan()` | 提供通用基础能力 | `this DateTimeOffset dto, DateTimeSpan timeSpan` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.cs:25` |
| `Bing.Date.DateTimeOffsetExtensions.Tomorrow()` | 用于将输入转换为目标表示 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:180` |
| `Bing.Date.DateTimeOffsetExtensions.WeekAfter()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:110` |
| `Bing.Date.DateTimeOffsetExtensions.WeekBefore()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:104` |
| `Bing.Date.DateTimeOffsetExtensions.Yesterday()` | 提供通用基础能力 | `this DateTimeOffset dto` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Navidation.cs:186` |
| `Bing.Date.DateTimeSpanExtensions.Before()` | 提供通用基础能力 | `this DateTimeSpan ts (+2 重载)` | `DateTime/DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:23` |
| `Bing.Date.DateTimeSpanExtensions.Day()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:395` |
| `Bing.Date.DateTimeSpanExtensions.Days()` | 提供通用基础能力 | `this int days (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:208` |
| `Bing.Date.DateTimeSpanExtensions.From()` | 提供通用基础能力 | `this DateTimeSpan ts, DateTime originalValue (+1 重载)` | `DateTime/DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:98` |
| `Bing.Date.DateTimeSpanExtensions.FromNow()` | 提供通用基础能力 | `this DateTimeSpan ts` | `DateTime` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:83` |
| `Bing.Date.DateTimeSpanExtensions.Hour()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:408` |
| `Bing.Date.DateTimeSpanExtensions.Hours()` | 提供通用基础能力 | `this int hours (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:230` |
| `Bing.Date.DateTimeSpanExtensions.Millisecond()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:447` |
| `Bing.Date.DateTimeSpanExtensions.Milliseconds()` | 提供通用基础能力 | `this int milliseconds (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:296` |
| `Bing.Date.DateTimeSpanExtensions.Minute()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:421` |
| `Bing.Date.DateTimeSpanExtensions.Minutes()` | 提供通用基础能力 | `this int minutes (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:252` |
| `Bing.Date.DateTimeSpanExtensions.Month()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:369` |
| `Bing.Date.DateTimeSpanExtensions.Months()` | 提供通用基础能力 | `this int months` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:172` |
| `Bing.Date.DateTimeSpanExtensions.OffsetBefore()` | 提供通用基础能力 | `this DateTimeSpan ts` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:51` |
| `Bing.Date.DateTimeSpanExtensions.OffsetFromNow()` | 提供通用基础能力 | `this DateTimeSpan ts` | `DateTimeOffset` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:111` |
| `Bing.Date.DateTimeSpanExtensions.Quarter()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:356` |
| `Bing.Date.DateTimeSpanExtensions.Quarters()` | 提供通用基础能力 | `this int quarters` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:158` |
| `Bing.Date.DateTimeSpanExtensions.Second()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:434` |
| `Bing.Date.DateTimeSpanExtensions.Seconds()` | 提供通用基础能力 | `this int seconds (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:274` |
| `Bing.Date.DateTimeSpanExtensions.Ticks()` | 提供通用基础能力 | `this int ticks (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:312` |
| `Bing.Date.DateTimeSpanExtensions.Week()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:382` |
| `Bing.Date.DateTimeSpanExtensions.Weeks()` | 提供通用基础能力 | `this int weeks (+1 重载)` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:186` |
| `Bing.Date.DateTimeSpanExtensions.Year()` | 提供通用基础能力 | `this int _` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:343` |
| `Bing.Date.DateTimeSpanExtensions.Years()` | 提供通用基础能力 | `this int years` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:144` |
| `Bing.Date.DayOfWeekExtensions.AddDays()` | 用于追加或增量计算 | `this DayOfWeek dayOfWeek, int days` | `DayOfWeek` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DayOfWeekExtensions.cs:43` |
| `Bing.Date.DayOfWeekExtensions.GetDaysBetween()` | 用于获取目标值 | `this DayOfWeek from, DayOfWeek to, bool includeBoundary = true` | `IEnumerable<DayOfWeek>` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DayOfWeekExtensions.cs:53` |
| `Bing.Date.DayOfWeekExtensions.ToChinese()` | 用于将输入转换为目标表示 | `this DayOfWeek week (+1 重载)` | `string` | `[证据] src/Bing.Utils.DateTime/Bing/Date/DayOfWeekExtensions.cs:15` |
| `Bing.Date.Chinese.ChineseSolarTermsExtensions.GetEnglishName()` | 用于获取目标值 | `this ChineseSolarTerms chineseSolarTerms` | `string` | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTermsExtensions.cs:22` |
| `Bing.Date.Chinese.ChineseSolarTermsExtensions.GetName()` | 用于获取目标值 | `this ChineseSolarTerms chineseSolarTerms, bool traditionalChineseCharacters = false` | `string` | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTermsExtensions.cs:14` |
| `Bing.Date.Chinese.DateTimeLeapExtensions.IsLeapDay()` | 用于判断条件是否成立 | `this DateTime dt (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/DateTimeLeapExtensions.cs:45` |
| `Bing.Date.Chinese.DateTimeLeapExtensions.IsLeapMonth()` | 用于判断条件是否成立 | `this DateTime dt (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/DateTimeLeapExtensions.cs:30` |
| `Bing.Date.Chinese.DateTimeLeapExtensions.IsLeapYear()` | 用于判断条件是否成立 | `this DateTime dt (+1 重载)` | `bool` | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/DateTimeLeapExtensions.cs:15` |
| `NodaTime.NodaDurationExtensions.AsDuration()` | 提供通用基础能力 | `this TimeSpan ts (+1 重载)` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:14` |
| `NodaTime.NodaDurationExtensions.AsDurationOfDays()` | 提供通用基础能力 | `this long days (+1 重载)` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:74` |
| `NodaTime.NodaDurationExtensions.AsDurationOfHours()` | 提供通用基础能力 | `this long hours (+1 重载)` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:62` |
| `NodaTime.NodaDurationExtensions.AsDurationOfMilliseconds()` | 提供通用基础能力 | `this long milliseconds (+1 重载)` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:26` |
| `NodaTime.NodaDurationExtensions.AsDurationOfMinutes()` | 提供通用基础能力 | `this long minutes (+1 重载)` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:50` |
| `NodaTime.NodaDurationExtensions.AsDurationOfSeconds()` | 提供通用基础能力 | `this long seconds (+1 重载)` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:38` |
| `NodaTime.NodaDurationExtensions.AsDurationOfWeeks()` | 提供通用基础能力 | `this int weeks` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaDurationExtensions.cs:86` |
| `NodaTime.NodaPeriodExtensions.AsDateTimeSpan()` | 提供通用基础能力 | `this Period p` | `DateTimeSpan` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:38` |
| `NodaTime.NodaPeriodExtensions.AsDuration()` | 提供通用基础能力 | `this Period p` | `Duration` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:44` |
| `NodaTime.NodaPeriodExtensions.AsPeriod()` | 提供通用基础能力 | `this TimeSpan ts (+2 重载)` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:14` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfDays()` | 提供通用基础能力 | `this int days` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:80` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfHours()` | 提供通用基础能力 | `this long hours` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:74` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfMilliseconds()` | 提供通用基础能力 | `this long milliseconds` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:56` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfMinutes()` | 提供通用基础能力 | `this long minutes` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:68` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfMonth()` | 提供通用基础能力 | `this int months` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:86` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfNanoseconds()` | 提供通用基础能力 | `this long nanoseconds` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:50` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfQuarters()` | 提供通用基础能力 | `this int quarters` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:92` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfSeconds()` | 提供通用基础能力 | `this long seconds` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:62` |
| `NodaTime.NodaPeriodExtensions.AsPeriodOfYears()` | 提供通用基础能力 | `this int years` | `Period` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:98` |
| `NodaTime.NodaPeriodExtensions.AsTimeSpan()` | 提供通用基础能力 | `this Period p` | `TimeSpan` | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:32` |
| `System.BingDateTimeExtensions.Clone()` | 提供通用基础能力 | `this DateTime dt` | `DateTime` | `[证据] src/Bing.Utils.DateTime/System/Extensions.DateTime.cs:14` |

### Drawing
- 公开类型: `16`, 扩展方法族: `31`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.Conversions.ColorConv` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Conversions/ColorConv.cs:9` |
| `Bing.Drawing.BitmapExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs:11` |
| `Bing.Drawing.CaptchaBuilder` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:11` |
| `Bing.Drawing.ColorEffect` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/ColorEffect.cs:9` |
| `Bing.Drawing.ColorExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs:9` |
| `Bing.Drawing.ColorMatrices` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/ColorMatrices.cs:10` |
| `Bing.Drawing.ImageEffect` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/ImageEffect.cs:10` |
| `Bing.Drawing.ImageHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/ImageHelper.Convert.cs:9` |
| `Bing.Drawing.ImageSharpHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:10` |
| `Bing.Drawing.SKEncodedImageFormatExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:10` |
| `Bing.Drawing.SkiaSharpHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs:8` |
| `Bing.Drawing.CaptchaType` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/CaptchaType.cs:6` |
| `Bing.Drawing.ImageLocationMode` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/ImageLocationMode.cs:6` |
| `Bing.Drawing.ThumbnailMode` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/ThumbnailMode.cs:6` |
| `Bing.Extensions.BitmapExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:10` |
| `Bing.Extensions.ImageExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs:10` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.Drawing.BitmapExtensions.PerPixelProcess()` | 提供通用基础能力 | `this Bitmap bitmap, Func<Color, Color> func` | `void` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs:40` |
| `Bing.Drawing.BitmapExtensions.SetBrightness()` | 用于设置目标值或更新实例 | `this Bitmap bitmap, float percentage` | `void` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs:18` |
| `Bing.Drawing.BitmapExtensions.SetContrast()` | 用于设置目标值或更新实例 | `this Bitmap bitmap, float percentage` | `void` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/BitmapExtensions.cs:29` |
| `Bing.Drawing.ColorExtensions.Blend()` | 提供通用基础能力 | `this Color color, Color backColor, double amount` | `Color` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs:41` |
| `Bing.Drawing.ColorExtensions.ColorDifference()` | 提供通用基础能力 | `this Color x, Color y` | `double` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs:80` |
| `Bing.Drawing.ColorExtensions.GetDuotoneColor()` | 用于获取目标值 | `this Color sourceColor, Color clr1, Color clr2` | `Color` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs:26` |
| `Bing.Drawing.ColorExtensions.GetGrayScale()` | 用于获取目标值 | `this Color color` | `float` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs:15` |
| `Bing.Drawing.ColorExtensions.IsSimilarColors()` | 用于判断条件是否成立 | `this Color x, Color y, int accuracy = 36` | `bool` | `[证据] src/Bing.Utils.Drawing/Bing/Drawing/Extensions/ColorExtensions.cs:55` |
| `Bing.Drawing.SKEncodedImageFormatExtensions.GetMimeType()` | 用于获取目标值 | `this SKEncodedImageFormat format` | `string` | `[证据] src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:16` |
| `Bing.Extensions.BitmapExtensions.AddBorder()` | 用于追加或增量计算 | `this byte[,] grayBytes, int border, byte gray = 255` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:433` |
| `Bing.Extensions.BitmapExtensions.Binaryzation()` | 提供通用基础能力 | `this byte[,] grayBytes, byte gray` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:181` |
| `Bing.Extensions.BitmapExtensions.ClearBorder()` | 提供通用基础能力 | `this byte[,] grayBytes, int border` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:406` |
| `Bing.Extensions.BitmapExtensions.ClearGray()` | 提供通用基础能力 | `this byte[,] grayBytes, byte minGray, byte maxGray` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:498` |
| `Bing.Extensions.BitmapExtensions.ClearNoiseArea()` | 提供通用基础能力 | `this byte[,] binBytes, byte gray, int minAreaPoints` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:276` |
| `Bing.Extensions.BitmapExtensions.ClearNoiseRound()` | 提供通用基础能力 | `this byte[,] binBytes, byte gray, int maxNearPoints` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:231` |
| `Bing.Extensions.BitmapExtensions.Clone()` | 提供通用基础能力 | `this byte[,] sourceBytes, int x1, int y1, int width, int height` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:553` |
| `Bing.Extensions.BitmapExtensions.DeepFore()` | 提供通用基础能力 | `this byte[,] grayBytes, byte gray = 200` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:204` |
| `Bing.Extensions.BitmapExtensions.DrawTo()` | 提供通用基础能力 | `this byte[,] smallBytes, byte[,] bigBytes, int x1, int y1` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:463` |
| `Bing.Extensions.BitmapExtensions.FloodFill()` | 提供通用基础能力 | `this byte[,] binBytes, Point point, byte replacementGray (+1 重载)` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:329` |
| `Bing.Extensions.BitmapExtensions.ShadowX()` | 提供通用基础能力 | `this byte[,] binBytes` | `int[]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:606` |
| `Bing.Extensions.BitmapExtensions.ShadowY()` | 提供通用基础能力 | `this byte[,] binBytes` | `int[]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:582` |
| `Bing.Extensions.BitmapExtensions.SplitShadowY()` | 提供通用基础能力 | `this byte[,] binBytes, byte minFontWidth = 0, byte minLines = 0` | `List<byte[,]>` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:633` |
| `Bing.Extensions.BitmapExtensions.ToBitmap()` | 用于将输入转换为目标表示 | `this Color[,] pixels (+1 重载)` | `Bitmap` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:115` |
| `Bing.Extensions.BitmapExtensions.ToCodeString()` | 用于将输入转换为目标表示 | `this byte[,] binBytes, byte gray, bool breakLine = false` | `string` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:675` |
| `Bing.Extensions.BitmapExtensions.ToGrayArray2D()` | 用于将输入转换为目标表示 | `this Bitmap bitmap (+1 重载)` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:49` |
| `Bing.Extensions.BitmapExtensions.ToPixelArray2D()` | 用于将输入转换为目标表示 | `this Bitmap bitmap` | `Color[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:18` |
| `Bing.Extensions.BitmapExtensions.ToValid()` | 用于将输入转换为目标表示 | `this byte[,] binBytes, byte gray = 200` | `byte[,]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/BitmapExtensions.cs:522` |
| `Bing.Extensions.ImageExtensions.ScaleImage()` | 提供通用基础能力 | `this Image image, int width, int height` | `Image` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs:35` |
| `Bing.Extensions.ImageExtensions.ToBase64String()` | 用于将输入转换为目标表示 | `this Image image, ImageFormat format` | `string` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs:17` |
| `Bing.Extensions.ImageExtensions.ToBase64StringWithPrefix()` | 用于将输入转换为目标表示 | `this Image image, ImageFormat format` | `string` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs:27` |
| `Bing.Extensions.ImageExtensions.ToBytes()` | 用于将输入转换为目标表示 | `this Image image, ImageFormat format` | `byte[]` | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs:42` |

### Http
- 公开类型: `47`, 扩展方法族: `13`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.Helpers.CookieHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.Http/Bing/Helpers/CookieHelper.cs:8` |
| `Bing.Helpers.Ip` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Helpers/Ip.cs:12` |
| `Bing.Helpers.UserAgentHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.Http/Bing/Helpers/UserAgentHelper.cs:8` |
| `Bing.Helpers.UserAgentInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Helpers/UserAgentHelper.cs:327` |
| `Bing.Helpers.Web` | class | 高频入口 API，用于组合常用功能 | - | - | `[证据] src/Bing.Utils.Http/Bing/Helpers/Web.cs:17` |
| `Bing.Http.HttpRequestExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:12` |
| `Bing.Http.HttpResponseExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs:11` |
| `Bing.Http.HttpResponseMessageExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/HttpResponseMessageExtensions.cs:9` |
| `Bing.Http.Clients.FileData` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Clients/FileData.cs:6` |
| `Bing.Http.Clients.HttpClientService` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs:9` |
| `Bing.Http.Clients.HttpRequest` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs:20` |
| `Bing.Http.Clients.Parameters.PhysicalFileParameter` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Clients/Parameters/PhysicalFileParameter.cs:6` |
| `Bing.Http.Clients.Parameters.IFileParameter` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Clients/Parameters/IFileParameter.cs:6` |
| `Bing.Http.Extensions.SessionExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.Session.cs:9` |
| `Bing.Net.IpAddressProvider` | class | 提供值生成或查询能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs:15` |
| `Bing.Net.IpValidator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IpValidator.cs:12` |
| `Bing.Net.IPv4.IPv4CidrCalculator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4CidrCalculator.cs:12` |
| `Bing.Net.IPv4.IPv4Converter` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4Converter.cs:12` |
| `Bing.Net.IPv4.IPv4Operator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4Operator.cs:12` |
| `Bing.Net.IPv4.IPv4SubnetInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4SubnetInfo.cs:6` |
| `Bing.Net.IPv4.IPv4Validator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4Validator.cs:13` |
| `Bing.Net.IPv6.IPv6AddressAnalysisResult` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressAnalysisResult.cs:6` |
| `Bing.Net.IPv6.IPv6AddressAnalyzer` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressAnalyzer.cs:11` |
| `Bing.Net.IPv6.IPv6AddressPool` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressPool.cs:6` |
| `Bing.Net.IPv6.IPv6AddressStatistics` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressStatistics.cs:8` |
| `Bing.Net.IPv6.IPv6CidrCalculator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6CidrCalculator.cs:12` |
| `Bing.Net.IPv6.IPv6Converter` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Converter.cs:14` |
| `Bing.Net.IPv6.IPv6Generator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Generator.cs:12` |
| `Bing.Net.IPv6.IPv6Operator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Operator.cs:12` |
| `Bing.Net.IPv6.IPv6SubnetInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6SubnetInfo.cs:6` |
| `Bing.Net.IPv6.IPv6Validator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Validator.cs:14` |
| `Bing.Net.IPv6.IPv6AddressScope` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressScope.cs:6` |
| `Bing.Net.IPv6.IPv6AddressType` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressType.cs:6` |
| `Bing.Net.IPv6.IPv6Format` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Format.cs:6` |
| `Bing.Net.Mac.MacAddressHelper` | class | 提供特定场景的辅助操作 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/Mac/MacAddressHelper.cs:12` |
| `Bing.Net.Mac.MacAddressInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/Mac/MacAddressInfo.cs:6` |
| `Bing.Net.NetworkInformation.InterfaceStatistics` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/InterfaceStatistics.cs:6` |
| `Bing.Net.NetworkInformation.NetworkChange` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkChange.cs:6` |
| `Bing.Net.NetworkInformation.NetworkChangeEventArgs` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkChangeEventArgs.cs:8` |
| `Bing.Net.NetworkInformation.NetworkInterfaceInfo` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkInterfaceInfo.cs:12` |
| `Bing.Net.NetworkInformation.NetworkInterfaceManager` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkInterfaceManager.cs:12` |
| `Bing.Net.NetworkInformation.NetworkQuality` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkQuality.cs:6` |
| `Bing.Net.NetworkInformation.NetworkStatistics` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkStatistics.cs:6` |
| `Bing.Net.NetworkInformation.PingResult` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/PingResult.cs:6` |
| `Bing.Net.NetworkInformation.NetworkChangeType` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkChangeType.cs:6` |
| `Bing.Net.NetworkInformation.NetworkQualityLevel` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkQualityLevel.cs:6` |
| `Bing.Parameters.UrlParameterBuilderExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Http/Bing/Parameters/UrlParameterBuilderExtensions.cs:9` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.Http.HttpRequestExtensions.GetAbsoluteUri()` | 用于获取目标值 | `this HttpRequest request` | `string` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:20` |
| `Bing.Http.HttpRequestExtensions.IsAjaxRequest()` | 用于判断条件是否成立 | `this HttpRequest request` | `bool` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:109` |
| `Bing.Http.HttpRequestExtensions.IsJsonContentType()` | 用于判断条件是否成立 | `this HttpRequest request` | `bool` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:130` |
| `Bing.Http.HttpRequestExtensions.IsMobileBrowser()` | 用于判断条件是否成立 | `this HttpRequest request` | `bool` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:169` |
| `Bing.Http.HttpRequestExtensions.Params()` | 提供通用基础能力 | `this HttpRequest request, string key` | `string` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:92` |
| `Bing.Http.HttpRequestExtensions.UserAgent()` | 提供通用基础能力 | `this HttpRequest request` | `string` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpRequest.cs:183` |
| `Bing.Http.HttpResponseExtensions.SetCache()` | 用于设置目标值或更新实例 | `this HttpResponse response, int maxAge` | `void` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs:46` |
| `Bing.Http.HttpResponseExtensions.SetNoCache()` | 用于设置目标值或更新实例 | `this HttpResponse response` | `void` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs:69` |
| `Bing.Http.HttpResponseExtensions.WriteHtmlAsync()` | 提供通用基础能力 | `this HttpResponse response, string html` | `Task` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs:86` |
| `Bing.Http.HttpResponseExtensions.WriteJsonAsync()` | 提供通用基础能力 | `this HttpResponse response, object obj (+1 重载)` | `Task` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/Extensions.HttpResponse.cs:20` |
| `Bing.Http.HttpResponseMessageExtensions.GetContentType()` | 用于获取目标值 | `this HttpResponseMessage message` | `string` | `[证据] src/Bing.Utils.Http/Bing/Http/Extensions/HttpResponseMessageExtensions.cs:15` |
| `Bing.Parameters.UrlParameterBuilderExtensions.LoadForm()` | 提供通用基础能力 | `this UrlParameterBuilder builder` | `void` | `[证据] src/Bing.Utils.Http/Bing/Parameters/UrlParameterBuilderExtensions.cs:15` |
| `Bing.Parameters.UrlParameterBuilderExtensions.LoadQuery()` | 提供通用基础能力 | `this UrlParameterBuilder builder` | `void` | `[证据] src/Bing.Utils.Http/Bing/Parameters/UrlParameterBuilderExtensions.cs:31` |

### Id
- 公开类型: `21`, 扩展方法族: `1`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.GuidExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/Extensions.Guid.cs:8` |
| `Bing.Helpers.Id` | class | 高频入口 API，用于组合常用功能 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:9` |
| `Bing.IdUtils.DefaultTraceIdMaker` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/TraceIdAccessor.DefaultTraceIdMaker.cs:6` |
| `Bing.IdUtils.GuidJudge` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidJudge.cs:8` |
| `Bing.IdUtils.GuidProvider` | class | 提供值生成或查询能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidProvider.Comb.cs:9` |
| `Bing.IdUtils.ModelIdAccessor` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/ModelIdAccessor.cs:15` |
| `Bing.IdUtils.RandomIdGenerator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/RandomIdGenerator.cs:6` |
| `Bing.IdUtils.RandomNonceStrGenerator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/RandomNonceStrGenerator.cs:6` |
| `Bing.IdUtils.SnowflakeGenerator` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:8` |
| `Bing.IdUtils.TimestampId` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/TimestampId.cs:9` |
| `Bing.IdUtils.TraceIdAccessor` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/TraceIdAccessor.cs:6` |
| `Bing.IdUtils.CombStyle` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/CombStyle.cs:6` |
| `Bing.IdUtils.GuidBytesStyle` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidBytesStyle.cs:6` |
| `Bing.IdUtils.GuidStyle` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidStyle.cs:6` |
| `Bing.IdUtils.GuidVersion` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidVersion.cs:6` |
| `Bing.IdUtils.NoRepeatMode` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/NoRepeatMode.cs:6` |
| `Bing.IdUtils.ISnowflakeId` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/ISnowflakeId.cs:6` |
| `Bing.IdUtils.ITraceIdMaker` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/TraceIdAccessor.ITraceIdMaker.cs:6` |
| `Bing.IdUtils.ObjectId` | struct | 表示轻量值对象 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18` |
| `Bing.IdUtils.GuidImplements.UnixTimeStampStyleProvider` | class | 提供值生成或查询能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidImplements/UnixTimeStampStyleProvider.cs:8` |
| `Bing.IdUtils.GuidImplements.Internals.GuidNamespaces` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidImplements/Internals/GuidNamespaces.cs:6` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.GuidExtensions.IsNullOrEmpty()` | 用于判断条件是否成立 | `this Guid? guid (+1 重载)` | `bool` | `[证据] src/Bing.Utils.IdUtils/Bing/Extensions.Guid.cs:14` |

### Reflection
- 公开类型: `6`, 扩展方法族: `21`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.Dynamic.DynamicBase` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Reflection/Bing/Dynamic/DynamicBase.cs:8` |
| `Bing.Reflection.AssemblyVisit` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs:10` |
| `Bing.Reflection.TypeMetaVisitExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Attributes.cs:16` |
| `Bing.Reflection.TypeVisit` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Attributes.cs:9` |
| `Bing.Reflection.TypeVisitExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs:205` |
| `Bing.Reflection.PropertyAccessOptions` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs:12` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.Reflection.TypeMetaVisitExtensions.GetBaseMethod()` | 用于获取目标值 | `this MethodInfo method` | `MethodInfo` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:188` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetDescription()` | 用于获取目标值 | `this MemberInfo member, ReflectionOptions options = ReflectionOptions.Default` | `string` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Description.cs:33` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetDescriptionOr()` | 用于获取目标值 | `this MemberInfo member, string defaultVal, ReflectionOptions options = ReflectionOptions.Default` | `string` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Description.cs:52` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetFullName()` | 用于获取目标值 | `this MethodInfo method` | `string` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:169` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetFullyQualifiedName()` | 用于获取目标值 | `this MethodInfo method (+1 重载)` | `string` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:175` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetMatchingConstructor()` | 用于获取目标值 | `this Type type, Type[] constructorParameterTypes` | `ConstructorInfo` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.Constructor.cs:79` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetMethodBySignature()` | 用于获取目标值 | `this Type type, MethodInfo method` | `MethodInfo` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:182` |
| `Bing.Reflection.TypeMetaVisitExtensions.GetParameterlessConstructor()` | 用于获取目标值 | `this Type type` | `ConstructorInfo` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.Constructor.cs:72` |
| `Bing.Reflection.TypeMetaVisitExtensions.HasParameterlessConstructor()` | 用于检查是否包含指定特征 | `this Type type` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Types.Constructor.cs:66` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsAbstract()` | 用于判断条件是否成立 | `this PropertyInfo property` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs:275` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsAsyncMethod()` | 用于判断条件是否成立 | `this MethodInfo method` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:153` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsAttributeDefined()` | 用于判断条件是否成立 | `this MemberInfo member, Type attributeType, ReflectionOptions options = ReflectionOptions.Default` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Attributes.cs:44` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsAttributeNotDefined()` | 用于判断条件是否成立 | `this MemberInfo member, Type attributeType, ReflectionOptions options = ReflectionOptions.Default` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Attributes.cs:53` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsDescriptionDefined()` | 用于判断条件是否成立 | `this MemberInfo member, ReflectionOptions options = ReflectionOptions.Default` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Description.cs:25` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsNumeric()` | 用于判断条件是否成立 | `this MemberInfo member, TypeIsOptions options = TypeIsOptions.Default (+1 重载)` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:45` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsOverridden()` | 用于判断条件是否成立 | `this MethodInfo method` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:163` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsStructType()` | 用于判断条件是否成立 | `this MemberInfo member, TypeIsOptions isOptions = TypeIsOptions.Default (+1 重载)` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:79` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsTupleType()` | 用于判断条件是否成立 | `this MemberInfo member, TypeOfOptions ofOptions = TypeOfOptions.Owner, TypeIsOptions isOptions = TypeIsOptions.Default (+1 重载)` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:62` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsVirtual()` | 用于判断条件是否成立 | `this PropertyInfo property` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Properties.cs:265` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsVisible()` | 用于判断条件是否成立 | `this MethodInfo method` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:200` |
| `Bing.Reflection.TypeMetaVisitExtensions.IsVisibleAndVirtual()` | 用于判断条件是否成立 | `this MethodInfo method (+1 重载)` | `bool` | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Methods.cs:194` |

### Text
- 公开类型: `22`, 扩展方法族: `7`

| API | 类别 | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|---|
| `Bing.Collections.StringCollectionExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs:9` |
| `Bing.Text.CaseFormatter` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs:10` |
| `Bing.Text.StringLines` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs:7` |
| `Bing.Text.StringLinesExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs:36` |
| `Bing.Text.StringProwessExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/StringProwess.cs:8` |
| `Bing.Text.StringTruncateExtensions` | class | 提供扩展方法入口 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs:35` |
| `Bing.Text.StringTruncators` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs:9` |
| `Bing.Text.Style` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs:217` |
| `Bing.Text.Joiners.CommonJoinUtils` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/CommonJoinUtils.cs:6` |
| `Bing.Text.Joiners.Joiner` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs:8` |
| `Bing.Text.Joiners.SkipNullType` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/SkipNullType.cs:6` |
| `Bing.Text.Joiners.IJoiner` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/IJoiner.cs:6` |
| `Bing.Text.Joiners.IMapJoiner` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/IMapJoiner.cs:6` |
| `Bing.Text.Joiners.ITupleJoiner` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/ITupleJoiner.cs:6` |
| `Bing.Text.Similarity.StringSimilarity` | class | 提供通用基础能力 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs:6` |
| `Bing.Text.Similarity.StringSimilarityTypes` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarityTypes.cs:6` |
| `Bing.Text.Splitters.Splitter` | class | 高频入口 API，用于组合常用功能 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8` |
| `Bing.Text.Splitters.IFixedLengthSplitter` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/IFixedLengthSplitter.cs:6` |
| `Bing.Text.Splitters.IMapSplitter` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/IMapSplitter.cs:6` |
| `Bing.Text.Splitters.ISplitter` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/ISplitter.cs:6` |
| `Bing.Text.Truncation.StringTruncateFrom` | enum | 表示可选值集合或模式 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncateFrom.cs:6` |
| `Bing.Text.Truncation.IStringTruncator` | interface | 声明可扩展契约 | - | - | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/IStringTruncator.cs:6` |

| 扩展 API | 用途 | 参数要点 | 返回值要点 | 证据 |
|---|---|---|---|---|
| `Bing.Collections.StringCollectionExtensions.JoinToString()` | 提供通用基础能力 | `this IEnumerable<string> list (+5 重载)` | `string` | `[证据] src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs:15` |
| `Bing.Text.StringLinesExtensions.CountByLines()` | 提供通用基础能力 | `this string text` | `int` | `[证据] src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs:44` |
| `Bing.Text.StringLinesExtensions.SplitByLines()` | 提供通用基础能力 | `this string text` | `IEnumerable<string>` | `[证据] src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs:62` |
| `Bing.Text.StringLinesExtensions.SplitInLinesWithoutEmpty()` | 提供通用基础能力 | `this string text` | `string[]` | `[证据] src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs:80` |
| `Bing.Text.StringLinesExtensions.TruncateByLines()` | 提供通用基础能力 | `this string text, int maxLines, string placeholder = "...", bool extraSpace = false (+1 重载)` | `string` | `[证据] src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Truncation.cs:49` |
| `Bing.Text.StringProwessExtensions.SplitByIndex()` | 提供通用基础能力 | `this string that, int index` | `Tuple<string, string>` | `[证据] src/Bing.Utils.Text/Bing/Text/StringProwess.cs:31` |
| `Bing.Text.StringTruncateExtensions.Truncate()` | 提供通用基础能力 | `this string text, int maxLength, string truncationString = "...", string shortTruncationString = ".", StringTruncateFrom from = StringTruncateFrom.Right, bool extraSpace = false (+2 重载)` | `string` | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs:47` |

## 高频 API Top 30

| Rank | API | 分类 | 命中信号 | 证据 |
|---:|---|---|---|---|
| 1 | `Bing.Helpers.Id` | Id | type-use dot=319, new=0, refs=483 | `[证据] src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:9` |
| 2 | `Bing.Net.IPv6.IPv6Converter` | Http | type-use dot=151, new=0, refs=151 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Converter.cs:14` |
| 3 | `Bing.Net.NetworkInformation.NetworkInterfaceManager` | Http | type-use dot=88, new=0, refs=89 | `[证据] src/Bing.Utils.Http/Bing/Net/NetworkInformation/NetworkInterfaceManager.cs:12` |
| 4 | `Bing.Helpers.Web` | Http | type-use dot=83, new=0, refs=87 | `[证据] src/Bing.Utils.Http/Bing/Helpers/Web.cs:17` |
| 5 | `Bing.Net.IPv6.IPv6Validator` | Http | type-use dot=69, new=0, refs=70 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Validator.cs:14` |
| 6 | `Bing.Net.IpAddressProvider` | Http | type-use dot=69, new=0, refs=70 | `[证据] src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs:15` |
| 7 | `Bing.Helpers.UserAgentHelper` | Http | type-use dot=66, new=0, refs=66 | `[证据] src/Bing.Utils.Http/Bing/Helpers/UserAgentHelper.cs:8` |
| 8 | `Bing.Collections.Arrays` | Collections | type-use dot=57, new=0, refs=58 | `[证据] src/Bing.Utils.Collections/Bing/Collections/Arrays.Copy.cs:22` |
| 9 | `Bing.IdUtils.ObjectId` | Id | type-use dot=31, new=29, refs=66 | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18` |
| 10 | `Bing.IdUtils.GuidProvider` | Id | type-use dot=52, new=0, refs=52 | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidProvider.Comb.cs:9` |
| 11 | `Bing.Reflection.TypeMetaVisitExtensions.IsAttributeDefined()` | Reflection | ext-calls=70, overloads=1 | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.Attributes.cs:44` |
| 12 | `Bing.Net.IPv4.IPv4CidrCalculator` | Http | type-use dot=49, new=0, refs=49 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4CidrCalculator.cs:12` |
| 13 | `Bing.Net.IpValidator` | Http | type-use dot=48, new=0, refs=50 | `[证据] src/Bing.Utils.Http/Bing/Net/IpValidator.cs:12` |
| 14 | `Bing.Net.IPv4.IPv4Validator` | Http | type-use dot=47, new=0, refs=47 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4Validator.cs:13` |
| 15 | `Bing.Collections.ArraysShortcutExtensions.Copy()` | Collections | ext-calls=59, overloads=4 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.Copy.cs:15` |
| 16 | `Bing.Net.IPv6.IPv6Operator` | Http | type-use dot=44, new=0, refs=44 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Operator.cs:12` |
| 17 | `Bing.Net.IPv6.IPv6CidrCalculator` | Http | type-use dot=43, new=0, refs=43 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6CidrCalculator.cs:12` |
| 18 | `Bing.Text.Splitters.Splitter` | Text | type-use dot=38, new=0, refs=38 | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8` |
| 19 | `Bing.Date.DateUtils.DateTimeCalc` | DateTime | type-use dot=36, new=0, refs=36 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateUtils/DateTimeCalc.cs:68` |
| 20 | `Bing.IdUtils.GuidStyle` | Id | type-use dot=34, new=0, refs=34 | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidStyle.cs:6` |
| 21 | `Bing.Net.IPv6.IPv6Generator` | Http | type-use dot=34, new=0, refs=34 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Generator.cs:12` |
| 22 | `Bing.Date.DateTimeExtensions.ToBytes()` | DateTime | ext-calls=46, overloads=1 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1160` |
| 23 | `Bing.Extensions.ImageExtensions.ToBytes()` | Drawing | ext-calls=46, overloads=1 | `[证据] src/Bing.Utils.Drawing/Bing/Extensions/ImageExtensions.cs:42` |
| 24 | `Bing.IdUtils.NoRepeatMode` | Id | type-use dot=32, new=0, refs=32 | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/NoRepeatMode.cs:6` |
| 25 | `Bing.Net.IPv4.IPv4Converter` | Http | type-use dot=32, new=0, refs=32 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4Converter.cs:12` |
| 26 | `Bing.Net.IPv4.IPv4Operator` | Http | type-use dot=27, new=0, refs=27 | `[证据] src/Bing.Utils.Http/Bing/Net/IPv4/IPv4Operator.cs:12` |
| 27 | `Bing.Collections.ArraysShortcutExtensions.Clear()` | Collections | ext-calls=33, overloads=2 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:60` |
| 28 | `Bing.Net.Mac.MacAddressHelper` | Http | type-use dot=24, new=0, refs=25 | `[证据] src/Bing.Utils.Http/Bing/Net/Mac/MacAddressHelper.cs:12` |
| 29 | `Bing.Reflection.TypeMetaVisitExtensions.IsTupleType()` | Reflection | ext-calls=29, overloads=2 | `[证据] src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:62` |
| 30 | `Bing.GuidExtensions.IsNullOrEmpty()` | Id | ext-calls=26, overloads=2 | `[证据] src/Bing.Utils.IdUtils/Bing/Extensions.Guid.cs:14` |

## Breaking-change 敏感 API（签名变更风险高）

| API | 风险原因 | 证据 |
|---|---|---|
| `Bing.Collections.ArraysShortcutExtensions.Sort()` | 重载数=8; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:164` |
| `Bing.Collections.StringCollectionExtensions.JoinToString()` | 重载数=6; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs:15` |
| `Bing.Collections.ArraysShortcutExtensions.Copy()` | 重载数=4; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.Copy.cs:15` |
| `Bing.Collections.ArraysShortcutExtensions.BinarySearch()` | 重载数=4; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:17` |
| `Bing.Date.DateTimeExtensions.SetTime()` | 重载数=4; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:69` |
| `Bing.Date.DateTimeOffsetExtensions.SetTime()` | 重载数=4; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:69` |
| `Bing.Date.DateTimeExtensions.GetWeekOfYear()` | 重载数=4; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Navigation.cs:428` |
| `Bing.Collections.ArraysShortcutExtensions.IndexOf()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:87` |
| `Bing.Collections.ArraysShortcutExtensions.LastIndexOf()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:117` |
| `Bing.Date.DateTimeExtensions.At()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:14` |
| `Bing.Date.DateTimeExtensions.SetDate()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.Update.cs:131` |
| `Bing.Date.DateTimeOffsetExtensions.At()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:14` |
| `Bing.Date.DateTimeOffsetExtensions.SetDate()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetExtensions.Update.cs:139` |
| `Bing.Date.DateTimeSpanExtensions.Before()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeSpanExtensions.cs:23` |
| `Bing.Text.StringTruncateExtensions.Truncate()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs:47` |
| `NodaTime.NodaPeriodExtensions.AsPeriod()` | 重载数=3; 签名变更将直接影响编译兼容 | `[证据] src/Bing.Utils.DateTime/NodaTime/NodaPeriodExtensions.cs:14` |
| `Bing.Http.Clients.Parameters.IFileParameter` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Http/Bing/Http/Clients/Parameters/IFileParameter.cs:6` |
| `Bing.IdUtils.ISnowflakeId` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/ISnowflakeId.cs:6` |
| `Bing.IdUtils.ITraceIdMaker` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/TraceIdAccessor.ITraceIdMaker.cs:6` |
| `Bing.Text.Joiners.IJoiner` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/IJoiner.cs:6` |
| `Bing.Text.Joiners.IMapJoiner` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/IMapJoiner.cs:6` |
| `Bing.Text.Joiners.ITupleJoiner` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Joiners/ITupleJoiner.cs:6` |
| `Bing.Text.Splitters.IFixedLengthSplitter` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/IFixedLengthSplitter.cs:6` |
| `Bing.Text.Splitters.IMapSplitter` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/IMapSplitter.cs:6` |
| `Bing.Text.Splitters.ISplitter` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Splitters/ISplitter.cs:6` |
| `Bing.Text.Truncation.IStringTruncator` | 公共契约接口; 成员变更会影响实现方与调用方 | `[证据] src/Bing.Utils.Text/Bing/Text/Truncation/IStringTruncator.cs:6` |
| `Bing.IdUtils.GuidStyle` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=34) | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/GuidStyle.cs:6` |
| `Bing.IdUtils.NoRepeatMode` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=32) | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/NoRepeatMode.cs:6` |
| `Bing.Net.IPv6.IPv6AddressType` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=19) | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6AddressType.cs:6` |
| `Bing.IdUtils.CombStyle` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=18) | `[证据] src/Bing.Utils.IdUtils/Bing/IdUtils/CombStyle.cs:6` |
| `Bing.Date.Chinese.ChineseSolarTerms` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=15) | `[证据] src/Bing.Utils.DateTime/Bing/Date/Chinese/ChineseSolarTerms.cs:9` |
| `Bing.Date.DateTimeOffsetOptions` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=12) | `[证据] src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetOptions.cs:6` |
| `Bing.Net.IPv6.IPv6Format` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=5) | `[证据] src/Bing.Utils.Http/Bing/Net/IPv6/IPv6Format.cs:6` |
| `Bing.Text.Similarity.StringSimilarityTypes` | 高频枚举; 枚举值变更可能导致序列化或分支不兼容(refs=5) | `[证据] src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarityTypes.cs:6` |
