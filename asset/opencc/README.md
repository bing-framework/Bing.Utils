# 简繁转换数据来源

固定快照来自 [OpenCC ver.1.4.1](https://github.com/BYVoid/OpenCC/releases/tag/ver.1.4.1) 的 `data/dictionary`：

- `STPhrases.txt`、`STCharacters.txt`：简体到繁体。
- `TSPhrases.txt`、`TSCharacters.txt`：繁体到简体。

台湾和香港地区词表也固定取自同一版本：`TWPhrases.txt`、`TWPhrasesRev.txt`、`TWVariants.txt`、`TWVariantsPhrases.txt`、`TWVariantsRevPhrases.txt`、`HKVariants.txt`、`HKVariantsPhrases.txt`、`HKVariantsRevPhrases.txt`。反向单字规则按该版本 OpenCC 的 `@reverse-prefer` 标记生成。生成顺序参考该版本的 `s2twp`、`tw2sp`、`s2hk`、`hk2s` 配置。

原始文件保留在本目录，适用随附的 Apache-2.0 `LICENSE`。生成的压缩资源放在 `data`，提交到 Git 后可按路径单独下载；**四份地区资源不进入 NuGet 包**。执行 `python -X utf8 build/generate-opencc-data.py --check` 可核对全部资源与快照一致。更新词表时应同步检查上游许可和转换预期。

| 文件 | SHA-256 |
| --- | --- |
| `data/s2t.gz` | `09ADC2B0C1C989750089A6307F52D96D2437EDE4DAFAEC27578EE9E27EA2A593` |
| `data/t2s.gz` | `A64294FC6C7D3D041C06C53636F117FBB79A40504CF718921937603463EDE8C3` |
| `data/tw-forward.gz` | `EAE742B425088741604251CDB99F092D29E4EA75528D62372FF4EE2434CBD1CE` |
| `data/tw-reverse.gz` | `BED46B776C170DB00A8C313B9303A65F841E5FA92617AC6BD50898CC258BB837` |
| `data/hk-forward.gz` | `3EE5F10FEFFCDADE9C3A31857BFBB50655DA164DF05D3D99A76A9CB3C3B17DCE` |
| `data/hk-reverse.gz` | `3551D3457FED88DF2E472DBBBBADEC07DC6847143DEBD0D51EFCB2307E6424FC` |

应用可以单独取得这两个压缩文件，放到自行管理的数据目录，通过 `ChineseConversionCatalog(Stream simplifiedToTraditional, Stream traditionalToSimplified)` 加载。构造后目录与文件无关，可并发读取；输入流仍由调用方管理。静态 `ChineseConverter` 继续使用内置词表，保持原有调用行为。

基础目录选取每个键的首个候选，先匹配最长词组，再回退到单字。它不包含 OpenCC 配置中的兼容汉字规范化及生成的区域词组规则，因此不是 OpenCC 全功能实现；遇到未收录的文本保持原样。

地区目录使用同一个基础目录和一组地区资源。台湾使用 `tw-forward.gz`、`tw-reverse.gz`；香港使用 `hk-forward.gz`、`hk-reverse.gz`：

```csharp
using var s2t = File.OpenRead(@"D:\data\opencc\s2t.gz");
using var t2s = File.OpenRead(@"D:\data\opencc\t2s.gz");
var standard = new ChineseConversionCatalog(s2t, t2s);
using var twForward = File.OpenRead(@"D:\data\opencc\tw-forward.gz");
using var twReverse = File.OpenRead(@"D:\data\opencc\tw-reverse.gz");
var taiwan = standard.WithRegionalRules(twForward, twReverse);
var traditional = taiwan.ToTraditional("鼠标和软件");
```

地区转换先应用基础简繁规则，再应用地区词组与字形规则；反向按相反顺序处理。新目录构造完成后可并发读取，不关闭输入流，也不修改基础目录。此实现采用独立最长匹配步骤，不复制 OpenCC 的兼容汉字规范化、mmseg 分词和所有生成词组规则，因此不保证与 OpenCC CLI 的每个输出相同。

## 外置数据构建

默认 `Bing.Utils.Text` 包会内嵌 `s2t.gz` 和 `t2s.gz`，静态 `ChineseConverter` 可以直接使用。需要自行管理简繁和地区文件时，可构建外置包：

```powershell
dotnet pack src/Bing.Utils.Text/Bing.Utils.Text.csproj -p:BingTextExternalOnly=true
```

该命令生成包名为 `Bing.Utils.Text.External`，程序集名仍为 `Bing.Utils.Text`。默认包和外置包必须二选一，不能在同一个应用目标中同时引用。外置包不包含内嵌基础简繁数据，也不提供静态 `ChineseConverter`；请使用本 README 中的 `ChineseConversionCatalog` 和 `WithRegionalRules`，从应用自行管理的文件加载：

```csharp
using Bing.Text.Chinese;

using var s2t = File.OpenRead(@"D:\data\opencc\s2t.gz");
using var t2s = File.OpenRead(@"D:\data\opencc\t2s.gz");
var standard = new ChineseConversionCatalog(s2t, t2s);
using var twForward = File.OpenRead(@"D:\data\opencc\tw-forward.gz");
using var twReverse = File.OpenRead(@"D:\data\opencc\tw-reverse.gz");
var taiwan = standard.WithRegionalRules(twForward, twReverse);
var result = taiwan.ToTraditional("鼠标和软件");
```

生成资源和对应原始快照仍保留在本仓库的 `asset/opencc` 目录中，不随外置包自动下载，也不会在运行时联网获取或切换版本。应用需要自行部署并管理文件；来源、许可、格式和校验值以本 README 前文为准。
