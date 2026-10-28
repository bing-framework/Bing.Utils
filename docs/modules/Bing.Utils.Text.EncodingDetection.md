# Bing.Utils.Text.EncodingDetection

编码检测优先按 UTF-32、UTF-8、UTF-16 的 BOM 顺序选择；没有 BOM 时返回完整候选、来源、置信度和证据。`MaxSampleBytes` 至少为 4 字节，以完整识别 UTF-32 BOM；更小值在读取前抛出参数异常。采样有界并报告 EOF/截断；ASCII 兼容内容和低置信度默认拒绝，可显式指定编码或回退。不可定位流的回放入口保留采样探测字节。转码使用可取消的增量 Decoder/Encoder，默认严格解码并输出无 BOM UTF-8。

跨包调用示例见 [集成能力示例](../integration-capabilities.md)。
