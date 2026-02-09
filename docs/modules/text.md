# 模块：Bing.Utils.Text

## 1. 模块定位
- 目标：提供一组“可组合”的文本处理能力（分割/拼接、大小写风格转换、截断、按行处理、相似度评估），并以接口 + Fluent 配置的方式暴露 API。
    - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Bing.Text.Splitters.Splitter
    - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs | Bing.Text.Joiners.Joiner
    - 证据：src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs | Bing.Text.CaseFormatter
- 非目标：不提供自然语言分词/断句、复杂的相似度/编辑距离算法集合、或文本渲染/排版引擎。
    - 证据：src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | Bing.Text.Similarity.StringSimilarity（仅实现一种递归近似相似度）
- 适用场景：
    - 将 delimited string 切分为列表/字典（含 Trim、忽略空项、限制条数等选项）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/ISplitter.cs | Bing.Text.Splitters.ISplitter
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs | Bing.Text.Splitters.Splitter.SplitToDictionary
    - 将 key/value 或 tuple 列表拼接为字符串（可配置跳过 null、替换 null、键值分隔符）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Map.cs | Bing.Text.Joiners.Joiner
        - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Tuple.cs | Bing.Text.Joiners.Joiner
    - 生成常见命名风格（camelCase、PascalCase、lower-hyphen、lower_underscore 等）。
        - 证据：src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs | Bing.Text.CaseFormatter.To
    - 文本按行处理（分行、计数、按行截断）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | Bing.Text.StringLines.SplitByLines
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs | Bing.Text.StringLines.CountByLines
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Truncation.cs | Bing.Text.StringLines.TruncateByLines
- 不适用场景：
    - 需要严格跨平台换行符解析（此模块的按行循环目前对换行长度做了固定假设，见“边界与异常处理”）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | Bing.Text.StringLines.SplitByLines
    - 需要高精度/可解释的字符串相似度（当前实现采用最大差异数阈值 + 递归尝试）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | Bing.Text.Similarity.StringSimilarity.EvaluateSimilarity

## 2. 目录结构
    src/Bing.Utils.Text/
        tests/Bing.Utils.Text.Tests/ （待确认：仓库中未找到该目录）
        - 证据：src/Bing.Utils.Text/Bing/Text/ （Joiners/Splitters/Truncation/Lines/Similarity 等）
        - 证据：tests/BingUtilsUT/SplitterUT/ （存在 Splitter 相关用例，但不在 Text.Tests 项目中）
        - 证据：tests/Bing.Utils.Tests/Bing/Text/Similarity/ （存在 StringSimilarity 用例，但不在 Text.Tests 项目中）

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `Splitter.On(char, params char[])` / `Splitter.On(string, params string[])` | 创建“分隔符模式”的分割器（可进一步配置 Trim/忽略空项/限制等） | `on`/`on2` 分隔符 | `ISplitter` |  | 
| `Splitter.On(Regex)` / `Splitter.OnPattern(string)` | 创建“正则模式”的分割器 | `pattern`/`separatorPattern` | `ISplitter` |  | 
| `Splitter.FixedLength(int)` | 创建“固定长度模式”的分割器 | `length` 固定段长 | `IFixedLengthSplitter` | `ArgumentOutOfRangeException`（length < 0） |
| `ISplitter.OmitEmptyStrings()` | 忽略空字符串分段 |  | `ISplitter` |  |
| `ISplitter.TrimResults()` / `ISplitter.TrimResults(Func<string,string>)` | 对每个分段进行裁剪（默认 Trim，可自定义） | `trimFunc` | `ISplitter` |  |
| `ISplitter.Limit(int)` | 限制分割结果最大数量（<=0 时内部会重置为“不限制”） | `limit` | `ISplitter` |  |
| `ISplitter.Split(...)` / `SplitToList(...)` / `SplitToArray(...)` | 执行分割（空/空白输入返回空序列） | `originalString` | `IEnumerable<string>` / `List<string>` / `string[]` |  |
| `ISplitter.WithKeyValueSeparator(char/string)` + `IMapSplitter.SplitToDictionary(...)` | 将分段按键值分隔符再拆成 KeyValuePair/Dictionary（value 缺失时置空字符串） | `separator`/`originalString` | `IEnumerable<KeyValuePair<string,string>>` / `Dictionary<string,string>` |  |
| `Joiner.On(char/string)` | 创建连接器（可进一步配置 SkipNulls/UseForNull 等） | `on` 分隔符 | `IJoiner` |  |
| `IJoiner.SkipNulls()` / `UseForNull(...)` / `Join(...)` / `AppendTo(...)` | 连接字符串或对象序列，并可选择跳过或替换 null（通过 Options 与 replacer 实现） | 见接口 | `string`/`StringBuilder` |  |
| `IJoiner.WithKeyValueSeparator(...)` + `IMapJoiner` / `ITupleJoiner` | 进入“键值对/元组”连接模式，支持 FromTuple、SkipNulls(type)、UseForNull(...) 等 | 见接口 | `string`/`StringBuilder` |  |
| `StringTruncators.*` + `StringTruncateExtensions.Truncate(...)` | 字符串截断：按长度/字符数/单词数/行数（由不同 `IStringTruncator` 实现） | `text`/`maxLength`/`truncator`/`from` 等 | `string` |  |
| `StringLines.*`（Split/Count/Truncate）+ 扩展方法 | 按行分割、统计行数、按行截断（提供静态方法与扩展方法） | `text`/`maxLines` 等 | `IEnumerable<string>`/`int`/`string` |  |
| `StringSimilarity.EvaluateSimilarity(...)` | 评估字符串相似度（返回 0~1 或相似度类型枚举） | `text`/`comparisonText`/`similarityMinimal` | `double` / `StringSimilarityTypes` |  |
| `CaseFormatter.Instance/Humanizer/...` + `CaseFormatter.To(Style, string)` | 大小写/命名风格转换（基于 Splitter + Joiner 组合实现） | `style`/`sequence` | `string` | `ArgumentNullException`（构造传入 splitter 为 null 时） |

> API 证据：
> - src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Bing.Text.Splitters.Splitter.On/OnPattern/FixedLength
> - src/Bing.Utils.Text/Bing/Text/Splitters/ISplitter.cs | Bing.Text.Splitters.ISplitter
> - src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs | Bing.Text.Splitters.Splitter.SplitToDictionary
> - src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs | Bing.Text.Joiners.Joiner.On/Join/AppendTo
> - src/Bing.Utils.Text/Bing/Text/Joiners/IJoiner.cs | Bing.Text.Joiners.IJoiner
> - src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Map.cs | Bing.Text.Joiners.Joiner（IMapJoiner 模式）
> - src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Tuple.cs | Bing.Text.Joiners.Joiner（ITupleJoiner 模式）
> - src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs | Bing.Text.StringTruncators / Bing.Text.StringTruncateExtensions.Truncate
> - src/Bing.Utils.Text/Bing/Text/Lines/StringLines.*.cs | Bing.Text.StringLines / Bing.Text.StringLinesExtensions
> - src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | Bing.Text.Similarity.StringSimilarity
> - src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs | Bing.Text.CaseFormatter

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Text.Splitters.Splitter` / `ISplitter` / `IMapSplitter` / `IFixedLengthSplitter` | 分割器：支持分隔符/正则/固定长度模式，提供 Trim/忽略空项/限制/键值对等配置 | 否（实例持有可变 Options，链式配置会修改状态） | 空/空白输入返回空序列；Map 模式 value 缺失返回空字符串 |
| `Bing.Text.Joiners.Joiner` / `IJoiner` / `IMapJoiner` / `ITupleJoiner` | 连接器：连接字符串/对象；Map/Tuple 模式支持键值分隔、跳过/替换 null | 否（实例持有可变 Options，链式配置会修改状态） | 内部依赖 `Bing.Collections.StringCollectionExtensions.JoinToString` 与 `CommonJoinUtils` |
| `Bing.Collections.StringCollectionExtensions` | 提供集合 JoinToString/JoinOnePerLine 等扩展，供 Joiner 复用 | 是（纯静态方法、无共享状态） | 位于 Text 包中但命名空间为 `Bing.Collections` |
| `Bing.Text.CaseFormatter` | 命名风格转换（通过 Splitter 分词、再用 Joiner 组合输出） | 基本可重入（每次 To 构造局部 list/joiner；但若复用同一 Splitter 实例且并发修改配置则不安全） | `Humanizer` 模式使用 `Regex("[-_ ]+")` 分割 |
| `Bing.Text.StringTruncators` / `Bing.Text.StringTruncateExtensions` | 截断入口：暴露不同 `IStringTruncator` 实例与扩展方法 `Truncate` | 是（纯静态入口；具体 truncator 实现为单例） | 默认 truncationString 为 `"..."`，short 为 `"."` |
| `Bing.Text.Truncation.IStringTruncator` | 截断策略接口 | 取决于实现 | `FixedLengthTruncator` 为 internal 实现之一 |
| `Bing.Text.StringLines` / `Bing.Text.StringLinesExtensions` | 按行处理：分行、计数、按行截断 | 是（纯静态方法） | 当前实现对 `Environment.NewLine` 长度做了固定偏移（见待办） |
| `Bing.Text.Similarity.StringSimilarity` / `StringSimilarityTypes` | 相似度评估（返回 double 或枚举） | 是（纯静态方法） | 采用差异阈值（MAX=2）+ 递归分支尝试 |
| `Bing.Text.StringProwessExtensions` | 少量额外字符串扩展：JoinStringFor、SplitByIndex、SplitTyped<T> | 是（纯静态扩展） | `SplitTyped<T>` 使用 `Convert.ChangeType`，可能抛转换异常 |

> 核心类型证据：
> - src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Bing.Text.Splitters.Splitter
> - src/Bing.Utils.Text/Bing/Text/Splitters/ISplitter.cs | Bing.Text.Splitters.ISplitter
> - src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs | Bing.Text.Splitters.Splitter（IMapSplitter 模式）
> - src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs | Bing.Text.Joiners.Joiner
> - src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Map.cs | Bing.Text.Joiners.Joiner（IMapJoiner 模式）
> - src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Tuple.cs | Bing.Text.Joiners.Joiner（ITupleJoiner 模式）
> - src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs | Bing.Collections.StringCollectionExtensions
> - src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs | Bing.Text.StringTruncators / Bing.Text.StringTruncateExtensions
> - src/Bing.Utils.Text/Bing/Text/Truncation/IStringTruncator.cs | Bing.Text.Truncation.IStringTruncator
> - src/Bing.Utils.Text/Bing/Text/Lines/StringLines.*.cs | Bing.Text.StringLines / Bing.Text.StringLinesExtensions
> - src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | Bing.Text.Similarity.StringSimilarity
> - src/Bing.Utils.Text/Bing/Text/StringProwess.cs | Bing.Text.StringProwessExtensions

## 5. 依赖关系
- 直接依赖：
    - `Bing.Utils`（项目引用）
        - 证据：src/Bing.Utils.Text/Bing.Utils.Text.csproj | ProjectReference=..\Bing.Utils\Bing.Utils.csproj
    - BCL：`System.Text.RegularExpressions`、`System.Text.StringBuilder`、`System.Linq` 等
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | using System.Text.RegularExpressions
        - 证据：src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs | StringBuilder
- 可选依赖：无（未发现外部第三方包引用）。
    - 证据：src/Bing.Utils.Text/Bing.Utils.Text.csproj | 未包含 PackageReference
- 禁止依赖：待确认（仓库层面未见该模块显式“禁止依赖”约束声明）。

## 6. 关键实现说明
### 6.1 算法/流程
- Splitter（分隔符/正则/固定长度三种模式）：
    - 分割：空/空白输入直接返回空集合；否则根据模式调用内部 split（string.Split / Regex.Split / fixed-length loop），然后按 Options 执行 OptionalRange（Trim/忽略空项）并可 Limit。
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Splitter.InternalSplitToEnumerable
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.FixedLength.cs | Splitter.InternalSplitToEnumerable2
    - Map：先按主分隔策略得到中间片段，再用 MapSeparator 拆 key/value；当 value 缺失时置空字符串。
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs | SplitterUtils.SplitMap
- Joiner（普通/Map/Tuple 三种模式）：
    - 普通 Join：基于 `Bing.Collections.StringCollectionExtensions.JoinToString` 完成连接，支持 predicate + replacer（SkipNulls/UseForNull）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs | IJoiner.Join
        - 证据：src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs | StringCollectionExtensions.JoinToString
    - Map/Tuple：将输入序列映射为 `key{MapSeparator}value` 的中间字符串列表，再按 `_on` 连接；并支持“跳过/修复 null 键值”。
        - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Map.cs | Joiner.JoinToKeyValuePairString
        - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Tuple.cs | Joiner.JoinToTupleString
- Truncation（截断）：
    - 入口扩展 `string.Truncate(...)` 会兜底 truncator 为 `StringTruncators.ByLength`，再调用 `IStringTruncator.Truncate(...)`。
        - 证据：src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs | StringTruncateExtensions.Truncate
    - `FixedLengthTruncator` 会根据 truncationString/shortTruncationString 与 maxLength 关系选择输出，并可在占位符前后插入空格。
        - 证据：src/Bing.Utils.Text/Bing/Text/Truncation/FixedLengthTruncator.cs | FixedLengthTruncator.Truncate
- Lines（按行）：
    - 通过 `IndexOf(Environment.NewLine)` 循环扫描产生每一行（yield return）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | StringLines.SplitByLines
    - 行截断直接复用按行截断器：`StringTruncators.ByNumberOfLines`。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Truncation.cs | StringLines.TruncateByLines
- Similarity（相似度）：
    - 定量相似度：最多容忍 2 个差异（MAX_DIF_TOLERADAS=2）；超过则递归尝试删除一个字符对齐并取分支最大值的平均化结果。
        - 证据：src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | StringSimilarity.EvaluateSimilarity
    - 定性相似度：仅判断 Same / MayorLong / MinorLong / Any。
        - 证据：src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | StringSimilarity.EvaluateSimilarity(string,string)

### 6.2 边界与异常处理
- 参数与输入：
    - `Splitter.FixedLength(length < 0)` 会抛 `ArgumentOutOfRangeException`。
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Splitter.FixedLength
    - `CaseFormatter` 通过 `CaseFormatter(ISplitter splitter)` 构造时，splitter 为 null 会抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs | CaseFormatter(ISplitter)
    - `StringProwessExtensions.SplitTyped<T>` 使用 `Convert.ChangeType`：遇到格式不匹配/溢出/类型不支持可能抛 `FormatException` / `InvalidCastException` / `OverflowException`（未在此方法中捕获）。
        - 证据：src/Bing.Utils.Text/Bing/Text/StringProwess.cs | StringProwessExtensions.SplitTyped
    - `StringLines.SplitByLines/CountByLines` 未对 `text` 做 null guard，可能触发 `NullReferenceException`。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | StringLines.SplitByLines
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs | StringLines.CountByLines
- 跨平台换行符风险（重要）：
    - `StringLines.SplitByLines/CountByLines` 在命中 `Environment.NewLine` 后使用 `index = newIndex + 2;` 固定偏移；当运行环境换行符长度不为 2（如 Linux 的 `\n`）时，可能出现跳过字符/计数错误。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | index = newIndex + 2
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs | index = newIndex + 2
- 依赖外部（同仓库）文本基础能力：
    - `StringSimilarity` 依赖 `Strings.RemoveWhiteSpace` 与 `EqualsIgnoreCase`；若输入为 null，后续调用可能抛异常（此处未做 null guard）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | StringSimilarity.EvaluateSimilarity
        - 证据：src/Bing.Utils/Bing/Text/Strings/Strings.cs | Strings.RemoveWhiteSpace

## 7. 性能与复杂度
- 时间复杂度（基于实现推断，未做 benchmark）：
    - Splitter（分隔符/固定长度）：通常 $O(n)$；Regex 模式受正则复杂度影响。
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Splitter.InternalSplitToEnumerable
    - Joiner：通常 $O(n)$（遍历 + StringBuilder 追加）。
        - 证据：src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs | StringCollectionExtensions.JoinToString
    - StringSimilarity：包含递归分支，最坏情况可能显著高于 $O(n)$（上界与输入差异位置相关；MAX_DIF_TOLERADAS=2 限制了递归深度）。
        - 证据：src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | MAX_DIF_TOLERADAS
- 空间复杂度：
    - Joiner/Splitter 通常 $O(n)$（会产生中间 List/数组/字符串）。
- 大数据量表现：待补 benchmark。
- Benchmark 链接：待补（benchmarks/Bing.Utils.Benchmark 目前未见 Text 模块对应基准用例，待确认）。

## 8. 测试策略
- 单测覆盖点（以仓库现有事实为准）：
    - Splitter：普通分割、忽略空项、Limit、TrimResults、Pattern 分割、Map 分割、FixedLength 分割。
        - 证据：tests/BingUtilsUT/SplitterUT/SplitterTest.cs | Splitter.On(...).Split/SplitToList
        - 证据：tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs | WithKeyValueSeparator(...).Split/SplitToDictionary
        - 证据：tests/BingUtilsUT/SplitterUT/FixedLengthSplitterTest.cs | Splitter.FixedLength(...)
    - StringSimilarity：Same/Any 等类型判定与阈值相似度。
        - 证据：tests/Bing.Utils.Tests/Bing/Text/Similarity/StringSimilarityTest.cs | StringSimilarity.EvaluateSimilarity
- 边界用例（建议补充，当前测试未覆盖或覆盖不足，待确认）：
    - `StringLines` 的跨平台换行符（`\r\n` vs `\n`）与空/末尾换行处理。
        - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | StringLines.SplitByLines
    - `SplitTyped<T>` 对非法输入、溢出、区域性格式（IFormatProvider）等情况。
        - 证据：src/Bing.Utils.Text/Bing/Text/StringProwess.cs | StringProwessExtensions.SplitTyped
- 回归用例：
    - 固定长度 Splitter 在 Limit/TrimResults 组合下的行为。
        - 证据：src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.FixedLength.cs | IFixedLengthSplitter.Limit/TrimResults

> 说明（待确认）：尽管模块骨架写明 tests/Bing.Utils.Text.Tests，但仓库中未发现该目录；当前相关用例分布在 tests/BingUtilsUT 与 tests/Bing.Utils.Tests。

## 9. 版本与兼容性
- 当前版本：1.5.0（全仓库版本号）。
    - 证据：version.props | VersionMajor/Minor/Patch
- 破坏性变更：待确认（未在本模块文档中发现变更记录；建议结合 docs/ReleaseNotes.md 或 git history 验证）。
- 升级建议：
    - 若此前使用 `Bing.Text` 下的同名/相近能力（如 `Strings.*`、`string` 扩展）来自 `Bing.Utils` 核心包，需要确认是否引入 `Bing.Utils.Text` 才能获得 Joiner/Splitter/Truncation/Lines/Similarity 等新增类型。
        - 证据：src/Bing.Utils.Text/Bing.Utils.Text.csproj | PackageId=Bing.Utils.Text
        - 证据：src/Bing.Utils/Bing/Text/Strings/Strings.cs | Bing.Text.Strings（核心包中也存在 Bing.Text 命名空间）

## 10. 使用示例
```csharp
using System;
using System.Text.RegularExpressions;
using Bing.Text;
using Bing.Text.Similarity;
using Bing.Text.Splitters;
using Bing.Text.Joiners;

// 1) Splitter：分割 + Trim + 忽略空项
var parts = Splitter.On(',')
        .TrimResults()
        .OmitEmptyStrings()
        .SplitToList(" a, b,, c ");

// 2) Map Splitter：拆 querystring
var dict = Splitter.On('&')
        .WithKeyValueSeparator('=')
        .SplitToDictionary("a=1&b=2&c=");

// 3) CaseFormatter：命名风格转换
var upperCamel = CaseFormatter.Instance.To(CaseFormatter.Style.UpperCamel, "hello_world"); // HelloWorld
var lowerHyphen = CaseFormatter.Instance.To(CaseFormatter.Style.LowerHyphen, "HelloWorld"); // helloworld（待确认：取决于输入是否包含分隔符）

// 4) Truncate：按长度截断
var truncated = "abcdef".Truncate(4); // "a..."（待确认：具体输出由 truncator 逻辑决定）

// 5) Similarity：相似度
var score = StringSimilarity.EvaluateSimilarity("foo", "fo o", 0.1);
var type = StringSimilarity.EvaluateSimilarity("foo", "fo o");
```

> 示例证据：
> - src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Splitter.On/TrimResults/OmitEmptyStrings
> - src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs | SplitToDictionary
> - src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs | CaseFormatter.To
> - src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs | StringTruncateExtensions.Truncate
> - src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | StringSimilarity.EvaluateSimilarity

## 11. 待办与改进
- [ ] 补齐 Text 模块专属测试工程：tests/Bing.Utils.Text.Tests（当前仓库未见该目录，需确认是否遗漏或已合并至其他测试工程）。
    - 证据：src/Bing.Utils.Text/Bing.Utils.Text.csproj | PackageId=Bing.Utils.Text
    - 证据：tests/BingUtilsUT/SplitterUT/ | Splitter 用例存在
- [ ] 修复 `StringLines` 对换行符长度的固定偏移：将 `+2` 替换为 `+Environment.NewLine.Length`（并补充 Linux `\n` 的回归测试）。
    - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | index = newIndex + 2
    - 证据：src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs | index = newIndex + 2
- [ ] 为 Joiner/Truncate/CaseFormatter/StringLines 补充单测（目前仅发现 Splitter 与 Similarity 的相关用例）。
    - 证据：src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs | Joiner.On/Join
    - 证据：src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs | StringTruncateExtensions.Truncate
- [ ] 为关键路径增加 benchmark（Splitter/Joiner/Similarity 的大输入性能）。
    - 证据：benchmarks/Bing.Utils.Benchmark/Program.cs | 基准工程入口（待确认是否已覆盖 Text）

## 12. 证据定位（汇总）
- src/Bing.Utils.Text/Bing.Utils.Text.csproj | ProjectReference=..\\Bing.Utils\\Bing.Utils.csproj
- src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs | Bing.Text.Splitters.Splitter.On/OnPattern/FixedLength/InternalSplitToEnumerable
- src/Bing.Utils.Text/Bing/Text/Splitters/ISplitter.cs | Bing.Text.Splitters.ISplitter
- src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs | Bing.Text.Splitters.Splitter.SplitToDictionary/SplitterUtils.SplitMap
- src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.FixedLength.cs | Bing.Text.Splitters.Splitter.SplitByFixedLength
- src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs | Bing.Text.Joiners.Joiner.On/Join/AppendTo
- src/Bing.Utils.Text/Bing/Text/Joiners/IJoiner.cs | Bing.Text.Joiners.IJoiner
- src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Map.cs | Bing.Text.Joiners.Joiner.JoinToKeyValuePairString
- src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.Tuple.cs | Bing.Text.Joiners.Joiner.JoinToTupleString
- src/Bing.Utils.Text/Bing/Collections/StringCollectionExtensions.cs | Bing.Collections.StringCollectionExtensions.JoinToString
- src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs | Bing.Text.CaseFormatter.To
- src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs | Bing.Text.StringTruncators / Bing.Text.StringTruncateExtensions.Truncate
- src/Bing.Utils.Text/Bing/Text/Truncation/FixedLengthTruncator.cs | Bing.Text.Truncation.FixedLengthTruncator.Truncate
- src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Split.cs | Bing.Text.StringLines.SplitByLines
- src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Count.cs | Bing.Text.StringLines.CountByLines
- src/Bing.Utils.Text/Bing/Text/Lines/StringLines.Truncation.cs | Bing.Text.StringLines.TruncateByLines
- src/Bing.Utils.Text/Bing/Text/Similarity/StringSimilarity.cs | Bing.Text.Similarity.StringSimilarity.EvaluateSimilarity
- src/Bing.Utils.Text/Bing/Text/StringProwess.cs | Bing.Text.StringProwessExtensions.SplitTyped
- src/Bing.Utils/Bing/Text/Strings/Strings.cs | Bing.Text.Strings.RemoveWhiteSpace/RemoveChars
- version.props | VersionMajor/VersionMinor/VersionPatch
- tests/BingUtilsUT/SplitterUT/SplitterTest.cs | Splitter 用例
- tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs | Map Splitter 用例
- tests/BingUtilsUT/SplitterUT/FixedLengthSplitterTest.cs | FixedLength Splitter 用例
- tests/Bing.Utils.Tests/Bing/Text/Similarity/StringSimilarityTest.cs | Similarity 用例

## 13. 待确认
- tests/Bing.Utils.Text.Tests 是否应存在（模板骨架如此约定，但仓库中未找到该目录）。
- `StringLines` 当前实现是否仅面向 Windows（CRLF）场景；若需跨平台，是否计划修复。 
- 使用示例中 `CaseFormatter` 对不含分隔符的输入（如 "HelloWorld"）转换为 LowerHyphen 的预期是什么（当前实现依赖分割器 `Regex("[-_ ]+")`，可能无法按驼峰边界拆分）。
