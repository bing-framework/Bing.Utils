# Bing.Utils.Text
## 1. 包职责（Scope）
- 解决的问题
- 提供字符串分割器、文本处理工具、Unicode 文本元素安全截断、拼音转换以及可复用的多关键词匹配器。[证据] `src/Bing.Utils.Text/Bing.Utils.Text.csproj:3` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8` [证据] `src/Bing.Utils.Text/Bing/Text/Truncation/FixedTextElementTruncator.cs:9` [证据] `src/Bing.Utils.Text/Bing/Text/Pinyin/PinyinUtil.cs:12` [证据] `src/Bing.Utils.Text/Bing/Text/Matching/KeywordMatcher.cs:15`
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

## 2.1 多关键词匹配

`Bing.Text.Matching.KeywordMatcher` 在构造时接收关键词快照，使用区分大小写的序数比较进行精确匹配。实例构建完成后不再修改，可由多个线程并发查询。[证据] `src/Bing.Utils.Text/Bing/Text/Matching/KeywordMatcher.cs:15`

```csharp
using Bing.Text.Matching;

var matcher = new KeywordMatcher(new[] { "中国", "中国人", "国人" });
var matches = matcher.FindAll("欢迎中国人");
matches[0].Keyword.ShouldBe("中国人");

var highlighted = matcher.Replace("欢迎中国人", match => $"<mark>{match.Value}</mark>");
var masked = matcher.Replace("联系方式：中国人", "***");
```

默认 `FindAll` 返回最左最长、不重叠的结果；传入 `includeOverlaps: true` 时返回全部命中，按起点升序、同起点长度降序排列。`KeywordMatch.Index` 和 `Length` 使用 UTF-16 代码单元，可直接用于 `Substring`。替换不会递归扫描替换结果，回调返回 `null` 表示删除匹配内容。

关键词序列为 `null`，或包含 `null`、空字符串时构造器抛出参数异常；重复关键词按序数比较去重。文本为 `null` 时 `ContainsAny` 返回 `false`、`FindAll` 返回空集合、`Replace` 返回 `null`，但替换字符串或回调为 `null` 始终抛出 `ArgumentNullException`。

## 2.2 拼音转换

`Bing.Text.Pinyin.PinyinUtil` 提供原有无声调全拼、拼音首字母、BMP 汉字区段判断，以及带声调和按词组读音转换的新增入口。原有接口继续使用固定 GBK 区码；新入口使用固定的离线单字与词组数据。

```csharp
using Bing.Text.Pinyin;

var pinyin = PinyinUtil.GetPinyin("中国工具", " "); // Zhong Guo Gong Ju
var initials = PinyinUtil.GetInitials("中国工具");   // zggj
var toned = PinyinUtil.GetPinyinWithTone("重庆银行", " "); // Chóng Qìng Yín Háng
var contextual = PinyinUtil.GetContextualPinyin("重庆银行", " "); // Chong Qing Yin Hang
```

新入口采用最长完整词组匹配，未命中词组时使用单字数据中的首个读音；不自动推断未收录词组。音节首字母大写，无调输出保留 `ü`。非中文及未收录字符保持原样，分隔符只添加在相邻且成功转换的音节之间。新词库支持收录的补充平面汉字，首次调用时懒加载，之后可并发读取。数据来源、版本和许可见 [拼音数据说明](../../asset/pinyin/README.md)；正常运行无需联网。

调用方也可以单独下载仓库中的两个压缩资源，用独立词库实例转换：

```csharp
using var characters = File.OpenRead(@"D:\data\pinyin\characters.gz");
using var phrases = File.OpenRead(@"D:\data\pinyin\phrases.gz");
var catalog = new PinyinCatalog(characters, phrases);
var customPinyin = catalog.GetPinyinWithTone("重庆银行", " ");
```

## 2.3 简繁中文转换

`Bing.Text.Chinese.ChineseConverter` 使用内置的固定词表，按最长词组优先转换，未命中词组时使用单字映射。两个方向分别按需加载；不依赖网络或系统区域设置。

```csharp
using Bing.Text.Chinese;

var traditional = ChineseConverter.ToTraditional("发展和头发"); // 發展和頭髮
var simplified = ChineseConverter.ToSimplified("發展和頭髮"); // 发展和头发
```

输入 `null` 返回 `null`，空字符串、未收录字符、Emoji 和孤立代理项保留原样。词表来自固定的 OpenCC 版本，采用首个候选，但不包含区域变体及兼容汉字规范化，详见 [数据来源](../../asset/opencc/README.md)。

需要外部词表时，可单独下载仓库中的压缩资源，创建独立目录：

```csharp
using var s2t = File.OpenRead(@"D:\data\opencc\s2t.gz");
using var t2s = File.OpenRead(@"D:\data\opencc\t2s.gz");
var catalog = new ChineseConversionCatalog(s2t, t2s);
var result = catalog.ToTraditional("发展和头发");
```

台湾和香港用词可以通过同版本的[外置地区词表](../../asset/opencc/README.md)叠加到基础目录：

```csharp
using var twForward = File.OpenRead(@"D:\data\opencc\tw-forward.gz");
using var twReverse = File.OpenRead(@"D:\data\opencc\tw-reverse.gz");
var taiwan = catalog.WithRegionalRules(twForward, twReverse);
var taiwanText = taiwan.ToTraditional("鼠标和软件"); // 滑鼠和軟體
var simplified = taiwan.ToSimplified(taiwanText);
```

香港目录使用 `hk-forward.gz` 与 `hk-reverse.gz` 创建。地区目录与基础目录相互独立，读取完成后由调用方管理词表流；转换按基础简繁和地区规则两个步骤执行。该入口不内嵌地区数据，也不承诺与 OpenCC CLI 的分词及兼容字规范化行为完全相同。

## 2.4 外置词典中文分词

`Bing.Text.Segmentation.ChineseSegmenter` 从调用方的 UTF-8 词典流建立词频快照。可直接使用仓库中可独立下载的 [jieba 主词典](../../asset/segmentation/README.md)，该文件不进入 Text NuGet 包。

```csharp
using Bing.Text.Segmentation;

using var dictionary = File.OpenRead(@"D:\data\jieba\dict.txt");
var segmenter = new ChineseSegmenter(dictionary);
var segments = segmenter.FindAll("我爱中国");
var words = segmenter.Cut("我爱中国");

using var businessDictionary = File.OpenRead(@"D:\data\jieba\business.txt");
var businessSegmenter = segmenter.WithDictionary(businessDictionary);
var searchSegments = businessSegmenter.FindForSearch("我爱中国");
var searchWords = businessSegmenter.CutForSearch("我爱中国");
```

`FindAll` 返回原文片段及 UTF-16 位置，所有片段依次拼接可还原输入。连续汉字由词频最佳路径切分；空输入返回空列表，构造完成后可以并发查询。`WithDictionary` 创建业务词频覆盖的独立快照，原分词器不受影响。`FindForSearch` 在原分词之外提取已收录的二字、三字子词，并按起点和长度排序；`CutForSearch` 返回对应词值。默认未收录汉字逐字保留。

需要识别未登录词时，可单独取得[外置 HMM 模型](../../asset/segmentation/README.md)并创建新分词器：

```csharp
using var model = File.OpenRead(@"D:\data\jieba\hmm-model.gz");
var inferredSegmenter = segmenter.WithHmmModel(model);
var inferredWords = inferredSegmenter.Cut("未登录词示例");
```

HMM 只处理词频路径中的连续 BMP 汉字单字片段，模型流由调用方管理；已收录词组、补充平面汉字及默认实例保持原有处理方式。

## 2.5 Unicode 安全截断与换行

`StringTruncators.ByTextElements` 和 `TruncateByTextElements(...)` 按 Unicode 文本元素计数，适合包含代理对、组合字符和常见 Emoji 组合的文本。边界由当前运行时的 `StringInfo.GetTextElementEnumerator` 判定，运行时使用的 Unicode 数据不同可能影响个别组合的分组。现有 `ByLength` 保留 UTF-16 长度语义；新入口不会改变旧调用结果。

`StringLines` 的分割、计数和类型转换支持 CRLF、LF、CR 及混合换行，末尾换行不额外产生空行。

Text 包仅发布一份 `netstandard2.0` 程序集，.NET 6、7、8 调用同一实现。拼音和简繁静态入口继续使用内嵌数据以保持已有调用；外置实例可使用仓库 `asset/pinyin/data` 与 `asset/opencc/data` 的独立文件。分词词典只在 Git 资源目录，不进入 NuGet 包。

## 2.6 可选外置数据构建

外置资源的统一相对路径、配套关系、来源许可和从 Git 获取后加载的流程见[外置资源索引](../../asset/README.md)。

默认构建保持现有包名 `Bing.Utils.Text`、程序集名 `Bing.Utils.Text` 和全部既有 API：

```powershell
dotnet pack src/Bing.Utils.Text/Bing.Utils.Text.csproj
```

如果应用希望自行管理较大的数据文件，可以选择外置数据构建：

```powershell
dotnet pack src/Bing.Utils.Text/Bing.Utils.Text.csproj -p:BingTextExternalOnly=true
```

在仓库根目录运行 `.\build\Test-TextExternalPackage.ps1`，可交替构建默认包与外置包，再从本地包隔离还原 .NET 8 消费者并验证外置资源。脚本只使用本机 NuGet 缓存和刚生成的包。

该模式生成包名为 `Bing.Utils.Text.External`，程序集名仍为 `Bing.Utils.Text`。它不嵌入拼音数据和基础简繁数据，也不提供静态 `ChineseConverter`、`PinyinUtil.GetPinyinWithTone` 和 `PinyinUtil.GetContextualPinyin`；外置目录 `PinyinCatalog`、`ChineseConversionCatalog`、`ChineseRegionalConversionCatalog` 和 `ChineseSegmenter` 仍可使用。默认包与外置包必须二选一，不能在同一个应用目标中同时引用，因为两者使用相同的程序集名。

外置模式下，调用方需要自行取得并管理以下 Git 资源，再通过流传入对应目录：

- `asset/pinyin/data/characters.gz`、`asset/pinyin/data/phrases.gz`：创建 `PinyinCatalog`。
- `asset/opencc/data/s2t.gz`、`asset/opencc/data/t2s.gz` 以及可选的 `tw-forward.gz`、`tw-reverse.gz`、`hk-forward.gz`、`hk-reverse.gz`：创建基础或地区简繁目录。
- `asset/segmentation/dict.txt` 以及可选的 `hmm-model.gz`：创建 `ChineseSegmenter` 和 HMM 分词实例。

这些原始快照和生成资源继续保留在仓库的 `asset` 目录中，构建或运行时不会自动下载、联网补齐或切换到其他数据版本。具体文件格式、来源、许可和校验值见对应资源 README。

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
- 模块新增能力由 `tests/Bing.Utils.Text.Tests` 验证；历史分割器测试仍位于聚合测试项目。[证据] `tests/Bing.Utils.Text.Tests/Bing.Utils.Text.Tests.csproj:11` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:17`
- 关键类：`SplitterTest`、`MapSplitterTest`、`FixedLengthSplitterTest`，覆盖常见分割与 map/trim/limit 组合。[证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:5` [证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:5` [证据] `tests/BingUtilsUT/SplitterUT/FixedLengthSplitterTest.cs:5`
- 未覆盖风险点
- `FixedLength` 负数长度异常路径已有显式断言。[证据] `tests/Bing.Utils.Text.Tests/Bing/Text/Splitters/SplitterTest.cs:236`

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- `Splitter` API 采用 fluent 设计，若未来引入不可变配置模型，需评估链式调用兼容性（待确认）。

