# 能力收敛探针

固定种子 `20261023`，每场景预热一次、采样三次。分配通过 `GC.GetTotalAllocatedBytes(true)` 统计整个进程的线程；计时不含采样前完整 GC。结果记录实际加载 DLL 的 MVID、运行时、系统、操作次数和峰值并发。

基线使用本轮开始时保存的未提交工作区产物。构建时通过 `CandidateDirectory` 指定 DLL，禁止用旧 HEAD 冒充基线。先构建两个探针，再串行运行；性能运行期间不要构建或测试。

```powershell
dotnet build benchmarks/CapabilityRefinementProbe/CapabilityRefinementProbe.csproj -c Release `
  -p:CandidateDirectory=D:/Bing_Framework/Bing.Utils/artifacts/capability-refinement-2026-10-23/baseline/binaries/net8.0 `
  -o artifacts/capability-refinement-2026-10-23/probe-before
dotnet build benchmarks/CapabilityRefinementProbe/CapabilityRefinementProbe.csproj -c Release `
  -p:Refined=true -o artifacts/capability-refinement-2026-10-23/probe-after
dotnet artifacts/capability-refinement-2026-10-23/probe-before/CapabilityRefinementProbe.dll before.json
dotnet artifacts/capability-refinement-2026-10-23/probe-after/CapabilityRefinementProbe.dll after.json
```

场景范围：

- 异常密集调度：1,000/10,000 个范围，degree=1/8/16，固定 Task.Yield 后抛 I/O 错误，记录实际峰值并发。
- 成功持久化：256 个一字节分片、degree=8，有/无每片检查点；记录保存次数和累计状态文件字节。包含文件创建、刷盘和提交，不将差额单独归因于 JSON 序列化。
- 单片写入和合并：8 MiB 固定随机输入，包含摘要、刷盘和文件提交，不代表网络吞吐。
- 默认首写租用：受控池记录请求并在分配前终止，避免分配旧默认 1,024,000,000 字节页面。这个数字是请求容量，不是实际分配或工作集测量。
- 显式页面：64 KiB/256 KiB/1 MiB 写入同一 8 MiB 内容；前后均有等价入口。共享池预热后复用页面，分配数不代表页面保有量。
- 新 v2 流式读写：1,000/10,000 行、int/string 两列，经本地文件写入再逐行读取，不构造全部数据行。新接口的 Before 为 `NOT_APPLICABLE`；总分配随行数增长，不代表同时保留全部对象。

最多进行一次诊断与一次正式确认，保留原始结果。首次诊断中的显式页面只记录候选，正式确认补齐基线，不将缺失基线解释为优化收益。文档中的正式对照只使用确认结果，诊断结果用于说明变化与局限。
