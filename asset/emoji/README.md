# Emoji 数据快照

本目录的数据仅用于离线生成与测试；运行时不读取外部文件，也不访问网络。

| 文件 | 固定版本 | 来源 |
| --- | --- | --- |
| `emoji-test.txt` | Unicode Emoji 18.0 | https://www.unicode.org/Public/18.0.0/emoji/emoji-test.txt |
| `gemoji.json` | gemoji v4.1.0 | https://raw.githubusercontent.com/github/gemoji/v4.1.0/db/emoji.json |
| `LICENSE.unicode.txt` | Unicode License V3 | https://www.unicode.org/license.txt |
| `LICENSE.gemoji.txt` | MIT | https://raw.githubusercontent.com/github/gemoji/v4.1.0/LICENSE |

导入日期：2026-09-27。保留上游版权声明；NuGet 包同时携带两份许可与本说明。

## 生成与更新

在仓库根目录执行：

```powershell
python -X utf8 build/generate-emoji-data.py
python -X utf8 build/generate-emoji-data.py --check
```

生成结果为 `src/Bing.Utils.Extra/Bing/Extra/Emoji/EmojiCatalog.Generated.cs`，随源码提交。
生成器仅使用 Python 标准库，文本读写显式使用 UTF-8，输出固定 LF；正常 .NET 构建不依赖 Python。
`--check` 不修改文件，用于验证生成结果与快照一致。

当前结果：3,963 个规范表情、5,235 个受支持序列、1,913 个别名。
接收 fully-qualified、minimally-qualified、unqualified，排除 component。
通过移除 U+FE0F 建立兼容形式与规范形式的映射；缺失规范形式、重复序列和别名冲突会使生成失败。
gemoji 仅为官方清单中存在的完整序列补充别名，第一项作为首选别名；不生成肤色别名或其他推测映射。

更新时人工选择并固定新版本，替换快照和许可，更新生成器版本校验与文档，运行生成检查、官方清单覆盖测试和手写回归测试。
不得在普通构建或运行期间自动追踪上游分支。
