# 验证码审查修复记录

本轮修复依据上一轮用户可见审查，用户已授权修复问题及测试缺口。保留原执行证据，独立复核后记录结论。

后续验证更新（2026-10-29）：[健康 Redis 集成验收](redis-validation.md) 已在真实 Redis 7.2.11 上完成，10/10 通过、0 跳过，临时环境已移除。下文的“10 项跳过”描述保留为修复当时的历史证据；当前仅远端 CI 仍未验证。

| ID | 问题及验收标准 | 当前状态 |
| --- | --- | --- |
| R1 | 前端等待会话完成才生成；初始化慢/失败、连续刷新可恢复且无并发请求 | CLOSED |
| R2 | 生成与渲染的字符集不兼容时，解析服务立即失败；兼容配置正常生成 | CLOSED |
| R3 | Redis 写失败、消费派发后取消、晚到故障观察及真实连接故障有专项测试 | CLOSED |
| R4 | Web 存储失败拒绝业务并映射 503；独立 IP 限流、400/429 no-store 与前端回归覆盖 | CLOSED |
| R5 | 对默认满容量执行有限探针，记录结果后再决定是否需优化 | CLOSED |

## 变更影响与验证范围

ChangedFiles/Projects：Captcha 契约及服务、Captcha.SkiaSharp 校验、CaptchaWeb 前端、对应测试和探针。
ChangedPublicContracts：新增可选 ICaptchaRendererValidator；ICaptchaRenderer 入口保持兼容。
ChangedRuntimePaths：服务初始化、页面初始化与刷新；Redis 本轮主要补验证。
ChangedProviders：SkiaSharp；ChangedTFMs：无，保持四框架。
ChangedBuildPackaging：Skia 集成测试增加 DI 引用；不更新发行版本。
ChangedBenchmarkHarness：有限内存容量探针；不重复无关绘图性能测量。
AffectedDependents：DI、Web、四包消费者；RiskLevel：MEDIUM。

先执行对应精准测试，再运行受影响三个单元项目及 Skia/Web 集成。公共契约变化后验证四框架构建、pack 和消费者。无源码影响的旧安全与绘图回归复用先前证据。

本机真实健康 Redis 与远端 CI 仍归 BLOCKED_EXTERNAL；故障路径使用受控本地端点，不冒充健康 Redis 并发/TTL 验收。

## 本轮实现与精准证据

- R1：初始化并入生成锁，等待有效防伪令牌后才发 POST；初始化失败释放锁，刷新可重试。实际页面脚本的 Node.js 时序回归 3/3；覆盖延迟初始化、失败重试、串行刷新、清空旧答案及错误消费后刷新。
- R2：新增可选验证接口，核心构造时调用；Skia 校验完整实际生成字符集，并复用不可变字符集。Skia Integration 16/16，包含 3 个配置不兼容、4 个兼容生成组合及空参数拒绝。中文与 Latin 字体使用本机显式配置。
- R3：Redis 三运行时各 6/6；增加写失败不重试、消费派发后取消及晚到成功、独立观察晚到故障。晚到故障测试不主动 await 底层故障 Task，利用弱引用及终结器事件确认释放时没有未观察异常。Integration 1/1 真实 loopback 连接故障通过，10 个健康服务用例仍显式跳过。
- R4：Web Integration 6/6；新增写入/消费故障返回 503 JSON、拒绝示例操作、恢复路径、跨会话共享 IP 限流；增加 400/429 的 no-store 断言。前端 3/3 由真实页面脚本加受控 DOM/网络桩执行，不代表完整浏览器视觉验收。
- 核心 26/26、DI 2/2，在 net8.0/net7.0/net6.0 分别通过。旧安全及旧绘图源代码本轮未变，复用 execution.md 的定向回归证据。

## 满容量测量（R5）

命令：`dotnet run --project benchmarks/CaptchaProbe -c Release --no-build -- --capacity`。

Windows、Intel Core Ultra 9 185H、.NET SDK 8.0.425、net8 Release。预填默认 10000 条后写入 100 个新上下文；分别验证均未过期、均到绝对过期点。串行工作负载，requestedConcurrency/maxActualParallelism/maxQueuedRequests = 1/1/0，无预热，包含 JIT；没有正式性能阈值。

| 初始状态 | 总耗时 ms | 平均 ms | P95 ms | 最大 ms | 托管分配 byte | 最终条目 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 全部有效 | 24.548 | 0.240 | 0.340 | 5.072 | 170440 | 10000 |
| 全部过期 | 1.425 | 0.014 | 0.005 | 0.991 | 170440 | 100 |

测量中始终未超过容量；过期场景第一次插入承担集中清理，其后耗时较低。默认满容量扫描仍为 O(N)，本轮补齐测量，不声称高并发延迟保证或性能提升，也不据此自行设定阈值。后续如需正式高负载 SLO，应使用目标宿主的并发测量决定是否替换过期索引。

证据文件：`output/captcha-capacity-probe.log`。该次测量对应 SHA-256：MemoryCaptchaStore.cs = `FB1A554C9EA62DEA135CED5AA7C6EE30D30528C875C1A7962B3635A34FF39D49`；CapacityProbe.cs = `8BD7446E3974D0712FF57B0BBCEEE44EFE8755A2BBE959E0AB0DCB6A255FE0C1`。先前诊断输出未保留，本次为第二次正式确认，不再重复同一实验。

独立复核结论见 [review-fixes.md](review-fixes.md)，由未参与修复的 luna_worker 只读审查并确认 R1～R5 均 CLOSED。执行者据该结论更新上表；未改写审查报告。

## 最终构建验证

`dotnet build Bing.Utils.sln -c Release -m:1 --no-restore` 成功，0 错误、30 个既有兼容/过时等警告；四个新包覆盖 net8/net7/net6/netstandard2.0。四包再次 pack 成功，保留原 1.5.0 版本，仅输出本地包。日志：`output/captcha-fix-build.log` 及 `output/captcha-fix-Bing.Utils.Captcha*-pack.log`。

包消费者新增可选校验接口调用断言，使用新的独立缓存 `output/captcha-fix-consumer-cache` 及本地源映射重新还原。四框架编译成功；net8/net7/net6 分别运行通过，PNG 160×60，启动校验、正确答案一次成功、重复提交失败。日志：`output/captcha-fix-consumer.log` 与三个运行时日志。

## 本轮收口

Task = 2026-10-28-captcha-lifecycle；Round = 用户审查后的修复第 1 轮。主代理负责契约、Web、集成及最终验证，luna_worker 负责 Redis 补测；另一独立 luna_worker 完成复核。Implementation = 5/5。L0 构建/格式/编码、L1 页面/配置回归、L2 三单元项目、L3 Skia/Web/Redis 故障及包消费者、L4 最终解决方案构建均通过；L5 为有限容量测量，无正式性能门槛。

本轮共 125 次 .NET 测试通过（包含三个目标运行时），页面脚本 3 次通过，健康 Redis 10 次显式跳过。旧绘图与安全回归为 REUSED_UNCHANGED_SCOPE；同一代码证据不因文档收尾而重跑。

| 分组 | 结论 |
| --- | --- |
| Completed | R1～R5；四框架构建、四包打包及包消费者验证 |
| Open Actionable | 无，独立审查未发现待修复项 |
| Blocked Approval | 无本次必须决策项；本次未要求正式性能阈值 |
| Blocked External | 健康 Redis 10 项服务端验收及远端 CI 运行证据 |
| Not Applicable | 没有同构历史性能基线，不构造 before/after |
| Accepted Limitations | 满容量清理仍 O(N)；本次短测未覆盖并发锁竞争；单实例存储和限流部署边界不变 |
| Verified Boundaries | 有限探针中条目始终不超默认 10000；过期优先清理与淘汰执行完成 |
| Deferred | 原计划排除的滑块/点选/音频与跨区域多主 |

No-Progress Check = CHANGED；IMPLEMENTATION_STATUS=PASS；TEST_STATUS=LOCAL_PASS_EXTERNAL_PARTIAL；PERFORMANCE_EVIDENCE_STATUS=MEASURED；EXTERNAL_GATE_STATUS=BLOCKED_EXTERNAL；RELEASE_STATUS=NOT_REQUESTED。Next Action = STOP；Goal Status = STOPPED_BLOCKED，仅剩外部验收，不开启下一轮自动修复。
