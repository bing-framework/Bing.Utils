# 拼音数据快照

- `characters.txt`：mozillazg/pinyin-data v0.15.0，单字读音；同字多读音按来源顺序保存。
- `phrases.txt`：mozillazg/phrase-pinyin-data v0.19.0，词组读音；同词组多记录时采用首条。
- 两份数据分别受相邻的 MIT 许可文件约束。
- 来源：https://github.com/mozillazg/pinyin-data/releases/tag/v0.15.0 及 https://github.com/mozillazg/phrase-pinyin-data/releases/tag/v0.19.0 。

执行 `python -X utf8 build/generate-pinyin-data.py` 生成本目录 `data` 下的压缩数据，执行 `--check` 验证一致性。正常构建及运行不需要 Python 或联网；默认 NuGet 包仅包含压缩数据和许可，不包含原始文本。

`data/characters.gz` 和 `data/phrases.gz` 位于独立资源目录；提交到 Git 后可按路径单独下载。将它们复制到应用管理的数据目录，用 `PinyinCatalog(Stream characters, Stream phrases)` 加载。实例在构造时复制词库，不关闭调用方流，之后可并发读取。两个压缩文件需配套使用；格式是生成器输出的 UTF-8 gzip 记录，不是原始来源文本。

| 文件 | SHA-256 |
| --- | --- |
| `data/characters.gz` | `65267D9D24EB9DEAC92EFA7EEC02D3C1DC2EDE90B617C5DF9E49E75529467432` |
| `data/phrases.gz` | `53D710C849392075F1D9EA6F0B7B135B29662FC2081969999126A54D861E399C` |

## 外置数据构建

默认 `Bing.Utils.Text` 包会内嵌这两个压缩资源，以保持现有静态拼音入口的调用方式。需要自行管理数据文件时，可构建外置包：

```powershell
dotnet pack src/Bing.Utils.Text/Bing.Utils.Text.csproj -p:BingTextExternalOnly=true
```

该命令生成包名为 `Bing.Utils.Text.External`，程序集名仍为 `Bing.Utils.Text`。默认包和外置包必须二选一，不能在同一个应用目标中同时引用。外置包不包含内嵌拼音数据，也不提供 `PinyinUtil.GetPinyinWithTone` 和 `PinyinUtil.GetContextualPinyin`；需要拼音词库时，由调用方从 Git 资源目录或自行管理的数据目录读取两个文件：

```csharp
using Bing.Text.Pinyin;

using var characters = File.OpenRead(@"D:\data\pinyin\characters.gz");
using var phrases = File.OpenRead(@"D:\data\pinyin\phrases.gz");
var catalog = new PinyinCatalog(characters, phrases);
var result = catalog.GetPinyinWithTone("重庆银行", " ");
```

压缩数据和原始来源文件仍保留在本仓库的 `asset/pinyin` 目录中，不随外置包自动下载，也不会在运行时联网获取或替换数据。应用需要自行部署并管理文件版本；格式、许可和校验值以本 README 前文为准。
