# HTTP 优化性能记录（2026-10-04）

基线为提交 `b87b706126eaa1f63001e2e89ca1a814b52b4b48`，表中候选为前阶段 HTTP 优化的测量版本。两者在同一台 Windows 主机以 .NET 8.0.30、Release 配置执行相同的上传/JSON 基准程序；当时的程序 SHA-256 均为 `06A72B418FFB63A5680F78C3508F0F1BCB4BD96EE685E70549A407E35CE5A0AD`。基线/表中候选 HTTP 程序集 SHA-256 分别为 `A9D862B3293281F44CDC37D3057D22A2CF45BD2D7DEF855FA3A0AA2584ACB136` 和 `C97417819F2D99B8CE74AEF536EC5262BBEBE7D21D74C310BA34432D228ACFD5`。前阶段候选更新涉及正常响应回调路径，因此重新测量候选；未修改的基线数据按固定基线复用。当前[基准程序](../../benchmarks/Bing.Utils.Http.Benchmark/Program.cs)已增加测量模式，源码哈希与前阶段版本不同。

上传负载使用 1 MiB 或 64 MiB 本地文件，通过真实 `HttpRequest<string>.FileContent(path).GetResultAsync()` 进入内存 Handler；Handler 将请求正文复制到 `Stream.Null`。JSON 负载在同一个请求对象上解析 1000 个小 JSON 响应。每种组合各运行 3 次，表格为 3 次中位数；首次迭代包含 JIT 成本。分配量来自 `GC.GetTotalAllocatedBytes`；堆大小每 5 ms 采样，受 GC 与采样时点影响，仅作辅助观察。

| 负载 | 请求并发 | 基线耗时 ms | 候选耗时 ms | 基线分配 MiB | 候选分配 MiB | 实际 Handler 最大并行度（基线/候选） |
|---|---:|---:|---:|---:|---:|---|
| 上传 1 MiB | 1 | 4.4 | 2.0 | 3.02 | 0.14 | 1 / 1 |
| 上传 1 MiB | 8 | 6.3 | 1.9 | 23.07 | 0.13 | 2 / 8 |
| 上传 64 MiB | 1 | 182.4 | 40.4 | 192.02 | 0.15 | 1 / 1 |
| 上传 64 MiB | 8 | 399.3 | 45.0 | 1535.09 | 0.12 | 2–4 / 8 |
| JSON 解析 1000 次 | 1 | 407.4 | 56.8 | 43.73 | 6.19 | 1 / 1 |

8 路场景的进入 Handler 前最大排队数在两边均为 8，表示调度时最多有 8 个请求尚未进入 Handler；它不是传输层队列深度。实际 Handler 并行度不同，因此耗时变化也包含并行度差异；分配量更直接反映移除整包上传缓冲和池化响应读取缓冲的效果。本地 Handler 不代表真实网络吞吐或生产机器资源门限。

原始样本格式：`模式,文件MiB,请求并发,重复序号,耗时ms,分配MiB,采样堆峰值MiB,最大Handler并行度,最大进入Handler前排队数`。

```csv
variant,mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
baseline,upload,1,1,1,261.4,3.09,3.44,1,1
baseline,upload,1,1,2,4.4,3.02,0.51,1,1
baseline,upload,1,1,3,3.0,2.89,0.64,1,1
baseline,upload,1,8,1,354.7,24.21,10.70,2,8
baseline,upload,1,8,2,6.2,23.07,22.83,2,8
baseline,upload,1,8,3,6.3,23.06,1.41,2,8
baseline,upload,64,1,1,456.8,192.10,128.52,1,1
baseline,upload,64,1,2,181.2,192.02,128.65,1,1
baseline,upload,64,1,3,182.4,192.02,128.78,1,1
baseline,upload,64,8,1,617.8,1536.18,1089.49,4,8
baseline,upload,64,8,2,399.3,1535.08,1281.47,3,8
baseline,upload,64,8,3,383.7,1535.09,1281.49,2,8
baseline,json,0,1,1,692.2,43.89,14.90,1,0
baseline,json,0,1,2,407.4,43.73,14.82,1,0
baseline,json,0,1,3,401.9,43.73,15.10,1,0
current,upload,1,1,1,75.2,0.23,0.58,1,1
current,upload,1,1,2,2.0,0.14,0.52,1,1
current,upload,1,1,3,1.1,0.02,0.65,1,1
current,upload,1,8,1,92.3,1.37,1.62,8,8
current,upload,1,8,2,1.9,0.13,1.56,8,8
current,upload,1,8,3,1.3,0.11,1.59,8,8
current,upload,64,1,1,153.9,0.23,0.59,1,1
current,upload,64,1,2,40.4,0.15,0.67,1,1
current,upload,64,1,3,40.0,0.02,0.67,1,1
current,upload,64,8,1,134.6,1.25,1.65,8,8
current,upload,64,8,2,45.0,0.12,1.59,8,8
current,upload,64,8,3,45.0,0.12,1.60,8,8
current,json,0,1,1,123.6,6.32,5.17,1,0
current,json,0,1,2,56.8,6.19,6.66,1,0
current,json,0,1,3,36.6,6.06,5.11,1,0
```

## 本轮拦截器路径复测

本轮修复连续替换请求正文时的释放逻辑，影响启用拦截器的发送路径。使用当前基准程序的 `interceptor` 模式（程序 SHA-256 `1BAB16BE68783E0D027DBF285F7212627B29D3C9E353407F0387AC047DC1AE2F`），在 Windows、.NET 8.0.30、Release、本地内存 Handler 上对 1000 次小 JSON 请求重复 3 次；当前 HTTP 程序集 SHA-256 为 `B7660AFF4E9CB89EB2DCA6CDB239D03496222E8D896A9E034BEC2C38099FF21E`。该模式的拦截器不替换正文，因而只观察正常拦截器路径的开销。三次耗时为 152.0、49.4、43.4 ms，分配为 6.65、6.52、6.40 MiB；中位数为 49.4 ms 和 6.52 MiB。首轮包含 JIT 成本。没有同负载的旧版拦截器或 SKIT 数据，因此这些数值只记录当前版本，不用于比较优劣或设置性能门槛。

## 完整响应原文开关测量

使用上节相同的基准程序和 HTTP 程序集，对 1000 次小 JSON 响应分别运行普通 `GetResultAsync`（`json`）、`GetResponseAsync` 且不保留原文（`metadata`）、`GetResponseAsync(includeRawContent: true)`（`raw`）。每种模式独立运行 3 次，首轮包含 JIT 成本；下表为 3 次中位数。各模式分开运行，耗时包含运行间波动，数据只用于记录当前负载的资源开销。

| 模式 | 耗时中位数 ms | 分配中位数 MiB | Handler 最大并行度 |
|---|---:|---:|---:|
| json | 42.2 | 6.19 | 1 |
| metadata | 50.1 | 7.10 | 1 |
| raw | 50.4 | 7.10 | 1 |

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
json,0,1,1,130.7,6.32,5.18,1,0
json,0,1,2,42.2,6.19,6.49,1,0
json,0,1,3,35.0,6.06,5.19,1,0
metadata,0,1,1,140.4,7.23,7.25,1,0
metadata,0,1,2,50.1,7.10,7.53,1,0
metadata,0,1,3,40.1,6.97,5.47,1,0
raw,0,1,1,144.1,7.23,6.67,1,0
raw,0,1,2,50.4,7.10,6.94,1,0
raw,0,1,3,44.9,7.10,6.71,1,0
```

## 响应头快照单次复制复测

本轮将完整响应结果的头快照改为直接从仍有效的响应复制一次，并保留公开构造函数对传入字典的防御性复制。使用相同基准程序（SHA-256 `1BAB16BE68783E0D027DBF285F7212627B29D3C9E353407F0387AC047DC1AE2F`）在 Windows、.NET 8.0.30、Release、本地内存 Handler 上复测 1000 次小 JSON 响应。HTTP 程序集 SHA-256 为 `B5DB2B0051D498F972AC9788A32634BF456756CBE7CA2C34ECB45F4C75229900`，核心程序集 SHA-256 为 `DF9598D44FE60ACA6CE4993C91C01710B2DC60911A6C89A2D02C458F2D04BB3E`。各模式独立运行 3 次，首轮包含 JIT 成本。

| 模式 | 耗时中位数 ms | 分配中位数 MiB | 上一候选分配中位数 MiB |
|---|---:|---:|---:|
| json | 39.4 | 6.19 | 6.19 |
| metadata | 48.3 | 6.66 | 7.10 |
| raw | 48.2 | 6.66 | 7.10 |

`metadata` 和 `raw` 的分配在该负载下均降低约 0.44 MiB/1000 次；耗时受独立运行和 JIT 波动影响，不据此断言速度改善。该本地 Handler 测量不代表真实网络吞吐，也没有同负载 SKIT 数据。

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
metadata,0,1,1,146.6,6.79,6.79,1,0
metadata,0,1,2,48.3,6.66,5.55,1,0
metadata,0,1,3,44.6,6.66,7.34,1,0
raw,0,1,1,140.7,6.79,6.08,1,0
raw,0,1,2,48.2,6.66,6.72,1,0
raw,0,1,3,43.1,6.53,6.68,1,0
json,0,1,1,127.7,6.32,6.31,1,0
json,0,1,2,39.4,6.19,6.18,1,0
json,0,1,3,31.5,6.06,5.52,1,0
```

## 发送方法与完成回调收尾复测

本轮修复发送尝试的重试资格判定，并统一字节读取及下载的完成回调异常优先级。仍使用相同基准程序（SHA-256 `1BAB16BE68783E0D027DBF285F7212627B29D3C9E353407F0387AC047DC1AE2F`），在 Windows、.NET 8.0.30、Release、本地内存 Handler 上独立执行各模式 3 次，首轮含 JIT 成本。实际运行的 HTTP 程序集 SHA-256 为 `0E2D2E2EA0A098F7748F6A0D7C3AA7907950B555FF8F0D4D0AB0CB7983BD0D13`，核心程序集为 `DF9598D44FE60ACA6CE4993C91C01710B2DC60911A6C89A2D02C458F2D04BB3E`。

| 模式 | 本轮耗时中位数 ms | 本轮分配中位数 MiB | 前一候选分配中位数 MiB |
|---|---:|---:|---:|
| json（1000 次） | 41.4 | 6.19 | 6.19 |
| metadata（1000 次） | 51.2 | 6.66 | 6.66 |
| raw（1000 次） | 45.6 | 6.66 | 6.66 |
| interceptor（1000 次） | 45.1 | 6.52 | 6.52 |
| upload（1 MiB，单请求） | 2.3 | 0.15 | — |

前四个模式与各自上一候选相比，分配量未出现明显回退。历史上传数据使用过不同版本的基准程序，此处只记录本轮上传样本。各模式独立运行，耗时受运行波动影响，不据此断言速度变化。普通拦截器模式不替换正文；基准不覆盖异常路径、重试路径或真实网络，也没有与 SKIT 同负载的比较。

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
json,0,1,1,127.6,6.32,5.04,1,0
json,0,1,2,41.4,6.19,4.61,1,0
json,0,1,3,34.1,6.06,5.97,1,0
metadata,0,1,1,143.3,6.79,6.64,1,0
metadata,0,1,2,51.2,6.66,6.96,1,0
metadata,0,1,3,45.6,6.66,6.54,1,0
raw,0,1,1,127.6,6.79,6.93,1,0
raw,0,1,2,45.6,6.66,6.01,1,0
raw,0,1,3,39.4,6.53,6.52,1,0
interceptor,0,1,1,134.8,6.65,6.34,1,0
interceptor,0,1,2,45.1,6.52,5.29,1,0
interceptor,0,1,3,37.6,6.40,5.82,1,0
upload,1,1,1,79.3,0.24,0.58,1,1
upload,1,1,2,2.3,0.15,0.52,1,1
upload,1,1,3,1.2,0.14,0.66,1,1
```

## HEAD/204 无正文处理后的读取路径复测

公共读取路径新增 HEAD/204 无正文判断后，仍使用相同基准程序（SHA-256 `1BAB16BE68783E0D027DBF285F7212627B29D3C9E353407F0387AC047DC1AE2F`）在 Windows、.NET 8.0.30、Release、本地内存 Handler 上独立运行 1000 次 JSON 正文请求，各模式重复 3 次。实际运行的 HTTP 程序集 SHA-256 为 `ADD3F64C62BE2001C9B3B7DEB2A64850CF09B177FDFFCF50CACD26B4E3266997`，核心程序集为 `DF9598D44FE60ACA6CE4993C91C01710B2DC60911A6C89A2D02C458F2D04BB3E`。

| 模式 | 耗时中位数 ms | 分配中位数 MiB | 上一候选分配中位数 MiB |
|---|---:|---:|---:|
| json | 40.8 | 6.19 | 6.19 |
| metadata | 48.0 | 6.66 | 6.66 |
| raw | 47.7 | 6.66 | 6.66 |

这些数据只观察正常带正文路径；HEAD/204 无正文行为由单元测试验证，本基准不测量其吞吐。首轮包含 JIT 成本，耗时受运行波动影响。

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
json,0,1,1,124.8,6.32,6.48,1,0
json,0,1,2,40.8,6.19,6.13,1,0
json,0,1,3,32.9,6.06,5.21,1,0
metadata,0,1,1,135.0,6.79,5.84,1,0
metadata,0,1,2,48.0,6.66,6.54,1,0
metadata,0,1,3,39.7,6.53,4.73,1,0
raw,0,1,1,142.1,6.79,6.43,1,0
raw,0,1,2,47.7,6.66,6.77,1,0
raw,0,1,3,38.6,6.53,4.93,1,0
```

## 下载结果与完成观测的小型分配测量

本轮在 Windows、.NET 8.0.30、Release 和本地内存 Handler 上顺序运行三个模式，各进行 1000 次调用、重复 3 轮。首轮包含 JIT 成本，不以这些耗时数据判断真实网络吞吐。`json` 为未启用观测的普通响应，`observed` 为相同负载且完成回调为空操作，`download-result` 为同一短正文写入新建内存流并取得结果；下载与 JSON 的工作量不同，不直接比较两者性能。基准程序 SHA-256 为 `B049C4E4DB8945E165FD3FF48D4E865F07D8654F043BA81DA6CB1AA843B2981F`，HTTP 程序集 SHA-256 为 `C46AD56843F05C445AC2B4A033E8891D7C3DDA2301CCAB3C5A836075ACE07660`，核心程序集 SHA-256 为 `8C2FF9D3314549EC0C9612E6C65B9C544940756E7FA27ADD8BEC4BD1EC027A62`。

| 模式 | 耗时中位数 ms | 分配中位数 MiB / 1000 次 |
|---|---:|---:|
| json | 43.8 | 6.29 |
| observed | 48.3 | 6.54 |
| download-result | 27.5 | 3.07 |

完成观测比本轮相同 JSON 负载多分配约 0.25 MiB / 1000 次。禁用时不创建完成信息或计时器。结果仅为当前候选的局部证据，未设置性能门槛，也未与 SKIT 同负载比较。

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
json,0,1,1,137.1,6.42,5.72,1,0
json,0,1,2,43.8,6.29,5.08,1,0
json,0,1,3,34.0,6.17,6.42,1,0
observed,0,1,1,147.6,6.67,5.98,1,0
observed,0,1,2,48.3,6.54,6.72,1,0
observed,0,1,3,40.1,6.41,7.01,1,0
download-result,0,1,1,83.4,3.14,1.97,1,0
download-result,0,1,2,27.5,3.07,3.11,1,0
download-result,0,1,3,23.8,2.94,1.78,1,0
```

## WriteAsync 观测及清理异常优先级收尾复测

本轮修正配置正文回调的 WriteAsync 落盘观测，并加入主异常优先的资源清理逻辑。在相同 Windows、.NET 8.0.30、Release、本地 Handler 环境顺序复测现有小负载。HTTP 程序集 SHA-256 为 `A37D0345669A8ED2E5C38D4D48F40E36784CDAC0B134B371903E346EA5B70F0E`，核心程序集为 `7708C1D04EAA572A7B3DEF66E68CE75CA08D79B14977CBF17E56371389FDCD59`；基准程序仍为 `B049C4E4DB8945E165FD3FF48D4E865F07D8654F043BA81DA6CB1AA843B2981F`。

| 模式 | 耗时中位数 ms | 分配中位数 MiB | 负载 |
|---|---:|---:|---|
| json | 44.5 | 6.29 | 1000 次 |
| observed | 48.0 | 6.54 | 1000 次 |
| download-result | 28.4 | 3.07 | 1000 次 |
| upload | 2.4 | 0.14 | 1 MiB 单请求 |

前三项同负载分配中位数与上一候选一致。上传仅记录局部样本。基准不覆盖 WriteAsync 本地文件写入或异常清理路径，这些行为由回归测试验证；不据此宣称网络吞吐提升或优于 SKIT，不设置性能门槛。

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
json,0,1,1,141.9,6.42,6.32,1,0
json,0,1,2,44.5,6.29,5.31,1,0
json,0,1,3,37.2,6.17,6.05,1,0
observed,0,1,1,151.2,6.67,6.44,1,0
observed,0,1,2,48.0,6.54,5.37,1,0
observed,0,1,3,43.0,6.41,5.29,1,0
download-result,0,1,1,88.3,3.14,3.44,1,0
download-result,0,1,2,28.4,3.07,2.61,1,0
download-result,0,1,3,24.1,2.94,3.02,1,0
upload,1,1,1,82.4,0.23,0.59,1,1
upload,1,1,2,2.4,0.02,0.52,1,1
upload,1,1,3,1.2,0.14,0.52,1,1
```

## 二进制正文、下载进度与流式 JSON（2026-10-05）

本轮 Windows、.NET 8.0.30、Release、本地 Handler 小型测量；每个模式顺序运行 3 轮。短 JSON 是相同的 `{"name":"benchmark"}`，普通与流式各 1000 次；大 JSON 在计时外预构造 1,048,576 字符的 name 字段，UTF-8 正文 1,048,587 字节，两种解析各 100 次并验证结果字段长度。下载两模式均将相同短正文写入新建 MemoryStream；进度模式注册空同步委托，包含初始和 EOF 通知。

候选身份：基准程序集 SHA-256 `7684C5F42811EDB8047B9CEB54E9B07DD08003632E996B25BC80903A9644A14C`；HTTP 程序集 `845119DF30C7F1DDD75FCAEAE57A6DF363DC71644DC09489DDECAD5E47095FC1`；核心程序集 `38F0493CDD47022DEE317B2CF3AD9D8CA182D2B334B1587866EF215EB09635DA`。实现与验收范围见[三项能力计划](../superpowers/plans/2026-10-05-http-binary-progress-streaming-json.md)，可用 API 见[HTTP 模块](../modules/http.md)。

| 模式 | 次数 | 耗时中位数 ms | 分配中位数 MiB |
|---|---:|---:|---:|
| json | 1000 | 42.7 | 6.45 |
| json-stream | 1000 | 34.5 | 2.15 |
| json-large | 100 | 737.8 | 1190.89 |
| json-large-stream | 100 | 185.6 | 204.35 |
| download-result | 1000 | 31.4 | 3.24 |
| download-progress | 1000 | 29.4 | 3.43 |

本候选同负载解析中流式路径减少了分配；进度短负载增加约 0.19 MiB/1000 次。普通 JSON 当前分配中位数为 6.45 MiB，上一阶段记录为 6.29 MiB，不能将旧阶段视为当前候选成绩。首轮诊断定位并移除了正文工厂检查对普通路径引入的闭包，表格和下列原始数据均为修正后的确认运行。

分配包括请求管线与结果对象，但不包括计时外的大正文构造；最终 DTO 字符串和解析器内部缓冲仍需要内存。首轮包含 JIT，采样器和 GC 带来波动；这些数据不证明真实网络吞吐，不覆盖二进制重放、进度回调实际日志开销或最终文件提交。正确性由相应用例验证；不新增性能门槛、不宣称优于 SKIT。

复现：构建基准 Release 后运行 `dotnet benchmarks/Bing.Utils.Http.Benchmark/bin/Release/net8.0/Bing.Utils.Http.Benchmark.dll <mode> <mib> 1 3`；短模式 mib=0，大模式 mib=1。

```csv
mode,mib,concurrency,run,ms,allocated_mib,heap_mib,active,queued
json,0,1,1,132.7,6.58,6.59,1,0
json,0,1,2,42.7,6.45,5.96,1,0
json,0,1,3,35.6,6.33,4.49,1,0
json-stream,0,1,1,111.8,2.28,1.76,1,0
json-stream,0,1,2,34.5,2.15,1.91,1,0
json-stream,0,1,3,29.0,2.13,1.84,1,0
json-large,1,1,1,906.2,1190.90,71.05,1,0
json-large,1,1,2,737.8,1190.89,52.34,1,0
json-large,1,1,3,710.4,1190.87,54.36,1,0
json-large-stream,1,1,1,260.8,204.48,23.51,1,0
json-large-stream,1,1,2,156.8,204.35,23.46,1,0
json-large-stream,1,1,3,185.6,204.34,43.49,1,0
download-result,0,1,1,82.4,3.31,3.30,1,0
download-result,0,1,2,31.4,3.24,2.32,1,0
download-result,0,1,3,25.8,3.11,2.49,1,0
download-progress,0,1,1,83.0,3.50,2.51,1,0
download-progress,0,1,2,29.4,3.43,3.65,1,0
download-progress,0,1,3,25.8,3.30,2.08,1,0
```


## 普通文本响应缓冲复用复测（2026-10-05）

本轮使用 Windows、.NET 8.0.30、Release 和本地内存 Handler，对大 JSON 正文的 GetResponseAsync 路径分别测量 metadata-large 和 raw-large。每种模式 100 次请求、重复 3 轮，首轮包含 JIT。实现将文本路径改为直接从 MemoryStream 解码，不改变字节数组、下载和自定义响应回调路径。

| 模式 | 次数 | 耗时中位数 ms | 分配中位数 MiB |
| --- | ---: | ---: | ---: |
| metadata-large | 100 | 697.3 | 1090.83 |
| raw-large | 100 | 676.7 | 1090.92 |

当前候选程序集身份：Bing.Utils.Http.dll SHA-256 DFE95521E4A1B1159C985090173E9B0774CCA876DD5CBEBF9EB112EB48671D96；Bing.Utils.dll SHA-256 D03C922831512F7C6A3AEE46AE005BA86D51D928BB904B91D3262F4DCBFE683A；基准程序 SHA-256 736807A67315937D445C236AC0DCA0509738DC7EC31D672824DA4BBD2B0EE6F3。本次没有同负载、同环境的修改前快照，数据仅作当前候选记录，不用于宣称前后性能提升。
