# Bing.Utils.Extra

## Emoji 工具

命名空间：`Bing.Extra.Emoji`。入口：`EmojiUtil`。
使用内置 Unicode Emoji 18.0 与 gemoji v4.1.0 快照，没有新增运行时第三方依赖，不访问网络。
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

foreach (var match in EmojiUtil.FindAll("A😀中👍🏽"))
{
    // 两次匹配的 (Index, Length) 分别为 (1, 2)、(4, 4)。
    var original = "A😀中👍🏽".Substring(match.Index, match.Length);
    var englishName = match.Emoji.Name;
}

EmojiUtil.TryGetByAlias(":smile:", out var smile);
EmojiUtil.TryGetByUnicode("❤", out var heart);  // heart.Unicode 为 "❤️"
var catalog = EmojiUtil.GetAll();              // 规范表情的只读列表
var names = EmojiUtil.Replace("😀", match => "[" + match.Emoji.Name + "]");
```

## 接口语义

| 接口 | 行为 |
| --- | --- |
| `IsEmoji` | 整个输入恰好为一个受支持序列，多个表情或混合文本返回 false |
| `ContainsEmoji` / `Count` | 判断包含或统计次数；组合表情算一次，重复出现分别计数 |
| `ExtractEmojis` / `FindAll` | 按原文顺序返回只读结果，保留重复与原文形式 |
| `RemoveAllEmojis` / `Replace` | 仅处理匹配内容，不裁剪空白；回调按顺序调用，结果不递归扫描 |
| `ToUnicode` | 只转换大小写敏感的 `:alias:`，未知别名原样保留，不解码 HTML |
| `ToAlias` | 使用完整序列的首选别名；没有映射时保留完整原文 |
| `TryGetByAlias` | 接受裸别名或一对冒号包围的别名；不裁剪空白，大小写敏感 |
| `TryGetByUnicode` | 接受规范序列及官方清单内的兼容形式，返回规范元数据 |
| `GetAll` | 按官方顺序返回规范表情，不重复列出兼容形式 |

`EmojiInfo` 提供 `Unicode`、`Name`、`Group`、`Subgroup`、`Version`、`Aliases`。
`Version` 是表情引入时的 Emoji 版本，不是所有组成字符首次编码的 Unicode 版本。
模型及公开集合不可变；`EmojiMatch` 保存 `Value`、UTF-16 `Index` / `Length` 与 `Emoji` 元数据。

文本为 null 时：判断为 false，计数为零，列表为空，转换／替换／清理返回 null。
空字符串相应返回空结果。查询失败返回 false，out 参数为 null。
替换字符串或回调为 null 时，即便文本为空也抛出 `ArgumentNullException`；回调返回 null 表示删除，回调异常直接传递。

## 识别边界与旧接口区别

- 接受官方清单中的 fully-qualified、minimally-qualified、unqualified，包含 `❤` 与 `❤️`；不将独立肤色、发型组件视为完整表情。
- 旗帜、肤色、家庭／职业 ZWJ、键帽和标签序列使用最长匹配。普通数字、`#`、`*` 本身不属于表情。
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
本版不包含 HTML 实体互转、标签查询、中文搜索、图片渲染、自定义注册或肤色变体生成。
这些能力可在明确数据来源与兼容契约后分别扩展。
