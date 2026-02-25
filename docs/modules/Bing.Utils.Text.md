# Bing.Utils.Text
## 1. 包职责（Scope）
- 解决的问题
- 提供字符串分割器（按分隔符、正则、固定长度、Map 键值对）及文本处理工具集。[证据] `src/Bing.Utils.Text/Bing.Utils.Text.csproj:3` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8`
- 不解决的问题（Out of Scope）
- 不提供全文检索或复杂 NLP 能力（聚焦基础文本处理）。

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `Splitter.On(...)`、`OnPattern(...)`、`FixedLength(...)`、`Split/SplitToList/SplitToDictionary`。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:242` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:277` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:288` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:186` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs:68`
- 可链式配置：`OmitEmptyStrings()`、`TrimResults()`、`Limit()`、`WithKeyValueSeparator()`。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:106` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:119` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:143` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:157`
- 输入输出约定
- 空白输入字符串直接返回空序列，不抛异常。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:202` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:203`
- 边界行为（null、空集合、非法参数）
- `FixedLength(length)` 对负数长度抛 `ArgumentOutOfRangeException`。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:290` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:291`
- `Limit(limit<=0)` 会回退为不限长（`LimitLength = -1`）。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:334` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:335`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var list = Splitter.On(",").SplitToList("a,b,c,d,e");
list.Count.ShouldBe(5);
list[0].ShouldBe("a");
```
[证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:162` [证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:163` [证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:165`
- 示例2：进阶用法
```csharp
var dict = Splitter.On("&")
    .WithKeyValueSeparator("=")
    .Limit(3)
    .SplitToDictionary("a=1&b=2&c=3&d=4");
dict.Count.ShouldBe(3);
```
[证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:60` [证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:61`
- 示例3：常见错误与修正
```csharp
Assert.Throws<ArgumentOutOfRangeException>(() => Splitter.FixedLength(-1));
// 修正：固定长度参数应 >= 0
```
[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:290` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:291`

## 4. 性能与线程安全说明
- 是否分配敏感
- `Split` 内部会构造 `List<string>` 并做投影，属于中等分配路径。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:205` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:220`
- 是否线程安全
- `Splitter` 实例持有可变 `Options`，链式配置会修改内部状态，不建议共享同一实例跨线程配置/调用。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:43` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:108` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:145`
- 是否可并发调用
- 建议每次构建独立 `Splitter` 实例后再调用。

## 5. 异常与日志策略
- 抛出哪些异常
- `FixedLength(-1)` 抛 `ArgumentOutOfRangeException`。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:291`
- 什么时候返回默认值而不是抛异常
- 输入字符串为空白时返回空枚举，不抛异常。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:202` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:203`

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils` 核心包。[证据] `src/Bing.Utils.Text/Bing.Utils.Text.csproj:11`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.Text`。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 测试项目通过聚合工程引用本包：`tests/BingUtilsUT`、`tests/Bing.Utils.Tests`。[证据] `tests/BingUtilsUT/BingUtilsUT.csproj:17` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:18`
- 关键类：`SplitterTest`、`MapSplitterTest`、`FixedLengthSplitterTest`，覆盖常见分割与 map/trim/limit 组合。[证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:5` [证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:5` [证据] `tests/BingUtilsUT/SplitterUT/FixedLengthSplitterTest.cs:5`
- 未覆盖风险点
- `FixedLength` 异常路径在测试侧未见显式断言（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- `Splitter` API 采用 fluent 设计，若未来引入不可变配置模型，需评估链式调用兼容性（待确认）。

