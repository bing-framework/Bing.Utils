# 密码模块性能基线

## 基线用途

本页记录密码模块的代表性性能数据，用于发现后续改动造成的明显性能回退。数据来自 BenchmarkDotNet 的 `ShortRun`，适合日常回归比较，不用于跨机器的绝对性能排名。

建议在相同机器、相同运行时和相同电源模式下复测。相较本基线，平均耗时增加超过 10% 或托管内存分配增加超过 15% 时，应检查实现或补充说明。

## 测试环境

- 日期：2026-09-18
- 操作系统：Windows 10 22H2（10.0.19045）
- 处理器：Intel Xeon E5-2697 v2，24 物理核心 / 48 逻辑核心
- SDK：.NET SDK 10.0.400；SM2 报告使用 10.0.301
- 运行时：.NET 6.0.36，x64 RyuJIT AVX
- BenchmarkDotNet：0.13.11
- Job：`ShortRun`，3 次预热、3 次测量、1 次启动

`Error` 是 99.9% 置信区间的半宽。吞吐量由 Mean 和输入尺寸计算；RSA、SM2 使用每秒操作数，数据原语和 BSS2 使用 MiB/s。

## 代表性结果

| 能力 | 输入/操作 | Mean | Error | StdDev | 吞吐量 | Allocated |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| AES-256-GCM | 加密 1 MiB | 1.2147 ms | 0.2166 ms | 0.0119 ms | 823.22 MiB/s | 1,050,349 B |
| SM3 | 摘要 1 MiB | 11.4947 ms | 0.5035 ms | 0.0276 ms | 87.00 MiB/s | 600 B |
| SM4-GCM | 加密 1 MiB | 18.1803 ms | 3.8347 ms | 0.2102 ms | 55.01 MiB/s | 3,147,663 B |
| RSA-OAEP-SHA256 | 加密 190 B | 300.4 us | 95.05 us | 5.21 us | 3,329 ops/s | 1,824 B |
| RSA-OAEP-SHA256 | 解密 | 3,658.2 us | 1,550.63 us | 85.00 us | 273 ops/s | 1,002 B |
| RSA-PSS-SHA256 | 签名 256 B | 3,778.7 us | 808.54 us | 44.32 us | 265 ops/s | 924 B |
| RSA-PSS-SHA256 | 验签 256 B | 273.5 us | 104.35 us | 5.72 us | 3,656 ops/s | 1,960 B |
| SM2 | 加密 32 B | 678.9 us | 125.89 us | 6.90 us | 1,473 ops/s | 475.44 KiB |
| SM2 | 解密 32 B | 466.7 us | 154.97 us | 8.49 us | 2,143 ops/s | 366.02 KiB |
| SM2 | 签名 32 B | 497.7 us | 13.19 us | 0.72 us | 2,009 ops/s | 240.78 KiB |
| SM2 | 验签 32 B | 531.9 us | 497.92 us | 27.29 us | 1,880 ops/s | 401.38 KiB |
| AES-GCM/BSS2 | 10 MiB 流式加密 | 41.27 ms | 3.138 ms | 0.172 ms | 242.31 MiB/s | 31.98 MiB |
| AES-GCM/BSS2 | 100 MiB 流式加密 | 367.60 ms | 50.163 ms | 2.750 ms | 272.03 MiB/s | 256.29 MiB |

完整输入规模为 32 B、256 B、4 KiB、64 KiB 和 1 MiB；原始报告由 BenchmarkDotNet 写入本地 `BenchmarkDotNet.Artifacts/results`。BSS2 另覆盖 10 MiB 和 100 MiB 文件流。

## 运行命令

```powershell
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -- --job short --filter *SecurityPrimitiveBenchmarks* --exporters json
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -- --job short --filter *AesGcmStreamBenchmarks* --exporters json
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -- --job short --filter *RsaSecurityBenchmarks* --exporters json
dotnet run --project benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj -c Release -- --job short --filter *Sm2Benchmarks* --exporters json
```

提交性能改动时，记录测试环境、输入尺寸、Mean、Error、StdDev、吞吐量与 Allocated，并说明超过阈值的原因。