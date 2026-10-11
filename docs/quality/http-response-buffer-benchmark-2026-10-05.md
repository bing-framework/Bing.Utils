# HTTP 文本响应缓冲复用性能记录（2026-10-05）

本轮验证普通文本/元数据响应直接在同一个 MemoryStream 上解码，保留 GetStreamAsync 的 byte[] 契约和自定义 OnSendAfter 的预缓冲路径。环境为 Windows、.NET 8.0.30、Release、本地内存 BenchmarkHandler；每个模式预热 10 次，正式执行 100 次，重复 3 次。大 JSON 在计时外构造，正文约 1 MiB。

候选程序集：

- HTTP：DFE95521E4A1B1159C985090173E9B0774CCA876DD5CBEBF9EB112EB48671D96
- Benchmark：736807A67315937D445C236AC0DCA0509738DC7EC31D672824DA4BBD2B0EE6F3

| 模式 | 次数 | 耗时 ms（3 次） | 分配 MiB（3 次） | 堆峰值 MiB（3 次） |
| --- | ---: | --- | --- | --- |
| metadata-large | 100 | 785.9 / 705.6 / 605.8 | 1090.80 / 1090.99 / 1091.09 | 69.15 / 54.36 / 48.73 |
| raw-large | 100 | 622.4 / 666.1 / 421.6 | 1090.82 / 1090.88 / 1091.43 | 50.25 / 67.71 / 43.91 |

三次中位数为：metadata-large 705.6 ms、1090.99 MiB；raw-large 622.4 ms、1090.88 MiB。所有结果均完整校验了 1 MiB 字段内容。

当前仓库没有与本次完全相同的旧候选、运行时、负载和 benchmark 身份，因此 BEFORE_STATUS=NOT_APPLICABLE。本报告只证明当前实现可运行并记录资源特征，不设置性能门槛，也不宣称相对旧版本的收益。普通 JSON、字符集、BOM-only、空正文、失败回调、精确限额、取消和读取异常由 HTTP net8.0 定向回归测试验证。