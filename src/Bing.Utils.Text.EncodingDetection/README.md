# Bing.Utils.Text.EncodingDetection

基于 UTF.Unknown 提供字节、文件和流的有界编码检测、不可定位流回放以及增量转码至无 BOM UTF-8。`MaxSampleBytes` 至少为 4 字节，以完整识别 UTF-32 BOM；更小值在读取前抛出参数异常。没有 BOM 时返回候选和证据，ASCII 兼容输入或低置信度默认抛错，可显式指定编码或回退。严格解码可选替换，取消令牌传入底层异步读写。组合示例见 `docs/integration-capabilities.md`。

目标框架：`netstandard2.0;net8.0`。
