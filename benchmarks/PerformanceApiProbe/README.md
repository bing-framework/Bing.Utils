# 性能与 API 专项探针

此探针只测量本轮改变的路径，不是发布耗时门禁。每组预热 2 次、采样 5 次，输入种子 42。分配量使用 `GC.GetTotalAllocatedBytes(true)` 统计独立探针进程的全部线程，包含调度与探针自身开销；不等于存活内存或峰值工作集。耗时受本机调度、JIT、磁盘缓存影响，应同时查看全部样本。

本次固定基线在 `artifacts/performance-api-2026-10-23/baseline/`：保存实施前实际工作区源码、构建产物与 manifest，不能用旧 HEAD 或当前候选覆盖。`before.json` 和 `after.json` 含加载程序集身份、运行时与原始采样。新增 API 的历史比较标记 `NOT_APPLICABLE`。

在仓库根目录运行，先完成 Release 构建；不要与测试或其他基准并行执行：

```powershell
$root = (Get-Location).Path
$evidence = "$root/artifacts/performance-api-2026-10-23"
dotnet build benchmarks/PerformanceApiProbe/PerformanceApiProbe.csproj -c Release `
    "-p:CandidateDirectory=$evidence/baseline/net8.0" -o "$evidence/probe-before"
dotnet "$evidence/probe-before/PerformanceApiProbe.dll" "$evidence/before.json"

dotnet build benchmarks/PerformanceApiProbe/PerformanceApiProbe.csproj -c Release `
    -p:ProbeCandidate=true "-p:CandidateDirectory=$root/output/release/net8.0" -o "$evidence/probe-after"
dotnet "$evidence/probe-after/PerformanceApiProbe.dll" "$evidence/after.json"
```

已有基线数据应复用，上述命令用于说明首次采样方法，不应每次修改重新运行 before。后续独立队列观测使用相同构建参数及不同输出目录，调用时增加 `--queue-probe`，结果文件另存为 `before-queue.json` / `after-queue.json`。队列定义为“已经从源领取、尚未进入操作委托”的元素数，不是线程池全局队列；该探针不参与原有耗时/分配对照，避免计数器开销改变测量方法。

ZIP 测量发生在本地临时目录，第一次预热新建文件，正式采样覆盖已有目标；Windows 当前路径为文件替换，旧路径为复制。平台不支持替换的回退由独立回归测试覆盖。HTTP 使用本地 `HttpResponse` 和计数字节流比较 JSON 写入资源特征，不代表真实网络吞吐。

随机字符串密码学随机输入不固定，但长度、调用次数固定；压缩及 ZIP 测试数据固定种子。随机填充次数、缓存构造次数和固定工作任务边界另由确定性测试及实现检查支持，不从耗时推算。
