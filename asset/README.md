# 外置资源索引

本页是 `Bing.Utils.Text` 外置数据文件的统一入口。下列路径均相对于仓库根目录；资源快照和对应的原始文件位于 `asset` 目录，提交后可从 Git 按路径取得。构建和运行时不会自动下载、联网获取或切换版本。每组资源的来源、许可、格式和校验信息以对应目录的 README 为准。

## 拼音

| 文件 | 配套要求 | 用途 |
| --- | --- | --- |
| `asset/pinyin/data/characters.gz` | 必须与 `asset/pinyin/data/phrases.gz` 配套，使用同一版本快照 | `PinyinCatalog(Stream characters, Stream phrases)` 的单字数据 |
| `asset/pinyin/data/phrases.gz` | 必须与 `asset/pinyin/data/characters.gz` 配套，使用同一版本快照 | `PinyinCatalog(Stream characters, Stream phrases)` 的词组数据 |

当前快照使用 `pinyin-data v0.15.0` 和 `phrase-pinyin-data v0.19.0`。来源版本、MIT 许可和生成格式见 [asset/pinyin/README.md](pinyin/README.md)。

## OpenCC 简繁转换

| 文件 | 配套要求 | 用途 |
| --- | --- | --- |
| `asset/opencc/data/s2t.gz`、`asset/opencc/data/t2s.gz` | 两个文件必须配套 | 创建基础 `ChineseConversionCatalog` |
| `asset/opencc/data/tw-forward.gz`、`asset/opencc/data/tw-reverse.gz` | 两个文件必须配套，并叠加到基础目录 | `WithRegionalRules` 创建台湾目录 |
| `asset/opencc/data/hk-forward.gz`、`asset/opencc/data/hk-reverse.gz` | 两个文件必须配套，并叠加到基础目录 | `WithRegionalRules` 创建香港目录 |

六个文件均来自 OpenCC `ver.1.4.1`。基础目录需要先使用 `s2t.gz` 和 `t2s.gz` 创建；台湾或香港地区目录再使用对应的正向、反向文件叠加。来源版本、Apache-2.0 许可、格式和生成规则见 [asset/opencc/README.md](opencc/README.md)。

## 中文分词

| 文件 | 配套要求 | 用途 |
| --- | --- | --- |
| `asset/segmentation/dict.txt` | 创建基础分词器时必需 | `ChineseSegmenter(Stream dictionary)` 的 jieba 主词典 |
| `asset/segmentation/hmm-model.gz` | 可选；必须先有一个分词器 | `ChineseSegmenter.WithHmmModel(Stream)` 的 HMM 模型，不替代主词典 |

词典和模型均固定来自 jieba `v0.42.1`；默认分词只需要 `dict.txt`，需要未登录词识别时再加载 `hmm-model.gz`。来源版本、MIT 许可、格式和模型生成说明见 [asset/segmentation/README.md](segmentation/README.md)。

## 从 Git 获取并加载

1. 在目标 Git 仓库和指定提交中，按上面的相对路径获取需要的二进制或文本文件，并保持文件内容为原始字节。此索引不假定一个可长期使用的文件直链，也不代表资源已发布到 NuGet 或其他下载服务。
2. 将文件复制到应用自行管理的数据目录。只取得基础能力时只需下载相应的必需文件；地区转换和 HMM 模型按需下载对应配套文件。
3. 用 `File.OpenRead` 打开文件，把流传给目录或分词器。各构造函数会读取自己的快照，输入流的关闭责任仍由调用方承担。

```csharp
using Bing.Text.Chinese;
using Bing.Text.Pinyin;
using Bing.Text.Segmentation;

using var characters = File.OpenRead(@"D:\data\pinyin\characters.gz");
using var phrases = File.OpenRead(@"D:\data\pinyin\phrases.gz");
var pinyin = new PinyinCatalog(characters, phrases);

using var s2t = File.OpenRead(@"D:\data\opencc\s2t.gz");
using var t2s = File.OpenRead(@"D:\data\opencc\t2s.gz");
var standard = new ChineseConversionCatalog(s2t, t2s);
using var twForward = File.OpenRead(@"D:\data\opencc\tw-forward.gz");
using var twReverse = File.OpenRead(@"D:\data\opencc\tw-reverse.gz");
var taiwan = standard.WithRegionalRules(twForward, twReverse);

using var dictionary = File.OpenRead(@"D:\data\jieba\dict.txt");
var segmenter = new ChineseSegmenter(dictionary);
using var hmmModel = File.OpenRead(@"D:\data\jieba\hmm-model.gz");
var inferredSegmenter = segmenter.WithHmmModel(hmmModel);
```

外置包构建方式和默认包、外置包的选择限制见 [Bing.Utils.Text 模块文档](../docs/modules/Bing.Utils.Text.md)。
