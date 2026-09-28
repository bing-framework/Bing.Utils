# 中文分词外置词典

`dict.txt` 是 [jieba v0.42.1](https://github.com/fxsjy/jieba/tree/v0.42.1) 的固定 UTF-8 主词典快照，许可见本目录的 `LICENSE.txt`（MIT）。词典只保存在仓库的 `asset/segmentation`，不会嵌入 `Bing.Utils.Text.dll`，也不会进入 `Bing.Utils.Text` NuGet 包。

SHA-256（`dict.txt`）：`18DFEFF162DEE8DA71332771CFAD5940CA5560B0B2346ACCDA85A15FCC9D475D`

应用可以从仓库单独取得 `asset/segmentation/dict.txt`，放到自行管理的数据目录，然后传入可读流：

```csharp
using Bing.Text.Segmentation;

using var dictionary = File.OpenRead(@"D:\data\jieba\dict.txt");
var segmenter = new ChineseSegmenter(dictionary);
var words = segmenter.Cut("我来到北京清华大学");

using var businessDictionary = File.OpenRead(@"D:\data\jieba\business.txt");
var businessSegmenter = segmenter.WithDictionary(businessDictionary);
var searchSegments = businessSegmenter.FindForSearch("北京清华大学");

using var model = File.OpenRead(@"D:\data\jieba\hmm-model.gz");
var inferredSegmenter = businessSegmenter.WithHmmModel(model);
var inferredWords = inferredSegmenter.Cut("未登录词示例");
```

构造函数和 `WithDictionary` 均从流当前位置读取词典，并保留调用方对流的所有权。词典可使用相同格式替换或精简：每行 `词 词频 [词性]`，词频为正整数；基础词典中的重复词保留首条，业务词典中的重复词以最后一条为准。业务词频覆盖基础词频，叠加返回独立实例，不改变原实例；可连续叠加。构造完成后词典快照与文件无关，可并发分词。

该实现使用词频最佳路径处理连续汉字。`FindForSearch` 保留原分词，并从长度不少于三个 Unicode 标量的汉字词中提取词典收录的二字、三字子词；结果按原文起点升序、同起点长度升序排列，位置和长度为 UTF-16 单位。`CutForSearch` 返回相同顺序的词值。ASCII 字母数字和空白分别按连续区段保留，其他非汉字按 Unicode 标量保留。按业务需要可更换专用词典，不保证不同词典产生相同切分。

`hmm-model.gz` 由同版本 jieba 的 `prob_start.py`、`prob_trans.py`、`prob_emit.py` 快照生成，三份原始概率表与 MIT 许可一同放在本目录。执行 `python -X utf8 build/generate-segmentation-hmm.py --check` 可核对压缩模型；SHA-256 为 `4F3ACA413BE49BEAB79B621F61C5C3B63FF911921E3978641F804C07F848AB00`。模型文件通过 Git 独立下载，不进入 NuGet 包，也不在运行时联网。

`WithHmmModel` 返回启用 HMM 的独立实例，读取后保留调用方对模型流的所有权。它只处理词频最佳路径中连续的 BMP 汉字单字片段；已选中的词组、补充平面汉字和非汉字仍沿用原切分。未提供 jieba 全模式，也不保证与 jieba Python 所有分词结果一致。
