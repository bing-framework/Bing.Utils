# C02 分片文件 I/O 探针

只测本轮变更的写盘、合并及其调度调用路径。固定种子 `20261023`，8 MiB 随机正文；数据创建、分片准备、场景清理不计时。合并自身的校验、刷盘、原子提交、旧分片清理计入测量。直接调用生产 `HttpDownloadSegmentFileStore` 和调度器，不用模拟实现充当 before。

每个进程、每个场景预热一次，样本数由第二个参数指定。分配使用 `GC.GetTotalAllocatedBytes(true)`，涵盖所有线程；共享池已预热，总分配不代表缓冲区保有量或工作集。`raw` 选项只用于分块大小与同步/异步句柄的诊断，不是生产路径对照。

```powershell
# CandidateDirectory 必须是已冻结的生产 DLL 目录；before/after 分别构建到独立目录。
dotnet build benchmarks/SegmentIoProbe/SegmentIoProbe.csproj -c Release `
  -p:CandidateDirectory=D:/path/to/frozen/dlls -o artifacts/probe-before
dotnet artifacts/probe-before/SegmentIoProbe.dll artifacts/before.json 3
```

输出包含运行时、操作系统、盘符、生产程序集 MVID、每次耗时、分配和检查点次数。正式对照在同一卷顺序执行 `before → after → after → before`，每次 3 个样本，汇总每个候选 6 个样本的中位数；不与构建、测试并行运行，不设置耗时百分比门禁。

本次固定基线、原始样本和汇总：`artifacts/c02-io-2026-10-23`。结果及未改善场景见 [C02 报告](../../docs/quality/c02-io-performance.md)。
