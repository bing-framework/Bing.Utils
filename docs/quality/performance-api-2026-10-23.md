# 性能与 API 易用性实施验收（2026-10-23）

## 结论

批准计划的 8 组改进已实施，独立审查发现的两项边界遗漏也已修复。最终 Release 解决方案构建成功；23 个 net8 测试程序集共 **12,777 通过、0 失败、18 跳过**。跳过项为缺少外部配置的既有 FTP 集成测试。`OPEN_ACTIONABLE = 0`，停止本轮代码修复。

分配与重复构造、调度保留量明显下降，但本机耗时不是全面改善。ZIP 替换比原复制路径更慢，冷正则多键场景及不可压缩大数据也有耗时增加，详见原始对照。未设或放宽耗时百分比门禁，未覆盖固定基线。新增流式 JSON 接口直接序列化到正文，但单个超大字符串仍可能使序列化器分配大缓冲区，不能称为恒定内存。

GitHub Actions 的 Windows/Linux/macOS 外部执行未触发。既有 ImageSharp 发布阻碍和历史图像、反射性能专项保持原状态；本地代码验收不代表可以发布。

## 基线、影响范围与证据身份

- 基线：实施前实际工作区，而非单独的 HEAD `9258eab3e9bf3e1916c2244b0a845413bb946903`。已有 F01—F10、CI、超时和限额修复包含在基线中。源文件及当时构建产物保存在 `artifacts/performance-api-2026-10-23/baseline/`，`manifest.json` 保存逐文件身份和 SDK 环境。
- 环境：Windows 11 / x64，SDK 8.0.425，性能运行时 .NET 8.0.31；数据种子 42，预热 2 次、正式样本 5 次。候选源文件身份见 `candidate-manifest.json`，运行程序集身份见 `before.json` / `after.json`。
- ChangedProjects：Bing.Utils、Bing.Utils.IdUtils、Bing.Utils.Http 及各自单元/集成测试。新增公开成员均为已批准的可选入口，不向 `IHttpRequest<TResult>` 增加必需成员，不移除旧入口。
- ChangedRuntimePaths：集合调度/取消/清理，文件读取资源清理，GZip 复制与限额，正则初始化与统计，分页构造、ZIP 提交、随机采样，HTTP 默认限额/流式写入及分片响应读取。
- ChangedTFMs / Dependencies / Version：无。构建依赖消费者，跨框架验证现有 netstandard2.0 分支；本轮没有包版本升级。
- ChangedBuildPackaging：扩展已有 netstandard 回归脚本与 CI 步骤描述；未改变打包策略。ChangedBenchmarkHarness：新增专项探针。RiskLevel：HIGH（取消、资源生命周期与跨项目 API），按定向 → 项目 → 跨框架 → 完整解决方案验证。
- 原始日志、TRX、源快照及 JSON 位于 `artifacts/performance-api-2026-10-23/`（本地被忽略目录）。仓库保留本报告中的数据、运行方法及测试代码；归档交付时应同时保留原始证据目录。

## 实现与契约

| 编号 | 实现 | 验收结果 |
|---|---|---|
| PA01 | ForEach 共享调度内核；预取消不枚举；有限并发最多保留 D 个工作任务、逐步枚举；取消/拒绝继续停止领取并等待已启动操作；新入口向委托传递令牌 | CLOSED；保留有限聚合/无限原异常形态，释放异常不覆盖取消 |
| PA02 | 文件读入内存流预检查取消，取消单独传播，失败释放未返回流 | CLOSED；其他失败仍为 null，成功位置仍在末尾 |
| PA03 | CompressTo / DecompressTo 同步异步接口；原 Compress(Stream) 移除完整输入中间数组 | CLOSED；当前位置、短读、不可定位、空 GZip、精确上限、部分输出、失败/预取消资源归属已测试 |
| PA04 | 缓存项共享 Lazy；热路径无创建锁/闭包分配；冷插入协调淘汰；失败精确移除；Clear 隔离旧统计代际 | CLOSED；同键构造一次、不同键并行、失败重试及替换竞态已测试 |
| PA05 | 公开小页构造；ZIP 新建移动/覆盖替换，仅不支持替换时复制回退；随机源最多 256 字节分块拒绝采样 | CLOSED；默认大页不变，旧文件保护/回退及 62 字符映射/批量调用已测试 |
| PA06 | GetBytes 别名；HttpClient 实例弱引用默认限额；请求继承/显式数值/无限制三态；每次执行固定快照 | CLOSED；容器/客户端隔离、默认/覆盖/恢复、缓冲/流式/下载及分片响应已测试 |
| PA07 | WriteJsonStreamAsync 直接使用 System.Text.Json 写响应；合并请求中止与显式取消 | CLOSED；选项、字符串 JSON 语义、双取消、响应体保持打开已测试 |
| PA08 | 更新手册及 API 指南，写明流位置/归属/失败、HTTP 选择、Cookie 连接成本、安全限额和随机串契约 | CLOSED；旧入口语义与新入口推荐明确区分 |

调用示例见 [流式与 API 选择指南](../reference/performance-api-guide.md)。测试按生产模块放置，没有无关迁移。

### 独立审查闭环

| Finding | 原触发条件 | 最终处理 |
|---|---|---|
| R01 / CLOSED | 操作已取消，枚举器 Dispose 再抛错，盖过调用方令牌取消 | 显式收尾后结算：外部取消优先，其次枚举/执行错误、操作错误、单独释放错误。null/1 两种并发验证三类失败组合 |
| R02 / CLOSED | 分片 Range 正文直接 CopyToAsync 到内存，绕过默认/请求响应上限 | 改用同一限额复制路径并释放未返回流；已知/未知长度 × 默认/请求覆盖四种集成回归。保持未完成状态及 failureObserver 契约，旧目标不提交 |

两项均完成复核，独立审查 PASS。一次主包完整测试暴露正则统计测试未加入既有串行集合的问题，已修复测试隔离；最终完整套件无该失败。

## 验证

| 检查 | 最终结果 | 证据 |
|---|---|---|
| Release 全解决方案、所有现有 TFM 构建 | 0 错误，1612 条编译/分析及依赖警告，未当作发布门禁清零 | `final-solution-build.log` |
| net8 全解决方案 | 12,777 通过，0 失败，18 FTP 跳过 | `final-reviewed-tests/`、`test-summary.json` |
| 受影响项目最终完整测试 | 主包 7110、IdUtils 323、HTTP 1880、HTTP 集成 119，均通过 | 同上，各程序集 TRX |
| 实际加载 netstandard2.0 DLL | 12 通过，包含 ZIP、新流式接口、小页、文件取消、调度与正则 | `final-netstandard-loader.log` |
| .NET Core 3.1 实际运行 | 首轮主包 43、IdUtils 14、HTTP 85、HTTP 集成 8；复核修改后主包调度 18、HTTP 集成 12 全通过 | `*-net31.log`、`reviewed-*-net31.log` |
| API 消费者 | 旧接口实现、别名、null/default、命名参数及新可选能力经跨 TFM 编译；真实 DI 请求链路覆盖三态默认值 | 单元与集成测试源码及上述构建 |
| 性能对照 | 固定旧产物一组、最终候选一组；全部样本保留 | `before.json`、`after.json` |
| 外部 CI | BLOCKED_EXTERNAL，本地未触发远程运行 | 既有跨平台矩阵保留，netstandard 脚本覆盖已扩展 |

性能行为探针在固定旧入口上验证：预取消 ForEach 原调用 3 次且完成，现调用 0 次并取消；并发度 1、处理器返回 false 时原调用 3 次，现 1 次；取消文件读取原返回 null，现传播取消。这些是实际旧产物对照；全新 API 的旧版本对照为 NOT_APPLICABLE，不伪造历史能力。

## 性能与资源结果

分配计数为 `GC.GetTotalAllocatedBytes(true)`，覆盖独立进程全部线程，包含探针开销；不是峰值存活内存。下表为五次样本中位数，毫秒与字节分开记录。未变化的 HTTP 缓冲控制项也存在耗时波动，因此不把微秒差异或某次吞吐推断为普遍保证。运行方法见 [PerformanceApiProbe](../../benchmarks/PerformanceApiProbe/README.md)。

| 场景 | 旧耗时 ms | 新耗时 ms | 旧分配 B | 新分配 B |
|---|---:|---:|---:|---:|
| `compress-1024-repeated` | 0.033 | 0.040 | 12,040 | 14,000 |
| `compress-1024-random` | 0.098 | 0.085 | 14,128 | 16,088 |
| `compress-1048576-repeated` | 1.674 | 0.542 | 2,109,248 | 16,104 |
| `compress-1048576-random` | 34.688 | 40.990 | 7,342,080 | 3,154,600 |
| `compress-8388608-repeated` | 9.489 | 4.226 | 16,807,480 | 34,552 |
| `compress-8388608-random` | 329.121 | 463.264 | 58,724,616 | 25,194,928 |
| `foreach-1000-1` | 4.855 | 3.481 | 553,704 | 217,704 |
| `foreach-1000-8` | 1.444 | 1.003 | 468,488 | 211,344 |
| `foreach-1000-16` | 1.202 | 0.840 | 460,792 | 225,464 |
| `foreach-10000-1` | 53.146 | 28.993 | 5,636,056 | 2,160,024 |
| `foreach-10000-8` | 12.526 | 6.013 | 4,660,232 | 2,160,672 |
| `foreach-10000-16` | 11.312 | 5.608 | 4,685,520 | 2,168,904 |
| `regex-hot-10000` | 3.683 | 2.028 | 1,440,960 | 960 |
| `regex-cold-same-key-16` | 4.492 | 3.307 | 4,870,640 | 334,184 |
| `regex-many-1100` | 71.835 | 100.819 | 8,207,656 | 8,365,304 |
| `nonce-16-1000` | 1.911 | 1.030 | 144,960 | 152,960 |
| `nonce-64-1000` | 15.041 | 2.541 | 336,960 | 392,960 |
| `nonce-1024-1000` | 97.084 | 17.067 | 4,176,960 | 4,424,960 |
| `zip-overwrite-1-8388608` | 48.817 | 109.322 | 14,128 | 13,552 |
| `zip-overwrite-128-1024` | 266.787 | 1073.283 | 1,546,960 | 1,546,960 |
| `http-json-buffered-1MiB` | 3.084 | 4.529 | 4,217,976 | 4,217,640 |

### 新能力资源特征（历史对照 NOT_APPLICABLE）

| 新入口场景 | 耗时 ms | 分配 B |
|---|---:|---:|
| `compress-to-1024-False` | 0.031 | 9,480 |
| `decompress-to-1024-False` | 0.030 | 13,616 |
| `compress-to-1024-True` | 0.108 | 9,480 |
| `decompress-to-1024-True` | 0.017 | 13,280 |
| `compress-to-1048576-False` | 0.458 | 9,480 |
| `decompress-to-1048576-False` | 2.332 | 13,616 |
| `compress-to-1048576-True` | 41.766 | 9,480 |
| `decompress-to-1048576-True` | 0.447 | 13,616 |
| `compress-to-8388608-False` | 4.368 | 9,480 |
| `decompress-to-8388608-False` | 25.375 | 13,616 |
| `compress-to-8388608-True` | 449.656 | 9,480 |
| `decompress-to-8388608-True` | 3.293 | 13,616 |
| `http-json-streamed-1MiB` | 0.692 | 4,211,848 |
| `large-stream-small-write-page-65536` | 0.080 | 74,848 |
| `large-stream-small-write-page-1048576` | 0.109 | 1,057,888 |

### 调度与操作次数

独立资源探针记录已领取但未进入委托的元素数，不是线程池全局队列；不将这些计数器加入耗时基准。所有场景最终 pending 均为 0。

| 元素数 / 请求并发 | 旧实际最大并发 | 新实际最大并发 | 旧最大待启动数 | 新最大待启动数 |
|---|---:|---:|---:|---:|
| 1000 / 1 | 1 | 1 | 1000 | 1 |
| 1000 / 8 | 8 | 8 | 1000 | 8 |
| 1000 / 16 | 15 | 16 | 1000 | 16 |
| 10000 / 1 | 1 | 1 | 10000 | 1 |
| 10000 / 8 | 8 | 8 | 10000 | 8 |
| 10000 / 16 | 11 | 15 | 10000 | 10 |

- 同键冷正则 16 路：旧统计 Miss=16、Hit=0；新统计 Miss=1、Hit=15。内部确定性工厂测试确认新项仅构造 1 次；清空/移除后可重新构造。
- 受限调度的工作 Task 数由每元素一次变为至多并发度 D 次；操作委托自身的异步状态机分配仍与元素数相关，不能称为总分配 O(D)。未配置并发上限仍保持无限语义。
- 随机源块最多 256 字节。完整映射、拒绝区间和跨块边界用确定性随机源测试验证；不把不可复现的安全随机字节变成固定种子。用少量固定缓冲分配换取显著减少随机源填充调用，输出字符和结果字符串仍按长度分配。
- ZIP 正式样本均覆盖已存在目标：旧版本 File.Copy，新版本 Windows File.Replace。预热的新目标用 Move；不支持替换的复制回退由测试接缝验证，本机未声称进入回退。本次实测替换耗时较高，但避免旧目标先删除和成功路径完整复制，保持计划要求的提交语义。
- 1 MiB HTTP JSON 工作负载是单个大字符串属性和本地计数目标流，不是真实网络。新入口省去应用层完整 JSON 字符串，但序列化器处理大值仍分配约 4 MiB；需控制单值大小，不能据此承诺所有对象树均恒定内存。
- 小页写入只验证显式 64 KiB / 1 MiB 构造；默认约 1 GB 页大小为兼容保留，没有分配 GB 级对象做正确性验证。

## 停止条件及后续边界

IMPLEMENTATION_STATUS=PASS；TEST_STATUS=PASS；PERFORMANCE_EVIDENCE_STATUS=PASS（已测量，无新增百分比阈值）；RESOURCE_EVIDENCE_STATUS=PASS；EXTERNAL_GATE_STATUS=BLOCKED_EXTERNAL；RELEASE_STATUS=BLOCKED（既有 ImageSharp）；OPEN_ACTIONABLE=0。

- Completed：PA01—PA08、R01—R02。本轮 TODO 已完成，不自动继续 Review → Fix。
- Blocked Approval：无，新增 API 和兼容取舍已有用户批准。未擅自增加性能门禁，不需要以新阈值审批阻塞本地交付。
- Blocked External：Windows/Linux/macOS CI、需要配置的 FTP 集成环境。
- Accepted Limitations：流式失败可留部分输出；ZIP 非归档级事务，复制回退非原子；分片上限按响应而非最终文件合计；无限并发仍可保留大量任务；默认大页不变；序列化超大值仍可分配大缓冲；本机部分耗时增大已披露。
- Verified Boundaries：限额包含精确边界，超出不写入额外解压字节；有限调度实际并发不超配置，逐步领取且取消后收尾。
- Deferred：ImageSharp 迁移、框架支持策略及其他历史依赖专项。未变化的图像/反射性能证据按 REUSED_UNCHANGED_SCOPE 处理，不重跑其资源门禁。
- Next Action：STOP。本地没有可执行 finding，不因未运行外部 CI 或发布状态阻塞再次改动代码。未提交、推送、发布或更新性能基线。

### 测量程序集身份（SHA-256）

| 程序集 | 固定旧产物 | 最终候选 |
|---|---|---|
| Bing.Utils | `DB6BE10DA9B395EC4F5F81403E9182FA8BEE0537277D373CAFBA8A2E65DF0EB7` | `AB38DFDE4F56FB5027F4C0F84FA898E92E288CA5F56F2E7CEF7CDDDCF89134A6` |
| Bing.Utils.IdUtils | `FE99C72494302B5E027117D5643132807D1E260D8D8A370EA70F9F9297AA3DDE` | `CF71856DB7D2367474B11B0C8940E56206C556FAFA5663F9A66E98664DCDC174` |
| Bing.Utils.Http | `17BD0FB4F48C3B179A0745D74C6B8A23902B15999127144B2EFD6DEEDE2B00ED` | `5B5BCB4707CB02C3605D64EC49173AB4B40BDDE70E085E8D3CCE087A7E9D74C6` |
