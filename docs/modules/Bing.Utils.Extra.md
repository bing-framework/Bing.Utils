# Bing.Utils.Extra

## Emoji 工具

命名空间：`Bing.Extra.Emoji`。入口：`EmojiUtil`。
使用内置 Unicode Emoji 18.0、gemoji v4.1.0 与 CLDR 48.2 快照，没有新增运行时第三方依赖，不访问网络。
支持 `netstandard2.0`、.NET 6、.NET 7、.NET 8；识别结果不依赖系统字体与运行时字素划分版本。

```csharp
using Bing.Extra.Emoji;

EmojiUtil.IsEmoji("👍🏽");                       // true
EmojiUtil.Count("A😀中👍🏽");                    // 2
EmojiUtil.ExtractEmojis("😀😀");                // ["😀", "😀"]
EmojiUtil.RemoveAllEmojis("你好👨‍👩‍👧‍👦！");      // "你好！"
EmojiUtil.Replace("好😀", "[表情]");            // "好[表情]"
EmojiUtil.ToUnicode("你好 :smile:");            // "你好 😄"
EmojiUtil.ToAlias("你好 😄");                   // "你好 :smile:"
EmojiUtil.Normalize("你好 ❤");                  // "你好 ❤️"

foreach (var match in EmojiUtil.FindAll("A😀中👍🏽"))
{
    // 两次匹配的 (Index, Length) 分别为 (1, 2)、(4, 4)。
    var original = "A😀中👍🏽".Substring(match.Index, match.Length);
    var englishName = match.Emoji.Name;
}

EmojiUtil.TryGetByAlias(":smile:", out var smile);
EmojiUtil.TryGetByUnicode("❤", out var heart);  // heart.Unicode 为 "❤️"
var catalog = EmojiUtil.GetAll();              // 规范表情的只读列表
var whitelist = EmojiUtil.CreateCatalog(new[] { smile, heart });
whitelist.FindAll("😀❤👍");                     // 只识别选入目录的表情
whitelist.Replace("😀❤👍", "[表情]");
var names = EmojiUtil.Replace("😀", match => "[" + match.Emoji.Name + "]");
EmojiUtil.ToHtmlEntities("A😀👍🏽");             // "A&#x1F600;&#x1F44D;&#x1F3FD;"
EmojiUtil.FromHtmlEntities("A&#x1F600;B");      // "A😀B"
var smiling = EmojiUtil.Search("smile");       // 按英文或本地化名称、关键词、分组、子分组、别名或标签搜索
var chineseFaces = EmojiUtil.Search("笑脸");    // 按英文或简体中文名称、关键词搜索
var happy = EmojiUtil.GetByTag("happy");       // 按 gemoji 英文标签筛选
var people = EmojiUtil.GetByGroup("People & Body");
var faces = EmojiUtil.GetBySubgroup("face-smiling");
var introduced = EmojiUtil.GetByVersion("1.0");
var groups = EmojiUtil.GetGroups();              // 按目录顺序枚举分组
var tags = EmojiUtil.GetTags();                  // 按目录顺序枚举英文标签
var locales = EmojiUtil.GetLocales();            // ["zh", "zh-Hant"]
var chineseByLocale = EmojiUtil.Search("笑脸", "zh");
var traditionalFaces = EmojiUtil.Search("笑臉", "zh-Hant");
EmojiUtil.TryGetLocalization("😀", "zh", out var chineseGrinning);
EmojiUtil.TryGetSkinTone("👍🏽", out var tone);   // 修饰符为 Medium，基础项为 "👍"
var toneVariants = EmojiUtil.GetSkinToneVariants("👍");
var mediumToneEmojis = EmojiUtil.GetBySkinTone(EmojiSkinTone.Medium);
EmojiUtil.ApplySkinTone("A👍🏽B👩‍💻", EmojiSkinTone.Dark); // "A👍🏿B👩🏿‍💻"
EmojiUtil.ApplySkinTones("🫱🏻‍🫲🏿", new[] { EmojiSkinTone.Medium, EmojiSkinTone.Dark }); // "🫱🏽‍🫲🏿"
EmojiUtil.RemoveSkinTones("A👍🏽B");              // "A👍B"
```

## 接口语义

| 接口 | 行为 |
| --- | --- |
| `IsEmoji` | 整个输入恰好为一个受支持序列，多个表情或混合文本返回 false |
| `ContainsEmoji` / `Count` | 判断包含或统计次数；组合表情算一次，重复出现分别计数 |
| `ExtractEmojis` / `FindAll` | 按原文顺序返回只读结果，保留重复与原文形式 |
| `RemoveAllEmojis` / `Replace` | 仅处理匹配内容，不裁剪空白；回调按顺序调用，结果不递归扫描 |
| `ToUnicode` | 只转换大小写敏感的 `:alias:`，未知别名原样保留 |
| `ToAlias` | 使用完整序列的首选别名；没有映射时保留完整原文 |
| `Normalize` | 将已识别的兼容序列转换为目录规范 Unicode 形式 |
| `ToHtmlEntities` | 将已识别 Emoji 转为大写十六进制 HTML 数字实体，未匹配文本保留 |
| `FromHtmlEntities` | 接受十进制或十六进制数字实体，只还原能够组成已知 Emoji 的序列 |
| `Search` | 按英文及目录中的本地化名称、关键词、分组、子分组、别名或标签进行不区分大小写的子串搜索 |
| `Search(query, locale)` | 仅按指定语言的本地化名称或关键词进行不区分大小写的子串搜索；语言精确匹配且不自动回退 |
| `GetByGroup` / `GetBySubgroup` | 按完整分组或子分组名称进行不区分大小写筛选 |
| `GetByVersion` | 按 Emoji 引入版本进行不区分大小写筛选 |
| `GetByTag` | 按 gemoji 英文标签进行不区分大小写的精确筛选 |
| `GetGroups` / `GetSubgroups` | 按目录首次出现顺序枚举去重后的分组名称 |
| `GetVersions` / `GetTags` / `GetLocales` | 按目录或本地化数据首次出现顺序枚举去重后的版本号、英文标签或语言标识 |
| `RemoveWhere` / `ReplaceWhere` | 按匹配条件选择性删除或替换 Emoji，不递归处理结果 |
| `TryGetByAlias` | 接受裸别名或一对冒号包围的别名；不裁剪空白，大小写敏感 |
| `TryGetByUnicode` | 接受规范序列及官方清单内的兼容形式，返回规范元数据 |
| `TryGetLocalization` | 按完整 Unicode 序列和语言标识查询本地化元数据；不自动回退 |
| `CreateCatalog` / `EmojiCatalog` | 从已有元数据创建不可变白名单目录，支持识别、查找、替换和筛选，不注册新 Unicode 序列 |
| `TryGetSkinTone` | 查询完整 Emoji 所属肤色家族、基础项、修饰符顺序和规范变体 |
| `GetSkinToneVariants` | 返回肤色家族的基础项（如果存在）及全部规范变体 |
| `GetBySkinTone` | 按单一肤色等级筛选规范表情；排除多个修饰符的组合 |
| `ApplySkinTone` | 将存在基础项且只有一个修饰符的序列应用到指定肤色等级 |
| `ApplySkinTones` | 按调用方提供的顺序应用官方多肤色变体；目标不存在或数量不匹配时保留原文 |
| `RemoveSkinTones` | 将可还原到无肤色基础项的匹配替换为基础项；无法还原时保留原文 |
| `GetAll` | 按官方顺序返回规范表情，不重复列出兼容形式 |

`EmojiInfo` 提供 `Unicode`、`Variants`、`Name`、`Group`、`Subgroup`、`Version`、`Aliases`、`Tags`、`Localizations`。
`Version` 是表情引入时的 Emoji 版本，不是所有组成字符首次编码的 Unicode 版本。
`Localizations` 提供语言标识、本地化名称和关键词，默认仅内置 `zh`、`zh-Hant`。英文由 `Name`、别名和标签提供，不隐式加入 `en` 本地化。
模型及公开集合不可变；`EmojiMatch` 保存 `Value`、UTF-16 `Index` / `Length` 与 `Emoji` 元数据。
`EmojiSkinToneInfo` 保存匹配表情、无肤色基础项（如果官方清单存在）、按出现顺序排列的肤色等级及同一家族的规范变体列表。
`EmojiInfo.Variants` 保存规范序列及官方兼容形式；`EmojiUtil.CreateCatalog` 可用这些快照构建自定义白名单目录。自定义目录为空时不匹配任何表情，输入目录包含重复序列或别名时抛出 `ArgumentException`，不会改变全局目录。

HTML 实体转换只处理数字实体：输出统一使用大写十六进制格式 `&#x...;`，输入同时接受十进制和十六进制（`x` 大小写均可）。
未知 Emoji、格式错误、超出 Unicode 范围或代理项范围的实体均保留原文；识别到的序列输出目录中的规范变体，组合序列按一个 Emoji 处理。
查询结果按目录顺序返回只读列表；无语言参数的搜索检查英文与目录中的本地化数据。分组、子分组、版本和标签筛选要求完整名称匹配。
无 locale 的 `Search(query)` 搜索英文及本地化元数据；指定 locale 时仅检查该语言的名称和关键词。内置翻译来自固定 CLDR 快照，导入翻译由调用方维护，不自动翻译或联网补全。
`Search(query, locale)` 及 `TryGetLocalization` 对语言标识使用不区分大小写的序数精确匹配；未知或空语言不会回退到英文或其他语言。`GetLocales` 只返回本地化数据中实际出现的语言，不包含英文元数据隐含的默认语言。
`Normalize` 只规范化已识别的完整序列，保留未匹配字符、未知组合和显式文本样式；元数据枚举结果按目录首次出现顺序去重。

`RemoveWhere` 和 `ReplaceWhere` 对每个完整匹配调用一次条件回调；固定替换、动态替换和删除均保留未选中的原文，回调返回 null 表示删除。
`TryGetSkinTone` 和 `GetSkinToneVariants` 只接受目录中的完整肤色序列或其兼容形式；多人物、握手和其他 ZWJ 序列会保留多个肤色等级的顺序。
`GetBySkinTone` 只返回恰好包含一个肤色修饰符且等级匹配的规范序列；包含多个修饰符的组合应通过 `TryGetSkinTone` 查询，不会被单一等级筛选误归类。
`ApplySkinTone` 只在同一家族存在无肤色基础项、且目标序列恰好包含一个修饰符时替换；多修饰符组合、无基础项家族、独立肤色组件和未知文本保持原文，不递归处理替换结果。
`ApplySkinTones` 要求非空的肤色等级列表，按调用方顺序精确匹配官方变体；已有肤色输入要求目标数量一致，因此可在没有无肤色基础项的多人物家族中重映射已存在的多肤色变体。目标不存在、数量不匹配、独立肤色组件和未知文本保持原文，不排序、去重或拼接新序列。
`RemoveSkinTones` 只移除能够还原到官方无肤色基础项的已识别序列；已识别但没有基础项的组合以及独立肤色组件保持原文，不自动为多人物序列选择单一肤色。未收录组合仍遵循固定清单匹配边界，可能匹配其中的已知子序列。

文本为 null 时：判断为 false，计数为零，列表为空，转换／替换／清理返回 null。
空字符串相应返回空结果。查询失败返回 false，out 参数为 null。
替换字符串或回调为 null 时，即便文本为空也抛出 `ArgumentNullException`；回调返回 null 表示删除，回调异常直接传递。

## 离线本地化导入

默认包仅保留英文元数据及简繁中文翻译。需要额外语言或业务名称时，调用方准备 UTF-8 JSON，每个文件描述一种语言：

```json
{
  "formatVersion": 1,
  "locale": "zh",
  "entries": [
    { "unicode": "😀", "name": "自定义笑脸", "keywords": ["开心", "笑"] }
  ]
}
```

```csharp
using System.IO;
using Bing.Extra.Emoji;

var baseline = EmojiUtil.CreateCatalog(EmojiUtil.GetAll());
using var stream = File.OpenRead("emoji-localization.json");
var localized = baseline.WithLocalizations(stream);
var matches = localized.Search("自定义笑脸", "zh");
var languages = localized.GetLocales();
localized.TryGetLocalization("😀", "ZH", out var translation);
```

目录实例也提供肤色查询与转换。以下示例只使用目录内的合法变体；对白名单目录，未纳入的变体不会被生成：

```csharp
var variants = localized.GetSkinToneVariants("👍");
localized.TryGetSkinTone("👍🏽", out var toneInfo);
var medium = localized.GetBySkinTone(EmojiSkinTone.Medium);
var darkText = localized.ApplySkinTone("👍🏽", EmojiSkinTone.Dark);
var ordered = localized.ApplySkinTones("🫱🏽‍🫲🏼", new[] { EmojiSkinTone.Dark, EmojiSkinTone.Medium });
var plain = localized.RemoveSkinTones("👍🏽");
```

- `WithLocalizations(Stream)` 返回不可变新目录，允许并发读取；原目录和静态 `EmojiUtil` 不受影响。可以连续调用导入多个文件，或只给白名单目录添加翻译。
- 同一表情、同一语言的名称和关键词整条替换。未涉及的表情、其他语言、英文名称、别名、识别和肤色元数据保留。新目录的查询、匹配结果、肤色索引和 `Items` 使用一致的元数据。
- `formatVersion` 必须为数字 `1`。语言、Unicode、名称必须为非空且非纯空白字符串；语言不得含空白。语言使用不区分大小写的精确匹配，不依赖系统语言列表；已有语言保留规范标识，如 `zh-Hant`，新增语言统一小写。
- `entries`、`keywords` 必须为非 null 数组，允许空数组；关键词必须非空且非纯空白，按区分大小写的序数比较去重并保留顺序。
- Unicode 必须是当前目录内完整的规范或兼容序列；兼容形式关联到规范表情。未知表情及关联后重复的记录均报错。不会注册新表情。
- `GetLocales()` 仅枚举目录中实际存在的本地化语言。`TryGetLocalization` 对空参数或未知映射返回 false，输出 null；支持兼容序列，不进行区域语言回退。默认没有 `en` 翻译，英文搜索使用不带 locale 的重载。
- 从输入流当前位置读取，不要求可定位，也不关闭调用方的流。导入期间不要并发修改输入流；返回后可以释放流。
- null 流抛出 `ArgumentNullException`，不可读流抛出 `ArgumentException`，格式与契约错误抛出 `InvalidDataException`，底层读取异常原样传播。失败不修改原目录；空记录数组返回内容等价的新目录。

本模块使用框架自带 JSON 解析器，无新增第三方依赖，不下载语言包。外部数据的来源、许可及更新由调用方维护。

## 识别边界与旧接口区别

- 接受官方清单中的 fully-qualified、minimally-qualified、unqualified，包含 `❤` 与 `❤️`；不将独立肤色、发型组件视为完整表情。
- 旗帜、肤色、家庭／职业 ZWJ、键帽和标签序列使用最长匹配。普通数字、`#`、`*` 本身不属于表情。
- 肤色家族查询按官方完整序列建立；支持单修饰符和多修饰符组合，不将独立肤色组件视为可移除表情。
- 显式文本样式 U+FE0E 不匹配，例如 `"\u2764\uFE0E"` 保持原样；孤立 UTF-16 代理项不会抛异常或被删除。
- 固定清单无法保证识别未来表情。未收录组合可能匹配其中已知子序列；未知残余代码单元保留原样，不进行通用字素清理。
- gemoji 的别名覆盖范围小于 Unicode 清单；没有映射的肤色、组合或新表情不会被拆开转换。
- `ToUnicode(ToAlias(text))` 可能补全表情选择符，不承诺逐字节还原输入。
- 不保证当前设备能显示每一个可识别表情，显示效果取决于字体及平台。

核心库已有 `CharJudge.IsEmoji(char)` 与 `Strings.MatchEmoji(string)`，采用字符范围／正则判断，行为保持不变。
需要完整序列、位置、别名或准确计数时使用本模块，不将新模块反向依赖引入核心库。

## 数据维护与后续范围

数据来源、许可及生成流程保存在仓库的 `asset/emoji/README.md`，许可同时打入 NuGet 包。
可在仓库根目录运行 `python -X utf8 build/generate-emoji-data.py --check` 检查内置数据是否与快照一致。
默认只包含 CLDR 48.2 简繁中文名称和关键词，其他语言由调用方显式离线导入。此前内置的其他语言已移除；默认目录对这些语言的查询返回空结果。稳定版 CLDR 未覆盖的表情仍可识别，但不虚构翻译。不再逐个扩充默认包的语言集合。
核心包仍不直接包含图片渲染、自定义注册或自动生成新的肤色变体；肤色家族查询及基于官方基础项的安全移除已提供。需要图像输出时可引用可选的 `Bing.Utils.Extra.Drawing.SkiaSharp` 适配包，该包按调用方字体生成 `SKImage`，不内置字体资源。
